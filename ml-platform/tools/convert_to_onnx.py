"""
Script to convert PyTorch models to ONNX format for optimized inference
Converts Whisper and Emotion Analysis models to ONNX
"""

import torch
import whisper
import onnx
import onnxruntime as ort
from pathlib import Path
import logging
import argparse

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def convert_whisper_to_onnx(model_size: str = "base", output_dir: str = "./models"):
    """
    Convert Whisper model to ONNX format
    """
    logger.info(f"Loading Whisper {model_size} model...")
    model = whisper.load_model(model_size)
    model.eval()

    output_path = Path(output_dir) / f"whisper_{model_size}.onnx"
    output_path.parent.mkdir(parents=True, exist_ok=True)

    # Create dummy input (mel spectrogram)
    # Whisper expects 80 mel bins, 3000 frames
    dummy_input = torch.randn(1, 80, 3000)

    logger.info("Converting Whisper encoder to ONNX...")

    # Export encoder to ONNX
    with torch.no_grad():
        torch.onnx.export(
            model.encoder,
            dummy_input,
            str(output_path),
            export_params=True,
            opset_version=14,
            do_constant_folding=True,
            input_names=['mel_spectrogram'],
            output_names=['encoder_output'],
            dynamic_axes={
                'mel_spectrogram': {0: 'batch_size'},
                'encoder_output': {0: 'batch_size'}
            }
        )

    logger.info(f"Whisper model saved to {output_path}")

    # Verify the model
    logger.info("Verifying ONNX model...")
    onnx_model = onnx.load(str(output_path))
    onnx.checker.check_model(onnx_model)

    # Test inference
    logger.info("Testing ONNX inference...")
    session = ort.InferenceSession(str(output_path))
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name

    test_input = dummy_input.numpy()
    result = session.run([output_name], {input_name: test_input})

    logger.info(f"ONNX inference successful. Output shape: {result[0].shape}")
    logger.info(f"✓ Whisper {model_size} model converted successfully!")

    return output_path


def create_emotion_model_onnx(output_dir: str = "./models"):
    """
    Create and convert Emotion Analysis model to ONNX
    This creates a simple CNN-based emotion classifier
    """
    logger.info("Creating Emotion Analysis model...")

    class EmotionCNN(torch.nn.Module):
        def __init__(self, input_size=86, num_classes=7):
            super(EmotionCNN, self).__init__()
            self.fc1 = torch.nn.Linear(input_size, 128)
            self.bn1 = torch.nn.BatchNorm1d(128)
            self.dropout1 = torch.nn.Dropout(0.3)
            self.fc2 = torch.nn.Linear(128, 64)
            self.bn2 = torch.nn.BatchNorm1d(64)
            self.dropout2 = torch.nn.Dropout(0.3)
            self.fc3 = torch.nn.Linear(64, num_classes)

        def forward(self, x):
            x = torch.relu(self.bn1(self.fc1(x)))
            x = self.dropout1(x)
            x = torch.relu(self.bn2(self.fc2(x)))
            x = self.dropout2(x)
            x = self.fc3(x)
            return torch.softmax(x, dim=1)

    model = EmotionCNN()
    model.eval()

    output_path = Path(output_dir) / "emotion_analyzer.onnx"
    output_path.parent.mkdir(parents=True, exist_ok=True)

    # Create dummy input (acoustic features)
    # 40 MFCCs mean + 40 MFCCs std + 6 prosodic features
    dummy_input = torch.randn(1, 86)

    logger.info("Converting Emotion model to ONNX...")

    with torch.no_grad():
        torch.onnx.export(
            model,
            dummy_input,
            str(output_path),
            export_params=True,
            opset_version=14,
            do_constant_folding=True,
            input_names=['features'],
            output_names=['probabilities'],
            dynamic_axes={
                'features': {0: 'batch_size'},
                'probabilities': {0: 'batch_size'}
            }
        )

    logger.info(f"Emotion model saved to {output_path}")

    # Verify the model
    logger.info("Verifying ONNX model...")
    onnx_model = onnx.load(str(output_path))
    onnx.checker.check_model(onnx_model)

    # Test inference
    logger.info("Testing ONNX inference...")
    session = ort.InferenceSession(str(output_path))
    input_name = session.get_inputs()[0].name
    output_name = session.get_outputs()[0].name

    test_input = dummy_input.numpy()
    result = session.run([output_name], {input_name: test_input})

    logger.info(f"ONNX inference successful. Output shape: {result[0].shape}")
    logger.info(f"Emotion probabilities: {result[0][0]}")
    logger.info("✓ Emotion model converted successfully!")

    return output_path


def optimize_onnx_model(model_path: Path):
    """
    Optimize ONNX model for inference
    """
    logger.info(f"Optimizing {model_path}...")

    # Use ONNX Runtime to optimize
    sess_options = ort.SessionOptions()
    sess_options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
    sess_options.optimized_model_filepath = str(model_path.with_suffix('.optimized.onnx'))

    session = ort.InferenceSession(str(model_path), sess_options)

    logger.info(f"Optimized model saved to {sess_options.optimized_model_filepath}")


def main():
    parser = argparse.ArgumentParser(description="Convert ML models to ONNX format")
    parser.add_argument(
        "--whisper-size",
        type=str,
        default="base",
        choices=["tiny", "base", "small", "medium"],
        help="Whisper model size to convert"
    )
    parser.add_argument(
        "--output-dir",
        type=str,
        default="./models",
        help="Output directory for ONNX models"
    )
    parser.add_argument(
        "--optimize",
        action="store_true",
        help="Optimize ONNX models after conversion"
    )

    args = parser.parse_args()

    logger.info("=" * 60)
    logger.info("HearLoveen ML Model Converter")
    logger.info("Converting PyTorch models to ONNX format")
    logger.info("=" * 60)

    # Convert Whisper
    logger.info("\n[1/2] Converting Whisper Speech-to-Text model...")
    whisper_path = convert_whisper_to_onnx(args.whisper_size, args.output_dir)

    # Convert Emotion model
    logger.info("\n[2/2] Converting Emotion Analysis model...")
    emotion_path = create_emotion_model_onnx(args.output_dir)

    # Optimize if requested
    if args.optimize:
        logger.info("\n[Optimization] Optimizing ONNX models...")
        optimize_onnx_model(whisper_path)
        optimize_onnx_model(emotion_path)

    logger.info("\n" + "=" * 60)
    logger.info("✓ All models converted successfully!")
    logger.info(f"Models saved to: {Path(args.output_dir).absolute()}")
    logger.info("=" * 60)

    # Print model info
    logger.info("\nModel Information:")
    logger.info(f"  • Whisper ({args.whisper_size}): {whisper_path.name}")
    logger.info(f"    Size: {whisper_path.stat().st_size / (1024*1024):.2f} MB")
    logger.info(f"  • Emotion Analyzer: {emotion_path.name}")
    logger.info(f"    Size: {emotion_path.stat().st_size / (1024*1024):.2f} MB")

    logger.info("\nNext steps:")
    logger.info("  1. Copy models to ml-platform/inference/api/models/")
    logger.info("  2. Update ML API to use ONNX models")
    logger.info("  3. Test inference with real audio data")


if __name__ == "__main__":
    main()
