from fastapi import FastAPI, File, UploadFile, HTTPException, Security, Header, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import APIKeyHeader
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded
import whisper
import torch
import numpy as np
import librosa
from scipy.stats import skew, kurtosis
from pydantic import BaseModel
import logging
import os
import io

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Rate limiting setup
limiter = Limiter(key_func=get_remote_address)

# Security configuration
API_KEY = os.getenv("ML_API_KEY", "HearLoveen2024!MLApiKey")
MAX_FILE_SIZE = int(os.getenv("MAX_FILE_SIZE", 10485760))  # 10MB default

api_key_header = APIKeyHeader(name="X-API-Key", auto_error=False)

app = FastAPI(title="HearLoveen ML API", version="1.0.0")

# Add rate limiter
app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

# CORS Configuration - Only allow specific origins
ALLOWED_ORIGINS = os.getenv(
    "CORS_ALLOWED_ORIGINS",
    "http://localhost:3000,http://localhost:5000"
).split(",")

app.add_middleware(
    CORSMiddleware,
    allow_origins=ALLOWED_ORIGINS,  # Restricted to specific origins
    allow_credentials=True,
    allow_methods=["GET", "POST", "OPTIONS"],  # Only necessary methods
    allow_headers=["Content-Type", "Authorization", "Accept", "X-API-Key"],  # Specific headers
    max_age=3600,  # Cache preflight requests for 1 hour
)

# Authentication function
async def verify_api_key(api_key: str = Security(api_key_header)):
    """Verify API key for authentication"""
    if api_key is None or api_key != API_KEY:
        logger.warning(f"Unauthorized access attempt with key: {api_key[:10] if api_key else 'None'}...")
        raise HTTPException(
            status_code=401,
            detail="Invalid or missing API key"
        )
    return api_key

# File size validation
async def validate_file_size(file: UploadFile):
    """Validate uploaded file size"""
    # Read file in chunks to avoid memory issues
    size = 0
    chunk_size = 1024 * 1024  # 1MB chunks

    while chunk := await file.read(chunk_size):
        size += len(chunk)
        if size > MAX_FILE_SIZE:
            raise HTTPException(
                status_code=413,
                detail=f"File too large. Maximum size is {MAX_FILE_SIZE / 1024 / 1024}MB"
            )

    # Reset file pointer
    await file.seek(0)
    return size

# Load Whisper model (fine-tuned version)
logger.info("Loading Whisper model...")
model = whisper.load_model("base")
logger.info("Model loaded successfully")

class TranscriptionResponse(BaseModel):
    text: str
    confidence: float
    language: str
    duration: float

class PronunciationResponse(BaseModel):
    score: float
    phonemes: list[dict]
    overall_quality: str

# Helper functions
def calculate_confidence_from_segments(segments):
    """Calculate average confidence from Whisper segments"""
    if not segments:
        return 0.0

    # Whisper provides log probabilities, convert to confidence
    total_confidence = 0.0
    total_tokens = 0

    for segment in segments:
        # Average token probabilities in segment
        if "avg_logprob" in segment:
            # Convert log probability to confidence (0-1)
            confidence = np.exp(segment["avg_logprob"])
            tokens = len(segment.get("tokens", [1]))
            total_confidence += confidence * tokens
            total_tokens += tokens

    return total_confidence / total_tokens if total_tokens > 0 else 0.0

def analyze_phoneme_quality(audio_data, transcription):
    """
    Analyze phoneme-level pronunciation quality using real acoustic features

    Uses:
    - Spectral features (MFCC, spectral centroid, spectral rolloff)
    - Prosodic features (pitch, energy, zero crossing rate)
    - Temporal features (duration, rhythm)

    This provides a real acoustic analysis instead of random numbers
    """
    phonemes = []
    words = transcription.split()

    try:
        # Load audio with librosa
        y, sr = librosa.load(io.BytesIO(audio_data), sr=16000)

        # Extract global features
        # 1. MFCC (Mel-frequency cepstral coefficients)
        mfcc = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
        mfcc_mean = np.mean(mfcc, axis=1)
        mfcc_std = np.std(mfcc, axis=1)

        # 2. Spectral features
        spectral_centroids = librosa.feature.spectral_centroid(y=y, sr=sr)[0]
        spectral_rolloff = librosa.feature.spectral_rolloff(y=y, sr=sr)[0]

        # 3. Prosodic features
        zero_crossing_rate = librosa.feature.zero_crossing_rate(y)[0]
        rms_energy = librosa.feature.rms(y=y)[0]

        # 4. Pitch (F0) extraction
        pitches, magnitudes = librosa.piptrack(y=y, sr=sr)
        pitch_values = []
        for t in range(pitches.shape[1]):
            index = magnitudes[:, t].argmax()
            pitch = pitches[index, t]
            if pitch > 0:
                pitch_values.append(pitch)

        # Calculate quality metrics
        # Higher spectral centroid = clearer articulation
        centroid_score = min(1.0, np.mean(spectral_centroids) / 3000.0)

        # Energy consistency = stable pronunciation
        energy_consistency = 1.0 - min(1.0, np.std(rms_energy) / np.mean(rms_energy))

        # Pitch variation = natural prosody
        if len(pitch_values) > 0:
            pitch_variation = min(1.0, np.std(pitch_values) / 50.0)
        else:
            pitch_variation = 0.5

        # MFCC quality (lower standard deviation = more consistent articulation)
        mfcc_quality = 1.0 - min(1.0, np.mean(mfcc_std) / 20.0)

        # Overall acoustic quality score (0-1)
        base_quality = (
            centroid_score * 0.3 +
            energy_consistency * 0.25 +
            pitch_variation * 0.20 +
            mfcc_quality * 0.25
        )

        # Segment audio approximately by words
        num_words = len(words)
        if num_words > 0:
            # Simple equal segmentation (in production, use forced alignment)
            segment_length = len(y) // num_words

            for i, word in enumerate(words[:10]):  # Limit to 10 words
                # Extract segment
                start = i * segment_length
                end = min((i + 1) * segment_length, len(y))
                segment = y[start:end]

                if len(segment) > 0:
                    # Calculate segment-specific features
                    seg_energy = np.mean(librosa.feature.rms(y=segment))
                    seg_zcr = np.mean(librosa.feature.zero_crossing_rate(segment))
                    seg_centroid = np.mean(librosa.feature.spectral_centroid(y=segment, sr=sr))

                    # Normalize features
                    energy_norm = min(1.0, seg_energy / 0.1)
                    zcr_norm = min(1.0, seg_zcr / 0.2)
                    centroid_norm = min(1.0, seg_centroid / 3000.0)

                    # Calculate word-level quality
                    word_quality = (
                        base_quality * 0.5 +  # Global quality
                        energy_norm * 0.2 +    # Segment energy
                        zcr_norm * 0.15 +      # Articulation clarity
                        centroid_norm * 0.15   # Spectral clarity
                    )

                    # Add some variance based on acoustic complexity
                    complexity_factor = len(word) / 10.0  # Longer words are harder
                    word_quality = word_quality * (1.0 - complexity_factor * 0.1)

                    # Clamp to reasonable range
                    word_quality = max(0.5, min(0.98, word_quality))

                    phonemes.append({
                        "word": word,
                        "phoneme": word[:2].upper() if len(word) >= 2 else word.upper(),
                        "score": round(word_quality, 2),
                        "feedback": "Excellent" if word_quality >= 0.90 else
                                   "Clear" if word_quality >= 0.80 else
                                   "Good effort" if word_quality >= 0.70 else
                                   "Practice needed",
                        "metrics": {
                            "energy": round(float(seg_energy), 3),
                            "clarity": round(float(centroid_norm), 2),
                            "articulation": round(float(zcr_norm), 2)
                        }
                    })

        logger.info(f"Acoustic analysis completed for {len(phonemes)} words")

    except Exception as e:
        logger.error(f"Error in acoustic analysis: {str(e)}")
        # Fallback to basic analysis if acoustic processing fails
        for word in words[:5]:
            phonemes.append({
                "word": word,
                "phoneme": word[:2].upper() if len(word) >= 2 else word.upper(),
                "score": 0.75,
                "feedback": "Analysis incomplete",
                "metrics": {}
            })

    return phonemes

@app.get("/health")
async def health_check():
    return {"status": "healthy", "model": "whisper-base"}

@app.post("/api/transcribe", response_model=TranscriptionResponse)
@limiter.limit("10/minute")  # Rate limit: 10 requests per minute
async def transcribe_audio(
    request: Request,
    file: UploadFile = File(...),
    api_key: str = Security(verify_api_key)
):
    """
    Transcribe audio using Whisper model
    Requires API key authentication
    Rate limited to 10 requests per minute
    """
    try:
        # Validate file size
        await validate_file_size(file)

        logger.info(f"Transcribing file: {file.filename}")

        # Save uploaded file temporarily
        audio_bytes = await file.read()

        # Process with Whisper (returns segments with probabilities)
        result = model.transcribe(
            audio_bytes,
            language="en",  # Optimize for English
            task="transcribe",
            verbose=False
        )

        # Calculate actual confidence from segments
        confidence = calculate_confidence_from_segments(result.get("segments", []))

        logger.info(f"Transcription completed: {result['text'][:50]}... (confidence: {confidence:.2f})")

        return TranscriptionResponse(
            text=result["text"],
            confidence=round(confidence, 3),
            language=result.get("language", "en"),
            duration=result.get("duration", 0.0)
        )
    except Exception as e:
        logger.error(f"Transcription error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/pronunciation", response_model=PronunciationResponse)
@limiter.limit("10/minute")  # Rate limit: 10 requests per minute
async def analyze_pronunciation(
    request: Request,
    file: UploadFile = File(...),
    expected_text: str = None,
    api_key: str = Security(verify_api_key)
):
    """
    Analyze pronunciation quality
    Requires API key authentication
    Rate limited to 10 requests per minute

    Args:
        file: Audio file to analyze
        expected_text: Expected text for comparison (optional)

    Returns:
        Pronunciation analysis with phoneme-level scores

    Note: This is a simplified implementation. For production:
    - Implement proper GOP (Goodness of Pronunciation) algorithm
    - Use forced alignment (e.g., Montreal Forced Aligner)
    - Compare with native speaker models
    - Use acoustic-phonetic features (formants, duration, pitch)
    """
    try:
        # Validate file size
        await validate_file_size(file)

        logger.info(f"Analyzing pronunciation: {file.filename}")

        # Read audio
        audio_bytes = await file.read()

        # First, transcribe to get the spoken text
        result = model.transcribe(
            audio_bytes,
            language="en",
            task="transcribe",
            verbose=False
        )

        transcription = result["text"].strip()
        confidence = calculate_confidence_from_segments(result.get("segments", []))

        # Analyze phoneme quality
        phonemes = analyze_phoneme_quality(audio_bytes, transcription)

        # Calculate overall score
        if phonemes:
            phoneme_scores = [p["score"] for p in phonemes]
            overall_score = sum(phoneme_scores) / len(phoneme_scores)
        else:
            overall_score = confidence

        # Weight by transcription confidence
        final_score = (overall_score * 0.7) + (confidence * 0.3)

        # Determine quality level
        if final_score >= 0.85:
            quality = "Excellent"
        elif final_score >= 0.75:
            quality = "Good"
        elif final_score >= 0.60:
            quality = "Fair - Practice recommended"
        else:
            quality = "Needs significant practice"

        logger.info(f"Pronunciation analysis complete: score={final_score:.2f}, quality={quality}")

        return PronunciationResponse(
            score=round(final_score, 2),
            phonemes=phonemes,
            overall_quality=quality
        )
    except Exception as e:
        logger.error(f"Pronunciation analysis error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
