# GDPR-First Data Lifecycle

Complete data lifecycle management with privacy-by-design principles and GDPR compliance.

## Overview

HearLoveen implements a **privacy-first** approach to data management, ensuring full compliance with GDPR (General Data Protection Regulation) and other privacy regulations. This document details how personal data flows through the system with appropriate safeguards at every stage.

## Complete Data Lifecycle

```mermaid
flowchart TB
    START([User Registration]) --> CONSENT

    subgraph "1. Collection Phase"
        CONSENT[Explicit Parental Consent<br/>Granular permissions]
        CONSENT --> LEGAL[Legal Basis Check<br/>Parental authority]
        LEGAL --> MIN[Collect Minimal Data<br/>Data minimization principle]
        MIN --> PURPOSE[Purpose Limitation<br/>Speech therapy only]
    end

    subgraph "2. Protection Phase"
        PURPOSE --> TRANSIT[Encrypt In Transit<br/>TLS 1.3]
        TRANSIT --> REST[Encrypt At Rest<br/>AES-256-GCM]
        REST --> PSEUDO[Pseudonymization<br/>Internal IDs only]
        PSEUDO --> ZONE[Data Residency<br/>EU-only storage]
    end

    subgraph "3. Storage Phase"
        ZONE --> PII_DB[(PII in PostgreSQL<br/>EU-West region)]
        ZONE --> AUDIO_BLOB[(Audio Files<br/>Private Blob + SAS)]
        ZONE --> ANALYSIS[(Analysis Results<br/>Separate schema)]
    end

    subgraph "4. Access Control"
        PII_DB --> RBAC[Role-Based Access<br/>Parent/Therapist/Admin]
        AUDIO_BLOB --> RBAC
        ANALYSIS --> RBAC
        RBAC --> AUDIT_ACCESS[Audit Logging<br/>Who accessed what, when]
    end

    subgraph "5. Retention & Lifecycle"
        RBAC --> RETENTION[Retention Policy<br/>Configurable per data type]
        RETENTION --> RAW_DEL[Raw Audio: 90 days]
        RETENTION --> TRANS_DEL[Transcripts: 2 years]
        RETENTION --> META_KEEP[Metadata: Indefinite<br/>or until DSR]
    end

    subgraph "6. User Rights"
        AUDIT_ACCESS --> ACCESS_RIGHT[Right to Access<br/>Automated export]
        AUDIT_ACCESS --> RECTIFY[Right to Rectification<br/>Update profile data]
        AUDIT_ACCESS --> PORTABILITY[Data Portability<br/>Machine-readable JSON]
        AUDIT_ACCESS --> ERASURE[Right to Erasure<br/>Cascading delete]
        AUDIT_ACCESS --> RESTRICT[Right to Restrict<br/>Processing freeze]
        AUDIT_ACCESS --> OBJECT[Right to Object<br/>Opt-out options]
    end

    subgraph "7. DSR Processing"
        ERASURE --> SOFT_DEL[Soft Delete<br/>Mark for deletion]
        SOFT_DEL --> VERIFY[Verification Period<br/>30-day grace]
        VERIFY --> HARD_DEL[Hard Delete<br/>Permanent removal]
        HARD_DEL --> BACKUP_PURGE[Purge from Backups<br/>Within 90 days]
    end

    BACKUP_PURGE --> AUDIT_TRAIL[(Audit Trail<br/>Non-PII logs only)]
    ACCESS_RIGHT --> AUDIT_TRAIL
    PORTABILITY --> AUDIT_TRAIL

    AUDIT_TRAIL --> END([Compliant Data Lifecycle])

    style CONSENT fill:#90EE90
    style ERASURE fill:#FFB6C1
    style HARD_DEL fill:#FF6B6B
    style AUDIT_TRAIL fill:#87CEEB
```

## Data Categories & Classification

### Personal Data Inventory

| Data Category | Examples | Storage Location | Retention | Encryption |
|---------------|----------|------------------|-----------|------------|
| **Direct Identifiers** | Name, Email, Phone | PostgreSQL PII schema | Account lifetime | AES-256 + TLS |
| **Child Data** | Name, DOB, Diagnosis | PostgreSQL PII schema | Account lifetime | AES-256 + TLS |
| **Audio Recordings** | Voice samples (WAV/MP3) | Azure Blob (EU) | 90 days | AES-256 + SAS |
| **Transcriptions** | Speech-to-text output | PostgreSQL | 2 years | AES-256 + TLS |
| **Analysis Results** | Scores, phonemes, emotions | PostgreSQL | 2 years | AES-256 + TLS |
| **Usage Metadata** | Login times, session duration | PostgreSQL | 1 year | TLS only |
| **Device Data** | IoT telemetry | IoT Hub + PostgreSQL | 90 days | TLS + field-level |
| **Therapist Notes** | Clinical observations | PostgreSQL | Account lifetime | AES-256 + TLS |

### Non-Personal Data

| Data Type | Purpose | Retention | Notes |
|-----------|---------|-----------|-------|
| **Aggregated Analytics** | Product improvement | Indefinite | Anonymized, non-reversible |
| **System Logs** | Debugging, security | 90 days | Sanitized of PII |
| **Performance Metrics** | Monitoring | 1 year | No user linkage |
| **Model Training Data** | ML improvements | Indefinite | Fully anonymized |

## Legal Basis for Processing

| Processing Activity | Legal Basis | Notes |
|---------------------|-------------|-------|
| **User Registration** | Consent | Explicit opt-in required |
| **Audio Analysis** | Consent | Per-session consent |
| **Therapist Access** | Legitimate Interest | Healthcare provision |
| **Service Improvement** | Legitimate Interest | Aggregated data only |
| **Security Monitoring** | Legal Obligation | Fraud prevention |
| **Backup Retention** | Legitimate Interest | Business continuity |

## Consent Management Flow

```mermaid
stateDiagram-v2
    [*] --> ConsentRequest
    ConsentRequest --> ReviewTerms: User views policy
    ReviewTerms --> AcceptAll: Accept all
    ReviewTerms --> Granular: Customize

    Granular --> Essential: Always required
    Granular --> Analytics: Optional
    Granular --> Research: Optional

    Essential --> Confirmed
    Analytics --> Confirmed
    Research --> Confirmed
    AcceptAll --> Confirmed

    Confirmed --> Active: Processing enabled
    Active --> Withdraw: User withdraws
    Active --> Expired: 2 years elapsed

    Withdraw --> Frozen: Processing stopped
    Expired --> ReConsent: Request renewal

    Frozen --> Deleted: 30-day grace period
    ReConsent --> ConsentRequest

    Deleted --> [*]
```

## Consent Granularity

| Consent Type | Required | Withdrawable | Purpose |
|--------------|----------|--------------|---------|
| **Essential Services** | ✅ Yes | ❌ No | Core platform functionality |
| **Audio Analysis** | ✅ Yes | ✅ Yes | Speech therapy features |
| **Therapist Sharing** | ❌ No | ✅ Yes | Professional oversight |
| **Analytics & Improvement** | ❌ No | ✅ Yes | Platform optimization |
| **Research Participation** | ❌ No | ✅ Yes | Clinical studies |
| **Marketing Communications** | ❌ No | ✅ Yes | Updates and newsletters |

## Data Subject Rights Implementation

### 1. Right to Access

```mermaid
sequenceDiagram
    participant User
    participant Portal as GDPR Portal
    participant API as Privacy API
    participant Collector as Data Collector
    participant DB as Databases
    participant Blob as Blob Storage

    User->>Portal: Request data export
    Portal->>API: POST /dsr/export
    API->>API: Verify identity (MFA)
    API->>Collector: Initiate collection job

    Collector->>DB: Query all user data
    Collector->>Blob: Retrieve audio files
    Collector->>Collector: Compile JSON + metadata
    Collector->>Blob: Upload export package
    Collector->>API: Export ready

    API->>User: Email with secure download link
    User->>Blob: Download (SAS token)

    Note over API,User: Link expires in 7 days<br/>Max 3 downloads
```

**Implementation**: `/api/v1/privacy/dsr/export`
- **Response Time**: Within 48 hours
- **Format**: Machine-readable JSON + CSV
- **Delivery**: Secure download link via email
- **Retention**: Export package deleted after 30 days

### 2. Right to Erasure

```mermaid
sequenceDiagram
    participant User
    participant Portal as GDPR Portal
    participant API as Privacy API
    participant Orchestrator as Delete Orchestrator
    participant Services as All Services
    participant DB as Databases
    participant Blob as Storage
    participant Queue as Event Bus

    User->>Portal: Request account deletion
    Portal->>API: POST /dsr/delete
    API->>API: Verify identity (MFA + confirm email)
    API->>Orchestrator: Initiate deletion workflow

    Orchestrator->>Queue: Publish UserDeletionRequested event

    par Parallel Deletion
        Queue->>Services: AudioService deletes audio files
        Queue->>Services: UserService soft-deletes account
        Queue->>Services: AnalysisService anonymizes results
        Queue->>Services: NotificationService removes tokens
    end

    Services->>DB: Mark records as deleted
    Services->>Blob: Delete personal files
    Services->>Orchestrator: Confirm deletion

    Orchestrator->>User: Deletion confirmation email

    Note over Orchestrator,User: 30-day grace period<br/>Account can be restored

    Orchestrator->>Orchestrator: Wait 30 days
    Orchestrator->>Services: Hard delete (irreversible)
    Services->>DB: Permanently purge data
    Services->>Blob: Permanently purge files
```

**Implementation**: `/api/v1/privacy/dsr/delete`
- **Grace Period**: 30 days (soft delete)
- **Final Deletion**: Day 31 (hard delete, irreversible)
- **Backup Purge**: Within 90 days
- **Exceptions**: Legal hold, active disputes

### 3. Right to Rectification

**Implementation**: User profile update APIs
- Users can update personal information directly
- Parents can update child information
- Changes audited and logged
- Real-time propagation to all services

### 4. Right to Data Portability

**Export Format**:
```json
{
  "export_date": "2024-12-08T10:30:00Z",
  "user": {
    "id": "usr_abc123",
    "email": "parent@example.com",
    "name": "John Doe",
    "registered": "2023-06-15T09:00:00Z"
  },
  "children": [
    {
      "id": "child_xyz789",
      "name": "Jane Doe",
      "dob": "2018-03-20",
      "recordings": 145,
      "analysis_history": "..."
    }
  ],
  "audio_files": [
    {
      "id": "audio_001",
      "recorded": "2024-12-01T14:30:00Z",
      "download_url": "https://...",
      "duration_seconds": 30,
      "transcription": "Hello world..."
    }
  ]
}
```

### 5. Right to Restrict Processing

- Users can pause all processing while keeping data
- Status: `account.processing_restricted = true`
- Restrictions:
  - No new audio analysis
  - No notifications sent
  - No data in analytics
  - No therapist access
- Data remains encrypted and stored
- Restrictions can be lifted anytime

### 6. Right to Object

Users can object to:
- Marketing communications (opt-out anytime)
- Profiling and automated decisions (manual review available)
- Research participation (opt-out anytime)
- Analytics tracking (anonymized aggregates only)

## Security Measures

### Encryption Standards

| Layer | Method | Key Management |
|-------|--------|----------------|
| **In Transit** | TLS 1.3 | Certificate rotation (90 days) |
| **At Rest (DB)** | AES-256-GCM | Azure Key Vault HSM |
| **At Rest (Blob)** | AES-256-GCM | Customer-managed keys |
| **Field-Level** | AES-256-GCM | Per-child encryption keys |
| **Backups** | AES-256-GCM | Separate backup keys |

### Access Controls

| Role | Access Level | Data Scope | MFA Required |
|------|--------------|------------|--------------|
| **Parent** | Read/Write | Own children only | Recommended |
| **Therapist** | Read/Comment | Assigned children | Required |
| **Admin** | Limited admin | Aggregates only | Required |
| **Support** | Read-only | Ticketed cases only | Required |
| **Developer** | No production access | Anonymized dev data | N/A |

### Audit Logging

All access to personal data is logged:

```json
{
  "timestamp": "2024-12-08T10:30:00Z",
  "user_id": "usr_abc123",
  "action": "data_access",
  "resource_type": "child_profile",
  "resource_id": "child_xyz789",
  "ip_address": "203.0.113.42",
  "user_agent": "HearLoveen/iOS 1.5.0",
  "result": "success",
  "accessed_fields": ["name", "recordings", "latest_score"]
}
```

**Retention**: Audit logs retained for 2 years (non-PII only)

## Data Residency & Transfers

### Regional Deployment

| Region | Services | Data Types | Status |
|--------|----------|------------|--------|
| **EU West (Primary)** | All services | All personal data | ✅ Active |
| **EU North (DR)** | Replicas | Encrypted backups | ✅ Standby |
| **US East** | CDN only | Static assets (no PII) | ✅ Active |

### Cross-Border Transfers

- **Prohibited**: Personal data never leaves EU
- **Exception**: User explicitly requests (e.g., moving countries)
- **Mechanism**: Standard Contractual Clauses (SCCs)
- **Assessment**: Transfer Impact Assessment (TIA) required

## Breach Response Plan

```mermaid
graph TB
    A[Breach Detected] --> B{Severity Assessment}

    B -->|High| C[Immediate Containment]
    B -->|Medium| D[Investigation]
    B -->|Low| E[Document & Monitor]

    C --> F[Notify DPA within 72h]
    C --> G[Notify Affected Users]

    D --> H{Personal Data Exposed?}
    H -->|Yes| F
    H -->|No| E

    F --> I[Remediation Actions]
    G --> I

    I --> J[Post-Incident Review]
    J --> K[Update Security Measures]

    E --> L[Regular Security Audit]
```

### Breach Notification Timeline

| Time | Action | Responsible |
|------|--------|-------------|
| **T+0** | Detect and contain breach | Security Team |
| **T+2h** | Assess scope and severity | Security + Legal |
| **T+24h** | Internal incident report | CISO |
| **T+72h** | Notify supervisory authority (if required) | DPO |
| **T+7d** | Notify affected users (if high risk) | Communications |
| **T+30d** | Post-incident review and remediation | All teams |

## GDPR Compliance Checklist

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| **Lawful Basis** | Explicit consent + legitimate interest | ✅ |
| **Transparency** | Clear privacy policy + data usage notices | ✅ |
| **Data Minimization** | Only collect necessary data | ✅ |
| **Accuracy** | User-updateable profiles | ✅ |
| **Storage Limitation** | Automated retention policies | ✅ |
| **Integrity & Confidentiality** | Encryption + access controls | ✅ |
| **Accountability** | DPO appointed + documentation | ✅ |
| **Data Protection by Design** | Privacy-first architecture | ✅ |
| **DPO Appointment** | External DPO retained | ✅ |
| **DPIA Conducted** | For high-risk processing | ✅ |
| **Records of Processing** | Maintained and current | ✅ |
| **Third-Party Processors** | Data Processing Agreements (DPAs) | ✅ |

## Related Documentation

- [Privacy Architecture](../technical/architecture/PRIVACY.md) - Technical privacy implementation
- [Security Implementation](../security/SECURITY_IMPLEMENTATION.md) - Security controls
- [Compliance Overview](../technical/architecture/COMPLIANCE.md) - Regulatory compliance
- [GDPR Portal](../product/gdpr-portal.md) - User-facing privacy tools
- [Data Processing Agreement](../../legal/DPA.md) - Third-party processor terms

## Data Protection Officer (DPO)

- **Name**: [DPO Name/Company]
- **Email**: dpo@hearloveen.com
- **Address**: [EU Address]
- **Supervisory Authority**: [National DPA]
- **Registration**: [DPA Registration Number]

## Contact

For privacy inquiries:
- **Email**: privacy@hearloveen.com
- **GDPR Portal**: https://hearloveen.com/privacy
- **Support**: Via in-app chat or support@hearloveen.com
