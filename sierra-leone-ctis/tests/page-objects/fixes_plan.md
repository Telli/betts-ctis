Implementation Plan: Fix Requirements Traceability Gaps
Overview
This plan addresses 17 gaps identified in the Requirements Traceability Matrix across three priority levels. The plan is organized by priority and includes specific file paths, implementation details, and dependencies.

Total Gaps: 17 items

Critical (7 items) - Blocking production
High Priority (5 items) - Required for compliance
Medium Priority (5 items) - Quality improvements
---

Phase 1: Critical Gaps (Production Blockers)
1.1 Payment Gateway Integration
Priority: CRITICAL
Files to Modify:

BettsTax/BettsTax.Core/Services/OrangeMoneyProvider.cs
BettsTax/BettsTax.Core/Services/AfricellMoneyProvider.cs
BettsTax/BettsTax.Core/Services/PayPalProvider.cs
BettsTax/BettsTax.Core/Services/StripeProvider.cs
BettsTax/BettsTax.Web/Controllers/PaymentsController.cs
BettsTax/BettsTax.Web/appsettings.json (add payment gateway configuration section)
Tasks:

Create payment gateway configuration options class (PaymentGatewayOptions.cs)
Add configuration to appsettings.json with placeholders for API keys
Update provider implementations to use configuration instead of hardcoded endpoints
Add proper error handling and retry logic
Implement webhook signature verification
Add integration tests for each provider
Document API credential setup process
Dependencies: Payment gateway merchant accounts and API credentials

---

1.2 Client Portal UI
Priority: CRITICAL
Files to Create/Modify:

sierra-leone-ctis/app/client-portal/page.tsx (new)
sierra-leone-ctis/app/client-portal/dashboard/page.tsx (new)
sierra-leone-ctis/app/client-portal/filings/page.tsx (new)
sierra-leone-ctis/app/client-portal/payments/page.tsx (new)
sierra-leone-ctis/app/client-portal/documents/page.tsx (new)
sierra-leone-ctis/components/client-portal/ (new components directory)
Tasks:

Create client portal layout with navigation
Build dashboard showing tax summary, compliance status, upcoming deadlines
Create tax filings list/view page
Create payments list/view page
Create documents list/upload page
Add client authentication middleware
Integrate with existing backend APIs
Add responsive design for mobile access
Dependencies: Backend APIs already exist (PaymentsController, DocumentController, etc.)

---

1.3 PDF Report Generation
Priority: CRITICAL
Files to Modify:

BettsTax/BettsTax.Core/Services/SimpleReportGenerator.cs
BettsTax/BettsTax.Core/BettsTax.Core.csproj (verify iText7 package)
Tasks:

Replace text-based PDF generation with iText7 implementation
Create PDF templates for each report type (Tax Filing, Payment History, Compliance, etc.)
Add PDF styling (headers, footers, tables, charts)
Implement proper PDF document structure
Add Sierra Leone branding/watermarking
Test PDF generation for all report types
Verify file size and performance
Current Issue: GeneratePdfReportAsync returns UTF-8 encoded text instead of PDF bytes

Dependencies: iText7 package (already referenced in .csproj)

---

1.4 Virus Scanning Implementation
Priority: CRITICAL
Files to Modify:

BettsTax/BettsTax.Core/Services/FileStorageService.cs (ScanFileForVirusAsync method)
Tasks:

Integrate ClamAV or Windows Defender for virus scanning
Add configuration options for antivirus (ClamAV socket path, Windows Defender options)
Implement async virus scanning with timeout
Add quarantine workflow for infected files
Update file upload validation to use real virus scanning
Add logging and metrics for scanning operations
Document ClamAV installation/setup for production
Current Issue: Placeholder implementation with basic file validation only

Dependencies: ClamAV installation or Windows Defender access

---

1.5 Document Retention (7 Years)
Priority: CRITICAL
Files to Modify:

BettsTax/BettsTax.Core/Options/DocumentRetentionOptions.cs
BettsTax/BettsTax.Web/appsettings.json
BettsTax/BettsTax.Core/Services/DocumentRetentionBackgroundService.cs
Tasks:

Update RetentionDays default from 365 to 2555 (7 years)
Update appsettings.json configuration
Add tax-type-specific retention rules (optional enhancement)
Update retention service logic to handle 7-year period
Verify database indexes for retention queries
Test retention service with 7-year cutoff
Current Issue: RetentionDays = 365 instead of 2555 (7 years)

---

1.6 Audit Log Retention and Tamper-Evidence
Priority: CRITICAL
Files to Create/Modify:

BettsTax/BettsTax.Core/Services/AuditLogRetentionBackgroundService.cs (new)
BettsTax/BettsTax.Core/Options/AuditLogRetentionOptions.cs (new)
BettsTax/BettsTax.Data/Models/Security/SecurityModels.cs (update AuditLog model)
BettsTax/BettsTax.Core/Services/Security/AuditService.cs (update LogAsync method)
BettsTax/BettsTax.Data/ApplicationDbContext.cs (prevent audit log modifications)
Tasks:

Add checksum and hash chain fields to AuditLog model (Checksum, PreviousLogHash, IsImmutable, SealedAt)
Implement checksum calculation in AuditService.LogAsync
Create hash chain (each log references previous log's hash)
Create AuditLogRetentionBackgroundService for 7-year retention (archive only, never delete)
Add integrity verification methods
Override SaveChangesAsync in ApplicationDbContext to prevent modifications to sealed logs
Add configuration options for retention period
Create database migration for new AuditLog fields
Dependencies: SHA256 hashing, background service infrastructure

---

1.7 File Encryption at Rest
Priority: CRITICAL
Files to Modify:

BettsTax/BettsTax.Core/Services/FileStorageService.cs (SaveFileAsync method)
BettsTax/BettsTax.Core/Services/FileStorageService.cs (GetFileAsync method)
Tasks:

Add file encryption using AES-256 before saving to disk
Decrypt files when reading from disk
Use EncryptionService for encryption/decryption
Store encryption keys in secure configuration (Azure Key Vault recommended)
Handle encrypted file migration (backfill existing files)
Add performance testing for encryption overhead
Document encryption key management procedures
Current Issue: Files saved as plain text on disk

Dependencies: EncryptionService (already exists)

---

Phase 2: High Priority Gaps (Compliance Requirements)
2.1 Fix KPI Calculations (Remove Hardcoded Values)
Priority: HIGH
Files to Modify:

BettsTax/BettsTax.Core/Services/KPIService.cs
Tasks:

Replace CalculateAverageFilingTimelinessAsync with real calculation (query TaxFilings, compare DueDate vs SubmittedDate)
Replace CalculatePaymentCompletionRateAsync with real calculation (query Payments, count on-time vs total)
Replace CalculateDocumentComplianceAsync with real calculation (query Documents vs DocumentRequirements)
Replace CalculateClientEngagementRateAsync with real calculation (query AuditLogs for login/action counts)
Replace CalculateClientFilingTimelinessAsync with real calculation (client-specific filing timeliness)
Replace CalculateClientPaymentPercentageAsync with real calculation (client-specific payment percentage)
Replace CalculateClientDocumentReadinessAsync with real calculation (client-specific document readiness)
Replace CalculateComplianceTrendAsync with real calculation (historical compliance scores)
Replace CalculateTaxTypeBreakdownAsync with real calculation (aggregate TaxFilings by TaxType)
Add database indexes for performance
Add caching for expensive calculations
Current Issue: 9 methods return hardcoded values instead of database queries

---

2.2 Email Notifications (10-Day Warning and Daily Reminders)
Priority: HIGH
Files to Modify:

BettsTax/BettsTax.Core/Services/DeadlineMonitoringService.cs
BettsTax/BettsTax.Core/Services/ComplianceMonitoringWorkflow.cs
BettsTax/BettsTax.Core/BackgroundJobs/ComplianceDeadlineMonitoringJob.cs
Tasks:

Add 10-day before deadline warning logic
Implement daily reminder logic (send once per day until deadline)
Add check to skip reminders if filing/payment already completed
Update DeadlineMonitoringService to calculate days until deadline
Add notification scheduling to avoid duplicate reminders
Update email templates for 10-day warning and daily reminders
Test notification timing and frequency
Current Issue: Missing 10-day warning and daily reminder logic

---

2.3 Fix Deadline Logic (Payroll Tax and Excise Duty)
Priority: HIGH
Files to Modify:

BettsTax/BettsTax.Core/Services/DeadlineMonitoringService.cs
BettsTax/BettsTax.Core/Services/SierraLeoneTaxCalculationService.cs (if deadline logic there)
Tasks:

Add Payroll Tax deadline rule: Annual returns due January 31
Add foreign employee filing deadline: Within 1 month of start date
Add Excise Duty deadline rule: 21 days from delivery/import date
Update GST deadline to use period end + 21 days (not fixed dates)
Add holiday/weekend handling (move to next business day if needed)
Add timezone awareness (Sierra Leone timezone)
Update deadline calculation tests
Current Issue: Missing specific Payroll Tax and Excise Duty deadline rules

---

2.4 PII Masking in Exports
Priority: HIGH
Files to Modify:

BettsTax/BettsTax.Core/Services/DataExportService.cs
BettsTax/BettsTax.Core/Services/Security/AuditService.cs (reuse MaskSensitiveData)
Tasks:

Identify PII fields in export data (names, emails, phone numbers, TINs, addresses)
Implement PII masking in export service (reuse MaskSensitiveData logic from AuditService)
Add configuration option to enable/disable PII masking per export type
Mask PII in CSV exports
Mask PII in Excel exports
Mask PII in PDF exports (if applicable)
Add export metadata indicating PII masking status
Test exports with and without PII masking
Current Issue: PII exported in plain text without masking

---

2.5 Enforce Document Status Transitions
Priority: HIGH
Files to Modify:

BettsTax/BettsTax.Core/Services/DocumentVerificationService.cs
BettsTax/BettsTax.Data/DocumentVerification.cs
Tasks:

Define valid status transition rules (NotRequested → Requested → Submitted → UnderReview → Verified/Filed/Rejected)
Add validation in DocumentVerificationService to enforce transitions
Throw InvalidOperationException for invalid transitions
Add status transition audit logging
Update document verification UI to handle invalid transitions gracefully
Add unit tests for status transition validation
Current Issue: Status transitions not enforced, can skip workflow steps

---

Phase 3: Medium Priority Gaps (Quality Improvements)
3.1 Bot Capabilities (FAQ, Guided Flows, Intent Detection)
Priority: MEDIUM
Files to Create/Modify:

BettsTax/BettsTax.Core/Services/ChatBotService.cs (new)
BettsTax/BettsTax.Data/Models/CommunicationModels.cs (add FAQ, KnowledgeBase entities)
BettsTax/BettsTax.Web/Controllers/ChatController.cs (integrate bot)
Tasks:

Create FAQ/knowledge base data model
Implement FAQ retrieval service
Add guided flows for common scenarios (missing documents, filing steps, payment guidance)
Add basic intent detection (keyword-based initially)
Integrate bot responses into chat system
Add bot fallback to human when confidence low
Create admin UI for managing FAQ/knowledge base
Dependencies: Chat system already exists, needs bot logic

---

3.2 Payment Processing E2E Tests
Priority: MEDIUM
Files to Create:

BettsTax/BettsTax.Web.Tests/Integration/PaymentProcessingE2ETests.cs
Tasks:

Create E2E test for payment creation → approval → gateway processing → status update
Test payment webhook handling
Test payment retry logic
Test payment failure scenarios
Test multiple payment gateway providers (mock implementations)
Verify audit logging for payment operations
Test payment reconciliation workflow
Dependencies: Integration test infrastructure exists

---

3.3 Code Coverage Measurement and Tracking
Priority: MEDIUM
Files to Modify:

BettsTax/BettsTax.Core.Tests/BettsTax.Core.Tests.csproj
BettsTax/BettsTax.Web.Tests/BettsTax.Web.Tests.csproj
Add .github/workflows/test-coverage.yml (CI/CD integration)
Tasks:

Configure Coverlet for code coverage collection
Set coverage threshold to 80%
Generate coverage reports (OpenCover, Cobertura formats)
Create HTML coverage reports
Integrate coverage reporting into CI/CD pipeline
Set up Codecov or similar for coverage tracking
Add coverage badges to README
Dependencies: Coverlet already installed, needs configuration

---

3.4 Performance Testing and Verification
Priority: MEDIUM
Files to Create:

BettsTax/BettsTax.Tests/Performance/PerformanceTests.cs
sierra-leone-ctis/tests/performance/load-testing.spec.ts (may already exist)
Tasks:

Create performance test suite for API endpoints
Verify page load times (P95 < 2.0s target)
Verify API latency (Read P95 < 400ms, Write P95 < 800ms)
Load testing for throughput (50 RPS sustained, 150 RPS burst)
Concurrency testing (500 concurrent sessions)
File upload performance testing
Database query performance optimization
Add performance metrics to monitoring dashboard
Dependencies: OpenTelemetry already configured

---

3.5 Document Status Transitions (Enforcement)
Priority: MEDIUM
Note: This overlaps with High Priority item 2.5 - consolidate into single implementation

---

Implementation Order and Dependencies
Week 1-2: Critical Backend Fixes

Document retention (7 years) - Simple config change
PDF report generation - Use existing iText7
File encryption at rest - Use existing EncryptionService
Audit log retention and tamper-evidence - New service, database migration
Week 3-4: Critical Integrations

Payment gateway configuration - API credentials needed
Virus scanning implementation - ClamAV installation needed
KPI calculations - Database queries
Week 5-6: High Priority Fixes

Email notifications (10-day warning, daily reminders)
Deadline logic fixes
PII masking in exports
Document status transition enforcement
Week 7-8: Frontend and Testing

Client portal UI - Full dashboard
Payment processing E2E tests
Code coverage setup
Performance testing
Week 9+: Medium Priority

Bot capabilities
Additional enhancements
---

Testing Requirements
For each gap fix:

Unit tests for new/changed logic
Integration tests for API changes
Manual testing for UI changes
Security testing for encryption/authentication changes
Performance testing for critical path changes
---

Configuration Changes Required
Payment Gateways: Add API credentials to appsettings.json or Azure Key Vault
Document Retention: Update DocumentRetentionOptions.RetentionDays = 2555
Audit Log Retention: Add AuditLogRetentionOptions configuration
Virus Scanning: Add ClamAV socket path or Windows Defender configuration
File Encryption: Add encryption key to secure storage
---

Risk Mitigation
Payment Gateway: Start with test/sandbox accounts before production
File Encryption: Implement backfill strategy for existing files
Audit Log Changes: Database migration required, plan for downtime
KPI Calculations: Add caching to prevent performance issues
Client Portal UI: Phased rollout with feature flags
---

Success Criteria
All Critical gaps resolved (7/7)
All High Priority gaps resolved (5/5)
Medium Priority gaps completed (4/5 minimum)
Code coverage >80%
All tests passing
Performance targets met
Security audit passed

---

## CTIS Security & Code Quality Remediation Implementation (Phased)

The REMEDIATION-PLAN.md defines 5 phases over 8 weeks to raise CTIS security and quality scores. The table below maps those phases to repository areas, primary owners, and the main delivery artifacts we need to land.

| Phase | Focus & Weeks | Primary Owners | Key Repo Areas |
| --- | --- | --- | --- |
| 1 | Critical Security (Weeks 1-2) | Backend + Frontend | `BettsTax.Web`, `BettsTax.Core`, `sierra-leone-ctis/app`, `lib/services` |
| 2 | High Priority Security (Week 3) | Backend + DevOps | `BettsTax.Web`, `BettsTax.Core`, middleware, hosting configs |
| 3 | Code Quality (Weeks 4-5) | Frontend + QA | `sierra-leone-ctis` app/components/tests, shared libs, API clients |
| 4 | Additional Improvements (Week 6) | Full-stack | Shared configs, forms, accessibility helpers |
| 5 | Production Readiness (Weeks 7-8) | Full-stack + DevOps + Security | Performance tuning, monitoring, docs, audit tooling |

### Phase 1 – Critical Security (Weeks 1-2)

**Objectives:** Real authN/Z, remove hardcoded secrets, harden inputs, add CSRF. All blockers to deploying CTIS must be resolved here.

1. **Backend authentication foundation**
   - Files: `BettsTax.Web/Program.cs`, `BettsTax.Web/Controllers/AuthController.cs`, `BettsTax.Core/Services/RefreshTokenService.cs`, EF models.
   - Tasks: Configure ASP.NET Core Identity + JWT (already scaffolded) with bcrypt (cost ≥12), add refresh token rotation, add `/api/auth/login|refresh|logout|change-password` endpoints.
   - Deliverable: Auth integration tests + Postman collection.

2. **Frontend authentication wiring**
   - Files: `sierra-leone-ctis/lib/services/auth-service.ts`, `context/auth-context.tsx`, `app/(auth)/login/page.tsx`, guards in `hooks/use-auth-guard.tsx`.
   - Tasks: Store JWT in HTTP-only cookies (set via backend), wire refresh flow, remove mock role toggles in any component, ensure session poll uses `/api/auth/me`.

3. **Role-based authorization**
   - Backend: Apply `[Authorize(Roles="Client,Staff,Admin,SystemAdmin")]` attributes across controllers (`BettsTax.Web/Controllers/**`). Extend `IAuthorizationHandler` in `BettsTax.Web.Authorization` for resource checks (client ownership, deadlines).
   - Frontend: Introduce `<Authorized role="Admin">` helpers (new component under `components/auth/Authorized.tsx`) and wrap protected routes in `middleware.ts`.

4. **Hardcoded credential removal**
   - Delete demo credentials from `sierra-leone-ctis/app/(auth)/login/page.tsx` and any `README` sections. Provide seeds via EF migration + `scripts/create-dev-seed.ps1`. Add `.env.example` updates and ensure `.env` is gitignored.

5. **Input validation & sanitization**
   - Backend: Enforce FluentValidation per DTO (`BettsTax.Core/DTOs/**` + validators). Add sanitization service reused in controllers, verify EF queries use parameters only.
   - Frontend: Standardize on React Hook Form + Zod for forms in `components/forms/**` and pages (Payments/Documents/Chat/Admin). Use DOMPurify for any HTML renderers.

6. **CSRF protection**
   - Backend: Enable Antiforgery (`Microsoft.AspNetCore.Antiforgery`) tokens, add validation filters on POST/PUT/DELETE.
   - Frontend: Fetch CSRF token via `/api/security/csrf` and inject header via central `api-client.ts` interceptor.

**Exit criteria:** Authentication + authorization e2e tests pass, no credentials in repo, all forms validated client/server, CSRF tokens validated, session timeout (≤30 min) enforced.

### Phase 2 – High Priority Security (Week 3)

1. **Rate limiting**
   - Integrate `AspNetCoreRateLimit` via `Program.cs`. Policies: login 5/15min, general API 100/min, file upload 10/hr. Wire custom responses/logging.

2. **Security headers middleware**
   - Create `SecurityHeadersMiddleware` under `BettsTax.Web/Middleware`. Configure CSP, HSTS, X-Frame-Options, etc. Validate using `securityheaders.com` screenshot stored under `docs/security/`.

3. **Secure file upload pipeline**
   - Extend `FileStorageService` to validate mime + size before saving. Move storage path outside `wwwroot`. Trigger ClamAV scan (`services/FileStorageService.cs` + new `VirusScanner` abstraction) before persisting.

4. **HTTPS enforcement**
   - Ensure reverse proxy/hosting config (e.g., `appsettings.Production.json`) forces HTTPS, sets `UseHsts`, and redirects HTTP traffic. Document SSL cert steps in `DEPLOYMENT_ALTERNATIVES.md`.

**Exit criteria:** 429 responses for overuse, A-grade security headers, uploads rejected unless sanitized, HTTP automatically redirected to HTTPS.

### Phase 3 – Code Quality Improvements (Weeks 4-5)

1. **Real API integrations**
   - Replace mock data inside `sierra-leone-ctis/components/**` with services from `lib/services/*-service.ts`. Add shared `api-client.ts` (Axios/fetch) and wrap data fetching with React Query.

2. **Testing stack**
   - Backend: Expand `BettsTax.Core.Tests` + `BettsTax.Web.Tests` with authentication, payment, document tests.
   - Frontend: Set up Vitest + React Testing Library (update `package.json`, `tsconfig`). Use MSW to mock API.
   - Goal: ≥80% coverage enforced in CI (GitHub Actions workflow `test-coverage.yml`).

3. **Error boundaries & loading states**
   - Create `components/common/ErrorBoundary.tsx` & `LoadingSpinner.tsx`. Wrap root layout (`app/layout.tsx`). Integrate toast notifications via shared hook.

4. **State management cleanup**
   - Adopt Context/Zustand for auth/user/global notifications stored under `context/`. Remove prop drilling from Dashboard/Admin flows.

**Exit criteria:** No mock data references, automated test suite stable in CI, user flows resilient with loading + error UI, shared state documented.

### Phase 4 – Additional Improvements (Week 6)

1. **Environment configuration**
   - Provide `.env.example` for both backend and frontend, add runtime config validation (e.g., `ConfigValidationService` in backend, `validateEnv.ts` in frontend).

2. **Form overhaul**
   - Ensure every form uses shared RHF components, lists inline validation errors, and has unit tests.

3. **Accessibility (a11y)**
   - Add ARIA labels, keyboard traps, and run Lighthouse audits. Track findings in `docs/a11y/audit.md`.

**Exit criteria:** Clean env docs, consistent form UX, Lighthouse accessibility score > 90.

### Phase 5 – Production Readiness (Weeks 7-8)

1. **Performance**
   - Implement Next.js code splitting, lazy data tables, pagination. Backend: analyze EF queries, add indexes/micro-optimizations. Target Lighthouse performance > 90.

2. **Monitoring & security operations**
   - Integrate Sentry/App Insights, centralize security event logging, monitor failed logins + anomalies. Document alert routing.

3. **Dependency audit**
   - Run `pnpm audit`/`npm audit` + `dotnet list package --vulnerable`. Fix highs/criticals, enable Dependabot.

4. **Documentation & final audit**
   - Update README, API docs (`docs/api/*.md`), deployment + troubleshooting guides. Schedule OWASP ZAP + pen-test; log findings in `SECURITY_TESTING_GUIDE.md` and obtain sign-off.

**Exit criteria:** Performance + monitoring KPIs met, dependency scans clean, documentation published, final security audit signed.

### Governance & Tracking

- Use existing weekly cadences (Friday stakeholder updates) and gate transitions with go/no-go checkpoints (Week 2 → Phase 2, Week 5 → Phase 5, Week 7 → Production approval).
- Track tasks in GitHub Projects columns mirroring phases; definition of done should reference the acceptance criteria above plus linked PRs/tests.
- QA + Security engineer involvement front-loaded for Phase 1/2 reviews; DevOps joins in Phase 2 onward for infra updates.

This phased implementation outline keeps the REMEDIATION-PLAN.md goals intact while mapping them to concrete files, services, and deliverables inside the CTIS repositories.