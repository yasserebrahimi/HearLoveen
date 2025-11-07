
# 🦻 **HearLoveen - Enterprise Platform**

**AI-Powered Speech Therapy Platform for Hearing-Impaired Children**

---

## 🌟 **Overview**

**HearLoveen** is an **enterprise-grade**, **cloud-native**, AI-driven platform designed to provide speech therapy for hearing-impaired children. Leveraging cutting-edge technologies such as **AI models** (Whisper fine-tuned), **Azure Cloud**, and **IoT devices**, it delivers personalized and effective speech therapy services to children and their families.

- **Target Users**: 500,000+ active users
- **Traction**: Serving 25+ families with 94% AI accuracy in speech-to-text conversion
- **Funding**: €750K seed round (ongoing)
- **Growth Metrics**: 25 families, 500+ recordings, 78% retention rate

### **Key Features:**
- **Real-time Speech-to-Text**: Using fine-tuned **Whisper** AI for high-accuracy transcription.
- **Emotion Analysis**: Detecting emotional states during therapy to adapt to the child's needs.
- **IoT Device Integration**: Real-time data collection from hearing aids and cochlear implants via **BLE**, **ASHA**, and **MFi** protocols.
- **Multi-language Support**: i18n support for **English**, **German**, and **Dutch**.
- **Real-Time Analytics**: Visualizing progress through custom dashboards and reports for parents and therapists.

---

## 🔧 **Tech Stack**

**Backend**:
- **.NET 8**, **CQRS**, **DDD**, **Clean Architecture**

**Machine Learning**:
- **Python**, **PyTorch**, **Whisper**, **MLflow**

**Frontend**:
- **React 18**, **TypeScript**, **Redux Toolkit**

**Mobile**:
- **React Native 0.73+**

**Cloud**:
- **Azure AKS**, **IoT Hub**, **Azure ML Services**, **Terraform**, **Kubernetes**

**Data**:
- **PostgreSQL 14**, **Redis Cluster**, **Kafka**

---

## 🚀 **Quick Start**

### **Prerequisites**:
- **Docker & Docker Compose** (for local development)
- **.NET 8 SDK** (for backend development)
- **Node.js 18+** (for web dashboard)
- **Python 3.11+** (for ML API)
- **Azure Subscription** (for cloud services)

### **Setup**:
1. **Clone the repository and set up the environment**:
```bash
git clone https://github.com/yasserebrahimi/HearLoveen.git
cd HearLoveen

# Copy and configure environment variables
cp .env.example .env
# Edit .env with your actual credentials (Azure, API keys)
```

2. **Start all services with Docker Compose**:
```bash
docker-compose up -d
```

This will start the following services:

- **API Gateway** (port 5000) - Main entry point
- **Microservices**:
  - AudioService (port 5001)
  - AnalysisService (port 5002)
  - NotificationService (port 5003)
  - UserService (port 5004)
  - IoTService (port 5005)
  - AnalysisProxy (port 5006)

---

## 📊 **Architecture Overview**

### **System Architecture**

```mermaid
graph TD;
    A[Frontend (React/Vite)] --> B[API Gateway (ASP.NET Core)];
    B --> C[AudioService (Microservice)];
    B --> D[AnalysisService (Microservice)];
    B --> E[NotificationService (Microservice)];
    B --> F[UserService (Microservice)];
    B --> G[IoTService (Microservice)];
    C --> H[Audio File Storage (Blob Storage)];
    D --> I[ML Models (Whisper, CNN)];
    F --> J[PostgreSQL Database];
    F --> K[Redis Cache];
    G --> L[IoT Device Telemetry (Azure IoT Hub)];
    B --> M[AnalysisProxy (Proxy to AI Service)];
    M --> I;
```

### **Service Flow**:
- **Frontend (React/Vite)** communicates with the **API Gateway** which routes the requests to different microservices (Audio, Analysis, Notification, User, IoT).
- **AudioService** processes audio files and stores them in **Blob Storage**.
- **AnalysisService** performs speech-to-text conversion and emotion detection using the **Whisper** model.
- **IoTService** manages real-time data from hearing aids and cochlear implants via **Azure IoT Hub**.
- The **AnalysisProxy** acts as a bridge to the **AI Service**, allowing it to process requests from the frontend and provide feedback to therapists and parents.

---

## 💻 **Components and Services**

### **1. Frontend (React/Vite)**
- **React 18** + **TypeScript** for building responsive UIs.
- **Material-UI** for pre-built, customizable UI components.
- **Recharts** for visualizing progress and reports.
- **i18n** for multilingual support (EN, DE, NL).

### **2. Backend (ASP.NET Core)**
- **CQRS Pattern** with **MediatR** for handling Commands and Queries.
- **JWT Authentication** and **Role-based Authorization**.
- **Rate Limiting** for protecting APIs from abuse (120 requests/minute).
- **RESTful APIs** with **Swagger Documentation**.

### **3. Machine Learning (Python)**
- **Whisper** for Speech-to-Text conversion.
- **Emotion Recognition** using **CNN**.
- **MLflow** for model management and deployment.
- **ONNX** for model conversion and deployment optimization (3-5x faster).

### **4. IoT Integration (Azure IoT Hub)**
- **BLE** (Bluetooth Low Energy) support for communication with hearing aids.
- **ASHA/MFi** protocols for iPhone-specific integration.
- **Telemetry** for real-time device data collection.

---

## 🛠 **API Endpoints**

### **Audio Service**
- **POST /audio/upload**: Upload audio files for processing.
- **GET /audio/{id}/progress**: Retrieve the status of audio processing.

### **Analysis Service**
- **POST /analysis/speech-to-text**: Convert speech to text using **Whisper**.
- **POST /analysis/emotion**: Detect emotion from audio using **CNN**.

### **User Service**
- **POST /users/register**: Register a new user (parent or therapist).
- **POST /users/login**: Authenticate user and issue JWT token.
- **GET /users/{id}**: Get user profile information.

### **IoT Service**
- **POST /iot/register-device**: Register a new hearing device.
- **GET /iot/telemetry**: Retrieve real-time data from IoT devices.

### **Privacy API (DSR Endpoints)**
- **POST /dsr/export**: Export user data as part of Data Subject Request.
- **POST /dsr/delete**: Delete user data as part of Data Subject Request.

---

## 🌍 **Cloud Infrastructure (Azure)**

### **Services Used**:
- **Azure Blob Storage** for storing audio files.
- **Azure IoT Hub** for managing IoT devices.
- **Azure Kubernetes Service (AKS)** for managing containerized microservices.
- **Azure SQL Database** for relational data storage.
- **Redis** for caching frequently accessed data.

---

## 🛡 **Security and Compliance**

### **Security Features**:
- **JWT Authentication** with **Role-based Access Control (RBAC)**.
- **Rate Limiting** (120 requests/min) to prevent DDoS attacks.
- **Data Encryption** using **TLS 1.3** and **AES-256** for secure communication.
- **OAuth2** and **OIDC** integration with **Azure AD B2C** for secure authentication.

### **Compliance**:
- **GDPR Compliance** with **DSR** (Data Subject Request) support.
- **MDR Compliance**: Ensuring compliance with **Medical Device Regulations** for healthcare applications.

---

## 📜 **MDR Compliance Documentation**
For detailed MDR documentation and CE marking process, refer to:
- **MDR Compliance**: [docs/compliance/MDR_COMPLIANCE.md]
- **Audit Trail** and **ISO 13485** standards are integrated for regulatory compliance.

---

## 📈 **Testing and Validation**

### **Automated Testing**:
- **Unit Tests** using **xUnit** for backend logic.
- **Integration Tests** for service communication.
- **End-to-End Tests** for frontend and backend integration.

### **Performance Testing**:
- **Load Testing** with **JMeter** for handling 500,000+ active users.
- **Penetration Testing** for security vulnerabilities.

---

## 🚀 **Deployment Guide**

### **Docker Setup**:
1. **Build and run services**:
```bash
docker-compose up --build
```

2. **Deploy to Kubernetes**:
   - Configure **Helm charts** for microservices.
   - Use **Terraform** for infrastructure provisioning.

---

## 📚 **Further Documentation**

- **[Installation Guide](docs/INSTALLATION_GUIDE.md)**
- **[API Documentation](docs/api/README.md)**
- **[Security Implementation Guide](docs/security/SECURITY_IMPLEMENTATION.md)**
- **[Compliance and MDR Docs](docs/compliance/MDR_COMPLIANCE.md)**

---

## 🔧 **Contributing**

For contributing to **HearLoveen**, refer to:
- **[Contributing Guidelines](CONTRIBUTING.md)**
- **[Code of Conduct](CODE_OF_CONDUCT.md)**

---

### **License**:
This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## **Final Notes**:
**HearLoveen** is designed for scalability, compliance, and performance. With the integration of cutting-edge technologies in **AI**, **IoT**, and **Cloud Infrastructure**, we aim to provide an impactful solution for hearing-impaired children globally.
