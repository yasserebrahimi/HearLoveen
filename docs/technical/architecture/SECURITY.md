# SECURITY.md

## Principles
- Defense-in-depth: WAF/App Gateway → API (.NET 8) → Service Bus → AI Workers → PostgreSQL/Blob.
- Secrets in Key Vault (CMK/BYOK for Blob & DB). TLS 1.2+ everywhere.
- Role-based access control (Parent / Therapist / Admin).

## Hardening Checklist
- [ ] HTTPS only; HSTS; secure cookies; proper CORS/CSRF.
- [ ] Input validation (FluentValidation), output encoding, rate limiting.
- [ ] Dependency scanning (SCA) + CodeQL (SAST). Container image scan pre‑deploy.
- [ ] Logging without PII; correlation IDs; audit trail of data actions.
- [ ] Backup/restore tested; geo‑redundant storage; KMS key rotation.
