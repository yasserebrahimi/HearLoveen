# HearLoveen Research & Complete Package

This `docs/` space integrates the **hearloveen-complete** research/clinical/business materials into the main repository.

## Structure
- `docs/research/clinical/` – RCT protocols, longitudinal study assets, consent templates, statistical analysis scripts
- `docs/research/xai/` – Explainable-AI demo backend (e.g., SHAP), notebooks, API stubs
- `docs/research/business/` – Business model canvas, competitive analysis, unit economics
- `docs/research/datasets/` – synthetic/sample data and data notes
- `docs/complete-package/` – original full snapshot (including `INDEX.md`)

## How to run (research profile)
Use `docker-compose.research.yml` at the repository root for the XAI demo backend (FastAPI/Uvicorn):

```bash
docker compose -f docker-compose.research.yml up --build
```