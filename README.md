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
- API Gateway (port 5000)
- Microservices (AudioService, AnalysisService, UserService, etc.)
- PostgreSQL, Redis, Kafka
- ML API (port 8000)
- Prometheus & Grafana

3. **Access the applications:**
- **API Gateway:** http://localhost:5000
- **Web Dashboard:** Build and run separately (see below)
- **ML API:** http://localhost:8000/docs (Swagger UI)
- **Grafana:** http://localhost:3000 (admin/your_password)

### Web Dashboard Development

```bash
cd web-dashboard
npm install
npm run dev
# Access at http://localhost:5173
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

# Web dashboard tests
cd web-dashboard
npm test
```

## Documentation
- [Executive Summary](docs/business/01_EXECUTIVE_SUMMARY.md)
- [Pitch Deck](docs/business/02_PITCH_DECK.md)
- [Architecture](docs/technical/ARCHITECTURE.md)
- [Setup Guide](SETUP_GUIDE.md)

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
