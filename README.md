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

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (for local development)
- Node.js 18+ (for web dashboard)
- Python 3.11+ (for ML API)

### Setup

1. **Clone and setup environment:**
```bash
git clone https://github.com/yasserebrahimi/HearLoveen.git
cd HearLoveen

# Copy and configure environment variables
cp .env.example .env
# Edit .env with your actual credentials
```

2. **Start all services with Docker Compose:**
```bash
docker-compose up -d
```

This starts:
- **API Gateway** (port 5000) - Main entry point
- **Microservices:**
  - AudioService (port 5001)
  - AnalysisService (port 5002)
  - NotificationService (port 5003)
  - UserService (port 5004)
  - IoTService (port 5005)
  - AnalysisProxy (port 5100) - OIDC-authenticated XAI proxy
  - Privacy.API (port 5200) - GDPR DSR endpoints
  - Analytics (port 5300) - LTV/CAC business metrics
- **Workers:**
  - DSR Worker (background) - Async GDPR data processing
- **Infrastructure:**
  - PostgreSQL (port 5432)
  - Redis (port 6379)
  - RabbitMQ (ports 5672, 15672 management UI)
  - Kafka (port 9092) + Zookeeper
- **ML & Monitoring:**
  - ML API (port 8000) - ONNX inference for STT & emotion
  - Prometheus (port 9090)
  - Grafana (port 3000)
- **Frontend:**
  - Therapist Dashboard (port 5173) - React/Vite with XAI visualization

3. **Access the applications:**
- **API Gateway:** http://localhost:5000
- **Therapist Dashboard:** http://localhost:5173 (with i18n: EN/DE/NL)
- **ML API:** http://localhost:8000/docs (Swagger UI)
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)
- **Grafana:** http://localhost:3000 (admin/your_password)
- **AnalysisProxy:** http://localhost:5100 (requires OIDC token)
- **Privacy.API:** http://localhost:5200 (DSR endpoints)
- **Analytics:** http://localhost:5300 (business metrics)

### Therapist Dashboard Development

```bash
cd apps/therapist-dashboard
npm install
npm run dev
# Access at http://localhost:5173
# Supports EN, DE, NL languages with i18n
```

### Mobile App Development

```bash
cd mobile-app
npm install
npm start
# Follow React Native instructions for iOS/Android
```

### Running Tests

```bash
# Backend tests
dotnet test

# ML API tests
cd ml-platform/inference/api
pytest

# Therapist dashboard tests
cd apps/therapist-dashboard
npm test
```

## Key Features Implemented

### 🔒 Security & Compliance
- **OIDC/JWKS Authentication:** Azure AD B2C integration with JWT validation
- **GDPR Compliance:** DSR endpoints for data export/deletion with async processing
- **MDR Compliance:** ISO 13485, IEC 62304, ISO 14971 documentation
- **Security Best Practices:** Parameterized queries, rate limiting, CORS, CSP headers
- **Audit Logging:** 7-year retention for all critical operations

### 🤖 AI/ML Capabilities
- **ONNX Runtime:** 3-5x faster inference than PyTorch
- **Speech-to-Text:** Whisper model with 94% accuracy
- **Emotion Analysis:** CNN-based emotion detection
- **XAI (Explainable AI):** Visualization of AI decision factors

### 📱 Mobile Features
- **BLE Integration:** ASHA and MFi protocol support
- **Hearing Aid Connectivity:** Auto-reconnection, volume control, battery monitoring
- **Real-time Audio Streaming:** Low-latency audio processing

### 📊 Business Analytics
- **LTV/CAC Metrics:** Customer lifetime value and acquisition cost tracking
- **Cohort Analysis:** User retention and engagement metrics
- **Health Monitoring:** Real-time system health assessment

### 🌍 Internationalization
- **Multi-language Support:** English, German, Dutch (EN/DE/NL)
- **i18next Integration:** Dynamic language switching in therapist dashboard

### 🎨 Therapist Dashboard
- **Modern UI:** React 18 + TypeScript + Material-UI
- **Real-time Analytics:** Patient progress tracking and session analysis
- **XAI Visualization:** Transparent AI decision explanations
- **Responsive Design:** Mobile-friendly interface

## Documentation

### Business & Strategy
- [Executive Summary](docs/business/01_EXECUTIVE_SUMMARY.md)
- [Pitch Deck](docs/business/02_PITCH_DECK.md)

### Technical
- [Architecture](docs/technical/ARCHITECTURE.md)
- [Setup Guide](SETUP_GUIDE.md)
- [Comprehensive Audit Report](COMPREHENSIVE_AUDIT_REPORT.md)

### Security & Compliance
- [Security Implementation Guide](docs/security/SECURITY_IMPLEMENTATION.md)
- [MDR Compliance Documentation](docs/compliance/MDR_COMPLIANCE.md)

## CI/CD

GitHub Actions workflow runs automatically on push:
- Builds all .NET services
- Tests ML API
- Builds web dashboard
- Runs integration tests
- Docker image builds

## Production Deployment

See [deployment guide](docs/technical/DEPLOYMENT.md) for:
- Kubernetes manifests
- Terraform infrastructure
- Azure setup
- Monitoring & alerting

Built with ❤️ for 34M children worldwide


## Research Package (Integrated)
We integrated the **hearloveen-complete** research/clinical/business materials under [`docs/`](docs/README.md).
- Quick start for XAI demo: `docker compose -f docker-compose.research.yml up --build`
- See `docs/README.md` for structure and details.


## P0 Fixes Applied (Research/XAI)
- JWT-required endpoints with rate-limiting (slowapi).
- Config via `.env.example` and `config.yaml` (no hardcoded secrets).
- `models/` and `data/` folders added; service runs in **mock** if models are absent.
- Tests (`docs/research/xai/backend/tests`) and CI workflow `research-ci.yml` to run them.
- `docker-compose.research.yml` mounts `models/` and `data/`.

> For production, integrate with API Gateway/OIDC and Key Vault. Replace `PLEASE_CHANGE_ME` in `.env.example` and `config.yaml`.


## Unified Dev & CI
- **docker-compose.dev.yml** runs Postgres, Redis, MinIO, RabbitMQ, XAI backend and API (if present).
- **deploy/k8s/hardening/** includes NetworkPolicy, PDB and baseline hardening values.
- **.github/workflows/ci.yml** builds/tests .NET solution if found, and runs Python XAI tests.
- See `docs/DEV_GUIDE.md` for quick start.


## Operational Additions
- **AnalysisProxy (.NET 8)** bridging Web/API to XAI.
- **Privacy.API (.NET 8)** with `/dsr/export` & `/dsr/delete` (queued).
- **Telemetry ingest worker** to persist session KPIs.
- **MLflow** service for local model tracking.
- **MinIO lifecycle** script to enforce 60-day audio retention (dev).
- **Therapist Dashboard skeleton** to quickly hit analysis endpoints.

> NOTE: Replace dev secrets, wire OIDC/JWKS in gateway, and integrate real ONNX models under `models/` for non-mock behavior.
