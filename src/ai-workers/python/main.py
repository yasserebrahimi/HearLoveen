import os, json, asyncio, io, math, time
from fastapi import FastAPI, Response
from pydantic import BaseModel
from azure.servicebus.aio import ServiceBusClient
from azure.storage.blob.aio import BlobClient
import numpy as np
import onnxruntime as ort
import soundfile as sf
from prometheus_client import Counter, Gauge, Histogram, generate_latest

# ---------- Metrics ----------
REQS = Counter("worker_requests_total", "Total messages processed")
ERRS = Counter("worker_errors_total", "Total errors")
LAT = Histogram("worker_processing_seconds", "Audio processing latency (s)")
DRIFT = Gauge("worker_phoneme_kl", "KL divergence vs baseline")

app = FastAPI(title="HearLoveen AI Worker")

# ---------- Config ----------
QUEUE = os.getenv("SB_QUEUE","audio-submitted")
SB_CONN = os.getenv("SB_CONNECTION","")
PG_CONN = os.getenv("PG_CONN","")  # e.g. postgresql://user:pass@host:5432/DB
ONNX_ASR = os.getenv("ONNX_ASR_PATH","/models/asr.onnx")
ONNX_SER = os.getenv("ONNX_SER_PATH","/models/ser.onnx")
TARGET_LEXICON = os.getenv("TARGET_LEXICON", "")

# phoneme labels file (JSON list) or default set
PHONEMES = os.getenv("PHONEMES", "")
if PHONEMES and os.path.isfile(PHONEMES):
    with open(PHONEMES,"r",encoding="utf-8") as f:
        PHONEME_SET = json.load(f)
else:
    PHONEME_SET = ["<blank>","AA","AE","AH","AO","AW","AY","B","CH","D","DH","EH","ER","EY","F","G","HH","IH","IY","JH","K","L","M","N","NG","OW","OY","P","R","S","SH","T","TH","UH","UW","V","W","Y","Z","ZH"]

# ---------- ONNX Sessions ----------
def _create_session(path: str):
    if not os.path.isfile(path):
        print(f"[WARN] ONNX model not found at {path}; using dummy.")
        return None
    providers = ["CPUExecutionProvider"]
    return ort.InferenceSession(path, providers=providers)

ASR_SESS = _create_session(ONNX_ASR)
SER_SESS = _create_session(ONNX_SER)

def softmax(x):
    e = np.exp(x - np.max(x, axis=-1, keepdims=True))
    return e / np.sum(e, axis=-1, keepdims=True)

def greedy_ctc_decode(logits):
    """
    Greedy CTC (Connectionist Temporal Classification) decoding.

    Decodes the most likely phoneme sequence from acoustic model logits by:
    1. Computing softmax probabilities from logits
    2. Taking argmax at each timestep
    3. Collapsing consecutive duplicate phonemes (CTC blank removal)

    Args:
      logits: np.ndarray [T, V] - unnormalized log probabilities
              where T = number of time frames, V = vocabulary size

    Returns:
      decoded (str): Space-separated phoneme sequence
      frame_ids (np.ndarray[T]): Phoneme ID at each frame
      probs (np.ndarray[T,V]): Softmax probabilities over vocabulary at each frame
    """
    probs = softmax(logits)
    ids = np.argmax(probs, axis=-1)  # [T]
    decoded_tokens = []
    frame_ids = []
    prev = None
    for t, i in enumerate(ids):
        if i != prev:
            if i != 0:
                ph = PHONEME_SET[i] if i < len(PHONEME_SET) else f"ID{i}"
                decoded_tokens.append(ph)
            prev = i
        frame_ids.append(i)
    return " ".join(decoded_tokens), np.array(frame_ids), probs

def viterbi_ctc_align(logits, target_seq_ids):
    """
    Viterbi alignment for forced CTC alignment.

    Given acoustic model outputs and a known target phoneme sequence,
    computes the most likely alignment between audio frames and phonemes
    using dynamic programming (Viterbi algorithm).

    This is used for teacher-forced alignment where we know the expected
    phoneme sequence and want to find which frames correspond to each phoneme.

    Algorithm:
    - dp[t,n] = max log probability of reaching phoneme n at time t
    - At each frame, can either stay on current phoneme (emit blank) or advance
    - Backtrack from best final state to recover alignment

    Args:
      logits: np.ndarray [T, V] - acoustic model logits (unnormalized)
      target_seq_ids: list[int] - expected phoneme IDs (excludes blanks)

    Returns:
      np.ndarray[T] - frame->target mapping, -1 indicates blank/no phoneme
    """
    import numpy as np
    T, V = logits.shape
    N = len(target_seq_ids)
    probs = softmax(logits)  # [T,V]
    # DP matrices
    dp = np.full((T+1, N+1), -1e9, dtype=np.float32)
    bp = np.full((T+1, N+1), -1, dtype=np.int32)
    dp[0,0] = 0.0
    for t in range(1, T+1):
        # stay (blank)
        val_blank = dp[t-1, :]
        pb = probs[t-1, 0]  # blank prob
        opt_blank = val_blank + np.log(max(pb, 1e-8))
        dp[t, :] = np.maximum(dp[t, :], opt_blank)
        # advance
        for n in range(1, N+1):
            pid = target_seq_ids[n-1]
            p = probs[t-1, pid]
            val_adv = dp[t-1, n-1] + np.log(max(p, 1e-8))
            if val_adv > dp[t, n]:
                dp[t, n] = val_adv
                bp[t, n] = 1  # from advance
    # backtrack best n at T
    n = int(np.argmax(dp[T, :]))
    assign = np.full(T, -1, dtype=np.int32)  # -1 = blank
    t = T
    while t > 0 and n >= 0:
        if bp[t, n] == 1:
            assign[t-1] = n-1  # frame t-1 assigned to target index (n-1)
            n -= 1
        t -= 1
    return assign


def forced_alignment(frame_ids, probs, hop=0.02):
    """
    Convert frame-level phoneme predictions to time-aligned segments.

    Groups consecutive frames with the same phoneme ID into segments,
    computing start/end times and average confidence for each phoneme.

    Args:
      frame_ids: np.ndarray[T] - phoneme ID at each frame (0 = blank)
      probs: np.ndarray[T,V] - probability distribution at each frame
      hop: float - hop size in seconds (default 0.02s = 20ms)

    Returns:
      list[dict] - segments with keys:
        - p (str): phoneme symbol
        - start (float): start time in seconds
        - end (float): end time in seconds
        - conf (float): average confidence score [0-1]
    """
    T = len(frame_ids)
    segs = []
    i = 0
    while i < T:
        pid = frame_ids[i]
        j = i + 1
        while j < T and frame_ids[j] == pid:
            j += 1
        if pid != 0:
            start_s = i * hop
            end_s = j * hop
            conf = float(np.mean(probs[i:j, pid])) if j > i else 0.0
            ph = PHONEME_SET[pid] if pid < len(PHONEME_SET) else f"ID{pid}"
            segs.append({"p": ph, "start": round(start_s,3), "end": round(end_s,3), "conf": round(conf,3)})
        i = j
    return segs

def composite_score(phoneme_segments, emotion_label):
    """
    Calculate overall pronunciation quality score from 0-100.

    Scoring methodology:
    1. Base score from average phoneme confidence: 60 + (40 * avg_confidence)
       - This maps confidence [0-1] to score range [60-100]
    2. Emotion penalty: -10 points for negative emotions (frustrated, angry, sad)
       - Negative emotions may indicate struggle or reduced engagement
    3. Final score clamped to [0, 100] range

    Args:
      phoneme_segments: list[dict] - segments with 'conf' (confidence) field
      emotion_label: str - detected emotion (neutral, happy, sad, angry, frustrated)

    Returns:
      int - overall score from 0 to 100
    """
    if not phoneme_segments:
        return 0
    avg_conf = float(np.mean([s["conf"] for s in phoneme_segments]))
    base = int(60 + 40 * avg_conf)  # Map [0-1] confidence to [60-100] score
    if emotion_label in ("frustrated","angry","sad"):
        base -= 10  # Penalty for negative emotions
    return max(0, min(100, base))

EMO_LABELS = ["neutral","happy","sad","angry","frustrated"]

def run_ser(wav, sr):
    if SER_SESS is None:
        # simple energy-based fallback
        energy = float(np.mean(np.abs(wav)))
        return "happy" if energy > 0.1 else "neutral"
    x = wav.astype("float32")[None, :]
    out = SER_SESS.run(None, {"input": x})[0]
    lab = int(np.argmax(out, axis=-1)[0])
    return EMO_LABELS[lab % len(EMO_LABELS)]

def run_asr_phoneme(wav, sr):
    if ASR_SESS is None:
        T = max(1, int(len(wav) / (sr*0.02)))
        V = len(PHONEME_SET)
        logits = np.random.randn(T, V).astype("float32") * 0.1
        logits[:, 0] += 4.0  # blank heavy
        logits[:, 8] += (np.abs(np.mean(wav)) * 5.0)  # bias
        return logits
    x = wav.astype("float32")[None, :]
    outputs = ASR_SESS.run(None, {"input": x})
    logits = outputs[0].squeeze(0)
    return logits

def persist_report(pg_conn, submission_id, score, weakness, recommendation, radar):
    import psycopg2
    try:
        with psycopg2.connect(pg_conn) as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    insert into "FeedbackReports"("Id","SubmissionId","Score0_100","Weakness","Recommendation","CreatedAtUtc")
                    values(gen_random_uuid(), %s, %s, %s, %s, now())
                    """,
                    (submission_id, score, weakness, recommendation))
                conn.commit()
    except Exception as ex:
        print(f"[ERROR] Failed to persist report for submission {submission_id}: {ex}")
        raise

@app.get("/health")
async def health(): 
    return {"status":"ok", "asr_loaded": ASR_SESS is not None, "ser_loaded": SER_SESS is not None}

@app.get("/metrics")
async def metrics():
    data = generate_latest()
    return Response(content=data, media_type="text/plain")

async def process_message(payload):
    start = time.time()
    REQS.inc()
    try:
        # Input validation
        submission_id = payload.get("submissionId")
        child_id = payload.get("childId")
        blob_url = payload.get("blobUrl")

        if not submission_id:
            raise ValueError("Missing submissionId")
        if not child_id:
            raise ValueError("Missing childId")
        if not blob_url:
            raise ValueError("Missing blobUrl")
        # Download audio with error handling
        try:
            async with BlobClient.from_blob_url(blob_url) as bc:
                data = await bc.download_blob()
                wav_bytes = await data.readall()
        except Exception as ex:
            print(f"[ERROR] Failed to download blob {blob_url}: {ex}")
            raise ValueError(f"Blob download failed: {ex}")

        # Validate and load audio format
        try:
            wav, sr = sf.read(io.BytesIO(wav_bytes), dtype="float32", always_2d=False)
        except Exception as ex:
            print(f"[ERROR] Invalid audio format for submission {submission_id}: {ex}")
            raise ValueError(f"Invalid audio format: {ex}")
        if hasattr(wav, "ndim") and wav.ndim > 1:
            wav = wav.mean(axis=1)
        logits = run_asr_phoneme(wav, sr)
        _, frame_ids, probs = greedy_ctc_decode(logits)
        segments = forced_alignment(frame_ids, probs, hop=0.02)
        # teacher-forced with per-child lexicon if available
        target_ph = None
        if PG_CONN and child_id:
            target_ph = await fetch_child_lexicon(PG_CONN, child_id)
        if not target_ph and TARGET_LEXICON:
            target_ph = [p.strip() for p in TARGET_LEXICON.split(',') if p.strip()]
        if target_ph:
            target_ids = [PHONEME_SET.index(p) if p in PHONEME_SET else 0 for p in target_ph]
            assign = viterbi_ctc_align(logits, target_ids)
            segs = []
            i = 0; hop=0.02
            while i < assign.shape[0]:
                idx = int(assign[i]); j=i+1
                while j < assign.shape[0] and int(assign[j])==idx: j+=1
                if idx>=0:
                    ph = target_ph[idx] if idx < len(target_ph) else f"IDX{idx}"
                    ph_id = PHONEME_SET.index(ph) if ph in PHONEME_SET else 0
                    conf = float(np.mean(softmax(logits[i:j])[:, ph_id])) if j>i else 0.0
                    segs.append({"p":ph,"start":round(i*hop,3),"end":round(j*hop,3),"conf":round(conf,3)})
                i=j
            segments = segs  # teacher-forced

        # Try lexicon-constrained alignment if provided
        if TARGET_LEXICON:
            target_ph = []
            if os.path.isfile(TARGET_LEXICON):
                try:
                    with open(TARGET_LEXICON,'r',encoding='utf-8') as f:
                        vals = json.load(f)
                        if isinstance(vals, list):
                            target_ph = vals
                except Exception:
                    target_ph = []
            else:
                target_ph = [p.strip() for p in TARGET_LEXICON.split(',') if p.strip()]
            target_ids = [PHONEME_SET.index(p) if p in PHONEME_SET else 0 for p in target_ph]
            if len(target_ids) > 0:
                assign = viterbi_ctc_align(logits, target_ids)  # [T] with -1/idx
                segs = []
                i = 0
                hop = 0.02
                while i < assign.shape[0]:
                    idx = int(assign[i])
                    j = i + 1
                    while j < assign.shape[0] and int(assign[j]) == idx:
                        j += 1
                    if idx >= 0:
                        ph = target_ph[idx] if idx < len(target_ph) else f"IDX{idx}"
                        # confidence approx: avg prob for this phoneme
                        ph_id = PHONEME_SET.index(ph) if ph in PHONEME_SET else 0
                        conf = float(np.mean(softmax(logits[i:j])[:, ph_id])) if j > i else 0.0
                        segs.append({"p": ph, "start": round(i*hop,3), "end": round(j*hop,3), "conf": round(conf,3)})
                    i = j
                segments = segs
    
        emotion = run_ser(wav, sr)
        score = composite_score(segments, emotion)
        # Drift detection
        try:
            import numpy as _np
            V = len(PHONEME_SET)
            hist = _np.bincount(frame_ids[frame_ids>0], minlength=V).tolist()
            base = load_save_baseline(PG_CONN)
            if base is None:
                load_save_baseline(PG_CONN, hist)
            else:
                kl = kl_divergence(hist, base)
                DRIFT.set(kl)
                # EMA update
                ema = []
                alpha = 0.01
                m = max(len(base), len(hist))
                for i in range(m):
                    b = base[i] if i < len(base) else 0
                    h = hist[i] if i < len(hist) else 0
                    ema.append((1-alpha)*b + alpha*h)
                load_save_baseline(PG_CONN, ema)
        except Exception as drift_ex:
            print(f"[WARN] Drift detection failed: {drift_ex}")

        weakness = "articulation" if score < 75 else "prosody"
        recommendation = "Slow down and repeat target words; emphasize endings." if weakness=="articulation" else "Vary pitch and stress; try call-and-response games."
        if PG_CONN:
            persist_report(PG_CONN, submission_id, score, weakness, recommendation, {"segments": segments, "emotion": emotion})
            if child_id:
                update_curriculum(PG_CONN, child_id, segments, score)
    except Exception as ex:
        ERRS.inc()
        print("[ERR]", ex)
    finally:
        LAT.observe(time.time()-start)

async def worker_loop():
    if not SB_CONN:
        print("ServiceBus connection not set; worker idle")
        return
    async with ServiceBusClient.from_connection_string(SB_CONN) as client:
        receiver = client.get_queue_receiver(queue_name=QUEUE, max_wait_time=5)
        async with receiver:
            while True:
                msgs = await receiver.receive_messages(max_message_count=5, max_wait_time=5)
                if not msgs:
                    await asyncio.sleep(1)
                    continue
                for m in msgs:
                    try:
                        payload = json.loads(str(m))
                        await process_message(payload)
                        await receiver.complete_message(m)
                    except Exception as ex:
                        print("Message error:", ex)
                        await receiver.abandon_message(m)

if __name__ == "__main__":
    import uvicorn, asyncio
    loop = asyncio.get_event_loop()
    loop.create_task(worker_loop())
    uvicorn.run(app, host="0.0.0.0", port=8000)


# --------- Simple G2P (stub) ---------
def g2p_words(words):
    # Minimal demo: naive mapping; replace with real g2p (Phonetisaurus/CMUdict) in prod
    base = {
        "cat":["K","AE","T"], "dog":["D","AO","G"], "mama":["M","AA","M","AA"],
        "papa":["P","AA","P","AA"], "car":["K","AA","R"], "ball":["B","AO","L"]
    }
    seq = []
    for w in words:
        w2 = ''.join([c for c in w.lower() if c.isalpha()])
        if w2 in base:
            seq.extend(base[w2]); continue
        # fallback heuristic: consonants/vowels
        for ch in w2:
            if ch in "aeiou":
                seq.append({"a":"AH","e":"EH","i":"IH","o":"AO","u":"UH"}[ch])
            else:
                seq.append({"b":"B","c":"K","d":"D","f":"F","g":"G","h":"HH","j":"JH","k":"K","l":"L","m":"M",
                            "n":"N","p":"P","q":"K","r":"R","s":"S","t":"T","v":"V","w":"W","x":"K","y":"Y","z":"Z"}.get(ch,"S"))
    return seq

async def fetch_child_lexicon(pg_conn, child_id):
    # Expect a table child_lexicon(child_id uuid primary key, phonemes jsonb or words text[])
    try:
        import psycopg2, psycopg2.extras
        with psycopg2.connect(pg_conn) as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
                cur.execute('select phonemes, words from child_lexicon where child_id=%s', (str(child_id),))
                row = cur.fetchone()
                if not row: return None
                if row.get("phonemes"):
                    return row["phonemes"]
                if row.get("words"):
                    return multilingual_g2p(row["words"], child_id)
    except Exception as ex:
        print("[WARN] fetch_child_lexicon:", ex)
    return None


# --------- Real G2P Backends (adapters) ---------
class G2PBackend:
    def phonemes(self, words):
        raise NotImplementedError

class G2P_ENG(G2PBackend):
    def __init__(self):
        try:
            from g2p_en import G2p
            self._g2p = G2p()
        except Exception as ex:
            self._g2p = None
            print("[WARN] g2p_en not available:", ex)
    def phonemes(self, words):
        if not self._g2p:
            return g2p_words(words)  # fallback stub
        phs = []
        for w in words:
            seq = [p for p in self._g2p(w) if p.isalpha()]
            phs.extend([s.upper() for s in seq])
        return phs

class G2P_Phonetisaurus(G2PBackend):
    def __init__(self, bin_path="phonetisaurus-g2p", model_path=None):
        self.bin = bin_path
        self.model = model_path
    def phonemes(self, words):
        import subprocess, os
        if not self.model:
            return g2p_words(words)
        try:
            out = subprocess.check_output([self.bin, "--model="+self.model], input="\n".join(words), text=True)
            phs = []
            for line in out.strip().splitlines():
                parts = line.split("\t")
                if len(parts) >= 2:
                    phs.extend([p.strip().upper() for p in parts[1].split()])
            return phs or g2p_words(words)
        except Exception as ex:
            print("[WARN] Phonetisaurus error:", ex)
            return g2p_words(words)

class G2P_Sequitur(G2PBackend):
    def __init__(self, bin_path="sequitur-g2p", model_path=None):
        self.bin = bin_path
        self.model = model_path
    def phonemes(self, words):
        import subprocess
        if not self.model:
            return g2p_words(words)
        try:
            out = subprocess.check_output([self.bin, "-m", self.model, "-x", " ", "-e", ""], input="\n".join(words), text=True)
            phs = []
            for line in out.strip().splitlines():
                phs.extend([p.strip().upper() for p in line.split()])
            return phs or g2p_words(words)
        except Exception as ex:
            print("[WARN] Sequitur error:", ex)
            return g2p_words(words)

def get_g2p_backend():
    backend = os.getenv("G2P_BACKEND", "g2p_en").lower()
    if backend == "phonetisaurus":
        return G2P_Phonetisaurus(model_path=os.getenv("G2P_MODEL"))
    if backend == "sequitur":
        return G2P_Sequitur(model_path=os.getenv("G2P_MODEL"))
    return G2P_ENG()



# --------- Per-child G2P cache (Postgres) ---------
def cache_lookup(pg_conn, child_id, words):
    try:
        import psycopg2, psycopg2.extras
        found = {}
        with psycopg2.connect(pg_conn) as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.DictCursor) as cur:
                cur.execute('create table if not exists child_g2p_cache(child_id uuid, word text, phonemes jsonb, primary key(child_id,word));')
                cur.execute('select word, phonemes from child_g2p_cache where child_id=%s and word = any(%s)', (str(child_id), words))
                for row in cur.fetchall():
                    found[row['word']] = row['phonemes']
        return found
    except Exception as ex:
        print("[WARN] cache_lookup:", ex)
        return {}

def cache_store(pg_conn, child_id, mapping):
    try:
        import psycopg2, json
        with psycopg2.connect(pg_conn) as conn:
            with conn.cursor() as cur:
                for w, ph in mapping.items():
                    cur.execute('insert into child_g2p_cache(child_id,word,phonemes) values(%s,%s,%s) on conflict(child_id,word) do update set phonemes=excluded.phonemes',
                                (str(child_id), w, json.dumps(ph)))
            conn.commit()
    except Exception as ex:
        print("[WARN] cache_store:", ex)


def g2p_for_child(words, child_id=None):
    words = [w for w in words if isinstance(w, str) and w.strip()]
    if not words:
        return []
    backend = get_g2p_backend()
    if child_id and PG_CONN:
        cached = cache_lookup(PG_CONN, child_id, words)
        miss = [w for w in words if w not in cached]
        mapping = dict(cached)
        if miss:
            ph = backend.phonemes(miss)
            # simplistic distribution if backend returns flat list
            if isinstance(ph, list):
                per_word = max(1, len(ph)//max(1,len(miss)))
                idx = 0
                for w in miss:
                    mapping[w] = ph[idx:idx+per_word] or ph
                    idx += per_word
            else:
                for w in miss:
                    mapping[w] = []
            cache_store(PG_CONN, child_id, mapping)
        seq = []
        for w in words:
            seq.extend(mapping.get(w, []))
        return seq
    return backend.phonemes(words)


def update_curriculum(pg_conn, child_id, segments, score):
    # pick weakest top-3 phonemes by avg confidence
    if not child_id or not segments:
        return

    # Validate phonemes against known set
    valid_phonemes = set(PHONEME_SET) - {"<blank>"}

    import psycopg2
    from collections import defaultdict
    agg = defaultdict(list)
    for s in segments:
        try:
            phoneme = s["p"]
            if phoneme not in valid_phonemes:
                print(f"[WARN] Invalid phoneme '{phoneme}' found in segments, skipping")
                continue
            agg[phoneme].append(float(s.get("conf", 0.0)))
        except Exception as ex:
            print(f"[WARN] Error processing segment {s}: {ex}")
            continue

    items = sorted(((p, sum(v)/len(v)) for p, v in agg.items() if v), key=lambda x: x[1])
    weak = [p for p, _ in items[:3]] or ["R", "S"]

    try:
        with psycopg2.connect(pg_conn) as conn:
            with conn.cursor() as cur:
                cur.execute(
                    """
                    insert into "ChildCurricula"("Id","ChildId","FocusPhonemesCsv","Difficulty","SuccessStreak","UpdatedAtUtc")
                    values(gen_random_uuid(), %s, %s, %s, 0, now())
                    on conflict ("ChildId") do update set
                        "FocusPhonemesCsv"=excluded."FocusPhonemesCsv",
                        "UpdatedAtUtc"=now()
                    """,
                    (str(child_id), ",".join(weak), 1 if score < 70 else 2)
                )
            conn.commit()
    except Exception as ex:
        print(f"[ERROR] Failed to update curriculum for child {child_id}: {ex}")
        raise



def kl_divergence(p, q, eps=1e-8):
    """
    Calculate Kullback-Leibler divergence between two distributions.

    Used for drift detection: monitors if the distribution of phonemes
    in incoming audio differs significantly from the baseline distribution.
    High KL divergence indicates data drift, which may require model retraining.

    KL(P||Q) = sum(P[i] * log(P[i] / Q[i]))

    Args:
      p: array-like - current distribution (e.g., phoneme histogram)
      q: array-like - reference distribution (baseline)
      eps: float - small constant to avoid log(0)

    Returns:
      float - KL divergence (always >= 0, higher = more drift)
    """
    import numpy as np
    p = np.asarray(p, dtype=np.float64) + eps
    q = np.asarray(q, dtype=np.float64) + eps
    p /= p.sum(); q /= q.sum()  # Normalize to valid probability distributions
    return float((p * (np.log(p) - np.log(q))).sum())

def load_save_baseline(pg_conn, new_hist=None):
    try:
        import psycopg2, psycopg2.extras, json
        with psycopg2.connect(pg_conn) as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
                cur.execute('create table if not exists worker_drift_baseline(name text primary key, hist jsonb);')
                cur.execute("select hist from worker_drift_baseline where name='phoneme_hist'")
                row = cur.fetchone()
                if new_hist is None:
                    return row['hist'] if row else None
                if row:
                    cur.execute("update worker_drift_baseline set hist=%s where name='phoneme_hist'", (json.dumps(new_hist),))
                else:
                    cur.execute("insert into worker_drift_baseline(name,hist) values('phoneme_hist', %s)", (json.dumps(new_hist),))
            conn.commit()
    except Exception as ex:
        print("[WARN] load_save_baseline:", ex)
    return None

# --------- Multilingual G2P routing ---------
def fa_g2p_words(words):
    map_tbl = {"ا":"AA","آ":"AA","ب":"B","پ":"P","ت":"T","ث":"S","ج":"JH","چ":"CH","ح":"HH","خ":"KH","د":"D","ذ":"Z","ر":"R","ز":"Z","ژ":"ZH","س":"S","ش":"SH","ص":"S","ض":"Z","ط":"T","ظ":"Z","ع":"AH","غ":"GH","ف":"F","ق":"G","ک":"K","گ":"G","ل":"L","م":"M","ن":"N","و":"V","ه":"HH","ی":"Y"}
    seq = []
    for w in words:
        for ch in str(w):
            seq.append(map_tbl.get(ch, "AH"))
    return seq

def de_g2p_words(words):
    seq = []
    vmap = {"a":"AA","e":"EH","i":"IH","o":"AO","u":"UH"}
    cmap = {"b":"B","c":"K","d":"D","f":"F","g":"G","h":"HH","j":"JH","k":"K","l":"L","m":"M","n":"N","p":"P","q":"K","r":"R","s":"S","t":"T","v":"V","w":"V","x":"K","y":"Y","z":"Z"}
    for w in words:
        wl = str(w).lower().replace("ä","ae").replace("ö","oe").replace("ü","ue").replace("ß","ss")
        for ch in wl:
            if ch in vmap: seq.append(vmap[ch])
            elif ch.isalpha(): seq.append(cmap.get(ch,"S"))
    return seq

def multilingual_g2p(words, child_id=None):
    lang = os.getenv("G2P_LANG","auto").lower()
    if lang == "fa": return fa_g2p_words(words)
    if lang == "de": return de_g2p_words(words)
    try:
        return g2p_for_child(words, child_id)
    except Exception:
        return g2p_words(words)
