# HearLoveen — Technical Architecture

This document summarizes the end‑to‑end system. All diagrams live under `docs/diagrams` and use Mermaid for maximum portability.

## 1. Context & Containers
- See **System Context**: `docs/diagrams/01-system-context.md`
- See **Containers / Deployment**: `docs/diagrams/02-containers.md`

## 2. Backend & Data Flow
- **Backend Components (Clean Architecture + CQRS)**: `docs/diagrams/03-backend-components.md`
- **Audio → Feedback Sequence**: `docs/diagrams/04-sequence-audio-to-feedback.md`

## 3. Data & Privacy
- **GDPR-first Data Lifecycle**: `docs/diagrams/05-gdpr-lifecycle.md`
- **Core ERD (fallback-safe)**: `docs/diagrams/06-erd-core.md`

## 4. DevOps
- **CI/CD Pipeline**: `docs/diagrams/07-cicd.md`

## 5. Security & RBAC
- **RBAC at a Glance**: `docs/diagrams/08-rbac.md`

## 6. UX & AI/ML
- **Parent Journey** (fallback-safe): `docs/diagrams/09-journey-parent.md`
- **AI/ML Pipeline**: `docs/diagrams/10-ml-pipeline.md`
- **IoT / Hearing Device**: `docs/diagrams/11-iot-edge.md`

## 7. Roadmap
- **Timeline (fallback-safe)**: `docs/diagrams/12-roadmap.md`

---

### Notes
- Prefer EU deployments (EU Data Boundary). Keys: Azure Key Vault. Storage: Private Blob with SAS.
- Avoid speaker-identification and minimize raw audio retention.
- Adhere to parental consent flows and DSR automation (export/erasure).


## Identity & RBAC
- Azure AD B2C (OIDC) with JWT Bearer; role claims: Parent / Therapist / Admin.
- Minimal APIs are protected via policy: `.RequireAuthorization("Parent")` etc.

## AI/ML Pipeline (Production)
- ONNX phoneme-CTC ASR with greedy decode + boundary-based forced alignment.
- SER classifier (ONNX). Composite scoring with phoneme confidences + emotion.
- Worker persists `FeedbackReport` to Postgres.

## Observability & SLO
- OpenTelemetry in API; Prometheus metrics in Worker (`/metrics`).
- Grafana dashboards under `deploy/observability/grafana/dashboards/`.
- SLOs defined in `docs/sre/slo.yaml`.

## Blue/Green on AKS
- NGINX Ingress canary via `templates/ingress-canary.yaml` and `values.canary.weight`.


## XPRO3 Additions
- **Per-child Lexicon** (DB-backed `child_lexicon` table) + **Teacher-forced decoding** via Viterbi over CTC logits.
- **G2P stub** to derive phonemes from words; replaceable by production G2P.
- **EF Global Query Filters**: Therapist scope enforced across queryables.
- **Alerting**: Prometheus multi-window burn-rate rules; Alertmanager routes → Slack/PagerDuty.


## XPRO4 Additions
- **Real G2P adapters**: Python `g2p_en`; optional Phonetisaurus/Sequitur via subprocess (env: `G2P_BACKEND`, `G2P_MODEL`).
- **Per-child G2P cache**: Postgres table `child_g2p_cache` with upsert.
- **Per-child Curriculum**: domain entity + CQRS endpoints (`/api/v1/curriculum/*`) + worker feedback loop auto-updates.
- **Tests**: EF global query filters and resource-based AuthZ (TherapistAssigned).

## XPRO5 Additions (2025-11-03T23:16:04.027375Z)
- **Feature Flags** (`IFeatureFlags`, appsettings `Features:*`), **Argo Rollouts** manifests with analysis guardrails.
- **Drift Monitoring**: KL divergence gauge `worker_phoneme_kl` + EMA baseline in Postgres.
- **Curriculum Graph**: `PhonemeRating` (Elo), `PhonemePrerequisite`, and `GetNextPromptElo` query (endpoint updated).
- **Security Hardening**: OPA policy (`policies/therapist.rego`), Postgres RLS (`deploy/db/rls.sql`), AES-GCM crypto service.
- **E2E Tests**: WebApplicationFactory scaffold + seed helper.
- **Performance**: k6 smoke script.
- **Dashboards & Alerts**: Curriculum progress dashboard, streak-drop & pattern-drift alerts.

## XPRO6 Additions (2025-11-03T23:19:05.458257Z)
- OpenFeature/LaunchDarkly adapter with `Variant()`.
- A/B infra (Bucketer + CUPED) + docs.
- Multilingual G2P (fa/de stubs) with `G2P_LANG` env; routed via `multilingual_g2p()`.
- Testcontainers integration scaffold, seeding hook (`DbSeeder.SeedAsync`).
- KEDA ScaledObject for Service Bus queue.
- OpenCost cost dashboard per-namespace.
- Pact contract tests (skeleton).
- ArgoCD GitOps manifests + docs, flagd sample config.


## XPRO7 Additions (2025-11-03T23:23:12.207597Z)
- **Privacy-Preserving ML**: Federated DP-SGD skeleton, Confidential Compute notes + attestation stub.
- **De-ident & Governance**: Great Expectations config, Data Contracts (JSON-schema) + OpenLineage sample.
- **Clinical Metrics**: Prosody metrics module; Outcome toolkit (effect size, power).
- **Signal/Edge**: AEC/NS/VAD pipeline stub; Edge MFCC (WASM-ready), BLE gateway notes.
- **Advanced Experimentation**: Bandit microservice; Sequential testing guide; Counterfactual log schema.
- **SRE/FinOps**: DR Runbook; Karpenter provisioner; Cost-per-Outcome Grafana dashboard.
- **Security/Supply Chain**: Supply-chain workflow (SBOM, Cosign), Gatekeeper policy, Hash-chain audit logger.
- **Developer Experience**: Backstage catalog, Property-based test skeleton, DVC/MLflow scaffolding.
- **Product/Growth**: Content CMS microservice skeleton; GDPR portal; LLM Coach guardrails.
- **Interop/Analytics**: FHIR mapping doc; PostHog deployment notes; Lakehouse medallion README.
