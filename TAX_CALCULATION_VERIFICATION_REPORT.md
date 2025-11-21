# Tax Calculation Verification Report

**Date:** December 2024  
**Scope:** Verification of Tax Calculation Logic against Finance Act 2025 Requirements  
**Status:** IN PROGRESS

---

## Executive Summary

This report documents the verification of tax calculation implementations against Sierra Leone Finance Act 2025 requirements. Initial review has identified several issues that require correction.

**Overall Status:** ⚠️ **PARTIALLY COMPLIANT** - Issues Found

---

## 1. Tax Type Support Verification

### Requirements
All 7 tax types must be supported:
1. PAYE (Pay As You Earn Tax)
2. WHT (Withholding Tax)
3. PIT (Personal Income Tax)
4. CIT (Corporate Income Tax)
5. GST (Goods and Services Tax)
6. Excise Tax
7. Payroll Tax

### Implementation Status
**File:** `BettsTax/BettsTax.Data/TaxFiling.cs`

```csharp
public enum TaxType { 
    IncomeTax, GST, PayrollTax, ExciseDuty, 
    PAYE, WithholdingTax, PersonalIncomeTax, CorporateIncomeTax 
}
```

**Verification Result:** ✅ **COMPLIANT**
- All 7 required tax types are present
- Additional types (IncomeTax, ExciseDuty) are also supported as umbrella categories

---

## 2. Taxpayer Category Thresholds

### Requirements
- **Large:** > 6,000,000 SLE
- **Medium:** 500,000 - 6,000,000 SLE
- **Small:** 10,000 - 500,000 SLE
- **Micro:** ≤ 10,000 SLE

### Implementation Status
**Files Found:**
- `BettsTax/BettsTax.Web/Controllers/TaxCalculationEngineController.cs` (lines 466-512)
- Frontend UI files in `sierra-leone-ctis/`

**Issues Found:** ❌ **NON-COMPLIANT**

**Controller Implementation (Incorrect):**
```csharp
Large: 10,000,000,000 SLE (10B)  // Should be > 6,000,000
Medium: 1,000,000,000 SLE (1B)   // Should be 500K - 6M
Small: 100,000,000 SLE (100M)    // Should be 10K - 500K
Micro: 10,000,000 SLE (10M)      // Should be ≤ 10,000
```

**Frontend UI (Incorrect):**
- Various files show different thresholds (2B, 500M-2B, etc.)

**Verification Result:** ❌ **NON-COMPLIANT**

**Action Required:**
1. Update `TaxCalculationEngineController.cs` with correct thresholds
2. Update all frontend UI files with correct thresholds
3. Verify `ClientService` uses correct thresholds when determining category
4. Ensure consistency across all files

**Priority:** 🔴 **HIGH** - Critical business logic error

---

## 3. Currency Display (SLE)

### Requirements
All transactions, reports, and dashboards must display amounts in Sierra Leone Leones (SLE).

### Implementation Status
**File:** `BettsTax/BettsTax.Core/Services/TaxCalculationEngineService.cs`

**Verification Result:** ✅ **COMPLIANT**
- Payment model has `Currency` field defaulting to "SLE" (line 198 in Payment model)
- All calculation services work with SLE amounts

---

## 4. GST Calculation

### Requirements
- Standard rate: **15%**
- Filing deadline: **21 days after period end**
- Calculation: `Output GST - Input GST` at 15%
- Transaction-level schedules required for inputs/outputs
- Diplomatic/ratified agreement entities: block GST at purchase

### Implementation Status
**File:** `BettsTax/BettsTax.Core/Services/TaxCalculationEngineService.cs`
- Lines 149-201: `CalculateGstAsync` method

**Verification Results:**

1. **GST Rate:** ✅ **COMPLIANT**
   - Default rate: 15% (line 216)
   - Configurable via settings: `Tax.GST.RatePercent` (line 219)

2. **GST Calculation Formula:** ✅ **COMPLIANT**
   - `OutputGst = TaxableSupplies * (GstRate / 100)` (line 167)
   - `NetGstLiability = max(0, OutputGst - InputTax)` (line 170)
   - Matches requirement: `Output GST - Input GST`

3. **Transaction-Level Schedules:** ❌ **NOT VERIFIED**
   - Code calculates aggregate amounts
   - **Action Required:** Verify if transaction-level schedules are tracked/stored separately

4. **Diplomatic/Relief Entities:** ❌ **MISSING**
   - No logic found to block GST for diplomatic/ratified agreement entities
   - **Action Required:** Implement entity type check and GST blocking logic

5. **Filing Deadline:** ⚠️ **NEEDS VERIFICATION**
   - Deadline logic should be verified in deadline calculation services

---

## 5. Minimum Alternate Tax (MAT) Calculation

### Requirements (Finance Act 2025)
- **MAT = 2% of revenue** (not profit)
- **Triggers when:** Company records losses for **≥ 2 consecutive years**
- **Applies to:** All companies, including mining/petroleum
- **Final liability:** `max(Base Income Tax, MAT)`

### Implementation Status

**File 1:** `BettsTax/BettsTax.Core/Services/TaxCalculationEngineService.cs`
- Lines 717-722: `CalculateMinimumTaxAsync`

```csharp
private async Task<decimal> CalculateMinimumTaxAsync(decimal grossIncome, string taxpayerCategory, int taxYear)
{
    // Finance Act 2025: Minimum tax for companies
    var minimumTaxRate = taxpayerCategory == "Large" ? 0.005m : 0.0025m; // 0.5% for large, 0.25% for medium
    return grossIncome * minimumTaxRate;
}
```

**Issues Found:**
1. ❌ **INCORRECT RATE:** This is calculating Minimum Tax (0.5%/0.25%), NOT MAT (should be 2%)
2. ❌ **NO LOSS CHECK:** Does not check for "losses ≥2 consecutive years"
3. ❌ **WRONG BASE:** Uses `grossIncome` instead of `revenue`

**File 2:** `BettsTax/BettsTax.Core/Services/SierraLeoneTaxCalculationService.cs`
- Lines 199-208: `CalculateMinimumAlternateTax`

```csharp
public decimal CalculateMinimumAlternateTax(decimal annualTurnover)
{
    // MAT uses configured rate (default 3% of turnover)
    decimal matRate = GetMatRateFraction();
    return Math.Round(annualTurnover * matRate, 2);
}
```

**Issues Found:**
1. ❌ **INCORRECT DEFAULT RATE:** Default is 3%, should be 2% (line 41, line 206)
2. ❌ **NO LOSS CHECK:** Does not check for "losses ≥2 consecutive years" before applying MAT
3. ✅ **CORRECT BASE:** Uses `annualTurnover` (revenue) which is correct

**File 3:** `BettsTax/BettsTax.Web/Controllers/TaxCalculationController.cs`
- Line 198: Default MAT rate is 3%

**Verification Result:** ❌ **NON-COMPLIANT**

### Required Fixes

1. **Update MAT Rate:**
   - Change default from 3% to 2%
   - Update `GetMatRateFraction()` default from 3m to 2m
   - Update controller default from 3m to 2m

2. **Add Loss Consecutive Years Check:**
   ```csharp
   public async Task<decimal?> CalculateMATAsync(decimal revenue, int clientId, int taxYear)
   {
       // Check if company has losses for ≥2 consecutive years
       bool hasLossesForTwoYears = await CheckConsecutiveLossYears(clientId, taxYear);
       
       if (!hasLossesForTwoYears) 
           return null; // MAT does not apply
       
       // MAT = 2% of revenue
       decimal matRate = 0.02m; // 2%
       return revenue * matRate;
   }
   ```

3. **Update Tax Calculation Logic:**
   ```csharp
   // In income tax calculation
   decimal baseTax = CalculateIncomeTax(taxableProfit, category);
   decimal? mat = await CalculateMATAsync(revenue, clientId, taxYear);
   
   decimal finalLiability = mat.HasValue 
       ? Math.Max(baseTax, mat.Value) 
       : baseTax;
   ```

**Priority:** 🔴 **HIGH** - Critical business logic error

---

## 6. Payroll Tax Deadlines

### Requirements
- **Annual payroll return due:** 31 January each year
- **Foreign employee filing due:** 1 month after start date

### Implementation Status
**Status:** ⚠️ **NEEDS VERIFICATION**
- **Action Required:** Review deadline calculation services for Payroll Tax deadlines
- Verify foreign employee special case handling

---

## 7. Excise Duty Deadline

### Requirements
- **Return + payment due:** 21 days after domestic delivery or import

### Implementation Status
**Status:** ⚠️ **NEEDS VERIFICATION**
- **Action Required:** Review deadline calculation services for Excise Duty deadlines

---

## 8. Income Tax Calculation

### Requirements
- Base Income Tax = `Taxable Profit × CIT rate (year)`
- Progressive brackets for individuals
- Corporate flat rate based on taxpayer category

### Implementation Status

**File:** `BettsTax/BettsTax.Core/Services/SierraLeoneTaxCalculationService.cs`
- Lines 46-92: `CalculateIncomeTax` and `CalculateIndividualIncomeTax`

**Verification Results:**

1. **Corporate Income Tax:** ✅ **COMPLIANT**
   - Flat 25% rate for all companies (line 54)

2. **Individual Income Tax:** ✅ **COMPLIANT**
   - Progressive brackets implemented:
     - 0 - 600,000 SLE: 0%
     - 600,001 - 1,200,000 SLE: 15%
     - 1,200,001 - 1,800,000 SLE: 20%
     - 1,800,001 - 2,400,000 SLE: 25%
     - Above 2,400,000 SLE: 30%

3. **Base Tax Calculation:** ✅ **COMPLIANT**
   - Uses taxable income × rate

---

## Summary of Issues

### Critical Issues (Must Fix)
1. ❌ **MAT Rate:** Default is 3%, should be 2%
2. ❌ **MAT Trigger:** Missing check for "losses ≥2 consecutive years"
3. ❌ **MAT Base:** Using wrong calculation in `CalculateMinimumTaxAsync` (should be separate from Minimum Tax)

### High Priority Issues (Should Fix)
4. ⚠️ **Diplomatic/Relief GST Blocking:** Not implemented
5. ⚠️ **Transaction-Level GST Schedules:** Verification needed

### Medium Priority Issues (Verify)
6. ⚠️ **Taxpayer Category Thresholds:** Need to verify implementation
7. ⚠️ **Deadline Calculations:** Need to verify GST (21 days), Payroll (31 Jan), Excise (21 days)

---

## Next Steps

1. ✅ Complete tax calculation verification (this document)
2. ⏳ Fix MAT calculation issues (create task)
3. ⏳ Verify deadline calculations
4. ⏳ Verify taxpayer category thresholds
5. ⏳ Implement diplomatic/relief GST blocking
6. ⏳ Verify transaction-level GST schedule tracking

---

## Test Cases Required

### MAT Calculation Test Cases
1. Company with 2 consecutive loss years → MAT should apply (2% revenue)
2. Company with 1 loss year → MAT should NOT apply
3. Company with 2 loss years but profitable current year → MAT should still apply
4. MAT vs Base Tax: Verify `max(Base Tax, MAT)` logic

### GST Test Cases
1. Standard entity: GST = 15% on taxable supplies
2. Diplomatic entity: GST should be blocked at purchase
3. Export: GST should be zero-rated
4. Transaction-level schedules: Verify each transaction tracked

### Deadline Test Cases
1. GST period ends Jan 31 → Filing due Feb 21 (21 days)
2. Payroll Tax → Due Jan 31 annually
3. Foreign employee starts Jan 15 → Filing due Feb 15 (1 month)
4. Excise delivery Jan 1 → Return due Jan 22 (21 days)

---

**Report Generated:** December 2024  
**Next Review:** After fixes implemented

