using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Core.DTOs.Tax;
using BettsTax.Core.Services.Interfaces;

namespace BettsTax.Core.Services;

/// <summary>
/// Tax Calculation Engine for Sierra Leone Finance Act 2025
/// Implements comprehensive tax calculations, penalty matrix, and rate management
/// Handles Income Tax, GST, Payroll Tax, and Excise Duty calculations
/// </summary>
public class TaxCalculationEngineService : ITaxCalculationEngineService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TaxCalculationEngineService> _logger;
    private readonly ISystemSettingService _settingService;

    public TaxCalculationEngineService(
        ApplicationDbContext context,
        ILogger<TaxCalculationEngineService> logger,
        ISystemSettingService settingService)
    {
        _context = context;
        _logger = logger;
        _settingService = settingService;
    }

    #region Income Tax Calculations

    public async Task<IncomeTaxCalculationDto> CalculateIncomeTaxAsync(IncomeTaxCalculationRequestDto request)
    {
        try
        {
            var taxRates = await GetIncomeTaxRatesAsync(request.TaxYear, request.TaxpayerCategory);
            var allowances = await GetTaxAllowancesAsync(request.TaxYear, request.TaxpayerCategory);
            
            var calculation = new IncomeTaxCalculationDto
            {
                TaxYear = request.TaxYear,
                TaxpayerCategory = request.TaxpayerCategory,
                GrossIncome = request.GrossIncome,
                TaxableIncome = CalculateTaxableIncome(request, allowances),
                TaxBrackets = new List<TaxBracketCalculationDto>()
            };

            // Apply progressive tax brackets
            decimal remainingIncome = calculation.TaxableIncome;
            decimal totalTax = 0;

            foreach (var rate in taxRates.OrderBy(r => r.MinIncome))
            {
                if (remainingIncome <= 0) break;

                var bracketIncome = Math.Min(remainingIncome, 
                    rate.MaxIncome.HasValue ? rate.MaxIncome.Value - rate.MinIncome : remainingIncome);
                
                var bracketTax = bracketIncome * (rate.Rate / 100);
                totalTax += bracketTax;

                calculation.TaxBrackets.Add(new TaxBracketCalculationDto
                {
                    MinIncome = rate.MinIncome,
                    MaxIncome = rate.MaxIncome,
                    Rate = rate.Rate,
                    TaxableAmount = bracketIncome,
                    TaxAmount = bracketTax
                });

                remainingIncome -= bracketIncome;
            }

            calculation.IncomeTaxDue = totalTax;
            
            // Apply minimum tax for companies (Finance Act 2025)
            if (request.TaxpayerCategory == "Large" || request.TaxpayerCategory == "Medium")
            {
                var minimumTax = await CalculateMinimumTaxAsync(request.GrossIncome, request.TaxpayerCategory, request.TaxYear);
                calculation.MinimumTax = minimumTax;
                calculation.IncomeTaxDue = Math.Max(calculation.IncomeTaxDue, minimumTax);
            }

            // Calculate penalties if payment is late
            if (request.PaymentDate.HasValue && request.DueDate.HasValue && request.PaymentDate > request.DueDate)
            {
                calculation.Penalties = await CalculateLatePenaltiesAsync(
                    calculation.IncomeTaxDue, 
                    request.DueDate.Value, 
                    request.PaymentDate.Value,
                    "Income Tax");
            }

            calculation.TotalAmountDue = calculation.IncomeTaxDue + calculation.Penalties.TotalPenalty;

            _logger.LogInformation("Income tax calculated: {TaxableIncome} SLE -> {IncomeTax} SLE tax due", 
                calculation.TaxableIncome, calculation.IncomeTaxDue);

            return calculation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate income tax for taxpayer category {Category}", request.TaxpayerCategory);
            throw new InvalidOperationException("Income tax calculation failed", ex);
        }
    }

    public async Task<List<IncomeTaxRateDto>> GetIncomeTaxRatesAsync(int taxYear, string taxpayerCategory)
    {
        try
        {
            var rates = await _context.TaxRates
                .Where(r => r.TaxYear == taxYear && 
                           r.TaxType == "Income Tax" && 
                           r.TaxpayerCategory == taxpayerCategory &&
                           r.IsActive)
                .OrderBy(r => r.MinIncome)
                .Select(r => new IncomeTaxRateDto
                {
                    Id = r.Id,
                    MinIncome = r.MinIncome,
                    MaxIncome = r.MaxIncome,
                    Rate = r.Rate,
                    Description = r.Description
                })
                .ToListAsync();

            if (!rates.Any())
            {
                // Apply default Finance Act 2025 rates if no custom rates found
                rates = GetDefaultIncomeTaxRates(taxpayerCategory);
                _logger.LogWarning("Using default income tax rates for {Category} taxpayer category", taxpayerCategory);
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get income tax rates for tax year {TaxYear}", taxYear);
            throw new InvalidOperationException("Failed to retrieve income tax rates", ex);
        }
    }

    #endregion

    #region GST Calculations

    public async Task<GstCalculationDto> CalculateGstAsync(GstCalculationRequestDto request)
    {
        try
        {
            var gstRate = await GetGstRateAsync(request.TaxYear, request.IsExport);
            
            var calculation = new GstCalculationDto
            {
                TaxYear = request.TaxYear,
                GrossSales = request.GrossSales,
                TaxableSupplies = request.TaxableSupplies,
                ExemptSupplies = request.ExemptSupplies,
                ZeroRatedSupplies = request.ZeroRatedSupplies,
                InputTax = request.InputTax,
                GstRate = gstRate.Rate
            };

            // Calculate output GST
            calculation.OutputGst = calculation.TaxableSupplies * (gstRate.Rate / 100);
            
            // Calculate net GST liability
            calculation.NetGstLiability = Math.Max(0, calculation.OutputGst - calculation.InputTax);
            
            // Apply reverse charge mechanism for imports
            if (request.IsImport && request.ImportValue > 0)
            {
                calculation.ReverseChargeGst = request.ImportValue * (gstRate.Rate / 100);
                calculation.NetGstLiability += calculation.ReverseChargeGst;
            }

            // Calculate penalties for late filing/payment
            if (request.FilingDate.HasValue && request.DueDate.HasValue && request.FilingDate > request.DueDate)
            {
                calculation.Penalties = await CalculateLatePenaltiesAsync(
                    calculation.NetGstLiability,
                    request.DueDate.Value,
                    request.FilingDate.Value,
                    "GST");
            }

            calculation.TotalAmountDue = calculation.NetGstLiability + calculation.Penalties.TotalPenalty;

            _logger.LogInformation("GST calculated: {TaxableSupplies} SLE -> {NetGst} SLE GST liability", 
                calculation.TaxableSupplies, calculation.NetGstLiability);

            return calculation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate GST");
            throw new InvalidOperationException("GST calculation failed", ex);
        }
    }

    public async Task<GstRateDto> GetGstRateAsync(int taxYear, bool isExport = false)
    {
        try
        {
            var rate = await _context.TaxRates
                .Where(r => r.TaxYear == taxYear && 
                           r.TaxType == "GST" && 
                           r.IsActive)
                .FirstOrDefaultAsync();

            if (rate == null)
            {
                // Use configured GST rate when no DB rate is found (default 15%)
                var configuredGst = 15m;
                try
                {
                    var settingValue = await _settingService.GetSettingAsync<decimal?>("Tax.GST.RatePercent");
                    if (settingValue.HasValue && settingValue.Value >= 0)
                    {
                        configuredGst = settingValue.Value;
                    }
                }
                catch { /* ignore and use default */ }

                return new GstRateDto
                {
                    Rate = isExport ? 0 : configuredGst,
                    Description = isExport ? "Zero-rated exports" : "Standard GST rate",
                    EffectiveDate = new DateTime(taxYear, 1, 1)
                };
            }

            return new GstRateDto
            {
                Id = rate.Id,
                Rate = isExport ? 0 : rate.Rate,
                Description = rate.Description,
                EffectiveDate = rate.EffectiveDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GST rate for tax year {TaxYear}", taxYear);
            throw new InvalidOperationException("Failed to retrieve GST rate", ex);
        }
    }

    #endregion

    #region Payroll Tax Calculations

    public async Task<PayrollTaxCalculationDto> CalculatePayrollTaxAsync(PayrollTaxCalculationRequestDto request)
    {
        try
        {
            var calculation = new PayrollTaxCalculationDto
            {
                TaxYear = request.TaxYear,
                TotalPayroll = request.TotalPayroll,
                PayrollTaxDue = 0,
                EmployeeContributions = new List<PayrollTaxEmployeeDto>()
            };

            // Calculate PAYE for each employee
            foreach (var employee in request.Employees)
            {
                var employeeTax = await CalculateEmployeePayeAsync(employee, request.TaxYear);
                calculation.EmployeeContributions.Add(employeeTax);
                calculation.PayrollTaxDue += employeeTax.PayeTax;
            }

            // Add skills development levy (Finance Act 2025)
            var skillsLevyRate = await GetSkillsLevyRateAsync(request.TaxYear);
            calculation.SkillsDevelopmentLevy = calculation.TotalPayroll * (skillsLevyRate / 100);
            calculation.PayrollTaxDue += calculation.SkillsDevelopmentLevy;

            // Calculate penalties for late remittance
            if (request.RemittanceDate.HasValue && request.DueDate.HasValue && request.RemittanceDate > request.DueDate)
            {
                calculation.Penalties = await CalculateLatePenaltiesAsync(
                    calculation.PayrollTaxDue,
                    request.DueDate.Value,
                    request.RemittanceDate.Value,
                    "Payroll Tax");
            }

            calculation.TotalAmountDue = calculation.PayrollTaxDue + calculation.Penalties.TotalPenalty;

            _logger.LogInformation("Payroll tax calculated: {TotalPayroll} SLE -> {PayrollTax} SLE tax due", 
                calculation.TotalPayroll, calculation.PayrollTaxDue);

            return calculation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate payroll tax");
            throw new InvalidOperationException("Payroll tax calculation failed", ex);
        }
    }

    private async Task<PayrollTaxEmployeeDto> CalculateEmployeePayeAsync(PayrollTaxEmployeeRequestDto employee, int taxYear)
    {
        var monthlyIncome = employee.AnnualSalary / 12;
        var taxFreeThreshold = await GetTaxFreeThresholdAsync(taxYear);
        
        var taxableIncome = Math.Max(0, monthlyIncome - taxFreeThreshold);
        var payeTax = 0m;

        // Apply PAYE brackets (Finance Act 2025)
        if (taxableIncome <= 1000000) // Up to 1M SLE
        {
            payeTax = taxableIncome * 0.15m; // 15%
        }
        else if (taxableIncome <= 5000000) // 1M - 5M SLE
        {
            payeTax = 150000 + (taxableIncome - 1000000) * 0.20m; // 20%
        }
        else // Above 5M SLE
        {
            payeTax = 950000 + (taxableIncome - 5000000) * 0.30m; // 30%
        }

        return new PayrollTaxEmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            AnnualSalary = employee.AnnualSalary,
            MonthlyIncome = monthlyIncome,
            TaxableIncome = taxableIncome,
            PayeTax = payeTax * 12 // Annual PAYE
        };
    }

    #endregion

    #region Excise Duty Calculations

    public async Task<ExciseDutyCalculationDto> CalculateExciseDutyAsync(ExciseDutyCalculationRequestDto request)
    {
        try
        {
            var exciseRates = await GetExciseDutyRatesAsync(request.TaxYear, request.ProductCategory);
            
            var calculation = new ExciseDutyCalculationDto
            {
                TaxYear = request.TaxYear,
                ProductCategory = request.ProductCategory,
                Quantity = request.Quantity,
                Value = request.Value,
                ExciseDutyItems = new List<ExciseDutyItemDto>()
            };

            decimal totalExciseDuty = 0;

            foreach (var item in request.Items)
            {
                var rate = exciseRates.FirstOrDefault(r => r.ProductCode == item.ProductCode);
                if (rate == null)
                {
                    _logger.LogWarning("No excise duty rate found for product code {ProductCode}", item.ProductCode);
                    continue;
                }

                var exciseDuty = rate.RateType == "Specific" 
                    ? item.Quantity * rate.Rate // Specific rate per unit
                    : item.Value * (rate.Rate / 100); // Ad valorem rate

                var exciseItem = new ExciseDutyItemDto
                {
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Value = item.Value,
                    ExciseRate = rate.Rate,
                    RateType = rate.RateType,
                    ExciseDuty = exciseDuty
                };

                calculation.ExciseDutyItems.Add(exciseItem);
                totalExciseDuty += exciseDuty;
            }

            calculation.TotalExciseDuty = totalExciseDuty;

            // Calculate penalties for late payment
            if (request.PaymentDate.HasValue && request.DueDate.HasValue && request.PaymentDate > request.DueDate)
            {
                calculation.Penalties = await CalculateLatePenaltiesAsync(
                    calculation.TotalExciseDuty,
                    request.DueDate.Value,
                    request.PaymentDate.Value,
                    "Excise Duty");
            }

            calculation.TotalAmountDue = calculation.TotalExciseDuty + calculation.Penalties.TotalPenalty;

            _logger.LogInformation("Excise duty calculated: {Items} items -> {ExciseDuty} SLE total duty", 
                calculation.ExciseDutyItems.Count, calculation.TotalExciseDuty);

            return calculation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate excise duty for category {Category}", request.ProductCategory);
            throw new InvalidOperationException("Excise duty calculation failed", ex);
        }
    }

    public async Task<List<ExciseDutyRateDto>> GetExciseDutyRatesAsync(int taxYear, string productCategory)
    {
        try
        {
            var rates = await _context.ExciseDutyRates
                .Where(r => r.TaxYear == taxYear && 
                           r.ProductCategory == productCategory &&
                           r.IsActive)
                .Select(r => new ExciseDutyRateDto
                {
                    Id = r.Id,
                    ProductCode = r.ProductCode,
                    ProductName = r.ProductName,
                    ProductCategory = r.ProductCategory,
                    Rate = r.Rate,
                    RateType = r.RateType,
                    UnitOfMeasure = r.UnitOfMeasure
                })
                .ToListAsync();

            if (!rates.Any())
            {
                // Apply default Finance Act 2025 excise duty rates
                rates = GetDefaultExciseDutyRates(productCategory);
                _logger.LogWarning("Using default excise duty rates for category {Category}", productCategory);
            }

            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get excise duty rates for category {Category}", productCategory);
            throw new InvalidOperationException("Failed to retrieve excise duty rates", ex);
        }
    }

    #endregion

    #region Penalty Calculations

    public async Task<TaxPenaltyCalculationDto> CalculateLatePenaltiesAsync(
        decimal taxAmount, 
        DateTime dueDate, 
        DateTime actualDate, 
        string taxType)
    {
        try
        {
            var penaltyRules = await GetPenaltyRulesAsync(taxType);
            var daysLate = (actualDate - dueDate).Days;
            
            var calculation = new TaxPenaltyCalculationDto
            {
                TaxType = taxType,
                TaxAmount = taxAmount,
                DueDate = dueDate,
                ActualDate = actualDate,
                DaysLate = daysLate,
                PenaltyItems = new List<PenaltyItemDto>()
            };

            decimal totalPenalty = 0;

            foreach (var rule in penaltyRules.OrderBy(r => r.Priority))
            {
                if (!IsPenaltyApplicable(rule, daysLate, taxAmount))
                    continue;

                var penaltyAmount = CalculatePenaltyAmount(rule, taxAmount, daysLate);
                
                calculation.PenaltyItems.Add(new PenaltyItemDto
                {
                    PenaltyType = rule.PenaltyType,
                    Description = rule.Description,
                    Rate = rule.Rate,
                    Amount = penaltyAmount,
                    AppliedDate = DateTime.UtcNow
                });

                totalPenalty += penaltyAmount;
            }

            // Interest on late payment (Finance Act 2025: 2% per month)
            if (daysLate > 0)
            {
                var interestMonths = Math.Ceiling(daysLate / 30.0);
                var interestAmount = taxAmount * 0.02m * (decimal)interestMonths;
                
                calculation.PenaltyItems.Add(new PenaltyItemDto
                {
                    PenaltyType = "Interest",
                    Description = $"Interest on late payment (2% per month for {interestMonths} months)",
                    Rate = 2.0m,
                    Amount = interestAmount,
                    AppliedDate = DateTime.UtcNow
                });

                totalPenalty += interestAmount;
            }

            calculation.TotalPenalty = totalPenalty;

            _logger.LogInformation("Penalties calculated: {DaysLate} days late -> {TotalPenalty} SLE penalty", 
                daysLate, totalPenalty);

            return calculation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate penalties for {TaxType}", taxType);
            throw new InvalidOperationException("Penalty calculation failed", ex);
        }
    }

    public async Task<List<TaxPenaltyRuleDto>> GetPenaltyRulesAsync(string taxType)
    {
        try
        {
            var rules = await _context.TaxPenaltyRules
                .Where(r => r.TaxType == taxType && r.IsActive)
                .OrderBy(r => r.Priority)
                .Select(r => new TaxPenaltyRuleDto
                {
                    Id = r.Id,
                    TaxType = r.TaxType,
                    PenaltyType = r.PenaltyType,
                    Description = r.Description,
                    Rate = r.Rate,
                    FixedAmount = r.FixedAmount,
                    MinDaysLate = r.MinDaysLate,
                    MaxDaysLate = r.MaxDaysLate,
                    MinTaxAmount = r.MinTaxAmount,
                    MaxTaxAmount = r.MaxTaxAmount,
                    Priority = r.Priority
                })
                .ToListAsync();

            if (!rules.Any())
            {
                // Apply default Finance Act 2025 penalty rules
                rules = GetDefaultPenaltyRules(taxType);
                _logger.LogWarning("Using default penalty rules for {TaxType}", taxType);
            }

            return rules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get penalty rules for {TaxType}", taxType);
            throw new InvalidOperationException("Failed to retrieve penalty rules", ex);
        }
    }

    #endregion

    #region Tax Rate Management

    public async Task<TaxRateDto> CreateTaxRateAsync(CreateTaxRateDto request, string createdBy)
    {
        try
        {
            var taxRate = new TaxRate
            {
                TaxYear = request.TaxYear,
                TaxType = request.TaxType,
                TaxpayerCategory = request.TaxpayerCategory,
                MinIncome = request.MinIncome,
                MaxIncome = request.MaxIncome,
                Rate = request.Rate,
                Description = request.Description,
                EffectiveDate = request.EffectiveDate,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            _context.TaxRates.Add(taxRate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tax rate created: {TaxType} {Rate}% for {Category}", 
                request.TaxType, request.Rate, request.TaxpayerCategory);

            return new TaxRateDto
            {
                Id = taxRate.Id,
                TaxYear = taxRate.TaxYear,
                TaxType = taxRate.TaxType,
                TaxpayerCategory = taxRate.TaxpayerCategory,
                MinIncome = taxRate.MinIncome,
                MaxIncome = taxRate.MaxIncome,
                Rate = taxRate.Rate,
                Description = taxRate.Description,
                EffectiveDate = taxRate.EffectiveDate,
                IsActive = taxRate.IsActive,
                CreatedAt = taxRate.CreatedAt,
                CreatedBy = taxRate.CreatedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tax rate for {TaxType}", request.TaxType);
            throw new InvalidOperationException("Failed to create tax rate", ex);
        }
    }

    public async Task<TaxRateDto> UpdateTaxRateAsync(int rateId, CreateTaxRateDto request, string updatedBy)
    {
        try
        {
            var taxRate = await _context.TaxRates.FindAsync(rateId);
            if (taxRate == null)
                throw new InvalidOperationException($"Tax rate with ID {rateId} not found");

            taxRate.TaxYear = request.TaxYear;
            taxRate.TaxType = request.TaxType;
            taxRate.TaxpayerCategory = request.TaxpayerCategory;
            taxRate.MinIncome = request.MinIncome;
            taxRate.MaxIncome = request.MaxIncome;
            taxRate.Rate = request.Rate;
            taxRate.Description = request.Description;
            taxRate.EffectiveDate = request.EffectiveDate;
            taxRate.IsActive = request.IsActive;
            taxRate.UpdatedAt = DateTime.UtcNow;
            taxRate.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tax rate updated: ID {RateId} -> {Rate}%", rateId, request.Rate);

            return new TaxRateDto
            {
                Id = taxRate.Id,
                TaxYear = taxRate.TaxYear,
                TaxType = taxRate.TaxType,
                TaxpayerCategory = taxRate.TaxpayerCategory,
                MinIncome = taxRate.MinIncome,
                MaxIncome = taxRate.MaxIncome,
                Rate = taxRate.Rate,
                Description = taxRate.Description,
                EffectiveDate = taxRate.EffectiveDate,
                IsActive = taxRate.IsActive,
                CreatedAt = taxRate.CreatedAt,
                CreatedBy = taxRate.CreatedBy,
                UpdatedAt = taxRate.UpdatedAt,
                UpdatedBy = taxRate.UpdatedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tax rate {RateId}", rateId);
            throw new InvalidOperationException("Failed to update tax rate", ex);
        }
    }

    public async Task<List<TaxRateDto>> GetTaxRatesAsync(int taxYear, string? taxType = null)
    {
        try
        {
            var query = _context.TaxRates
                .Where(r => r.TaxYear == taxYear);

            if (!string.IsNullOrEmpty(taxType))
                query = query.Where(r => r.TaxType == taxType);

            var rates = await query
                .OrderBy(r => r.TaxType)
                .ThenBy(r => r.MinIncome)
                .Select(r => new TaxRateDto
                {
                    Id = r.Id,
                    TaxYear = r.TaxYear,
                    TaxType = r.TaxType,
                    TaxpayerCategory = r.TaxpayerCategory,
                    MinIncome = r.MinIncome,
                    MaxIncome = r.MaxIncome,
                    Rate = r.Rate,
                    Description = r.Description,
                    EffectiveDate = r.EffectiveDate,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    CreatedBy = r.CreatedBy,
                    UpdatedAt = r.UpdatedAt,
                    UpdatedBy = r.UpdatedBy
                })
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} tax rates for tax year {TaxYear}", rates.Count, taxYear);
            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tax rates for tax year {TaxYear}", taxYear);
            throw new InvalidOperationException("Failed to retrieve tax rates", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private decimal CalculateTaxableIncome(IncomeTaxCalculationRequestDto request, List<TaxAllowanceDto> allowances)
    {
        var totalAllowances = allowances.Sum(a => a.Amount);
        return Math.Max(0, request.GrossIncome - request.Deductions - totalAllowances);
    }

    private async Task<decimal> CalculateMinimumTaxAsync(decimal grossIncome, string taxpayerCategory, int taxYear)
    {
        // Finance Act 2025: Minimum tax for companies
        var minimumTaxRate = taxpayerCategory == "Large" ? 0.005m : 0.0025m; // 0.5% for large, 0.25% for medium
        return grossIncome * minimumTaxRate;
    }

    private async Task<List<TaxAllowanceDto>> GetTaxAllowancesAsync(int taxYear, string taxpayerCategory)
    {
        return await _context.TaxAllowances
            .Where(a => a.TaxYear == taxYear && 
                       a.TaxpayerCategory == taxpayerCategory &&
                       a.IsActive)
            .Select(a => new TaxAllowanceDto
            {
                Id = a.Id,
                AllowanceType = a.AllowanceType,
                Description = a.Description,
                Amount = a.Amount,
                Percentage = a.Percentage
            })
            .ToListAsync();
    }

    private async Task<decimal> GetSkillsLevyRateAsync(int taxYear)
    {
        var rate = await _context.TaxRates
            .Where(r => r.TaxYear == taxYear && r.TaxType == "Skills Levy" && r.IsActive)
            .FirstOrDefaultAsync();

        return rate?.Rate ?? 1.0m; // Default 1% skills development levy
    }

    private async Task<decimal> GetTaxFreeThresholdAsync(int taxYear)
    {
        var threshold = await _context.TaxAllowances
            .Where(a => a.TaxYear == taxYear && a.AllowanceType == "Personal" && a.IsActive)
            .FirstOrDefaultAsync();

        return threshold?.Amount ?? 600000m; // Default 600,000 SLE per year (50,000 per month)
    }

    private bool IsPenaltyApplicable(TaxPenaltyRuleDto rule, int daysLate, decimal taxAmount)
    {
        if (rule.MinDaysLate.HasValue && daysLate < rule.MinDaysLate.Value)
            return false;

        if (rule.MaxDaysLate.HasValue && daysLate > rule.MaxDaysLate.Value)
            return false;

        if (rule.MinTaxAmount.HasValue && taxAmount < rule.MinTaxAmount.Value)
            return false;

        if (rule.MaxTaxAmount.HasValue && taxAmount > rule.MaxTaxAmount.Value)
            return false;

        return true;
    }

    private decimal CalculatePenaltyAmount(TaxPenaltyRuleDto rule, decimal taxAmount, int daysLate)
    {
        if (rule.FixedAmount.HasValue)
            return rule.FixedAmount.Value;

        if (rule.Rate > 0)
            return taxAmount * (rule.Rate / 100);

        return 0;
    }

    #region Default Tax Rates (Finance Act 2025)

    private List<IncomeTaxRateDto> GetDefaultIncomeTaxRates(string taxpayerCategory)
    {
        return taxpayerCategory switch
        {
            "Individual" => new List<IncomeTaxRateDto>
            {
                new() { MinIncome = 0, MaxIncome = 7200000, Rate = 0, Description = "Tax-free allowance" },
                new() { MinIncome = 7200000, MaxIncome = 12000000, Rate = 15, Description = "Low income bracket" },
                new() { MinIncome = 12000000, MaxIncome = 60000000, Rate = 20, Description = "Middle income bracket" },
                new() { MinIncome = 60000000, MaxIncome = null, Rate = 30, Description = "High income bracket" }
            },
            "Large" => new List<IncomeTaxRateDto>
            {
                new() { MinIncome = 0, MaxIncome = null, Rate = 30, Description = "Corporate income tax - Large companies" }
            },
            "Medium" => new List<IncomeTaxRateDto>
            {
                new() { MinIncome = 0, MaxIncome = null, Rate = 25, Description = "Corporate income tax - Medium companies" }
            },
            "Small" => new List<IncomeTaxRateDto>
            {
                new() { MinIncome = 0, MaxIncome = null, Rate = 20, Description = "Corporate income tax - Small companies" }
            },
            "Micro" => new List<IncomeTaxRateDto>
            {
                new() { MinIncome = 0, MaxIncome = null, Rate = 0, Description = "Tax-exempt micro businesses" }
            },
            _ => new List<IncomeTaxRateDto>()
        };
    }

    private List<ExciseDutyRateDto> GetDefaultExciseDutyRates(string productCategory)
    {
        return productCategory switch
        {
            "Tobacco" => new List<ExciseDutyRateDto>
            {
                new() { ProductCode = "TOB001", ProductName = "Cigarettes", Rate = 150, RateType = "Specific", UnitOfMeasure = "Per pack" },
                new() { ProductCode = "TOB002", ProductName = "Cigars", Rate = 200, RateType = "Specific", UnitOfMeasure = "Per piece" }
            },
            "Alcohol" => new List<ExciseDutyRateDto>
            {
                new() { ProductCode = "ALC001", ProductName = "Beer", Rate = 500, RateType = "Specific", UnitOfMeasure = "Per liter" },
                new() { ProductCode = "ALC002", ProductName = "Wine", Rate = 800, RateType = "Specific", UnitOfMeasure = "Per liter" },
                new() { ProductCode = "ALC003", ProductName = "Spirits", Rate = 2000, RateType = "Specific", UnitOfMeasure = "Per liter" }
            },
            "Fuel" => new List<ExciseDutyRateDto>
            {
                new() { ProductCode = "FUEL001", ProductName = "Petrol", Rate = 3500, RateType = "Specific", UnitOfMeasure = "Per liter" },
                new() { ProductCode = "FUEL002", ProductName = "Diesel", Rate = 3000, RateType = "Specific", UnitOfMeasure = "Per liter" }
            },
            _ => new List<ExciseDutyRateDto>()
        };
    }

    private List<TaxPenaltyRuleDto> GetDefaultPenaltyRules(string taxType)
    {
        return new List<TaxPenaltyRuleDto>
        {
            new()
            {
                TaxType = taxType,
                PenaltyType = "Late Filing",
                Description = "5% penalty for late filing",
                Rate = 5,
                MinDaysLate = 1,
                Priority = 1
            },
            new()
            {
                TaxType = taxType,
                PenaltyType = "Failure to File",
                Description = "Additional 5% penalty after 30 days",
                Rate = 5,
                MinDaysLate = 30,
                Priority = 2
            }
        };
    }

    #endregion

    #region Comprehensive Tax Assessment

    public async Task<ComprehensiveTaxAssessmentDto> PerformComprehensiveTaxAssessmentAsync(ComprehensiveTaxAssessmentRequestDto request)
    {
        try
        {
            var assessment = new ComprehensiveTaxAssessmentDto
            {
                ClientId = request.ClientId,
                TaxYear = request.TaxYear,
                TaxpayerCategory = request.TaxpayerCategory,
                AssessmentDate = DateTime.UtcNow
            };

            // Calculate Income Tax
            if (request.GrossIncome > 0)
            {
                assessment.IncomeTax = await CalculateIncomeTaxAsync(new IncomeTaxCalculationRequestDto
                {
                    TaxYear = request.TaxYear,
                    TaxpayerCategory = request.TaxpayerCategory,
                    GrossIncome = request.GrossIncome,
                    Deductions = request.Deductions,
                    DueDate = request.IncomeTaxDueDate
                });
            }

            // Calculate GST
            if (request.GrossSales > 0)
            {
                assessment.Gst = await CalculateGstAsync(new GstCalculationRequestDto
                {
                    TaxYear = request.TaxYear,
                    GrossSales = request.GrossSales,
                    TaxableSupplies = request.TaxableSupplies,
                    ExemptSupplies = request.ExemptSupplies,
                    InputTax = request.InputTax,
                    DueDate = request.GstDueDate
                });
            }

            // Calculate Payroll Tax
            if (request.TotalPayroll > 0 && request.Employees.Any())
            {
                assessment.PayrollTax = await CalculatePayrollTaxAsync(new PayrollTaxCalculationRequestDto
                {
                    TaxYear = request.TaxYear,
                    TotalPayroll = request.TotalPayroll,
                    Employees = request.Employees,
                    DueDate = request.PayrollTaxDueDate
                });
            }

            // Calculate Excise Duty
            if (request.ExciseDutyItems.Any())
            {
                // Group by product category
                var categoryGroups = request.ExciseDutyItems
                    .GroupBy(i => GetProductCategory(i.ProductCode))
                    .ToList();

                foreach (var group in categoryGroups)
                {
                    var exciseCalculation = await CalculateExciseDutyAsync(new ExciseDutyCalculationRequestDto
                    {
                        TaxYear = request.TaxYear,
                        ProductCategory = group.Key,
                        Items = group.ToList(),
                        DueDate = request.ExciseDutyDueDate
                    });

                    // Combine with existing excise duty calculation
                    assessment.ExciseDuty.TotalExciseDuty += exciseCalculation.TotalExciseDuty;
                    assessment.ExciseDuty.ExciseDutyItems.AddRange(exciseCalculation.ExciseDutyItems);
                    assessment.ExciseDuty.Penalties.TotalPenalty += exciseCalculation.Penalties.TotalPenalty;
                    assessment.ExciseDuty.TotalAmountDue += exciseCalculation.TotalAmountDue;
                }
            }

            // Calculate totals
            assessment.TotalTaxLiability = assessment.IncomeTax.IncomeTaxDue + 
                                         assessment.Gst.NetGstLiability + 
                                         assessment.PayrollTax.PayrollTaxDue + 
                                         assessment.ExciseDuty.TotalExciseDuty;

            assessment.TotalPenalties = assessment.IncomeTax.Penalties.TotalPenalty + 
                                      assessment.Gst.Penalties.TotalPenalty + 
                                      assessment.PayrollTax.Penalties.TotalPenalty + 
                                      assessment.ExciseDuty.Penalties.TotalPenalty;

            assessment.GrandTotal = assessment.TotalTaxLiability + assessment.TotalPenalties;

            // Identify compliance issues
            assessment.ComplianceIssues = await IdentifyComplianceIssuesAsync(request.ClientId, request.TaxYear);

            // Calculate compliance score
            assessment.ComplianceScore = await CalculateComplianceScoreAsync(request.ClientId, request.TaxYear);

            _logger.LogInformation("Comprehensive tax assessment completed for client {ClientId}: {GrandTotal} SLE total liability", 
                request.ClientId, assessment.GrandTotal);

            return assessment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform comprehensive tax assessment for client {ClientId}", request.ClientId);
            throw new InvalidOperationException("Comprehensive tax assessment failed", ex);
        }
    }

    public async Task<TaxComplianceScoreDto> CalculateComplianceScoreAsync(int clientId, int taxYear)
    {
        try
        {
            var score = 100m; // Start with perfect score
            var positiveFactors = new List<string>();
            var improvementAreas = new List<string>();

            // Get client's tax filings for the year
            var filings = await _context.TaxFilings
                .Where(f => f.ClientId == clientId && f.TaxYear == taxYear)
                .ToListAsync();

            // Get client's payments
            var payments = await _context.Payments
                .Where(p => p.ClientId == clientId && p.PaymentDate.Year == taxYear)
                .ToListAsync();

            // Check filing timeliness
            var lateFilings = filings.Count(f => f.FilingDate > f.DueDate);
            if (lateFilings > 0)
            {
                score -= lateFilings * 10; // -10 points per late filing
                improvementAreas.Add($"{lateFilings} late filing(s)");
            }
            else if (filings.Any())
            {
                positiveFactors.Add("All filings submitted on time");
            }

            // Check payment timeliness
            var latePayments = payments.Count(p => p.PaymentDate > p.DueDate);
            if (latePayments > 0)
            {
                score -= latePayments * 15; // -15 points per late payment
                improvementAreas.Add($"{latePayments} late payment(s)");
            }
            else if (payments.Any())
            {
                positiveFactors.Add("All payments made on time");
            }

            // Check for audit issues
            var auditIssues = await _context.ComplianceTrackers
                .Where(c => c.ClientId == clientId && c.TaxYearId == taxYear && 
                           c.Status == ComplianceStatus.NonCompliant)
                .CountAsync();

            if (auditIssues > 0)
            {
                score -= auditIssues * 20; // -20 points per audit issue
                improvementAreas.Add($"{auditIssues} compliance issue(s)");
            }

            // Check record keeping
            var documentsCount = await _context.Documents
                .Where(d => d.ClientId == clientId && d.UploadedAt.Year == taxYear)
                .CountAsync();

            if (documentsCount >= 10) // Good record keeping
            {
                positiveFactors.Add("Excellent record keeping");
            }
            else if (documentsCount < 5)
            {
                score -= 10;
                improvementAreas.Add("Insufficient supporting documents");
            }

            // Ensure score doesn't go below 0
            score = Math.Max(0, score);

            var grade = score switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };

            var description = score switch
            {
                >= 90 => "Excellent tax compliance",
                >= 80 => "Good tax compliance",
                >= 70 => "Satisfactory tax compliance",
                >= 60 => "Poor tax compliance",
                _ => "Very poor tax compliance"
            };

            return new TaxComplianceScoreDto
            {
                Score = score,
                Grade = grade,
                Description = description,
                PositiveFactors = positiveFactors,
                ImprovementAreas = improvementAreas
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate compliance score for client {ClientId}", clientId);
            throw new InvalidOperationException("Compliance score calculation failed", ex);
        }
    }

    public async Task<List<TaxComplianceIssueDto>> IdentifyComplianceIssuesAsync(int clientId, int taxYear)
    {
        try
        {
            var issues = new List<TaxComplianceIssueDto>();

            // Check for missing filings
            var expectedFilings = new[] { TaxType.IncomeTax, TaxType.GST, TaxType.PayrollTax };
            var actualFilings = await _context.TaxFilings
                .Where(f => f.ClientId == clientId && f.TaxYear == taxYear)
                .Select(f => f.TaxType)
                .ToListAsync();

            foreach (var expectedFiling in expectedFilings)
            {
                if (!actualFilings.Contains(expectedFiling))
                {
                    issues.Add(new TaxComplianceIssueDto
                    {
                        IssueType = "Missing Filing",
                        Description = $"{expectedFiling} return not filed for {taxYear}",
                        Severity = "High",
                        RecommendedAction = $"File {expectedFiling} return immediately",
                        Deadline = new DateTime(taxYear + 1, 3, 31) // March 31 deadline
                    });
                }
            }

            // Check for late filings
            var lateFilings = await _context.TaxFilings
                .Where(f => f.ClientId == clientId && f.TaxYear == taxYear && f.FilingDate > f.DueDate)
                .ToListAsync();

            foreach (var filing in lateFilings)
            {
                var lateDays = filing.DueDate.HasValue 
                    ? (filing.FilingDate - filing.DueDate.Value).Days 
                    : 0;
                issues.Add(new TaxComplianceIssueDto
                {
                    IssueType = "Late Filing",
                    Description = $"{filing.TaxType} return filed {lateDays} days late",
                    Severity = lateDays > 30 ? "Critical" : "Medium",
                    RecommendedAction = "Ensure future filings are submitted on time",
                    Deadline = null
                });
            }

            // Check for outstanding payments
            var outstandingPayments = await _context.Payments
                .Where(p => p.ClientId == clientId && p.PaymentDate.Year == taxYear && 
                           p.Status == PaymentStatus.Pending)
                .ToListAsync();

            foreach (var payment in outstandingPayments)
            {
                issues.Add(new TaxComplianceIssueDto
                {
                    IssueType = "Outstanding Payment",
                    Description = $"Outstanding {payment.TaxType} payment of {payment.Amount:N2} SLE",
                    Severity = payment.DueDate < DateTime.Now ? "Critical" : "High",
                    RecommendedAction = "Make payment immediately to avoid penalties",
                    Deadline = payment.DueDate
                });
            }

            // Check for GST registration requirement
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null && client.TaxpayerCategory != TaxpayerCategory.Micro)
            {
                var hasGstRegistration = await _context.TaxFilings
                    .AnyAsync(f => f.ClientId == clientId && f.TaxType == TaxType.GST);

                if (!hasGstRegistration)
                {
                    issues.Add(new TaxComplianceIssueDto
                    {
                        IssueType = "GST Registration",
                        Description = "Business may be required to register for GST",
                        Severity = "Medium",
                        RecommendedAction = "Review GST registration requirements and register if necessary",
                        Deadline = null
                    });
                }
            }

            _logger.LogDebug("Identified {Count} compliance issues for client {ClientId}", issues.Count, clientId);
            return issues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to identify compliance issues for client {ClientId}", clientId);
            throw new InvalidOperationException("Compliance issue identification failed", ex);
        }
    }

    #endregion

    #region Additional Helper Methods

    private string GetProductCategory(string productCode)
    {
        return productCode.Substring(0, 3).ToUpper() switch
        {
            "TOB" => "Tobacco",
            "ALC" => "Alcohol",
            "FUE" => "Fuel",
            _ => "Other"
        };
    }

    #endregion

    #endregion
}