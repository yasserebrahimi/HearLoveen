from fastapi.testclient import TestClient
import os
from jose import jwt

from main import app, JWT_SECRET, JWT_ALGO

client = TestClient(app)

def auth_header():
    token = jwt.encode({"sub":"tester"}, JWT_SECRET, algorithm=JWT_ALGO)
    return {"Authorization": f"Bearer {token}"}

def test_health():
    r = client.get("/healthz")
    assert r.status_code == 200
    assert "status" in r.json()

def test_pron_requires_auth():
    r = client.post("/api/explain/pronunciation", json={"text":"mama"}, headers={})
    assert r.status_code == 401

def test_pron_ok():
    r = client.post("/api/explain/pronunciation", json={"text":"mama papa"}, headers=auth_header())
    assert r.status_code == 200
    js = r.json()
    assert "word_scores" in js and "overall" in js