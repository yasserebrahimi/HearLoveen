import math
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
PY_SRC = ROOT / "src" / "ai-workers" / "python"
if str(PY_SRC) not in sys.path:
    sys.path.insert(0, str(PY_SRC))

from federated.aggregator import aggregate, DPConfig


def test_aggregate_returns_empty_for_no_updates():
    cfg = DPConfig()
    assert aggregate([], cfg) == []


def test_aggregate_respects_noise_length(monkeypatch):
    cfg = DPConfig(clip_norm=10.0, noise_multiplier=0.0)

    def fake_noise(d, sigma):
        return [0.0] * d

    monkeypatch.setattr("federated.aggregator.gaussian_noise", fake_noise)
    result = aggregate([[1.0, 2.0], [3.0, 4.0]], cfg)
    # With clipping default > gradients, average should be mean of columns
    assert result == [2.0, 3.0]
