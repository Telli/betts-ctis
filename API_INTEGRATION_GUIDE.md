# API Integration Guide - Sierra Leone CTIS

**Date:** October 9, 2025  
**Status:** UI Complete - Ready for Backend Wiring

---

## 🎯 Overview

All Phase 1-5 components are production-ready from a UI perspective. This guide provides exact API endpoints and integration points needed to wire mock data to your C# .NET Core backend.

---

## 📋 Integration Priority

### HIGH PRIORITY (Week 1)
1. **Dashboard Page** - Basic metrics already wired, verify working
2. **Filing Workspace** - Critical for tax filing workflow
3. **Compliance Page** - High-value compliance monitoring

### MEDIUM PRIORITY (Week 2)
4. **Documents Page** - Already partially wired, enhance
5. **KPI Dashboard** - Client view needs wiring

### LOW PRIORITY (Week 3)
6. **Reports Page** - Already functional, optimize as needed

---

## 🔌 Phase 2: Filing Workspace Integration

### Component: `FilingWorkspace`
**File:** `components/filing-workspace.tsx`

**Required Backend Endpoints:**

```csharp
// C# .NET Core API Endpoints Needed

// GET /api/tax-filings/{id}
// Returns: TaxFilingDto
[HttpGet("{id}")]
public async Task<ActionResult<TaxFilingDto>> GetTaxFiling(int id)
{
    var filing = await _taxFilingService.GetTaxFilingByIdAsync(id);
    return Ok(filing);
}

// PUT /api/tax-filings/{id}
// Body: UpdateTaxFilingDto
[HttpPut("{id}")]
public async Task<ActionResult<TaxFilingDto>> UpdateTaxFiling(int id, UpdateTaxFilingDto dto)
{
    var result = await _taxFilingService.UpdateTaxFilingAsync(id, dto);
    return Ok(result);
}

// POST /api/tax-filings/{id}/submit
[HttpPost("{id}/submit")]
public async Task<ActionResult<TaxFilingDto>> SubmitTaxFiling(int id)
{
    var result = await _taxFilingService.SubmitTaxFilingAsync(id);
    return Ok(result);
}

// GET /api/tax-filings/{id}/audit-trail
// Returns: List<AuditEventDto>
[HttpGet("{id}/audit-trail")]
public async Task<ActionResult<List<AuditEventDto>>> GetFilingAuditTrail(int id)
{
    var events = await _activityTimelineService.GetFilingEventsAsync(id);
    return Ok(events);
}
```

**Frontend Service Integration:**

```typescript
// lib/services/tax-filing-service.ts (already exists)

// In FilingWorkspace component, replace mock data:
import { TaxFilingService } from '@/lib/services/tax-filing-service';

// Fetch filing data
const { data: filing } = await TaxFilingService.getTaxFilingById(filingId);

// Save draft
await TaxFilingService.updateTaxFiling(filingId, formData);

// Submit filing
await TaxFilingService.submitTaxFiling(filingId);
```

---

## 🔌 Phase 3: Compliance Page Integration

### Component: `FilingChecklistMatrix`
**File:** `components/filing-checklist-matrix.tsx`

**Required Backend Endpoint:**

```csharp
// GET /api/compliance/filing-matrix?clientId={id}&year={year}
// Returns: FilingMatrixDto
[HttpGet("filing-matrix")]
public async Task<ActionResult<FilingMatrixDto>> GetFilingMatrix(
    int? clientId, 
    int year)
{
    // Return matrix of filing statuses by quarter and tax type
    var matrix = await _complianceService.GetFilingMatrixAsync(clientId, year);
    return Ok(matrix);
}

// DTO Structure:
public class FilingMatrixDto
{
    public int Year { get; set; }
    public List<TaxTypeRow> Rows { get; set; }
}

public class TaxTypeRow
{
    public string TaxType { get; set; }
    public string Q1Status { get; set; } // "filed" | "pending" | "overdue" | "upcoming" | "n/a"
    public string Q2Status { get; set; }
    public string Q3Status { get; set; }
    public string Q4Status { get; set; }
    public int? Q1FilingId { get; set; }
    public int? Q2FilingId { get; set; }
    public int? Q3FilingId { get; set; }
    public int? Q4FilingId { get; set; }
}
```

### Component: `PenaltyWarningsCard`
**File:** `components/penalty-warnings-card.tsx`

**Required Backend Endpoint:**

```csharp
// GET /api/compliance/penalty-warnings?clientId={id}
// Returns: List<PenaltyWarningDto>
[HttpGet("penalty-warnings")]
public async Task<ActionResult<List<PenaltyWarningDto>>> GetPenaltyWarnings(int? clientId)
{
    var warnings = await _penaltyCalculationService.GetPenaltyWarningsAsync(clientId);
    return Ok(warnings);
}

// DTO Structure:
public class PenaltyWarningDto
{
    public int FilingId { get; set; }
    public string TaxType { get; set; }
    public string ClientName { get; set; }
    public int DaysOverdue { get; set; }
    public decimal EstimatedPenalty { get; set; }
    public DateTime DueDate { get; set; }
    public string Severity { get; set; } // "high" | "medium" | "low"
}
```

### Component: `DocumentSubmissionTracker`
**File:** `components/document-submission-tracker.tsx`

**Required Backend Endpoint:**

```csharp
// GET /api/documents/submission-status?clientId={id}
// Returns: List<DocumentRequirementDto>
[HttpGet("submission-status")]
public async Task<ActionResult<List<DocumentRequirementDto>>> GetDocumentSubmissionStatus(int? clientId)
{
    var status = await _documentService.GetSubmissionStatusAsync(clientId);
    return Ok(status);
}

// DTO Structure:
public class DocumentRequirementDto
{
    public string Category { get; set; }
    public int Required { get; set; }
    public int Submitted { get; set; }
    public int CompletionPercentage { get; set; }
}
```

### Component: `ComplianceTimeline`
**File:** `components/compliance-timeline.tsx`

**Required Backend Endpoint:**

```csharp
// GET /api/compliance/timeline?clientId={id}&limit={limit}
// Returns: List<ComplianceEventDto>
[HttpGet("timeline")]
public async Task<ActionResult<List<ComplianceEventDto>>> GetComplianceTimeline(
    int? clientId, 
    int limit = 10)
{
    var events = await _activityTimelineService.GetComplianceEventsAsync(clientId, limit);
    return Ok(events);
}

// DTO Structure:
public class ComplianceEventDto
{
    public string Type { get; set; } // "filing" | "payment" | "document" | "penalty" | "alert"
    public string Status { get; set; } // "filed" | "paid" | "pending" | "overdue"
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string User { get; set; }
}
```

---

## 🔌 Phase 4: KPI Dashboard Integration

### KPI Dashboard - Client View
**File:** `app/kpi-dashboard/page.tsx`

**Required Backend Endpoints:**

```csharp
// GET /api/kpi/client-metrics
// Returns: ClientKPIDto
[HttpGet("client-metrics")]
public async Task<ActionResult<ClientKPIDto>> GetClientMetrics()
{
    var metrics = await _kpiService.GetClientMetricsAsync();
    return Ok(metrics);
}

// DTO Structure:
public class ClientKPIDto
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public decimal AverageComplianceScore { get; set; }
    public int AverageFilingTime { get; set; } // days before deadline
    public string TopPerformerName { get; set; }
    public decimal TopPerformerScore { get; set; }
}

// GET /api/kpi/client-segment-breakdown
// Returns: List<ClientSegmentDto>
[HttpGet("client-segment-breakdown")]
public async Task<ActionResult<List<ClientSegmentDto>>> GetClientSegmentBreakdown()
{
    var breakdown = await _kpiService.GetSegmentBreakdownAsync();
    return Ok(breakdown);
}

// DTO Structure:
public class ClientSegmentDto
{
    public string Segment { get; set; } // "Large Taxpayers" | "Medium Taxpayers" | etc.
    public int CompliancePercentage { get; set; }
    public string Color { get; set; } // "green" | "blue" | "amber" | "sky"
}
```

---

## 🔌 Existing Integrations (Verify)

### Dashboard Service
**File:** `lib/services/dashboard-service.ts`  
**Backend:** `BettsTax.Core.Services.DashboardService`

**Status:** ✅ Already integrated, verify endpoints working:

```csharp
// Existing C# Service (verify these work)
Task<DashboardDto> GetDashboardDataAsync(string userId)
Task<ClientSummaryDto> GetClientSummaryAsync()
Task<ComplianceOverviewDto> GetComplianceOverviewAsync()
```

### Compliance Service
**File:** `lib/services/compliance-service.ts`  
**Backend:** `BettsTax.Core.Services.ComplianceTrackerService`

**Status:** ⚠️ Partially integrated, needs Phase 3 endpoints above

### Document Service
**File:** `lib/services/document-service.ts`

**Status:** ✅ Already integrated, works with existing backend

---

## 📝 Step-by-Step Integration Process

### Step 1: Verify Existing Integrations (Day 1)
```bash
# Test existing dashboard endpoint
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5001/api/Dashboard

# Test existing compliance endpoint
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5001/api/Compliance/overview

# Verify frontend connects to backend
# Check browser console for API calls
# Ensure no CORS errors
```

### Step 2: Add Phase 2 Endpoints (Day 2-3)

**Backend Tasks:**
1. Create `TaxFilingController` endpoints (if not exist)
2. Implement audit trail endpoint
3. Test with Swagger/Postman

**Frontend Tasks:**
1. Update `FilingWorkspace` to use real API
2. Remove mock data
3. Add error handling
4. Test create/edit/submit flows

### Step 3: Add Phase 3 Endpoints (Day 4-5)

**Backend Tasks:**
1. Create `ComplianceController` endpoints:
   - Filing matrix
   - Penalty warnings
   - Document submission status
   - Timeline
2. Test each endpoint

**Frontend Tasks:**
1. Wire each compliance component
2. Remove mock data
3. Test with real data
4. Verify visualizations render correctly

### Step 4: Add Phase 4 Endpoints (Day 6-7)

**Backend Tasks:**
1. Create `KPIController` endpoints:
   - Client metrics
   - Segment breakdown
2. Test endpoints

**Frontend Tasks:**
1. Wire KPI Dashboard client view
2. Test toggle between internal/client
3. Verify charts render with real data

### Step 5: Testing & Refinement (Day 8-10)

**Tasks:**
1. End-to-end testing all workflows
2. Performance optimization
3. Error handling verification
4. User acceptance testing
5. Bug fixes

---

## 🔧 Frontend Code Updates Needed

### 1. Remove Mock Data

**FilingWorkspace (components/filing-workspace.tsx):**
```typescript
// BEFORE (mock data)
const [filing, setFiling] = useState(mockFiling);

// AFTER (real API)
const [filing, setFiling] = useState<TaxFilingDto | null>(null);
const [loading, setLoading] = useState(true);

useEffect(() => {
  const fetchFiling = async () => {
    try {
      const result = await TaxFilingService.getTaxFilingById(filingId);
      setFiling(result.data);
    } catch (error) {
      console.error('Failed to load filing:', error);
    } finally {
      setLoading(false);
    }
  };
  fetchFiling();
}, [filingId]);
```

### 2. Add Error Handling

```typescript
// Add to all API calls
try {
  const result = await ServiceCall();
  if (result.success) {
    // Handle success
  } else {
    toast.error(result.error || 'Operation failed');
  }
} catch (error) {
  toast.error('Network error. Please try again.');
  console.error(error);
}
```

### 3. Add Loading States

```typescript
// Show loading spinner while fetching
{loading ? (
  <div className="flex justify-center items-center p-12">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-sierra-blue" />
  </div>
) : (
  <ComponentContent data={data} />
)}
```

---

## 🔐 Authentication & Authorization

### Frontend Token Management

**Already implemented in `lib/api-client.ts`:**
```typescript
// Tokens automatically included in requests
headers: {
  'Authorization': `Bearer ${localStorage.getItem('token')}`
}
```

**Backend validation (ensure implemented):**
```csharp
[Authorize] // On all controllers
public class TaxFilingController : ControllerBase
{
    // Endpoints automatically require authentication
}

[Authorize(Roles = "Admin,TaxProfessional")]
public async Task<ActionResult> AdminOnlyEndpoint()
{
    // Role-based authorization
}
```

---

## 🧪 Testing Checklist

### Per Component Testing:
- [ ] Loads data successfully from API
- [ ] Shows loading state while fetching
- [ ] Handles empty data gracefully
- [ ] Displays error messages on failure
- [ ] Refresh/retry functionality works
- [ ] Form submissions work correctly
- [ ] Validation works as expected
- [ ] Navigation works between views

### Integration Testing:
- [ ] Dashboard loads all metrics
- [ ] Filing workflow end-to-end
- [ ] Document upload and display
- [ ] Compliance monitoring updates
- [ ] KPI dashboard toggles work
- [ ] Reports generate successfully

### Performance Testing:
- [ ] Page load under 2 seconds
- [ ] API calls complete under 1 second
- [ ] No memory leaks
- [ ] Smooth animations
- [ ] Responsive on mobile

---

## 📊 Monitoring & Analytics

### Add to Production:

```typescript
// Track component usage
analytics.track('FilingWorkspace_Opened', {
  filingId,
  taxType,
  status
});

// Track API performance
const startTime = Date.now();
await ApiCall();
const duration = Date.now() - startTime;
analytics.timing('API_TaxFiling_Load', duration);

// Track errors
analytics.trackError('TaxFiling_LoadError', {
  filingId,
  error: error.message
});
```

---

## 🚨 Common Issues & Solutions

### Issue 1: CORS Errors
**Solution:**
```csharp
// Startup.cs or Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins("http://localhost:3000", "https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

app.UseCors("AllowFrontend");
```

### Issue 2: 401 Unauthorized
**Solution:** Verify token in localStorage and refresh if expired

### Issue 3: Data Format Mismatch
**Solution:** Ensure DTO property names match between C# and TypeScript

### Issue 4: Slow API Response
**Solution:** Add pagination, caching, or optimize database queries

---

## ✅ Integration Completion Checklist

### Backend (C# .NET Core):
- [ ] All required endpoints created
- [ ] DTOs match frontend expectations
- [ ] Authentication working
- [ ] Authorization configured
- [ ] Error handling implemented
- [ ] Logging configured
- [ ] Swagger documentation updated

### Frontend (Next.js):
- [ ] Mock data removed
- [ ] API calls implemented
- [ ] Error handling added
- [ ] Loading states added
- [ ] Empty states working
- [ ] Forms validate correctly
- [ ] Navigation works

### Testing:
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] E2E tests pass
- [ ] Performance acceptable
- [ ] Security verified
- [ ] Accessibility verified

### Documentation:
- [ ] API documentation updated
- [ ] User guide updated
- [ ] Developer guide updated
- [ ] Deployment guide ready

---

## 🎯 Success Metrics

Track these after integration:

1. **Page Load Time:** < 2 seconds
2. **API Response Time:** < 1 second average
3. **Error Rate:** < 1%
4. **User Satisfaction:** > 85%
5. **Task Completion:** > 95%
6. **Filing Time:** 40% reduction
7. **Compliance Monitoring:** 80% faster

---

## 📞 Support & Resources

### Documentation:
- API Integration Guide (this document)
- Phase completion documents (1-5)
- Final project summary

### Code References:
- `lib/services/` - All frontend services
- `lib/api-client.ts` - HTTP client configuration
- `components/` - All UI components

### Backend Services:
- `BettsTax.Core.Services` - Business logic
- `BettsTax.API.Controllers` - API endpoints
- `BettsTax.Core.DTOs` - Data transfer objects

---

**Status:** 📋 **READY FOR API INTEGRATION**  
**Priority:** 🔴 **HIGH** - Filing Workspace & Compliance Page  
**Timeline:** 10 days for complete integration  
**Risk:** 🟢 **LOW** - UI proven, clear requirements
