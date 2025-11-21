# CTIS Living Traceability Matrix

Scope: Sierra Leone CTIS frontend (Next.js) tracked against requirements in `.kiro/specs/ctis-production-ready/requirements.md` and client vision in `sierra-leone-ctis/Client_concept.md`.

Legend
- Status: I = Implemented, P = Partially Implemented, M = Missing
- Files are representative entry points (services, pages, components). Backend responsibilities are noted where FE cannot verify.

## Req 1. Enhanced KPI Dashboard System (Status: P)
- Admin KPI dashboard metrics (I)
  - Files: components/kpi/InternalKPIDashboard.tsx; lib/hooks/useKPIs.ts
  - Tests: tests/e2e/kpi-dashboard.spec.ts
- Client KPI dashboard metrics (P)
  - Files: components/client-portal/client-dashboard.tsx
  - Gap: Filing Timeliness, On-Time Payment %, Document Readiness tiles missing
  - Tests: tests/e2e/client-portal.spec.ts
- KPI refresh within 5 minutes (P)
  - Files: lib/hooks/useKPIs.ts (alerts have refetchInterval=5m; main KPIs use staleTime only)
  - Tests: tests/e2e/kpi-dashboard.spec.ts
- Alerts on threshold breach (P)
  - Files: lib/hooks/useKPIs.ts (alerts, thresholds update)
  - Tests: tests/e2e/kpi-dashboard.spec.ts

## Req 2. Comprehensive Reporting System (Status: P)
- Tax filing, payment history reports with PDF/Excel/CSV (P)
  - Files: lib/services/report-service.ts; components/reports/ReportGenerator.tsx; app/reports/page.tsx; app/client-portal/reports/page.tsx
  - Fix implemented: download filename extension respects blob MIME (admin + client pages)
  - Tests: tests/e2e/reports-integration.spec.ts
- Compliance report w/ deadlines/penalties (P)
  - Files: components/reports/ReportGenerator.tsx (TaxCompliance alias to Compliance)
  - Tests: tests/e2e/reports-integration.spec.ts
- Internal admin reports (Revenue/KPI, Client Activity, Case Mgmt) (P)
  - Files: lib/services/report-service.ts (ReportType mappings); components/reports/ReportGenerator.tsx (exposed CaseManagement, TaxCalendar, DocumentSubmission)
  - Tests: tests/e2e/reports-integration.spec.ts
- Alternative access on failure (M)
  - Files: components/reports/ReportHistory.tsx (toasts only)
  - Tests: N/A

## Req 3. Advanced Compliance Monitoring (Status: P)
- Status summary tiles (I)
  - Files: app/compliance/page.tsx
  - Tests: tests/e2e/full-system-integration.spec.ts
- Filing checklist for GST/PAYE/Income Tax (P)
  - Files: app/compliance/page.tsx (references checklist component)
  - Tests: tests/e2e/tax-filing-form.spec.ts
- Deadlines w/ countdown & priority (P)
  - Files: app/deadlines/page.tsx; lib/services/deadline-service.ts
  - Tests: tests/e2e/full-system-integration.spec.ts
- Penalty warnings (Finance Act 2025) (P)
  - Files: components/penalty-warnings-card.tsx (mock); lib/services/tax-calculation-service.ts
  - Tests: tests/e2e/full-system-integration.spec.ts
- Document tracker completeness (P)
  - Files: app/compliance/page.tsx (tracker ref), lib/services/document-service.ts
  - Tests: tests/e2e/full-system-integration.spec.ts

## Req 4. Integrated Communication System (Status: P)
- Messaging w/ history (I)
  - Files: app/associate/messages/page.tsx; lib/services/client-portal-service.ts
  - Tests: tests/e2e/admin-interface.spec.ts; tests/e2e/full-system-integration.spec.ts
- Real-time chat via SignalR (M)
  - Files: lib/signalr-client.ts (implemented); not wired in messages page
  - Tests: N/A
- Assignment & internal notes (M)
- Priority routing SLA (M)

## Req 5. Multi-Gateway Payment Integration (Status: P)
- Multiple methods + approvals (P)
  - Files: lib/services/payment-service.ts; app/payments/page.tsx
  - Tests: tests/e2e/payment-gateway-integration.spec.ts
- Orange/Africell initiation UI (M)
  - Files: lib/services/payment-gateway-service.ts (stubs); missing UI flow
  - Tests: tests/e2e/payment-gateway-integration.spec.ts
- Failure handling alternatives (P)

## Req 6. Associate Permission Management (Status: P)
- Granular permissions (I)
  - Files: lib/services/associate-permission-service.ts; app/associate/permissions/page.tsx
  - Tests: tests/e2e/associate-permission-system.spec.ts
- Action logging per associate (P)
  - Files: lib/services/on-behalf-action-service.ts
  - Tests: tests/e2e/security/security-smoke.spec.ts (if present) or full-system-integration.spec.ts
- Permission templates & bulk (P)

## Req 7. Document Management with Version Control (Status: P)
- Uploads/types/limits (I)
  - Files: components/document-upload-form.tsx; lib/services/document-service.ts
  - Tests: tests/e2e/client-portal.spec.ts
- Version history (M)
- Sharing/permissions (P)
- Requirements-driven alerts (P)

## Req 8. Real-time Notification System (Status: P)
- Reminders schedule 30/14/7/1 (M)
  - Files: lib/services/notification-service.ts; app/deadlines/page.tsx (static text)
  - Tests: tests/e2e/full-system-integration.spec.ts
- Payment processed + compliance score notifications (P)
  - Files: lib/signalr-client.ts hubs; lib/hooks/useKPIs.ts (alerts)
  - Tests: tests/e2e/payment-gateway-integration.spec.ts; kpi-dashboard.spec.ts
- System maintenance / critical errors (M)

## Req 9. Tax Calculation Engine (Status: P)
- GST 15%, penalties, MAT 2% (P)
  - Files: lib/services/tax-calculation-service.ts
  - Tests: tests/e2e/tax-filing-form.spec.ts
- Regulation updates trigger recalculation & notify (M)

## Req 10. Production Security & Compliance (Status: P)
- RBAC / route guards (I)
  - Files: middleware.ts
  - Tests: tests/e2e/auth.spec.ts; admin-interface.spec.ts
- MFA/2FA (M)
  - Files: lib/services/auth-service.ts (login only)
  - Tests: N/A
- Breach detection & account lock (M)

---

Maintenance
- Update this matrix when: files or tests change, features added, or requirements updated.
- Owners: Engineering leads; PR template should prompt touching this file for scope changes.
