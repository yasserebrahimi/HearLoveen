from fastapi import FastAPI
app = FastAPI(title="HearLoveen XAI Demo")
@app.get("/healthz")
def health():
    return {"status":"ok"}