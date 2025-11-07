from fastapi import FastAPI, Depends, HTTPException, status, Request
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
from slowapi import Limiter
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded
from slowapi.middleware import SlowAPIMiddleware
import os, yaml, json, time, hashlib

# Auth (HS256 demo) — gateway should validate; here we require presence + signature fallback
from jose import jwt, JWTError

APP_NAME = "HearLoveen XAI"
VERSION = "1.0.0"

# Load config
CONFIG_PATH = os.environ.get("HL_CONFIG", "config.yaml")
CONFIG = {}
if os.path.exists(CONFIG_PATH):
    with open(CONFIG_PATH, "r", encoding="utf-8") as f:
        CONFIG = yaml.safe_load(f) or {}

JWT_SECRET = os.environ.get("HL_JWT_SECRET", CONFIG.get("auth", {}).get("jwt_secret", "CHANGE_ME"))
JWT_ALGO = os.environ.get("HL_JWT_ALGO", "HS256")

# Rate limiting
limiter = Limiter(key_func=get_remote_address)

app = FastAPI(title=APP_NAME, version=VERSION, docs_url="/docs", redoc_url="/redoc")
app.state.limiter = limiter
app.add_middleware(SlowAPIMiddleware)

bearer = HTTPBearer(auto_error=False)

def require_auth(credentials: Optional[HTTPAuthorizationCredentials] = Depends(bearer)):
    if not credentials or credentials.scheme.lower() != "bearer":
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Missing bearer token")
    token = credentials.credentials
    try:
        # For demo: accept HS256; in production use OIDC/JWKS validation
        jwt.decode(token, JWT_SECRET, algorithms=[JWT_ALGO])
        return True
    except JWTError:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid token")

@app.exception_handler(RateLimitExceeded)
def ratelimit_handler(request: Request, exc: RateLimitExceeded):
    return fastapi.responses.PlainTextResponse(str(exc), status_code=429)

class PronExplainRequest(BaseModel):
    session_id: Optional[str] = None
    text: str
    audio_uri: Optional[str] = None

class EmotionExplainRequest(BaseModel):
    session_id: Optional[str] = None
    audio_uri: str

def _mock_hash(s: str) -> float:
    return (int(hashlib.sha256(s.encode()).hexdigest(), 16) % 1000) / 10.0

def load_model_or_mock() -> Dict[str, Any]:
    # In a real impl, load ONNX/TFLite here
    model_dir = os.environ.get("HL_MODELS", "models")
    pron_path = os.path.join(model_dir, "pronunciation_scorer.onnx")
    emo_path = os.path.join(model_dir, "emotion_detector.onnx")
    return {
        "mode": "real" if os.path.exists(pron_path) and os.path.exists(emo_path) else "mock",
        "pron": pron_path if os.path.exists(pron_path) else None,
        "emo": emo_path if os.path.exists(emo_path) else None,
    }

MODEL = load_model_or_mock()

@app.get("/healthz")
def healthz():
    return {"status": "ok", "model_mode": MODEL["mode"]}

@app.post("/api/explain/pronunciation")
@limiter.limit("60/minute")
def explain_pron(req: PronExplainRequest, ok: bool = Depends(require_auth)):
    started = time.time()
    if MODEL["mode"] == "mock":
        # simple deterministic mock
        words = req.text.strip().split()
        scores = {w: round(70 + (_mock_hash(w) % 30), 1) for w in words}
        payload = {
            "session_id": req.session_id,
            "mode": "mock",
            "word_scores": scores,
            "overall": round(sum(scores.values()) / max(1, len(scores)), 1),
            "latency_ms": int((time.time() - started)*1000)
        }
        return payload
    # Real path (placeholder)
    return {"detail": "real model path not wired in this demo", "mode": MODEL["mode"]}

@app.post("/api/explain/emotion")
@limiter.limit("60/minute")
def explain_emotion(req: EmotionExplainRequest, ok: bool = Depends(require_auth)):
    started = time.time()
    if MODEL["mode"] == "mock":
        labels = ["calm", "engaged", "frustrated", "neutral"]
        h = _mock_hash(req.audio_uri)
        label = labels[int(h) % len(labels)]
        return {
            "session_id": req.session_id,
            "mode": "mock",
            "label": label,
            "confidence": round(0.6 + (h % 40)/100, 2),
            "latency_ms": int((time.time() - started)*1000)
        }
    return {"detail": "real model path not wired in this demo", "mode": MODEL["mode"]}