
# Federated Aggregator (DP-SGD skeleton)
# Notes: Use Opacus / Tensorflow Privacy in production.
import math, json
from typing import List, Dict

class DPConfig:
    def __init__(self, clip_norm=1.0, noise_multiplier=1.2, delta=1e-5):
        self.clip_norm = clip_norm
        self.noise_multiplier = noise_multiplier
        self.delta = delta

def clip_gradients(grads, C):
    norm = math.sqrt(sum(g*g for g in grads))
    scale = min(1.0, C / (norm + 1e-12))
    return [g * scale for g in grads]

def gaussian_noise(d, sigma):
    import random
    # Placeholder noise (use numpy/randomgen in prod)
    return [random.gauss(0, sigma) for _ in range(d)]

def aggregate(client_updates: List[List[float]], cfg: DPConfig) -> List[float]:
    if not client_updates:
        return []
    # Clip
    clipped = [clip_gradients(u, cfg.clip_norm) for u in client_updates]
    # Average
    avg = [sum(vals)/len(vals) for vals in zip(*clipped)]
    # Add noise
    sigma = cfg.noise_multiplier * cfg.clip_norm
    noise = gaussian_noise(len(avg), sigma)
    return [a + n for a, n in zip(avg, noise)]

def privacy_budget(epsilon: float, delta: float) -> Dict[str, float]:
    return {"epsilon": epsilon, "delta": delta}
