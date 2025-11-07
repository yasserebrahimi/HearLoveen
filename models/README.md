# HearLoveen ONNX Models

This directory contains ONNX models used by the AI workers for speech analysis.

## Models

### 1. ASR Model (`asr.onnx`)
**Purpose**: Automatic Speech Recognition
**Input**: Audio features (MFCC or mel-spectrogram)
**Output**: Phoneme or character logits
**Use**: Transcription and phoneme-level analysis

### 2. SER Model (`ser.onnx`)
**Purpose**: Speech Emotion Recognition
**Input**: Audio embeddings (Wav2Vec2 or similar)
**Output**: Emotion probabilities
**Use**: Emotional state detection for engagement tracking

## Setup

### Quick Start (Development)

```bash
# Install dependencies
pip install torch onnx numpy

# Download/create models
python download_models.py
```

This will create dummy ONNX models for development and testing.

### Production Setup

For production use, you need real trained models:

#### Option 1: Train Your Own Models

```python
# Example: Convert PyTorch model to ONNX
import torch

# Load your trained model
model = YourASRModel.load_from_checkpoint("checkpoint.pt")
model.eval()

# Create dummy input
dummy_input = torch.randn(1, 100, 80)  # [batch, time, features]

# Export to ONNX
torch.onnx.export(
    model,
    dummy_input,
    "asr.onnx",
    export_params=True,
    opset_version=13,
    input_names=['audio_features'],
    output_names=['logits'],
    dynamic_axes={
        'audio_features': {1: 'time'},
        'logits': {1: 'time'}
    }
)
```

#### Option 2: Download Pre-trained Models

1. **ASR Models** (Hugging Face):
   - Wav2Vec2: https://huggingface.co/facebook/wav2vec2-base-960h
   - Whisper (ONNX): https://github.com/openai/whisper
   - QuartzNet: https://catalog.ngc.nvidia.com/orgs/nvidia/teams/nemo/models/stt_en_quartznet15x5

2. **SER Models**:
   - Wav2Vec2-SER: https://huggingface.co/ehcalabres/wav2vec2-lg-xlsr-en-speech-emotion-recognition
   - Custom SER: Train on IEMOCAP, RAVDESS, or similar datasets

#### Option 3: Use Azure ML Model Registry

```bash
# Download from Azure ML
az ml model download \
  --name hearloveen-asr \
  --version 1 \
  --download-path ./models
```

## Model Specifications

### ASR Model Expected Format

```
Input:
  - Name: audio_features
  - Type: float32
  - Shape: [batch_size, sequence_length, feature_dim]
  - Feature_dim: 80 (MFCC) or 128 (mel-spectrogram)

Output:
  - Name: logits
  - Type: float32
  - Shape: [batch_size, sequence_length, vocab_size]
  - Vocab_size: Number of phonemes or characters (typically 32-50)
```

### SER Model Expected Format

```
Input:
  - Name: audio_features
  - Type: float32
  - Shape: [batch_size, sequence_length, 768]
  - 768: Wav2Vec2 embedding dimension

Output:
  - Name: emotion_logits
  - Type: float32
  - Shape: [batch_size, 7]
  - Emotions: [happy, sad, angry, fear, disgust, surprise, neutral]
```

## Verification

Verify models are valid:

```bash
python -c "import onnx; onnx.checker.check_model('asr.onnx')"
python -c "import onnx; onnx.checker.check_model('ser.onnx')"
```

## Usage in AI Worker

Models are loaded by the AI worker automatically:

```python
# In src/ai-workers/python/main.py
import onnxruntime as ort

asr_session = ort.InferenceSession("/models/asr.onnx")
ser_session = ort.InferenceSession("/models/ser.onnx")
```

## Performance Optimization

For production deployment:

1. **Quantization**: Reduce model size with INT8 quantization
   ```bash
   python -m onnxruntime.quantization.preprocess \
     --input asr.onnx \
     --output asr_quantized.onnx
   ```

2. **GPU Acceleration**: Use CUDA execution provider
   ```python
   session = ort.InferenceSession(
       "asr.onnx",
       providers=['CUDAExecutionProvider', 'CPUExecutionProvider']
   )
   ```

3. **Batch Processing**: Process multiple audios simultaneously

## Notes

- **Dummy Models**: The `download_models.py` script creates dummy models for development. These are NOT suitable for production.
- **Model Size**: Real ASR models are typically 50-500MB, SER models 100-300MB.
- **License**: Ensure you have appropriate licenses for any pre-trained models you use.
- **Privacy**: Never include trained models in git repository. Use DVC or Azure ML for model versioning.

## Troubleshooting

### Model not found error
```bash
# Ensure models directory is mounted in Docker
# Check docker-compose.yml has:
volumes:
  - ./models:/models
```

### ONNX Runtime error
```bash
# Update ONNX Runtime
pip install --upgrade onnxruntime onnxruntime-gpu
```

### Model validation fails
```bash
# Check model with Netron (visual inspector)
pip install netron
netron asr.onnx
```

## Contact

For questions about model training or deployment, contact the ML team.
