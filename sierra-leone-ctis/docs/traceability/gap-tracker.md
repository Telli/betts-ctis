# CTIS Gap Tracker (Living)

Owner: Engineering Leads
Scope: Sierra Leone CTIS frontend gaps vs requirements and client vision.
Update Rule: Update status and links when addressing a gap. Link PRs and tests.

## Status Legend
- [ ] Pending
- [~] In Progress
- [x] Done

## Gaps and Tasks

### KPI Dashboards
- [ ] Add client KPI tiles for `Filing Timeliness`, `On-Time Payments`, `Document Readiness` on `components/client-portal/client-dashboard.tsx`
  - Source endpoints: compliance/documents/payments services
  - Tests: e2e `client-portal.spec.ts` (extend)

### Reporting
- [x] Fix report download file extension based on content type
  - Files: `app/reports/page.tsx`, `app/client-portal/reports/page.tsx`
  - Tests: e2e `reports-integration.spec.ts`
- [~] Expose missing report types in UI: `DocumentSubmission`, `TaxCalendar`, `CaseManagement`
  - Files: `components/reports/ReportGenerator.tsx`, `app/reports/page.tsx`
  - Tests: e2e `reports-integration.spec.ts`
- [ ] Add alternative access on failure (retry/history/deep link)
  - Files: `components/reports/ReportHistory.tsx`

### Compliance Monitoring
- [ ] Wire `PenaltyWarningsCard` to `tax-calculation-service` and render computed penalties in compliance page
  - Files: `components/penalty-warnings-card.tsx`, `app/compliance/page.tsx`, `lib/services/tax-calculation-service.ts`
  - Tests: e2e `full-system-integration.spec.ts`
- [ ] Add countdown timers/badges for deadlines
  - Files: `app/deadlines/page.tsx`

### Messaging
- [ ] Integrate SignalR in `app/associate/messages/page.tsx` for real-time updates and typing indicators
  - Files: `lib/signalr-client.ts`, messages page
  - Tests: e2e `admin-interface.spec.ts`
- [ ] Add assignment and internal notes to conversations (associate workflow)

### Payments
- [ ] Implement Orange/Africell initiation UI with status updates and PIN workflow
  - Files: `lib/services/payment-gateway-service.ts`, payments UI
  - Tests: e2e `payment-gateway-integration.spec.ts`
- [ ] Threshold-based approvals configuration UI

### Documents
- [ ] Implement document versioning UI and history
  - Tests: e2e `client-portal.spec.ts`
- [ ] Document-level sharing controls using associate permissions

### Notifications
- [ ] Implement schedule presets (30/14/7/1 days) and persistence
  - Files: `lib/services/notification-service.ts`, notifications settings page
  - Tests: e2e `full-system-integration.spec.ts`
- [ ] System maintenance and critical error notifications

### Security
- [ ] MFA/2FA flows (setup, verification, recovery)
  - Files: `lib/services/auth-service.ts`, auth UI
  - Tests: e2e `auth.spec.ts`
- [ ] Breach detection & account lock (frontend indicators; backend support required)

---

## Links
- Requirements: `Betts/.kiro/specs/ctis-production-ready/requirements.md`
- Client Concept: `Betts/sierra-leone-ctis/Client_concept.md`
- Matrix: `docs/traceability/matrix.md`
