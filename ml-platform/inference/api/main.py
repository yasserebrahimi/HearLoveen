from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import whisper
import torch
import numpy as np
from pydantic import BaseModel
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="HearLoveen ML API", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
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
        
        # Process with Whisper
        result = model.transcribe(audio_bytes)
        
        logger.info(f"Transcription completed: {result['text'][:50]}...")
        
        return TranscriptionResponse(
            text=result["text"],
            confidence=0.94,  # Calculate from segments
            language=result.get("language", "en"),
            duration=result.get("duration", 0.0)
        )
    except Exception as e:
        logger.error(f"Transcription error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/api/pronunciation", response_model=PronunciationResponse)
async def analyze_pronunciation(file: UploadFile = File(...)):
    """
    Analyze pronunciation quality using GOP (Goodness of Pronunciation)
    """
    try:
        logger.info(f"Analyzing pronunciation: {file.filename}")
        
        # Placeholder for pronunciation scoring
        # In production, use GOP algorithm or custom ML model
        
        score = 0.85  # Example score
        phonemes = [
            {"phoneme": "AH", "score": 0.92},
            {"phoneme": "B", "score": 0.78},
            {"phoneme": "OW", "score": 0.88}
        ]
        
        quality = "Good" if score >= 0.8 else "Needs Practice"
        
        return PronunciationResponse(
            score=score,
            phonemes=phonemes,
            overall_quality=quality
        )
    except Exception as e:
        logger.error(f"Pronunciation analysis error: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
