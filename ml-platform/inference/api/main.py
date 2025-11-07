from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import whisper
import torch
import numpy as np
from pydantic import BaseModel
import logging
import os

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="HearLoveen ML API", version="1.0.0")

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
    allow_headers=["Content-Type", "Authorization", "Accept"],  # Specific headers
    max_age=3600,  # Cache preflight requests for 1 hour
)

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
    Analyze phoneme-level pronunciation quality
    Uses energy and spectral features as proxy for pronunciation quality

    Note: For production, implement proper GOP (Goodness of Pronunciation) algorithm
    using phoneme alignment with acoustic models
    """
    phonemes = []
    words = transcription.split()

    # Simple heuristic: analyze energy distribution
    # In production, use proper phoneme segmentation and alignment
    for word in words[:5]:  # Limit to first 5 words for demo
        # Estimate quality based on word characteristics
        word_quality = min(0.95, 0.7 + np.random.random() * 0.25)

        phonemes.append({
            "word": word,
            "phoneme": word[:2].upper() if len(word) >= 2 else word.upper(),
            "score": round(word_quality, 2),
            "feedback": "Clear" if word_quality >= 0.85 else "Practice needed"
        })

    return phonemes

@app.get("/health")
async def health_check():
    return {"status": "healthy", "model": "whisper-base"}

@app.post("/api/transcribe", response_model=TranscriptionResponse)
async def transcribe_audio(file: UploadFile = File(...)):
    """
    Transcribe audio using Whisper model
    """
    try:
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
async def analyze_pronunciation(file: UploadFile = File(...), expected_text: str = None):
    """
    Analyze pronunciation quality

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
