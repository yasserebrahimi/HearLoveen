
# Client-side stub for federated updates (device/edge)
# Replace with real gradient extraction from on-device model.
def local_update(batch_audio_features) -> list[float]:
    # Return a vector-shaped update (example only)
    return [0.01 * sum(sum(fr) for fr in batch_audio_features)]
