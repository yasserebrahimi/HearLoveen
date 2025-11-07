# Audio to Feedback Sequence Diagram

Complete end-to-end workflow from audio recording to feedback delivery.

## Overview

This sequence diagram illustrates the complete journey of an audio recording through the HearLoveen platform, from initial upload by a parent to the final delivery of personalized feedback and analysis results.

## Main Sequence Flow

```mermaid
sequenceDiagram
    autonumber
    participant Parent as Parent<br/>(Mobile App)
    participant API as API Gateway<br/>(.NET 8)
    participant AudioSvc as AudioService<br/>(Microservice)
    participant Blob as Azure Blob<br/>Storage
    participant Bus as Azure Service<br/>Bus Queue
    participant AI as AI Worker<br/>(Python + ONNX)
    participant Analysis as AnalysisService<br/>(Microservice)
    participant DB as PostgreSQL<br/>Database
    participant Notify as Notification<br/>Service
    participant Cache as Redis<br/>Cache

    Note over Parent,Cache: Phase 1: Audio Upload & Processing

    Parent->>API: POST /api/v1/audio/upload<br/>(multipart/form-data)
    API->>API: Validate JWT & permissions
    API->>AudioSvc: Forward audio file

    AudioSvc->>AudioSvc: Validate file<br/>(format, size, duration)
    AudioSvc->>Blob: Upload to private container<br/>(generate SAS URL)
    Blob-->>AudioSvc: Return blob URL + SAS token

    AudioSvc->>DB: Store audio metadata<br/>(childId, blobUrl, status: pending)
    AudioSvc->>Bus: Publish AudioSubmitted event<br/>{audioId, blobUrl, childId, priority}
    AudioSvc-->>API: Return 202 Accepted
    API-->>Parent: {audioId, trackingId, status: processing}

    Note over Parent,Cache: Phase 2: Async AI Processing

    Bus->>AI: Consume AudioSubmitted event
    AI->>DB: Update status: processing
    AI->>Blob: Download audio file (SAS)
    Blob-->>AI: Audio stream

    AI->>AI: 1. Audio preprocessing<br/>(noise reduction, normalization)
    AI->>AI: 2. Feature extraction<br/>(MFCC, spectrograms)
    AI->>AI: 3. Whisper ASR<br/>(speech-to-text)
    AI->>AI: 4. Phoneme alignment<br/>(G2P engine)
    AI->>AI: 5. Emotion recognition<br/>(SER classifier)
    AI->>AI: 6. Composite scoring<br/>(0-100 scale)

    AI->>Analysis: POST /internal/analysis/save<br/>{transcription, phonemes, emotion, scores}
    Analysis->>DB: Save complete analysis result
    Analysis->>DB: Generate feedback report<br/>(tips, recommendations)
    Analysis-->>AI: Success confirmation

    AI->>DB: Update status: completed
    AI->>Cache: Cache results (15 min TTL)
    AI->>Notify: Trigger notification<br/>{userId, audioId, score}

    Note over Parent,Cache: Phase 3: Notification & Retrieval

    Notify->>Parent: Push notification<br/>"Analysis complete: Score 87/100"

    Parent->>API: GET /api/v1/analysis/{audioId}
    API->>Cache: Check for cached result

    alt Cache Hit
        Cache-->>API: Return cached analysis
    else Cache Miss
        API->>DB: Query analysis result
        DB-->>API: Analysis data
        API->>Cache: Store in cache
    end

    API-->>Parent: 200 OK<br/>{transcription, scores, feedback, charts}

    Parent->>Parent: Display results<br/>with visualizations
```

## Detailed Component Responsibilities

### 1. Parent Mobile App

| Action | Details |
|--------|---------|
| **Audio Recording** | Records child's speech using device microphone |
| **Format** | WAV/MP3, 16kHz sample rate, mono channel |
| **Validation** | Pre-validates duration (5s - 60s) before upload |
| **Progress** | Shows upload progress and processing status |
| **Results Display** | Visualizes transcription, scores, and recommendations |

### 2. API Gateway

| Responsibility | Implementation |
|----------------|----------------|
| **Authentication** | Validates JWT token from Azure AD B2C |
| **Authorization** | Checks user has permission for child |
| **Rate Limiting** | Enforces 60 requests/minute per user |
| **Request Routing** | Routes to appropriate microservice |
| **Error Handling** | Returns standardized error responses |

### 3. AudioService

| Function | Description |
|----------|-------------|
| **File Validation** | Checks format (WAV/MP3), size (<50MB), duration |
| **Storage Management** | Uploads to Azure Blob with SAS token |
| **Metadata Persistence** | Stores audio metadata in PostgreSQL |
| **Event Publishing** | Publishes to Service Bus for async processing |
| **Status Tracking** | Maintains processing status (pending → processing → completed) |

### 4. AI Worker

| Stage | Process | Technology | Duration |
|-------|---------|------------|----------|
| **1. Preprocessing** | Noise reduction, silence trimming, resampling | librosa, scipy | ~2s |
| **2. Feature Extraction** | MFCC, mel-spectrograms, pitch | librosa | ~1s |
| **3. Speech Recognition** | Transcription with confidence scores | Whisper ONNX | ~3-5s |
| **4. Phoneme Alignment** | G2P conversion and forced alignment | g2p_en, CTC | ~0.5s |
| **5. Emotion Detection** | Sentiment classification | CNN ONNX | ~0.3s |
| **6. Scoring** | Composite score calculation | Custom algorithm | ~0.2s |
| **Total** | | | **~7-9s** |

### 5. AnalysisService

| Task | Implementation |
|------|----------------|
| **Result Aggregation** | Combines ASR, phoneme, and emotion data |
| **Report Generation** | Creates structured feedback report |
| **Scoring Logic** | Applies per-child curriculum weighting |
| **Recommendations** | Generates personalized therapy tips |
| **Trend Analysis** | Compares with historical data |

### 6. NotificationService

| Notification Type | Trigger | Channel |
|-------------------|---------|---------|
| **Analysis Complete** | AI processing finished | Push + In-app |
| **High Score** | Score >= 90 | Push (celebratory) |
| **Needs Attention** | Score < 60 | Push + Email to parent & therapist |
| **Daily Reminder** | Scheduled | Push (8 AM local time) |

## Error Handling Flow

```mermaid
sequenceDiagram
    participant Parent
    participant API
    participant AudioSvc
    participant AI
    participant DB

    Parent->>API: Upload audio
    API->>AudioSvc: Process

    alt Invalid File Format
        AudioSvc-->>API: 400 Bad Request
        API-->>Parent: Error: Unsupported format
    else Upload Failed
        AudioSvc->>Blob: Upload attempt
        Blob-->>AudioSvc: Error
        AudioSvc-->>API: 500 Internal Error
        API-->>Parent: Error: Upload failed, retry
    else AI Processing Failed
        AI->>AI: Processing error
        AI->>DB: Update status: failed
        AI->>NotificationService: Send error notification
        NotificationService->>Parent: "Processing failed, please re-record"
    end
```

## Performance Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| **Upload Time** (10MB file) | <2s | 1.4s | ✅ |
| **Queue Latency** | <1s | 0.3s | ✅ |
| **AI Processing** (30s audio) | <10s | 7-9s | ✅ |
| **Total End-to-End** | <15s | 11-13s | ✅ |
| **Notification Delivery** | <2s | 1.1s | ✅ |
| **Cache Hit Rate** | >80% | 85% | ✅ |

## Data Flow Summary

```mermaid
graph LR
    A[Audio File<br/>30s WAV] --> B[Preprocessing<br/>16kHz Mono]
    B --> C[Features<br/>MFCC + Spectro]
    C --> D[Whisper ASR<br/>Transcription]
    C --> E[Emotion CNN<br/>Sentiment]
    D --> F[Phoneme G2P<br/>Alignment]
    F --> G[Composite Score<br/>0-100]
    E --> G
    G --> H[Feedback Report<br/>JSON + Charts]
    H --> I[Cache<br/>15 min]
    H --> J[Database<br/>Permanent]
    H --> K[Push Notification<br/>To Parent]
```

## API Endpoints Involved

| Endpoint | Method | Purpose | Auth |
|----------|--------|---------|------|
| `/api/v1/audio/upload` | POST | Upload audio file | Required |
| `/api/v1/audio/{id}/status` | GET | Check processing status | Required |
| `/api/v1/analysis/{id}` | GET | Retrieve analysis results | Required |
| `/api/v1/analysis/{id}/report` | GET | Download PDF report | Required |
| `/internal/analysis/save` | POST | Save AI results (internal) | Service-to-service |

## Retry & Resilience Strategies

### Service Bus Retry Policy

| Attempt | Delay | Max Attempts |
|---------|-------|--------------|
| 1 | Immediate | - |
| 2 | 2 seconds | - |
| 3 | 5 seconds | - |
| 4 | 10 seconds | - |
| 5+ | 30 seconds | 10 attempts |

After 10 failed attempts, message moves to Dead Letter Queue (DLQ) for manual investigation.

### Circuit Breaker Settings

| Service | Threshold | Timeout | Recovery |
|---------|-----------|---------|----------|
| **Blob Storage** | 5 failures in 30s | 60s open | 30s half-open |
| **Database** | 3 failures in 10s | 30s open | 10s half-open |
| **AI Worker** | 10 failures in 60s | 120s open | 60s half-open |

## Related Documentation

- [System Architecture](../technical/architecture/ARCHITECTURE.md) - Complete system overview
- [ML Pipeline Details](10-ml-pipeline.md) - AI/ML processing details
- [API Documentation](../../src/ApiGateway/README.md) - Full API reference
- [Audio Service](../../src/AudioService/README.md) - Audio service documentation
- [Analysis Service](../../src/AnalysisService/README.md) - Analysis service documentation

## Notes

- All audio files are stored in EU-West region for GDPR compliance
- Raw audio files are automatically deleted after 90 days (configurable)
- Analysis results are retained indefinitely unless user requests deletion
- SAS tokens expire after 1 hour for security
- All communication uses TLS 1.3 encryption
- PII data never leaves the EU region
