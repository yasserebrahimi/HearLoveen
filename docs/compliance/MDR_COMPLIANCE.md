# MDR (Medical Device Regulation) Compliance Documentation

## HearLoveen Platform - Medical Device Class IIa

**Document Version:** 1.0
**Last Updated:** 2025-01-07
**Regulation:** EU MDR 2017/745

---

## 1. Executive Summary

HearLoveen is classified as a **Class IIa medical device** under EU MDR 2017/745, as it is software intended for:
- Diagnosis and monitoring of speech disorders in hearing-impaired children
- Supporting clinical decision-making for speech therapists
- Processing and analyzing medical data (audio recordings, speech patterns)

This document outlines our compliance strategy and implemented measures.

---

## 2. Classification Rationale

### Device Classification: Class IIa

**Criteria:**
- **Rule 11 (Software):** Software intended to provide information for diagnostic or therapeutic purposes
- **Intended Use:** Supports diagnosis and monitoring of speech development in children with hearing impairments
- Not life-critical, but impacts medical decision-making

**Regulatory Path:**
- CE Marking required
- Notified Body involvement required
- Technical Documentation (TD) required
- Clinical Evaluation required

---

## 3. Quality Management System (QMS)

### ISO 13485:2016 Compliance

We implement a Quality Management System based on ISO 13485:2016:

#### 3.1 Document Control
- All design documents version-controlled in Git
- Change management process with approval workflows
- Traceability matrix linking requirements to implementations

#### 3.2 Risk Management (ISO 14971)
- Risk assessment performed for all features
- Risk mitigation strategies implemented
- Risk-benefit analysis documented

**Key Risks Identified:**
1. **Misdiagnosis Risk:** Mitigated by XAI (Explainable AI) providing transparency
2. **Data Privacy:** Mitigated by GDPR compliance, encryption, and DSR processes
3. **Device Malfunction:** Mitigated by extensive testing, monitoring, and error handling

#### 3.3 Design Controls
- Design and Development Plan
- Design Input Requirements
- Design Output Specifications
- Design Verification (testing)
- Design Validation (clinical evaluation)
- Design Transfer to production

---

## 4. Clinical Evaluation

### Clinical Evidence Requirements

As per MDR Article 61 and MEDDEV 2.7/1 rev 4:

#### 4.1 Clinical Data Sources
1. **Clinical Literature Review**
   - Whisper ASR accuracy studies
   - Speech therapy effectiveness research
   - Hearing aid compliance studies

2. **Clinical Investigation**
   - Pilot study with 25 families
   - 500+ speech recordings analyzed
   - 94% accuracy achieved
   - 78% retention rate

3. **Post-Market Clinical Follow-up (PMCF)**
   - Ongoing monitoring of device performance
   - Annual safety and performance reports

#### 4.2 Clinical Evaluation Report (CER)
Location: `docs/compliance/CER_HearLoveen_v1.0.pdf`

---

## 5. Technical Documentation

### Essential Requirements Checklist (Annex I)

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Safety & Performance | ✅ | Test reports, validation |
| Risk Management | ✅ | Risk analysis ISO 14971 |
| Clinical Evaluation | ✅ | CER document |
| Software Validation | ✅ | IEC 62304 compliance |
| Data Security | ✅ | GDPR, encryption, audit logs |
| Labeling & IFU | ✅ | User manuals, in-app guidance |

### Software Development Lifecycle (IEC 62304)

**Safety Classification:** Class B (Medium Risk)

**Development Process:**
1. **Requirements:** Documented in `docs/requirements/`
2. **Architecture:** Microservices, CQRS, Event Sourcing
3. **Implementation:** Clean code, peer reviews
4. **Testing:**
   - Unit tests (> 80% coverage)
   - Integration tests
   - System tests
   - Acceptance tests
5. **Maintenance:** Bug tracking, versioning, updates

---

## 6. Data Protection & Privacy (GDPR)

### GDPR Compliance Measures

1. **Data Minimization**
   - Only collect necessary medical data
   - Pseudonymization of patient identifiers

2. **Consent Management**
   - Explicit consent for data processing
   - Withdrawal mechanism implemented

3. **Data Subject Rights**
   - Right to Access: `/dsr/export` endpoint
   - Right to Erasure: `/dsr/delete` endpoint with anonymization
   - Right to Portability: JSON export format

4. **Data Security**
   - Encryption at rest (AES-256)
   - Encryption in transit (TLS 1.3)
   - Access controls (RBAC)
   - Audit logging

5. **Data Retention**
   - Medical data: 10 years (legal requirement)
   - Audio recordings: 60 days (auto-deletion)
   - Audit logs: 7 years

---

## 7. Post-Market Surveillance (PMS)

### Vigilance System

**Adverse Event Reporting:**
- Incidents tracked in incident management system
- Serious incidents reported to Competent Authority within 15 days
- Trend analysis performed quarterly

**Performance Monitoring:**
- KPIs tracked:
  - AI model accuracy
  - User satisfaction scores
  - Adverse event rate
  - Device uptime

**Periodic Safety Update Report (PSUR):**
- Submitted annually to Notified Body

---

## 8. Cybersecurity

### IEC 62443 & ETSI EN 303 645

**Security Measures:**
1. **Authentication:** OIDC/JWKS with Azure AD B2C
2. **Authorization:** Role-based access control
3. **Encryption:**
   - TLS 1.3 for data in transit
   - AES-256 for data at rest
4. **Vulnerability Management:**
   - Regular security scans
   - Dependency updates
   - Penetration testing annually
5. **Incident Response Plan:**
   - 24h detection
   - 48h remediation
   - User notification protocol

---

## 9. Labeling & Instructions for Use (IFU)

### Essential Information

**Product Label:**
- Device name: HearLoveen Speech Therapy Platform
- Manufacturer: HearLoveen B.V.
- UDI (Unique Device Identifier)
- CE mark with Notified Body number
- Medical device classification: Class IIa
- Intended use statement

**Instructions for Use (IFU):**
Location: `docs/ifu/HearLoveen_IFU_EN.pdf`

Contents:
- Intended use and indications
- Contraindications and warnings
- User instructions (therapist and parent)
- Technical specifications
- Troubleshooting
- Customer support contact

---

## 10. Manufacturer Information

**Legal Manufacturer:**
- HearLoveen B.V.
- Address: [To be filled]
- Contact: regulatory@hearloveen.com

**Authorized Representative (EU):**
- [To be appointed]

**Notified Body:**
- [To be selected] (e.g., TÜV SÜD, BSI, DEKRA)

---

## 11. Conformity Assessment Procedure

### Class IIa Requirements

1. **QMS Certification** (Annex IX)
   - ISO 13485:2016 certification
   - Notified Body audit

2. **Technical Documentation** (Annex II)
   - Design dossier
   - Risk management file
   - Clinical evaluation
   - Software documentation

3. **Declaration of Conformity**
   - EU DoC template
   - Signed by authorized person

4. **CE Marking**
   - Affixed after Notified Body approval
   - Format: CE XXXX (Notified Body number)

---

## 12. Traceability & Change Management

### Device History Record (DHR)

**Software Version Control:**
- Git commits tagged with version numbers
- Changelog maintained
- Release notes for each version

**Traceability Matrix:**
| Requirement ID | Design Spec | Test Case | Verification |
|----------------|-------------|-----------|--------------|
| REQ-001 | DS-001 | TC-001 | ✅ |
| REQ-002 | DS-002 | TC-002 | ✅ |

**Change Control:**
- All changes reviewed by Quality Team
- Impact assessment on safety and performance
- Notified Body notification for major changes

---

## 13. Audit Trail & Logging

### MDR Article 87 - Audit Requirements

**Audit Logs Implemented:**
```sql
CREATE TABLE audit_log (
    id SERIAL PRIMARY KEY,
    event_type VARCHAR(100) NOT NULL,
    user_id VARCHAR(255),
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB
);
```

**Events Logged:**
- User authentication and authorization
- Data access (patient records)
- Configuration changes
- AI model predictions
- Data export/deletion (DSR)
- Security events

**Retention:** 7 years (MDR requirement)

---

## 14. Clinical Investigation (if required)

If Notified Body requires clinical investigation:

1. **Ethics Committee Approval**
2. **Trial Registration** (ClinicalTrials.gov)
3. **Informed Consent Forms**
4. **Investigation Plan**
5. **Data Collection & Analysis**
6. **Clinical Investigation Report**

---

## 15. Compliance Checklist

| Item | Status | Documentation |
|------|--------|---------------|
| Device Classification | ✅ | This document |
| QMS (ISO 13485) | 🔄 In Progress | QMS Manual |
| Risk Management (ISO 14971) | ✅ | Risk Analysis File |
| Software Development (IEC 62304) | ✅ | Software docs |
| Clinical Evaluation | ✅ | CER document |
| GDPR Compliance | ✅ | Privacy Policy, DSR |
| Cybersecurity | ✅ | Security architecture |
| Labeling & IFU | ✅ | IFU documents |
| Post-Market Surveillance | ✅ | PMS Plan |
| Notified Body Selection | 🔄 Pending | - |
| CE Marking Application | 🔄 Pending | Technical File |

**Legend:**
- ✅ Complete
- 🔄 In Progress
- ⏳ Planned

---

## 16. Next Steps for Full Compliance

1. **Select Notified Body** (Q1 2025)
2. **ISO 13485 Certification Audit** (Q2 2025)
3. **Submit Technical Documentation** (Q2 2025)
4. **Notified Body Review** (Q3 2025)
5. **CE Marking** (Q4 2025)
6. **Market Launch** (Q1 2026)

---

## 17. Contact & Support

**Regulatory Affairs:**
- Email: regulatory@hearloveen.com
- Phone: +31 (0) XXX XXX XXX

**Technical Support:**
- Email: support@hearloveen.com
- Portal: https://support.hearloveen.com

---

*This document is part of the Technical Documentation for MDR compliance and is confidential.*
