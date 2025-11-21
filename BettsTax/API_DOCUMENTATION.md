## BettsTax API Documentation (Backend ↔ Frontend Integration)

This document summarizes the critical endpoints used by the Sierra Leone CTIS frontend and their request/response formats. All JSON is camelCase and wrapped as `{ success: boolean, data: T, meta?: any }`.

Base URL (local): http://localhost:5001
Authentication: Bearer JWT (Authorization: Bearer <token>)

---

### 1) Dashboard

- GET /api/dashboard
  - Description: Returns dashboard data including metrics, recentActivity, upcomingDeadlines, etc.
  - Auth: Required
  - Response (200):
    {
      "success": true,
      "data": {
        "clientSummary": { ... },
        "complianceOverview": { ... },
        "recentActivity": [ ... ],
        "upcomingDeadlines": [ ... ],
        "pendingApprovals": [ ... ],
        "metrics": {
          "complianceRate": number,
          "complianceRateTrend": "up" | "down" | "neutral",
          "complianceRateTrendValue": string,
          "filingTimeliness": number,
          "filingTimelinessTrend": "up" | "down" | "neutral",
          "filingTimelinessTrendValue": string,
          "paymentCompletionRate": number,
          "paymentCompletionTrend": "up" | "down" | "neutral",
          "paymentCompletionTrendValue": string,
          "documentSubmissionCompliance": number,
          "documentSubmissionTrend": "up" | "down" | "neutral",
          "documentSubmissionTrendValue": string
        }
      }
    }

- GET /api/dashboard/recent-activity?count=10
- GET /api/dashboard/deadlines?days=30
- GET /api/dashboard/pending-approvals
- GET /api/dashboard/navigation-counts

Notes:
- Property casing is camelCase globally (Program.cs configured with JsonNamingPolicy.CamelCase).
- Metrics are populated from IKpiComputationService when available; otherwise metrics may be null.

---

### 2) Deadlines

New controller: DeadlinesController
Route prefix: /api/deadlines
Auth: Required

- GET /api/deadlines/upcoming?days=30&clientId=<int?>
  - Returns upcoming deadlines within the specified number of days.
  - Valid `days`: 1..365 (else 400).
  - Response (200):
    {
      "success": true,
      "data": [
        {
          "id": number,
          "taxType": number,
          "taxTypeName": string,
          "dueDate": string,              // ISO date
          "daysRemaining": number,
          "priority": number,             // maps from ComplianceRiskLevel
          "priorityName": string,         // "Low" | "Medium" | "High" | "Critical"
          "status": number,
          "statusName": string,
          "estimatedTaxLiability": number,
          "documentsReady": boolean,
          "isOverdue": boolean,
          "potentialPenalty": number,
          "requirements": string[]
        }
      ],
      "meta": { "count": number, "daysAhead": number, "clientId": number|null }
    }

- GET /api/deadlines/overdue?clientId=<int?>
  - Returns overdue deadlines.
  - Response (200): same shape as /upcoming (with `isOverdue=true` and negative daysRemaining).

- GET /api/deadlines?days=30&clientId=<int?>
  - Returns combined upcoming + overdue deadlines sorted by dueDate.
  - Response meta includes totalCount, upcomingCount, overdueCount.

- GET /api/deadlines/stats?clientId=<int?>
  - Returns aggregate stats used by the frontend (counts and breakdowns).
  - Response (200):
    {
      "success": true,
      "data": {
        "total": number,
        "upcoming": number,
        "dueSoon": number,    // next 7 days
        "overdue": number,
        "thisWeek": number,
        "thisMonth": number,
        "byType": { [key: string]: number },        // e.g., { "vat": 3, "payee": 2 }
        "byPriority": { [key: string]: number }     // e.g., { "low": 1, "medium": 3, "high": 2 }
      }
    }

Notes:
- These endpoints are backed by IDeadlineMonitoringService.
- Responses are already camelCase and wrapped in `{ success, data }`.

---

### 3) Auth and Common

- All routes require valid JWT token unless explicitly marked [AllowAnonymous].
- Use Authorization: Bearer <token> in requests.

---

## Integration Test Endpoints (Automation)

Controller: IntegrationTestController
Route prefix: /api/integrationtest

- GET /api/integrationtest/test-all
  - Runs a suite of health checks across reporting, SignalR hubs, payment gateways, compliance, and communication services.

- GET /api/integrationtest/test-api-compatibility
  - Lists key endpoints expected by the frontend with implementation status.

- GET /api/integrationtest/test-dashboard-deadlines
  - NEW: Verifies dashboard data (including metrics) and deadlines service.
  - Response (200):
    {
      "success": boolean,
      "data": {
        "Dashboard": { "Status": "Success"|"Error", "HasMetrics": boolean, ... },
        "UpcomingDeadlines": { "Status": "Success"|"Error", "Count": number },
        "OverdueDeadlines": { "Status": "Success"|"Error", "Count": number }
      },
      "meta": { "successCount": number, "total": number, "testedAt": string }
    }

How to run locally (Windows PowerShell):
1. Backend
   - cd Betts/BettsTax/BettsTax.Web
   - dotnet run
   - Swagger UI: http://localhost:5001/swagger
2. Frontend
   - cd Betts/sierra-leone-ctis
   - npm install
   - $env:NEXT_PUBLIC_API_URL="http://localhost:5001"
   - npm run dev
3. Quick curl tests (replace <TOKEN>)
   - curl -H "Authorization: Bearer <TOKEN>" http://localhost:5001/api/dashboard
   - curl -H "Authorization: Bearer <TOKEN>" http://localhost:5001/api/deadlines/upcoming?days=30
   - curl -H "Authorization: Bearer <TOKEN>" http://localhost:5001/api/deadlines/overdue
   - curl -H "Authorization: Bearer <TOKEN>" http://localhost:5001/api/deadlines/stats
   - curl -H "Authorization: Bearer <TOKEN>" http://localhost:5001/api/integrationtest/test-dashboard-deadlines

---

## Notes on Data and Compatibility

- JSON casing is camelCase globally (verified in Program.cs AddJsonOptions).
- Dashboard metrics are present as `data.metrics` in /api/dashboard response.
- Deadlines DTOs map cleanly to the frontend types; category/title mapping may still be adjusted in the frontend mapping layer if required by UI.
- If seeded data is empty, upcoming/overdue counts may be 0; use existing seeders or create sample deadlines via IDeadlineMonitoringService.CreateDeadlineAlertAsync.

## Error Responses

- 400 BadRequest: `{ success: false, message: string }`
- 401 Unauthorized: no/invalid token
- 500 InternalServerError: `{ success: false, message: "An error occurred ..." }`

