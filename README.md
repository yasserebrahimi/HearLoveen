# 🦻 HearLoveen - Enterprise Platform

**AI-Powered Speech Therapy Platform for Hearing-Impaired Children**

## Overview
Enterprise-grade, cloud-native speech therapy ecosystem. Serving 25+ families with 94% AI accuracy.

- **Funding:** €750K seed round (in progress)
- **Traction:** 25 families, 500+ recordings, 78% retention
- **AI:** Whisper fine-tuned (94% accuracy)
- **Architecture:** CQRS, Event Sourcing, Microservices
- **Cloud:** Azure (AKS, IoT Hub, ML Services)

## Tech Stack
- **Backend:** .NET 8, CQRS, DDD, Clean Architecture
- **ML:** Python, PyTorch, Whisper, MLflow
- **Frontend:** React 18, TypeScript, Redux Toolkit
- **Mobile:** React Native 0.73+
- **Cloud:** Azure AKS, Terraform, Kubernetes
- **Data:** PostgreSQL 14, Redis Cluster, Kafka

## Quick Start
```bash
docker-compose up -d
cd src/ApiGateway && dotnet run
cd ml-platform/inference/api && python main.py
cd web-dashboard && npm run dev
```

## Documentation
- [Executive Summary](docs/business/01_EXECUTIVE_SUMMARY.md)
- [Pitch Deck](docs/business/02_PITCH_DECK.md)
- [Architecture](docs/technical/ARCHITECTURE.md)

Built with ❤️ for 34M children worldwide


---

## Architecture & Diagrams
See `docs/technical/architecture/ARCHITECTURE.md` for an index of all Mermaid diagrams:
- Context, Containers, Backend, Sequence
- GDPR lifecycle, ERD
- CI/CD, RBAC
- Parent journey, AI/ML pipeline, IoT edge
- Roadmap

## Quickstart (Developer)

```bash
# API
dotnet restore
dotnet build -c Release
dotnet run --project src/api/HearLoveen.Api

# Worker (Python)
python -m venv .venv && source .venv/bin/activate
pip install -r src/ai-workers/python/requirements.txt
export SB_CONNECTION="Endpoint=sb://..."
python src/ai-workers/python/main.py
```
# HearLoveen
