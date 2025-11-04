
# Switchable AEC/NS/VAD pipeline (stubs)
def process(wave, sr, use_aec=True, use_ns=True, use_vad=True):
    # TODO: integrate WebRTC AEC3 / RNNoise if available
    return wave, {"aec": use_aec, "ns": use_ns, "vad": use_vad}
