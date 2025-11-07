# HearLoveen — Dev Guide (Unified)
This guide describes how to run a **local unified stack** for development.

## 1) Research/XAI stack
```bash
docker compose -f docker-compose.research.yml up --build
```
- Swagger: http://localhost:8000/docs

## 2) Full Dev stack (DB/cache/object/broker + XAI + API)
```bash
docker compose -f docker-compose.dev.yml up --build
```
- Postgres: `localhost:5432` (hlv/hlvpass, db=hearloveen)
- Redis: `localhost:6379`
- MinIO: Console on http://localhost:9001 (hlvminio/hlvminiopass)
- RabbitMQ: http://localhost:15672 (guest/guest)
- XAI backend: http://localhost:8000/docs
- API (.NET): http://localhost:8080 (if project exists)

## 3) Notes
- Replace `PLEASE_CHANGE_ME` in `.env.example` / `config.yaml` before any external demo.
- For production: prefer Azure resources (AKS, Blob, ASB) and Key Vault + Managed Identity.
- If API uses Azure Service Bus only, toggle RabbitMQ in `Messaging__Broker` won't be effective unless supported in code.## New services
- **analysis-proxy (.NET 8)** → Proxies to XAI backend with rate-limit; runs on :8081
- **privacy-api (.NET 8)** → DSR export/delete queuing; runs on :8082
- **telemetry-ingest (Python)** → Consumes RabbitMQ `telemetry` and writes KPIs to Postgres
- **mlflow** → Local MLflow at http://localhost:5000

## Apply MinIO retention (dev)
```bash
bash setup/minio-retention.sh
```