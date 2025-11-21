# Compliance Score Consolidation Report

**Date:** December 2024  
**Scope:** Analysis and consolidation plan for multiple compliance score calculation implementations  
**Status:** IN PROGRESS

---

## Executive Summary

The system currently has **5 different compliance score calculation implementations** across multiple services. This creates inconsistency, potential confusion for users, and maintenance challenges. This report identifies all implementations, analyzes their differences, and provides a consolidation plan.

**Overall Status:** 🔴 **CRITICAL ISSUE** - Multiple conflicting implementations

---

## Requirements (Finance Act 2025 / Business Requirements)

According to "New Set of Requirements for the Tax Info.md":

### Client KPIs (Client View):
- **Compliance Score:** A cumulative score or traffic light system based on deadlines met
- **My Filing Timeliness:** "Your average filing is 3 days before deadline"
- **On-Time Payments:** % of tax payments made on time
- **Document Readiness Score:** % of tax period documentation submitted

### Compliance Metrics (Visual Tiles):
- **Compliance Score:** Dynamic: green, yellow, red
- **Filing Timeliness:** On-time %
- **Payment Timeliness:** Payment success %
- **Supporting Documents Status:** Completed %, Pending %, Rejected %
- **Deadline Adherence History:** Month-by-month breakdown

### Weighted Calculation (from requirements analysis):
Expected formula: **Filing 40% + Payment 40% + Timeliness 20%**

---

## Current Implementations Analysis

### 1. ComplianceTrackerService.UpdateComplianceScoreAsync

**Location:** `BettsTax/BettsTax.Core/Services/ComplianceTrackerService.cs` (lines 519-557)

**Method Signature:**
```csharp
private async Task UpdateComplianceScoreAsync(ComplianceTracker tracker)
```

**Formula:**
- Starts at 100
- Deducts: `DaysOverdueForFiling * 2` (max 30 points)
- Deducts: `DaysOverdueForPayment * 1.5` (max 30 points)
- Deducts: `OutstandingPenalties / 1000` (max 20 points)
- Deducts: 10 points if documentation incomplete

**Final Score:** `100 - deductions` (minimum 0)

**Usage:**
- Private method, updates `ComplianceTracker` entity
- Called internally when tracker is updated

**Issues:**
- ❌ Not weighted (not 40/40/20)
- ❌ Deduction-based (subtracts from 100)
- ⚠️ Only updates ComplianceTracker entity

---

### 2. DashboardService.CalculateComplianceScore

**Location:** `BettsTax/BettsTax.Core/Services/DashboardService.cs` (lines 508-525)

**Method Signature:**
```csharp
private decimal CalculateComplianceScore(IEnumerable<TaxYear> taxYears, IEnumerable<Payment> payments)
```

**Formula:**
- `filingScore = (completedFilings / totalFilings) * 100`
- `latePenalty = lateFilings * 10`
- `finalScore = filingScore - latePenalty` (clamped 0-100)

**Usage:**
- Private method for dashboard calculations
- Used internally in DashboardService

**Issues:**
- ❌ Very simple (only considers filings, not payments or timeliness)
- ❌ Not weighted
- ❌ Ignores payment timeliness

---

### 3. ComplianceService.GetClientComplianceSummaryAsync

**Location:** `BettsTax/BettsTax.Core/Services/ComplianceService.cs` (line 79)

**Method Signature:**
```csharp
public async Task<ComplianceStatusSummaryDto> GetClientComplianceSummaryAsync(int clientId)
```

**Formula:**
- `totalItems = filings.Count + payments.Count + documents.Count`
- `compliantItems = filedFilings + approvedPayments + taxReturnDocuments`
- `complianceScore = (compliantItems / totalItems) * 100`

**Usage:**
- Public API method
- Called by: `ComplianceController.GetComplianceScorecard` (line 136)

**Issues:**
- ❌ Very simple ratio (counts compliant items, doesn't consider timeliness)
- ❌ Not weighted
- ❌ Doesn't differentiate between on-time and late items

---

### 4. ReportTemplateService.CalculateComplianceScore

**Location:** `BettsTax/BettsTax.Core/Services/ReportTemplateService.cs` (lines 795-804)

**Method Signature:**
```csharp
private static decimal CalculateComplianceScore(
    int totalFilings, int onTimeFilings, 
    int totalPayments, int onTimePayments, 
    decimal totalPenalties)
```

**Formula:**
- `filingScore = (onTimeFilings / totalFilings) * 50`
- `paymentScore = (onTimePayments / totalPayments) * 50`
- `penaltyDeduction = min(totalPenalties / 1000 * 10, 20)`
- `score = filingScore + paymentScore - penaltyDeduction` (minimum 0)

**Usage:**
- Private static method for report generation
- Used in report templates

**Issues:**
- ✅ Closest to requirements (weighted: 50/50)
- ❌ Missing timeliness component (should be 40/40/20, not 50/50)
- ❌ No consideration of document readiness

---

### 5. TaxCalculationEngineService.CalculateComplianceScoreAsync

**Location:** `BettsTax/BettsTax.Core/Services/TaxCalculationEngineService.cs` (lines 981-1085)

**Method Signature:**
```csharp
public async Task<TaxComplianceScoreDto> CalculateComplianceScoreAsync(int clientId, int taxYear)
```

**Formula:**
- Starts at 100
- Deducts: `lateFilings * 10`
- Deducts: `latePayments * 15`
- Deducts: `auditIssues * 20`
- Deducts: 10 if insufficient documents

**Final Score:** `100 - deductions` (minimum 0)

**Usage:**
- **Public API method**
- **Called by:** `TaxCalculationEngineController.GetComplianceScore` (line 394)
- **Frontend calls:** `/api/taxcalculationengine/compliance/score/{clientId}/{taxYear}`

**Issues:**
- ❌ Deduction-based (not weighted)
- ❌ Not 40/40/20 formula
- ⚠️ Includes audit issues and document count (more comprehensive)

---

## Comparison Table

| Implementation | Formula Type | Filing Component | Payment Component | Timeliness | Weighted | Used By |
|---------------|--------------|------------------|-------------------|------------|----------|---------|
| **ComplianceTrackerService** | Deduction | Days overdue * 2 | Days overdue * 1.5 | Partial | ❌ | Internal (ComplianceTracker) |
| **DashboardService** | Ratio | completed/total * 100 | ❌ None | ❌ None | ❌ | Internal (Dashboard) |
| **ComplianceService** | Ratio | filed/total | approved/total | ❌ None | ❌ | **API (ComplianceController)** |
| **ReportTemplateService** | Weighted | onTime/total * 50 | onTime/total * 50 | ❌ None | ⚠️ Partial (50/50) | Internal (Reports) |
| **TaxCalculationEngineService** | Deduction | lateFilings * 10 | latePayments * 15 | Partial | ❌ | **API (TaxCalculationEngineController)** |

---

## API Usage Analysis

### Frontend API Calls

1. **`/api/compliance/score?clientId={id}`**
   - Calls: `ComplianceService` (via ComplianceController)
   - Used by: Frontend `ComplianceService.getComplianceScore` (TypeScript)
   - Formula: Simple ratio

2. **`/api/taxcalculationengine/compliance/score/{clientId}/{taxYear}`**
   - Calls: `TaxCalculationEngineService.CalculateComplianceScoreAsync`
   - Used by: Frontend `TaxCalculationService.getComplianceScore` (TypeScript)
   - Formula: Deduction-based

**Problem:** Two different APIs return different scores for the same client!

---

## Required Formula (Based on Requirements)

According to business requirements analysis:

```
Compliance Score = (Filing Score × 40%) + (Payment Score × 40%) + (Timeliness Score × 20%)

Where:
- Filing Score = (On-Time Filings / Total Required Filings) × 100
- Payment Score = (On-Time Payments / Total Required Payments) × 100
- Timeliness Score = (Average Days Before Deadline) / (Max Expected Days) × 100
  - OR simpler: % of obligations completed on-time
```

---

## Consolidation Plan

### Phase 1: Create Single Authoritative Service

**New Service:** `IComplianceScoreService` / `ComplianceScoreService`

**Location:** `BettsTax/BettsTax.Core/Services/ComplianceScoreService.cs`

**Method:**
```csharp
public async Task<ComplianceScoreResult> CalculateComplianceScoreAsync(
    int clientId, 
    int? taxYear = null)
{
    // 1. Get filing metrics
    var filingMetrics = await GetFilingMetricsAsync(clientId, taxYear);
    
    // 2. Get payment metrics
    var paymentMetrics = await GetPaymentMetricsAsync(clientId, taxYear);
    
    // 3. Get timeliness metrics
    var timelinessMetrics = await GetTimelinessMetricsAsync(clientId, taxYear);
    
    // 4. Calculate weighted score (40/40/20)
    var filingScore = (filingMetrics.OnTimeCount / filingMetrics.TotalRequired) * 100;
    var paymentScore = (paymentMetrics.OnTimeCount / paymentMetrics.TotalRequired) * 100;
    var timelinessScore = CalculateTimelinessScore(filingMetrics, paymentMetrics);
    
    var overallScore = (filingScore * 0.4m) + (paymentScore * 0.4m) + (timelinessScore * 0.2m);
    
    // 5. Apply penalty adjustments
    var penaltyAdjustment = CalculatePenaltyAdjustment(clientId, taxYear);
    overallScore = Math.Max(0, overallScore - penaltyAdjustment);
    
    // 6. Determine level and color
    var level = DetermineComplianceLevel(overallScore);
    var color = GetComplianceColor(level);
    
    return new ComplianceScoreResult
    {
        OverallScore = overallScore,
        FilingScore = filingScore,
        PaymentScore = paymentScore,
        TimelinessScore = timelinessScore,
        Level = level,
        Color = color,
        // ... additional details
    };
}
```

### Phase 2: Update All Callers

1. **ComplianceController:**
   - Replace `ComplianceService.GetClientComplianceSummaryAsync` call
   - Use new `IComplianceScoreService.CalculateComplianceScoreAsync`

2. **TaxCalculationEngineController:**
   - Replace `TaxCalculationEngineService.CalculateComplianceScoreAsync` call
   - Use new `IComplianceScoreService.CalculateComplianceScoreAsync`

3. **DashboardService:**
   - Replace private `CalculateComplianceScore` method
   - Call new `IComplianceScoreService.CalculateComplianceScoreAsync`

4. **ReportTemplateService:**
   - Replace private static `CalculateComplianceScore` method
   - Call new `IComplianceScoreService.CalculateComplianceScoreAsync`

5. **ComplianceTrackerService:**
   - Keep `UpdateComplianceScoreAsync` but have it call new service
   - Or: Update ComplianceTracker entity directly but use same formula

### Phase 3: Deprecate Old Methods

1. Mark old methods as `[Obsolete]`
2. Add redirect comments pointing to new service
3. Update all internal callers
4. Remove after 1 release cycle

### Phase 4: API Consolidation

**Option A:** Single API endpoint
- `GET /api/compliance/score/{clientId}/{taxYear?}`
- Consolidate both existing endpoints

**Option B:** Keep both but make them call same service
- `/api/compliance/score` → `ComplianceScoreService`
- `/api/taxcalculationengine/compliance/score` → `ComplianceScoreService`
- Both return same results

---

## Recommended Formula Implementation

```csharp
public async Task<ComplianceScoreResult> CalculateComplianceScoreAsync(int clientId, int? taxYear)
{
    var filings = await GetFilingsAsync(clientId, taxYear);
    var payments = await GetPaymentsAsync(clientId, taxYear);
    
    // Filing Component (40%)
    var totalRequiredFilings = CalculateRequiredFilings(clientId, taxYear);
    var onTimeFilings = filings.Count(f => f.Status == FilingStatus.Filed && 
                                          f.FilingDate <= f.DueDate);
    var filingScore = totalRequiredFilings > 0 
        ? (onTimeFilings / (decimal)totalRequiredFilings) * 100 
        : 100m;
    
    // Payment Component (40%)
    var totalRequiredPayments = payments.Count(p => p.TaxFilingId != null || p.DueDate.HasValue);
    var onTimePayments = payments.Count(p => p.Status == PaymentStatus.Approved && 
                                           p.PaymentDate <= p.DueDate);
    var paymentScore = totalRequiredPayments > 0 
        ? (onTimePayments / (decimal)totalRequiredPayments) * 100 
        : 100m;
    
    // Timeliness Component (20%)
    var averageDaysBeforeDeadline = CalculateAverageDaysBeforeDeadline(filings, payments);
    var timelinessScore = CalculateTimelinessScore(averageDaysBeforeDeadline);
    
    // Weighted Calculation
    var overallScore = (filingScore * 0.4m) + 
                       (paymentScore * 0.4m) + 
                       (timelinessScore * 0.2m);
    
    // Apply penalties (deduct, but don't let go below 0)
    var penalties = await GetPenaltiesAsync(clientId, taxYear);
    var penaltyDeduction = Math.Min(penalties.Sum(p => p.Amount) / 1000m * 5, 20m);
    overallScore = Math.Max(0, overallScore - penaltyDeduction);
    
    // Determine compliance level
    var level = overallScore switch
    {
        >= 90m => ComplianceLevel.Excellent,
        >= 80m => ComplianceLevel.Good,
        >= 70m => ComplianceLevel.Satisfactory,
        >= 60m => ComplianceLevel.Poor,
        _ => ComplianceLevel.VeryPoor
    };
    
    // Determine color (traffic light)
    var color = overallScore switch
    {
        >= 80m => ComplianceColor.Green,
        >= 60m => ComplianceColor.Yellow,
        _ => ComplianceColor.Red
    };
    
    return new ComplianceScoreResult
    {
        OverallScore = Math.Round(overallScore, 2),
        FilingScore = Math.Round(filingScore, 2),
        PaymentScore = Math.Round(paymentScore, 2),
        TimelinessScore = Math.Round(timelinessScore, 2),
        Level = level,
        Color = color,
        OnTimeFilings = onTimeFilings,
        TotalRequiredFilings = totalRequiredFilings,
        OnTimePayments = onTimePayments,
        TotalRequiredPayments = totalRequiredPayments,
        PenaltyAmount = penalties.Sum(p => p.Amount),
        CalculatedAt = DateTime.UtcNow
    };
}
```

---

## Migration Steps

### Step 1: Create New Service
- [ ] Create `IComplianceScoreService` interface
- [ ] Create `ComplianceScoreService` implementation
- [ ] Implement weighted calculation (40/40/20)
- [ ] Add unit tests

### Step 2: Register Service
- [ ] Add to DI container in `Program.cs`
- [ ] Verify service registration

### Step 3: Update Controllers
- [ ] Update `ComplianceController` to use new service
- [ ] Update `TaxCalculationEngineController` to use new service
- [ ] Test API endpoints

### Step 4: Update Internal Services
- [ ] Update `DashboardService` to use new service
- [ ] Update `ReportTemplateService` to use new service
- [ ] Update `ComplianceTrackerService` to use new service (or keep entity-specific logic)

### Step 5: Deprecate Old Methods
- [ ] Mark old methods as `[Obsolete]`
- [ ] Add migration comments
- [ ] Update documentation

### Step 6: Testing
- [ ] Unit tests for new service
- [ ] Integration tests for API endpoints
- [ ] Verify consistency across all callers
- [ ] Compare old vs new scores (document differences)

### Step 7: Cleanup
- [ ] Remove deprecated methods after 1 release cycle
- [ ] Update frontend if needed (should work with same API format)

---

## Expected Outcomes

1. ✅ **Single Source of Truth:** One authoritative calculation method
2. ✅ **Consistent Results:** Same score returned everywhere
3. ✅ **Requirements Compliance:** Weighted 40/40/20 formula
4. ✅ **Maintainability:** Easier to update calculation logic
5. ✅ **Testability:** Single method to test
6. ✅ **Performance:** Can cache results if needed

---

## Risk Assessment

**Low Risk:**
- Creating new service (additive change)
- Testing alongside existing code

**Medium Risk:**
- API changes might affect frontend (but should be backward compatible)
- Score differences need documentation

**Mitigation:**
- Implement alongside existing code
- Gradual migration with deprecation markers
- Comprehensive testing before removal

---

**Report Generated:** December 2024  
**Next Steps:** Create implementation plan and begin Phase 1

