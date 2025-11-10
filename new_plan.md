# CTIS Enhancement Implementation Plan (Derived from New Set of Requirements)

Legend: 
- Status: TODO | IN-PROGRESS | DONE | BLOCKED
- Priority: P0 (Critical), P1 (High), P2 (Medium)
- Est: dev-days (focused)

## 0. Global Prereqs / Infra
- [x] (P0, 0.5d) Add missing database indexes (Payments, TaxFilings, Documents, Chat) before heavy KPI/report queries
- [x] (P0, 0.5d) Re-enable & configure Quartz or Hangfire for background jobs (KPI snapshots, compliance history)
- [ ] (P1, 0.5d) Introduce feature flags/config for staged rollout of new metrics

## 1. Payment Methods Integration
- [DONE] (P0, 0.5d) Extend Payment entity & add migration for external gateway fields
- [DONE] (P0, 0.25d) Basic webhook signature (HMAC) + ISO 20022 validation scaffold
- [x] (P0, 0.5d) Implement full XSD loading & strict validation (embed schemas)
- [x] (P0, 0.5d) Robust idempotency store (hash of payload + transaction) for webhooks
- [x] (P0, 0.5d) Background polling job for pending external payments
- [ ] (P0, 0.5d) Implement Diaspora gateways (Stripe/PayPal) initiation + callback handlers
- [ ] (P0, 0.5d) Extend `Payment` entity: Gateway, ExternalReference, WebhookStatus, Metadata (JSON)
- [ ] (P0, 0.5d) Secure webhook endpoints (HMAC signature validation + idempotency)
- [ ] (P0, 0.5d) Client payment initiation endpoint (`POST /api/payments/initiate`) issuing session/token
- [ ] (P0, 0.5d) Frontend payment initiation UI (gateway selection + status polling)
- [ ] (P1, 0.5d) Payment retry & reconciliation job
- [ ] (P1, 0.5d) Currency normalization & FX rate caching

#### 1.a Detailed Implementation Plan

- API endpoints
  - POST `/api/payments/initiate`
    - Request: `ClientId`, `Amount`, `Currency`, `Gateway`, `ReturnUrl`, `CancelUrl`, `Metadata`
    - Response: `PaymentId`, `Gateway`, `SessionToken`, `ExternalSessionUrl`, `ExpiresAt`
    - Semantics: 201 Created; supports `Idempotency-Key` header; validates amount/currency against client.
  - GET `/api/payments/{paymentId}/status`
    - Used for UI polling; returns normalized `Status` (Pending, Authorized, Succeeded, Failed, Canceled), `ExternalReference`, `LastUpdatedAt`.
  - POST `/api/payments/webhook/{gateway}`
    - Headers: `X-Signature`, `X-Timestamp`, `X-Event`
    - Verify HMAC over raw body + timestamp, enforce clock-skew window, persist raw payload, and apply exactly-once semantics via idempotency store.

- Entity updates
  - `Payment`: `Gateway`, `ExternalReference`, `ExternalSessionId`, `WebhookStatus`, `Metadata` (JSON), `IdempotencyKey`, `WebhookReceivedAt`, `LastReconciledAt`
  - Enums: `PaymentStatus` mapped per provider; `WebhookStatus`: None, Received, Processed, Duplicate, InvalidSignature
  - Indexes: `(ExternalReference)`, `(Gateway, ExternalSessionId)`, `(Status)`, `(CreatedAt)`

- Background jobs
  - Pending payment poller: every 2 min; gateways with polling; max 50 per run; exponential backoff with jitter
  - Reconciliation: nightly; re-validate last 7 days; repair drift; emit audit events

- Security and resilience
  - HMAC validation; replay protection using `X-Timestamp` window ≤5m and nonce cache
  - Rate limit webhook endpoints; acknowledge 2xx only after durable write; redact PII in logs
  - Feature flags to enable per gateway (Local, Orange, Africell, Stripe, PayPal)

- Frontend flow
  - Payment modal: gateway selection → initiate → redirect/popup per gateway; visible spinner; fallback path
  - Status polling: GET `/api/payments/{id}/status` every 2–3s until terminal
  - Error handling: friendly messages; retry; link to support

- Test plan
  - Unit: signature verifier, idempotency store, status mapper
  - Integration: initiate + webhook happy path; tampered signature; replay
  - E2E: UI flow for Local and Orange in stage; Stripe sandbox for diaspora

- Rollout
  - Stage: Local gateway default; feature-flag Stripe/PayPal off
  - Canary: enable Stripe for 5% internal users; monitor metrics; then expand

- Acceptance criteria
  - Initiate endpoint creates a Payment and returns a session within 200ms p95
  - Valid webhooks update status exactly-once; duplicates ignored
  - UI shows real-time status and final receipt
  - Observability: logs + metrics for initiations, callbacks, failures

## 2. KPI Computations (Internal & Client)
- [x] (P0, 0.5d) Replace placeholder filing timeliness calculation with actual diff (DueDate vs SubmittedAt)
- [x] (P0, 0.5d) Implement payment completion rate (Approved on/before due vs total due)
- [x] (P0, 0.5d) Implement document submission compliance (% required per tax period submitted before deadline)
- [x] (P0, 0.5d) Implement client engagement metric (logins + meaningful events / active clients)
- [x] (P0, 0.5d) Implement client document readiness & breakdown (Completed/Pending/Rejected)
- [x] (P0, 0.5d) Implement on-time payment percentage per client
- [x] (P0, 0.5d) Implement filing timeliness per client (avg early days; negative = early)
  - [x] (P0, 0.5d) Add KPI trend query using actual persisted snapshots
  - [x] (P1, 0.5d) Introduce KPI snapshot table & scheduled persistence job
  - [x] (P1, 0.5d) KPI alert auto-generation & notification broadcast
  - [x] (P2, 0.5d) Performance tuning & cached aggregated windows (monthly/quarterly)

 #### 2.a Detailed Metrics Spec & Snapshotting
 
 - Metrics catalog (definitions)
   - Filing timeliness (global): avg of `(SubmittedAt - DueDate)` in days; negative = early.
   - On-time payment %: `count(Succeeded before DueDate) / count(Total Payments Due)` per period.
   - Payment completion rate: `count(Succeeded) / count(Total Initiated)`.
   - Document submission compliance: `sum(RequiredSubmittedBeforeDeadline) / sum(RequiredDocs)`.
   - Client engagement: `(logins + meaningfulEvents) / activeClients`.
   - Document readiness breakdown: counts per status (Completed, Pending, Rejected).
 
 - Data sources
   - Tables: `TaxFilings`, `Payments`, `Documents`, `AuditEvents` (login, upload, approve), `Clients`.
   - Filters: active clients only; respect RBAC scoping.
 
 - Snapshotting
   - Table: `KpiSnapshots(SnapshotDate, ScopeType [Global|Client], ScopeId, PeriodStart, PeriodEnd, MetricsJson, CreatedAt)`
   - Indexes: `(ScopeType, ScopeId, SnapshotDate DESC)`, `(PeriodStart, PeriodEnd)`.
   - Job: daily at 01:00 local; recompute last 7 days; idempotent lock key `kpi:daily:<date>`.
 
 - Endpoints
   - GET `/api/kpi/overview?range=30d&aggregate=monthly`
   - GET `/api/kpi/client/{clientId}/trend?metric=filingTimeliness&range=180d`
   - Both served from snapshots first; live compute fallback with `Cache-Control: max-age=60`.
 
 - Alerts
   - Threshold rules (e.g., filing timeliness > +2d or on-time payment < 90%) generate alerts; batched notifications.
 
 - Performance & caching
   - Cache snapshot reads for 5m; pre-aggregate monthly/quarterly windows; include ETag for client caching.
 
 - Acceptance criteria
   - Snapshot job finishes < 2 min and is idempotent.
   - Overview endpoint p95 < 400ms for 30d window.
   - Metrics match manual SQL spot-checks within 0.1%.
  
  ## 3. Reports (PDF & Excel) 
  - [x] (P0, 1d) Create new report DTOs (Document Submission, Tax Calendar, Client Compliance Overview, Revenue, Case Management, Enhanced Activity)
  - [x] (P0, 1d) Extend `IReportTemplateService` with new report data methods
  - [x] (P0, 1d) Implement report data retrieval in `ReportTemplateService`
  - [x] (P0, 1d) Extend `IReportService` and `ReportService` with new report generation methods
  - [x] (P0, 0.5d) Create `CaseIssue` model and related entities for case management reporting
  - [x] (P0, 0.5d) Add regression tests for existing 3 client reports
  - [x] (P1, 0.5d) Resume & configure `ReportService` + Quartz scheduling
  - [x] (P1, 0.5d) Add CSV toggle to UI for all report types (fallback)
  - [x] (P2, 0.5d) Report rate limiting & expiry cleanup job

 #### 3.a Report Generation Templates & Scheduling
 
 - Templates & data
   - Use `IReportTemplateService` to assemble DTOs; templates versioned per report type (PDF/Excel/CSV).
   - Report types: `DocumentSubmission`, `TaxCalendar`, `ClientComplianceOverview`, `Revenue`, `CaseManagement`, `EnhancedActivity`.
 
 - Endpoints
   - POST `/api/reports/generate` with `{ type, parameters, format }` → returns `ReportId`.
   - GET `/api/reports/{reportId}` → metadata; GET `/api/reports/{reportId}/download` → file.
   - Access controlled via RBAC; pre-signed URL for download with short TTL.
 
 - Scheduling & retention
   - Quartz jobs per type; concurrency limit 3; queue excess; per-user rate limiting.
   - Retain generated files for 7 days; cleanup job runs daily; metadata kept for 30 days.
 
 - Observability & performance
   - Log generation time, size, and template version; emit metrics per type and failures.
   - p95 generation time targets: PDF ≤ 3s, Excel ≤ 2s, CSV ≤ 1s (for median-sized datasets).
 
 - Test plan
   - Unit: DTO builders; template rendering for edge cases.
   - Integration: end-to-end generate + download; permission checks; large dataset pagination.
 
 - Acceptance criteria
   - Reports generate within targets and are retrievable via secured link.
   - Scheduling executes on cron; stale files are purged automatically.
   - CSV fallback available for every report type.
   
   ## 4. Compliance Tab & Metrics ✅ COMPLETED
- [x] (P0, 0.5d) Create `IComplianceService` aggregating filings, payments, documents, deadlines
- [x] (P0, 0.5d) Status Summary endpoint `/api/compliance/client/{id}/summary`
- [x] (P0, 0.75d) Filing Checklist endpoint `/api/compliance/client/{id}/checklist` per tax type
- [x] (P0, 0.5d) Upcoming Deadlines endpoint (real query)
- [x] (P0, 0.75d) Penalty warnings calculation service + endpoint
- [x] (P0, 0.5d) Document Tracker endpoint (detailed statuses + counts)
- [x] (P0, 0.5d) Deadline Adherence History (compute from filings; monthly aggregate)
- [x] (P0, 0.25d) Integrate compliance metrics tiles with real endpoints
- [x] (P1, 0.5d) Compliance snapshot persistence (daily)
- [x] (P1, 0.5d) Penalty simulation (what-if) endpoint
- [x] (P2, 0.5d) Caching & stale indicator for heavy compliance queries

## 5. Chat System Enhancements ✅ COMPLETED
- [x] (P0, 0.5d) Extend schema: ChatRoom (AssignedToUserId), ChatMessage (IsInternal, EditedAt, DeletedAt)
- [x] (P0, 0.5d) Add migrations & update DbContext config
- [x] (P0, 0.75d) Implement `ChatHub` (SignalR) with auth & groups
- [x] (P0, 0.75d) Implement `ChatController` (history pagination, assignment, internal note creation)
- [x] (P0, 0.5d) Frontend real-time chat UI upgrade (assignment, internal note toggle)
- [x] (P1, 0.5d) Chat search endpoint with filters (date/user/text)
- [x] (P1, 0.5d) Audit log for edits/deletes
- [x] (P2, 0.5d) Typing indicators & presence
- [x] (P2, 0.5d) Conversation analytics (volume, response time)

## 6. Data & Schema Migrations
- [x] (P0, 0.5d) Migration: Payment extended fields
- [x] (P0, 0.5d) Migration: Chat schema extensions
- [ ] (P0, 0.5d) Migration: CaseIssue table
- [ ] (P0, 0.5d) Migration: KPI Snapshot table
- [ ] (P0, 0.5d) Migration: ComplianceHistory table
- [ ] (P0, 0.5d) Migration: PenaltyRule enhancements (if needed for formulas)

 #### 6.a Migration Plan & Scripts
 
 - Migrations to add/modify
   - New tables: `CaseIssue`, `KpiSnapshots`, `ComplianceHistory`.
   - Modified: `Payments` (Gateway, ExternalReference, ExternalSessionId, WebhookStatus, Metadata JSON, IdempotencyKey, WebhookReceivedAt, LastReconciledAt), `PenaltyRule` (formula fields if required).
   - Indexes per spec: ensure `(ExternalReference)`, `(Gateway, ExternalSessionId)`, `(Status)`, `(CreatedAt)`.
 
 - Execution
   - Create EF Core migrations: `AddCaseIssue`, `AddKpiSnapshots`, `AddComplianceHistory`, `ExtendPaymentsForGateways`, `EnhancePenaltyRule`.
   - Verify `Up`/`Down` scripts; include default values and nullability to avoid lockups.
   - Backfill: generate historical `ComplianceHistory` from `TaxFilings` and `Payments` for last 12 months; chunked batches (1k rows) with transaction per batch.
   - Idempotency: guard backfill with a migration marker table `__MigrationLog` or use checks on existing rows.
 
 - Rollout & safety
   - Run in maintenance window; take snapshot/backup; apply migrations; run backfill jobs; validate row counts and indexes.
   - Rollback: `dotnet ef database update <prev>` after restore if structural issues; otherwise disable features via flags.
 
 - Acceptance criteria
   - Migrations apply in < 60s and create expected tables, columns, and indexes.
   - Backfill completes without deadlocks; verifiable by sample queries matching reports.
   - Down scripts revert cleanly in test environment.
  
  ## 7. Background Jobs
  - [x] (P0, 0.5d) Re-enable Quartz/Hangfire base setup
  - [x] (P0, 0.5d) Job: Daily KPI snapshots
  - [x] (P0, 0.5d) Job: Daily Compliance history + penalty recalculation
  - [x] (P1, 0.5d) Job: Payment reconciliation
  - [x] (P2, 0.25d) Job: Report cleanup (expired files)

 #### 7.a Job Specs & Schedules
 
 - Scheduler
   - Quartz.NET with persistent store; single-node acquisition lock; all times in tenant-local TZ where applicable.
 
 - Jobs
   - KPI Snapshots: cron `0 0 1 * * ?` (01:00 daily). Recompute last 7 days; single instance; lock key `job:kpi-snapshot`.
   - Compliance History & Penalties: cron `0 0 2 * * ?` (02:00 daily). Incremental from last watermark.
   - Payment Poller: every 2 minutes; polls gateways that require it; processes ≤50 pending payments/run; exponential backoff with jitter for failing items.
   - Payment Reconciliation: cron `0 0 3 * * ?` (03:00 daily). Re-verify last 7 days; fix drift; add audit entries.
   - Report Cleanup: cron `0 0 4 * * ?` (04:00 daily). Delete expired files; keep metadata 30 days.
 
 - Reliability & observability
   - Retries: 3 attempts with backoff (2s, 10s, 30s). Poison messages quarantined for manual review.
   - Metrics: success/failure counts, duration, items processed, lag; logs include jobId and correlationId.
 
 - Acceptance criteria
   - All jobs visible in dashboard with last run, next run, and status.
   - Success rate ≥ 99% weekly; p95 duration within targets (snapshot < 120s; reconciliation < 180s).
   - Back-pressure prevents CPU spikes; no overlapping executions.
  
  ## 8. Testing Strategy
  - [ ] (P0, 0.5d) Add unit tests for KPI real calculators
  - [ ] (P0, 0.5d) Add integration tests for compliance endpoints
  - [ ] (P0, 0.5d) Add gateway initiation + webhook tests
  - [ ] (P0, 0.5d) Add report generation tests (new reports)
  - [ ] (P0, 0.5d) Add chat hub tests (assignment, internal note visibility)
  - [ ] (P1, 0.5d) Performance tests (KPI & compliance endpoints)
  - [ ] (P1, 0.25d) Security tests for webhook signature tampering
  - [ ] (P2, 0.25d) Load test chat concurrency

 #### 8.a Test Coverage & Scenarios
 
 - Coverage targets
   - Core services (KPI, Compliance, Payments): ≥ 75% statement coverage; critical paths 100% branch coverage.
 
 - Unit tests
   - KPI calculators (timeliness, on-time %, readiness breakdown) with boundary conditions.
   - Webhook signature verification (valid, invalid, replay, clock skew).
   - Status mapping per gateway.
 
 - Integration tests
   - Payments: initiate → webhook → status updates; idempotency on duplicate webhook.
   - Compliance endpoints: summary, checklist, deadlines with seeded data.
   - Reports: generate and download; RBAC enforcement; large dataset pagination.
 
 - Non-functional
   - Performance: p95 latency under load for KPI and compliance.
   - Security: negative tests for tampering and unauthorized access.
   - Load: chat concurrency with typing indicators and presence enabled.
 
 - CI/CD
   - PR gates run unit + integration suites in parallel; artifacts include coverage report and sample generated reports.
 
 - Acceptance criteria
   - All tests pass in CI; coverage report meets thresholds.
   - Flaky tests tracked and quarantined with retry disabled by default.
   - Test data fixtures reusable across modules.
  
  ## 9. Performance & Observability
  - [ ] (P1, 0.5d) Add query performance logging around KPI & compliance services
  - [ ] (P1, 0.25d) Add metrics (Prometheus/OpenTelemetry) hooks
  - [ ] (P2, 0.25d) Dashboard for report generation durations

 #### 9.a Observability Plan & SLOs
 
 - Logging
   - Structured logging (Serilog) with `CorrelationId` and `UserId` where present; avoid PII in logs by default.
   - Log levels: Info for business milestones, Debug for diagnostics; Error with exception + minimal context.
   - Chat messages: content logging disabled; only metadata (roomId, lengths) with sampling for volume metrics.
 
 - Metrics (OpenTelemetry/Prometheus)
   - API: request duration histogram (by route/method), error rate, throughput.
   - DB: query duration, rows scanned, cache hit ratio; tagged by service (KPI, Compliance).
   - Jobs: run duration, items processed, failures; per job type.
   - Payments: initiations, webhooks received/validated/duplicates, status transitions.
   - Reports: generation time, file size, failure count; by report type/format.
 
 - Tracing
   - Enable OpenTelemetry tracing with W3C TraceContext; propagate across API → DB → jobs; export to Jaeger/OTLP.
 
 - Dashboards & SLOs
   - API SLOs: p95 GET < 300ms; p95 heavy analytics < 800ms; error rate < 1% rolling 5m.
   - Jobs SLOs: KPI snapshot < 120s p95; reconciliation < 180s p95; success ≥ 99% weekly.
   - Payment flow: webhook validation success ≥ 99.9%; duplicate rate monitored.
 
 - Alerts
   - Page on error rate > 2% 5m, job failures > 3 consecutive, webhook invalid signatures spike.
   - Budget burn alerts for SLOs (e.g., p95 latency breach for 30m).
 
 - Acceptance criteria
   - Dashboards published for API, DB, Jobs, Payments, Reports with above metrics.
   - SLOs encoded and alerting active; trial alert verified in stage.
   - Correlated traces visible for a sample payment flow.

  ## 10. Documentation & DevEx
  - [ ] (P1, 0.5d) Update README with new endpoints & gateway flows
  - [ ] (P1, 0.25d) ADR for payment gateway architecture
  - [ ] (P2, 0.25d) Developer guide for compliance calculations

 #### 10.a Documentation & Developer Experience Plan
 
 - README updates
   - Quickstart with `dotnet` and local DB; environment variables table.
   - Payment gateway flows with diagrams; cURL examples for initiate, status, webhook verification sample.
   - KPI endpoints usage with query params and examples.
 
 - ADRs
   - Payment gateway abstraction + idempotency store decisions.
   - KPI snapshotting vs live compute tradeoffs.
   - Notifications architecture and provider abstraction.
 
 - Developer guides
   - Compliance calculations reference with formulas and SQL examples.
   - Adding a new payment provider: checklist and code touchpoints.
 
 - DevEx
   - Local `docker-compose` for DB + Jaeger; `make`/`pwsh` scripts for migrations, seed, run jobs.
   - Pre-commit hooks: `dotnet format`, lint, tests; EditorConfig baseline.
   - PR template with checklists for logging, metrics, tests, docs.
 
 - Acceptance criteria
   - New developer can run app locally in < 15 minutes following README.
   - ADRs reviewed and merged; guides linked from README.
   - CI enforces formatting and basic tests on PRs.

  ## 11. Notifications & Reminders
  - [ ] (P0, 0.5d) Define `INotificationService` and channel adapters (Email, SMS, In-App) with provider abstractions
  - [ ] (P0, 0.5d) Implement scheduling via Quartz (quiet hours, time zone aware) and retry/backoff policy
  - [ ] (P0, 0.5d) Triggers: deadlines (N-days), missing docs, status changes (filing/payment), chat assignment/mention
  - [ ] (P1, 0.25d) Delivery status tracking (queued/sent/failed) + idempotent send log
  - [ ] (P1, 0.25d) Digest modes (daily/weekly) with per-user preferences
  - [ ] (P2, 0.25d) Push notifications (web/mobile) hook points (feature-flagged)

 #### 11.a Notifications Architecture & Scheduling
 
 - Domain model
   - `Notification` (id, templateKey, channel, payload, to, scheduledAt, status), `SendLog`, `UserPreference`, `Template` (locale-aware).
   - Channels: Email (SMTP/SendGrid), SMS (Twilio), In-App (SignalR/toast), Push (stubbed/flagged).
 
 - Service
   - `INotificationService` with `SendAsync`, `ScheduleAsync`, provider registry, idempotency via `clientMessageId`.
   - Quiet hours + time zone handling; batch digest builder.
 
 - Scheduling
   - Quartz job scans due notifications; retry policy with exponential backoff; dead-letter on permanent failures.
 
 - Endpoints
   - POST `/api/notifications/test` (admin), GET/PUT `/api/users/{id}/preferences` (per-channel toggles, quiet hours).
 
 - Templates & i18n
   - Use Scriban/Liquid templates; translation keys; safe variable substitution; preview endpoint.
 
 - Delivery tracking
   - Provider webhooks (if available) update `SendLog` with status; calculate delivery rate metrics.
 
 - Acceptance criteria
   - Notifications fire for defined triggers with correct channel and respect preferences.
   - Scheduled sends honor quiet hours and user time zones.
   - Delivery statuses visible in admin UI/logs with idempotent send log.

  ## 12. Roles & Access Control (RBAC)
  - [ ] (P0, 0.5d) Seed roles: Super Admin, Manager, Staff, Client Admin, Client User
  - [ ] (P0, 0.5d) Permission matrix + policy names (e.g., `filings.view`, `payments.create`, `chat.assign`)
  - [ ] (P0, 0.5d) Policy-based authorization across controllers and SignalR hubs
  - [ ] (P1, 0.5d) Row-/client-level scoping filters on queries (assigned clients only by default)
  - [ ] (P1, 0.25d) Impersonation flow (audit logged, banner)
  - [ ] (P2, 0.25d) Tenant scoping for multi-tenant deployments

 #### 12.a RBAC Design & Enforcement
 
 - Roles & permissions
   - Roles seeded with predefined permissions; permission matrix stored as constants or config for ease of rollout.
   - Policy names: `filings.view`, `payments.initiate`, `payments.webhook.manage`, `chat.assign`, `reports.generate`, etc.
 
 - Enforcement
   - Use `[Authorize(Policy="...")]` on controllers and SignalR hubs; resource-based checks for client ownership.
   - Row-level scoping: extension method `ApplyClientScope(user)` on EF queries; default to assigned clients only.
 
 - Impersonation
   - Admin-only; requires reason; audit logged with banner; limited to tenants where allowed.
 
 - Multi-tenant
   - Tenant context resolved per request; enforce tenant filters; ensure migrations and seeds are tenant-safe.
 
 - Acceptance criteria
   - Unauthorized access blocked with 403; audit logs capture denials.
   - Row-level scoping verified in integration tests.
   - Impersonation guarded and clearly indicated in UI and logs.

## 13. Document Management
  - [ ] (P0, 0.5d) Standardize folder structure per client/tax type/period (storage URIs)
  - [ ] (P0, 0.5d) Upload validations (file type/size) + required checklist enforcement
  - [ ] (P0, 0.5d) Versioning with change logs and restore
  - [ ] (P1, 0.5d) Retention and purge rules configurable by tax type/jurisdiction
  - [ ] (P2, 0.5d) OCR + full-text search toggle (where enabled)

 #### 13.a Document Management Architecture & APIs
 
 - Storage & structure
   - Standardized URI: `documents/{clientId}/{taxType}/{year}-{period}/{slug}/{documentId}/{version}/{filename}`.
   - Providers: Local/Azure Blob/S3 via abstraction; uploads via server-side streaming; downloads via pre-signed URL (TTL 5–15m).
   - Metadata on folder nodes: `ClientId`, `TaxType`, `Period`, `Jurisdiction`, `Confidential`.
 
 - Entities & indexing
   - `Document(Id, ClientId, TaxType, Period, Title, Status, CurrentVersion, StorageRootUri, RequiredKey, Confidential, CreatedAt, CreatedBy, UpdatedAt)`.
   - `DocumentVersion(Id, DocumentId, Version, StorageUri, HashSha256, SizeBytes, MimeType, UploadedBy, ChangeLog, CreatedAt)`.
   - `DocumentRequirement(Id, TaxType, Jurisdiction, Periodicity, Key, Title, IsMandatory, Description)` for checklist enforcement.
   - Indexes: `(ClientId, TaxType, Period)`, `(RequiredKey)`, `(Status)`, `(CreatedAt DESC)`.
 
 - Endpoints
   - GET `/api/documents/requirements?clientId={id}&taxType={t}&period={p}` → required list + satisfaction state.
   - POST `/api/documents/upload` multipart with metadata: `ClientId, TaxType, Period, Title, RequiredKey?, Confidential?` → returns `DocumentId` and `Version`.
   - GET `/api/documents/{id}` → metadata + versions; GET `/api/documents/{id}/download?version=n` → pre-signed URL.
   - POST `/api/documents/{id}/versions/{version}/restore` → sets `CurrentVersion`; audit logged.
   - DELETE `/api/documents/{id}/versions/{version}` → soft-delete only; prevents gaps in version history.
   - GET `/api/documents/search?q=...&clientId=...` → metadata search; full-text content available only if OCR enabled.
 
 - Validations & security
   - File type/size allowlist; MIME sniffing; optional AV scan hook (ClamAV or provider API) with quarantine on suspicion.
   - RBAC: object-level checks; client scope via `ApplyClientScope(user)`; pre-signed download URLs redacted from logs.
   - Checklist enforcement: block filing submission if mandatory `RequiredKey` not satisfied for period.
 
 - Versioning & audit
   - Every upload creates a new `DocumentVersion`; `ChangeLog` required for overwrite/restore; maintain immutability of prior versions.
   - Audit events: `document.upload`, `document.restore`, `document.deleteVersion`, with `UserId`, `ClientId`, `DocumentId`.
 
 - Retention & purge
   - Policy engine: per `TaxType/Jurisdiction` rules with default; Quartz job `0 30 3 * * ?` enforces retention and moves to cold storage or deletes.
 
 - OCR & search (feature-flagged)
   - When enabled, async OCR pipeline extracts text to `DocumentText(Index)` table; search endpoint switches to content index.
 
 - Acceptance criteria
   - Uploads succeed for allowlisted types; versioning and restore work; requirements reflected in UI/API.
   - Pre-signed URLs expire and are single-use if configured; audit logs present for all mutations.
   - Retention job enforces policies with safety window and dry-run mode in stage.

  ## 14. Integrations & Data Import
  - [ ] (P0, 0.5d) CSV/XLS import templates (clients, filings, payments) + schema validation
  - [ ] (P0, 0.5d) Import endpoints and jobs with preview-before-commit and dry-run mode
  - [ ] (P1, 0.5d) Bank statement import (CSV/OFX) and mapping for reconciliation
  - [ ] (P1, 0.5d) Webhooks for key events (filing submitted, payment posted, document approved)
  - [ ] (P2, 0.5d) Accounting connectors scaffold (QuickBooks/Xero) via provider abstraction

 #### 14.a Integrations Pipeline & Import Staging
 
 - Templates & validation
   - Versioned templates per entity: `Clients`, `Filings`, `Payments` with sample files and required columns.
   - Strict schema validation (headers, datatypes, enums); collect row-level errors with codes suitable for UI display.
 
 - Staging model
   - Tables: `ImportBatch(Id, Type, Status, CreatedBy, CreatedAt, SummaryJson)`, `ImportRow(BatchId, RowNumber, PayloadJson, ValidationErrorsJson, State)`.
   - States: Uploaded → Validated → Previewed → Committed/Aborted; idempotency via `ClientExternalId`/`NaturalKey` where applicable.
 
 - Endpoints
   - POST `/api/imports/start?type=clients|filings|payments` → `ImportBatchId`.
   - POST `/api/imports/{batchId}/upload` (multipart CSV/XLSX) → server validates and populates staging rows.
   - GET `/api/imports/{batchId}/preview` → aggregates (valid/invalid counts) + sample errors.
   - POST `/api/imports/{batchId}/commit?dryRun=true|false` → enqueues Quartz job; dry-run writes only to staging with diff report.
   - GET `/api/imports/{batchId}` → status, summary, links to error report download.
 
 - Processing jobs
   - Quartz worker processes rows in chunks (e.g., 500); transactional upserts with natural keys; emits audit `import.row.committed`.
   - Retry on transient errors; poison rows quarantined with reason.
 
 - Bank statements & reconciliation
   - Parsers: CSV/OFX normalize to `BankTransaction(Date, Amount, Currency, Description, ExternalRef)`.
   - Reconciliation suggests `PaymentId` matches via amount/date/fuzzy description; manual override endpoint persists linkage.
 
 - Webhooks & connectors
   - Outbound webhooks on major events (`filing.submitted`, `payment.succeeded`, `document.approved`); HMAC signing and retry with backoff.
   - Connector abstraction for QuickBooks/Xero (scaffolded); per-tenant credentials; rate limiting and circuit breakers.
 
 - Acceptance criteria
   - Import flows support preview and dry-run; commits are idempotent and auditable.
   - Reconciliation reduces unmatched transactions; manual link available; webhooks signed and retried.
   - Large files (50k rows) validate within acceptable time and do not block API threads.
 
  ## 15. Security & Compliance
  - [ ] (P0, 0.5d) Enforce TLS/HSTS in production; secure headers baseline
  - [ ] (P0, 0.5d) MFA for staff; optional for clients (feature-flagged)
  - [ ] (P0, 0.5d) Comprehensive audit logging across modules; tamper-evident storage strategy
  - [ ] (P1, 0.5d) PII minimization/masking and redaction in exports
  - [ ] (P1, 0.5d) Backup & DR runbook (RPO≤15m, RTO≤4h) validation
  - [ ] (P2, 0.25d) Secrets management hardening and key rotation procedures

 #### 15.a Security Hardening & Compliance Controls
 
 - Transport & headers
   - Enforce TLS 1.2+; HSTS (preload-ready), secure cookies, SameSite, CSRF tokens on unsafe methods.
   - Security headers: CSP (strict-dynamic with allowlist), X-Frame-Options DENY, X-Content-Type-Options nosniff, Referrer-Policy.
 
 - Authentication & MFA
   - TOTP-based MFA (RFC 6238) for staff; enrollment flow with backup codes; feature-flag client MFA.
   - Device remember (encrypted cookie) with configurable lifetime; step-up for sensitive actions.
 
 - Audit logging
   - Append-only `AuditLog(Id, UserId, Action, ResourceType, ResourceId, Timestamp, ClientIp, CorrelationId, DiffJson)`.
   - Tamper-evident: hash chain `PrevHash` + `Hash` over record; periodic anchor to external store.
 
 - Data protection
   - PII minimization in logs and exports; field-level redaction; optional column encryption for SSNs/IDs.
   - Secrets in Key Vault/Parameter Store; rotation schedule and dual key support.
 
 - Resilience & DR
   - Backup plan: point-in-time DB recovery (RPO ≤ 15m); disaster exercises quarterly; restore runbooks validated.
 
 - Acceptance criteria
   - Security headers verified via automated checks; MFA enrollment usable and recoverable with backup codes.
   - Audit log integrity verifiable; exports and logs contain no disallowed PII.
   - Backup and restore drills documented; secrets rotated in non-prod successfully.
 
  ## 16. Tax Calendar & Jurisdictions
  - [ ] (P0, 0.5d) Multi-jurisdiction calendar models and seed data
  - [ ] (P0, 0.5d) Obligation generation per client profile/registrations (auto + manual overrides)
  - [ ] (P1, 0.5d) Holiday calendars & time zone–aware deadlines (DST safe)
  - [ ] (P1, 0.25d) Dependency rules (e.g., payment due only after filing submitted)
  - [ ] (P2, 0.25d) iCal export/subscribe URLs

 #### 16.a Tax Calendar Modeling & Generation Engine
 
 - Data model
   - `Jurisdiction(Id, Name, TimeZone, DefaultHolidaysProvider)`; `TaxType(Id, Name, Periodicity)`.
   - `Obligation(Id, ClientId, JurisdictionId, TaxTypeId, PeriodStart, PeriodEnd, FilingDueAt, PaymentDueAt, Status, Source)`.
   - `Holiday(Id, JurisdictionId, Date, Name)`; seeded via provider per region.
 
 - Generation logic
   - On client registration or profile change, generate upcoming obligations based on `TaxType.Periodicity` and jurisdiction rules.
   - Apply DST-safe deadline computation using `TimeZone` for tenant; adjust for holidays/weekends per rule (next biz day).
   - Dependencies: `PaymentDueAt` computed relative to `FilingDueAt` when applicable.
 
 - Endpoints
   - GET `/api/calendar/client/{id}?range=next90d` → obligations with statuses and deadline highlights.
   - POST `/api/calendar/generate` (admin) → backfill/migrate obligations; idempotent by period keys.
   - GET `/api/calendar/ical/{clientId}/{token}` → signed iCal feed for read-only calendar subscription.
 
 - Acceptance criteria
   - Obligations generate correctly across DST boundaries and regional holidays; dependencies enforced.
   - iCal feed subscribes in common clients (Google/Outlook); revocation rotates token.
   - Performance: generation for one client ≤ 300ms typical; backfill job completes within window.
 
  ## 17. Workflow Automation
  - [ ] (P0, 0.5d) MVP rule engine (trigger/condition/action) with safe evaluation
  - [ ] (P0, 0.5d) Built-in actions: assign, notify, create task, escalate
  - [ ] (P1, 0.5d) State machines for filings, payments, documents with SLA timers
  - [ ] (P2, 0.5d) Bulk operations with safeguards and audit

 #### 17.a Rule Engine, State Machines & SLAs
 
 - Rule model
   - `Rule(Id, Name, Trigger, ConditionExpr, ActionsJson, Enabled, CreatedBy, CreatedAt)`; triggers: `filing.created`, `payment.failed`, `document.uploaded`, etc.
   - Safe evaluation: whitelist operators/functions; no dynamic code; guard execution time and depth.
 
 - Actions
   - Built-ins: `assign(userId)`, `notify(templateKey, to)`, `createTask(title, due)`, `escalate(queue)`.
   - Extensible via provider registry; idempotency key per event to prevent duplication.
 
 - State machines
   - Finite states for Filings/Payments/Documents with allowed transitions; SLA timers generate `breach` events and escalate.
 
 - Endpoints
   - GET `/api/rules` CRUD endpoints; POST `/api/rules/test` to dry-run against sample payload.
   - GET `/api/state/{entity}/{id}` → current state + history; POST `/api/state/{entity}/{id}/transition` (policy-guarded).
 
 - Acceptance criteria
   - Rules execute deterministically and idempotently; SLAs produce timely escalations.
   - Transitions blocked if not permitted; full audit of state changes.
   - Dry-run and test harness available for rule validation.
 
  ## 18. Localization & Internationalization (i18n)
  - [ ] (P1, 0.5d) Resource files and translation keys; locale switcher in UI
  - [ ] (P1, 0.25d) Locale-aware dates, numbers, currency formatting
  - [ ] (P2, 0.25d) Multi-language content management for client-facing templates

 #### 18.a i18n/i10n Strategy & Keys
 
 - Resource management
   - Centralized resource files per culture (`en-US`, `fr-FR`, etc.); key naming `area.page.element` with developer comments.
   - Missing-key detector in dev; CI check to prevent untranslated additions in core areas.
 
 - Formatting & locale
   - Use culture-specific `DateTime`, number, and currency formatting; time zone handling per tenant/user.
   - Currency display uses ISO codes and locale symbols; fallback when unknown.
 
 - Templates & notifications
   - Email/SMS templates localized via translation keys; `Template(locale)` fallback to default language.
 
 - Endpoints & UX
   - GET/PUT `/api/users/{id}/preferences` includes `Locale`; UI switcher persists preference.
 
 - Acceptance criteria
   - Strings externalized; no hard-coded user-facing text in new code paths.
   - Locale switch updates UI and notifications; formatting correct for numbers/dates/currency.
   - CI guards fail on missing translations for critical keys.
 
  ## 19. Accessibility (a11y)
  - [ ] (P1, 0.5d) WCAG 2.1 AA baseline checklist; keyboard navigation + focus management
  - [ ] (P1, 0.25d) Screen reader labels/roles and live region announcements
  - [ ] (P2, 0.25d) Color contrast/motion preferences; a11y linting in CI

 #### 19.a Accessibility Baseline & Testing
 
 - Keyboard & focus
   - Tabbable order, visible focus outlines, skip-to-content, trap focus in modals; no keyboard-only blockers.
 
 - Screen readers & semantics
   - Proper roles/labels; form error associations; live regions for chat/notifications with polite/assertive modes.
 
 - Visuals & motion
   - Contrast ≥ 4.5:1; reduced motion honors OS preference; avoid color-only distinctions.
 
 - Tooling & tests
   - Lint with axe/Pa11y in CI on key pages; manual audits for complex screens each release.
 
 - Acceptance criteria
   - Core flows pass axe checks with no critical violations; keyboard navigation complete.
   - Screen reader announcements present for dynamic updates; contrast validated.
   - Accessibility notes added to PR template checklist.
 
  ## 20. Onboarding & Data Migration
  - [ ] (P0, 0.5d) Guided client onboarding checklist
  - [ ] (P0, 0.5d) Import wizards with mapping + validation + preview
  - [ ] (P1, 0.5d) Dedup rules and merge review queue
  - [ ] (P1, 0.5d) Dry-run mode and rollback on failure with audit logs

 #### 20.a Onboarding Flows & Migration Safety
 
 - Guided onboarding
   - Checklist items for company profile, jurisdiction registrations, users, document templates, bank details.
   - Progress saved server-side; contextual help links and prerequisite checks.
 
 - Import wizards
   - Stepper UI over Import Batches: upload → validate → preview diffs → commit; mapping aids for headers.
 
 - Dedup & merge
   - Fuzzy match (name, email, phone) suggestions; merge queue with side-by-side diff and audit.
 
 - Rollback & audit
   - Dry-run produces diff; commit writes with per-row audit; rollback strategy via compensating changes or snapshot restore.
 
 - Acceptance criteria
   - Onboarding completion unblocks usage; wizards complete with clear error reporting.
   - Dedup prevents dup creation; merge actions audited; rollback verified in stage.
   - Time-to-first-value for a new client ≤ 1 hour with sample data.
 
  ## 21. Reporting Builder / Ad Hoc Analytics
  - [ ] (P1, 0.5d) Column/metric picker with saved views + permissions
  - [ ] (P1, 0.5d) Filters, grouping, aggregation; charting (line/bar/pie)
  - [ ] (P2, 0.5d) Cross-filtering between KPIs and detailed tables; scheduled delivery

 #### 21.a Ad Hoc Query Engine & Saved Views
 
 - Data model & guardrails
   - Whitelist of tables/views and columns; semantic layer names for UI; RBAC and client scoping applied server-side.
 
 - Query builder
   - Server composes SQL safely from AST (no raw text input); supports filters, group-by, aggregates, order, limit/offset.
   - Pagination with cursor/offset; max row limits; execution timeout and cancellation.
 
 - Saved views & sharing
   - `SavedView(Id, OwnerId, Name, DefinitionJson, Permissions)`; share with roles/teams respecting RBAC.
 
 - Endpoints
   - POST `/api/adhoc/query` with AST JSON → results stream; POST `/api/adhoc/views` CRUD saved views.
   - POST `/api/adhoc/schedule` to deliver saved view as report on cadence.
 
 - Acceptance criteria
   - Queries are safe, scoped, and performant; p95 < 1s for typical filtered queries.
   - Saved views persist and enforce permissions; scheduling delivers on time with audit.
   - UI supports charting and CSV export from the same query result.
 
  ## 22. DevOps & Environments
  - [ ] (P0, 0.5d) Isolated dev/stage/prod; automated database migrations
  - [ ] (P0, 0.5d) CI/CD: tests, security scans, approvals; rate limiting and WAF
  - [ ] (P1, 0.5d) Blue/green or canary deploys; instant rollback
  - [ ] (P2, 0.25d) Observability dashboards & SLOs per module (expand current)

 #### 22.a Environments, CI/CD & Release Safety
 
 - Environments & config
   - Separate dev/stage/prod with distinct credentials; configuration via environment variables and secrets store.
   - Database migrations run automatically on startup in dev/stage; gated and manual in prod with backup.
 
 - CI/CD
   - Pipelines: build → unit/integration tests → security scans (SAST/dep) → artifact → deploy to stage → smoke tests → manual approval to prod.
   - Rate limiting & WAF policies as code; infra drift detection; IaC validation.
 
 - Deployment strategies
   - Blue/green or canary with health checks and automated rollback on SLO/SLA breach; feature flags for risky changes.
 
 - Observability
   - Dashboards per module; release markers in tracing; error budget burn alerts integrated into promotion gates.
 
 - Acceptance criteria
   - One-click deploy to stage; prod deploy requires approvals and passes gates.
   - Rollback is fast (< 5m) and documented; migrations reversible; feature flags toggle off risky code paths.
   - Dashboards reflect releases; alerts wired to on-call.
 
  ## 23. Support & Help Center
  - [ ] (P1, 0.5d) In-app guides/tooltips and contextual help per screen
  - [ ] (P1, 0.5d) Public help center (searchable KB) and release notes
  - [ ] (P2, 0.25d) Feedback widget and CSAT/NPS surveys; ticketing integration stubs

 #### 23.a Help Center, Guides & Feedback Loop
 
 - In-app help
   - Contextual tooltips, help panels, and links to relevant KB articles; searchable command palette for help.
 
 - Public KB & release notes
   - Static site with categorized articles; search; versioned release notes with change impact and screenshots.
 
 - Feedback & ticketing
   - Widget to capture feedback with screenshots; CSAT/NPS surveys; ticketing integration stub (webhook) with HMAC.
 
 - Acceptance criteria
   - Help links exist on core pages; KB articles published for top workflows; release notes updated each sprint.
   - Feedback visible to internal team with triage workflow; CSAT/NPS tracked.
   - Ticketing webhook validated with signature and retry policy.

## Sequencing (Initial Execution Order)
1. Infra (0) + background jobs (7) + indexes → enable KPI (2) and Compliance baseline (4)
2. Payment gateways (1) incl. initiation UI → finalize missing reports (3)
3. Chat real-time features (5) wrap-up; perf/observability (9) ongoing
4. Notifications & RBAC foundations (11, 12)
5. Document Management & Integrations (13, 14)
6. Security hardening (15)
7. Tax Calendar & Workflow MVP (16, 17)
8. i18n & a11y baseline (18, 19)
9. Onboarding & migration tooling (20)
10. Ad hoc reporting builder (21)
11. DevOps hardening + Support center (22, 23)

## Current Progress Log
- (t0) Plan file created.
- (t1) Gateway abstraction & factory implemented; Local gateway added.
- (t2) Orange & Africell adapters added; unified PaymentGatewayRequest/Response types; DI updated.

