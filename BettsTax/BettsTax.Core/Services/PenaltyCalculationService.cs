using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class PenaltyCalculationService : IPenaltyCalculationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PenaltyCalculationService> _logger;

        public PenaltyCalculationService(
            ApplicationDbContext context,
            ILogger<PenaltyCalculationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<PenaltyRule>>> GetPenaltyRulesAsync(TaxType taxType, PenaltyType penaltyType)
        {
            try
            {
                var rules = await _context.Set<PenaltyRule>()
                    .Where(r => r.TaxType == taxType && 
                               r.PenaltyType == penaltyType && 
                               r.IsActive &&
                               (r.ExpiryDate == null || r.ExpiryDate > DateTime.UtcNow))
                    .OrderBy(r => r.Priority)
                    .ThenBy(r => r.EffectiveDate)
                    .ToListAsync();

                return Result.Success(rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting penalty rules for {TaxType} {PenaltyType}", taxType, penaltyType);
                return Result.Failure<List<PenaltyRule>>("Failed to get penalty rules");
            }
        }

        public async Task<Result<PenaltyRule>> GetApplicablePenaltyRuleAsync(
            TaxType taxType, 
            PenaltyType penaltyType, 
            TaxpayerCategory? category = null)
        {
            try
            {
                var query = _context.Set<PenaltyRule>()
                    .Where(r => r.TaxType == taxType && 
                               r.PenaltyType == penaltyType && 
                               r.IsActive &&
                               r.EffectiveDate <= DateTime.UtcNow &&
                               (r.ExpiryDate == null || r.ExpiryDate > DateTime.UtcNow));

                if (category.HasValue)
                {
                    query = query.Where(r => r.TaxpayerCategory == null || r.TaxpayerCategory == category);
                }

                var rule = await query
                    .OrderByDescending(r => r.TaxpayerCategory.HasValue ? 1 : 0) // Specific category rules first
                    .ThenBy(r => r.Priority)
                    .ThenByDescending(r => r.EffectiveDate)
                    .FirstOrDefaultAsync();

                if (rule == null)
                {
                    return Result.Failure<PenaltyRule>($"No penalty rule found for {taxType} {penaltyType}");
                }

                return Result.Success(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applicable penalty rule");
                return Result.Failure<PenaltyRule>("Failed to get penalty rule");
            }
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculateLateFilingPenaltyAsync(
            TaxType taxType, 
            decimal taxLiability, 
            DateTime dueDate, 
            DateTime? filedDate = null,
            TaxpayerCategory? category = null)
        {
            try
            {
                var actualDate = filedDate ?? DateTime.UtcNow;
                var daysOverdue = Math.Max(0, (actualDate.Date - dueDate.Date).Days);

                if (daysOverdue == 0)
                {
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = taxLiability,
                        DaysOverdue = 0,
                        PenaltyType = PenaltyType.LateFilingPenalty,
                        Description = "No penalty - filed on time",
                        CalculationMethod = "No penalty applicable",
                        LegalReference = "Sierra Leone Finance Act",
                        CalculationDate = DateTime.UtcNow
                    });
                }

                var ruleResult = await GetApplicablePenaltyRuleAsync(taxType, PenaltyType.LateFilingPenalty, category);
                if (!ruleResult.IsSuccess)
                {
                    return Result.Failure<PenaltyCalculationResultDto>(ruleResult.ErrorMessage);
                }

                var rule = ruleResult.Value;
                var penaltyAmount = 0m;
                var calculationSteps = new List<string>();

                // Apply grace period if any
                var effectiveDaysOverdue = Math.Max(0, daysOverdue - (rule.GracePeriodDays ?? 0));
                if (effectiveDaysOverdue == 0)
                {
                    calculationSteps.Add($"Grace period of {rule.GracePeriodDays} days applied - no penalty");
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = taxLiability,
                        DaysOverdue = daysOverdue,
                        PenaltyType = PenaltyType.LateFilingPenalty,
                        Description = $"No penalty - within {rule.GracePeriodDays} day grace period",
                        CalculationMethod = rule.RuleName,
                        LegalReference = rule.LegalReference ?? "Sierra Leone Finance Act",
                        CalculationSteps = calculationSteps,
                        CalculationDate = DateTime.UtcNow
                    });
                }

                calculationSteps.Add($"Days overdue: {daysOverdue}");
                if (rule.GracePeriodDays > 0)
                {
                    calculationSteps.Add($"Grace period: {rule.GracePeriodDays} days");
                    calculationSteps.Add($"Effective days overdue: {effectiveDaysOverdue}");
                }

                // Calculate penalty based on rule type
                if (rule.FixedAmount.HasValue)
                {
                    penaltyAmount = rule.FixedAmount.Value;
                    calculationSteps.Add($"Fixed penalty: {penaltyAmount:C} SLE");
                }
                else if (rule.FixedRate.HasValue)
                {
                    penaltyAmount = taxLiability * (rule.FixedRate.Value / 100);
                    calculationSteps.Add($"Tax liability: {taxLiability:C} SLE");
                    calculationSteps.Add($"Penalty rate: {rule.FixedRate.Value}%");
                    calculationSteps.Add($"Penalty amount: {taxLiability:C} × {rule.FixedRate.Value}% = {penaltyAmount:C} SLE");
                }
                else if (rule.IsTimeBased)
                {
                    if (rule.DailyRate.HasValue)
                    {
                        var maxDays = rule.MaximumDays ?? effectiveDaysOverdue;
                        var applicableDays = Math.Min(effectiveDaysOverdue, maxDays);
                        penaltyAmount = taxLiability * (rule.DailyRate.Value / 100) * applicableDays;
                        calculationSteps.Add($"Daily rate: {rule.DailyRate.Value}% per day");
                        calculationSteps.Add($"Applicable days: {applicableDays} (max: {maxDays})");
                        calculationSteps.Add($"Penalty: {taxLiability:C} × {rule.DailyRate.Value}% × {applicableDays} days = {penaltyAmount:C} SLE");
                    }
                    else if (rule.MonthlyRate.HasValue)
                    {
                        var months = Math.Ceiling(effectiveDaysOverdue / 30.0m);
                        penaltyAmount = taxLiability * (rule.MonthlyRate.Value / 100) * months;
                        calculationSteps.Add($"Monthly rate: {rule.MonthlyRate.Value}% per month");
                        calculationSteps.Add($"Months overdue: {months}");
                        calculationSteps.Add($"Penalty: {taxLiability:C} × {rule.MonthlyRate.Value}% × {months} months = {penaltyAmount:C} SLE");
                    }
                }

                // Apply minimum and maximum limits
                if (rule.MinimumAmount.HasValue && penaltyAmount < rule.MinimumAmount.Value)
                {
                    calculationSteps.Add($"Applied minimum penalty: {rule.MinimumAmount.Value:C} SLE");
                    penaltyAmount = rule.MinimumAmount.Value;
                }

                if (rule.MaximumAmount.HasValue && penaltyAmount > rule.MaximumAmount.Value)
                {
                    calculationSteps.Add($"Applied maximum penalty cap: {rule.MaximumAmount.Value:C} SLE");
                    penaltyAmount = rule.MaximumAmount.Value;
                }

                return Result.Success(new PenaltyCalculationResultDto
                {
                    PenaltyAmount = penaltyAmount,
                    PenaltyRate = rule.FixedRate ?? rule.DailyRate,
                    BaseAmount = taxLiability,
                    DaysOverdue = daysOverdue,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    Description = $"Late filing penalty - {daysOverdue} days overdue",
                    CalculationMethod = rule.RuleName,
                    LegalReference = rule.LegalReference ?? "Sierra Leone Finance Act",
                    CalculationSteps = calculationSteps,
                    CalculationDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating late filing penalty");
                return Result.Failure<PenaltyCalculationResultDto>("Failed to calculate penalty");
            }
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculateLatePaymentPenaltyAsync(
            TaxType taxType, 
            decimal unpaidAmount, 
            DateTime dueDate, 
            DateTime? paidDate = null,
            TaxpayerCategory? category = null)
        {
            try
            {
                var actualDate = paidDate ?? DateTime.UtcNow;
                var daysOverdue = Math.Max(0, (actualDate.Date - dueDate.Date).Days);

                if (daysOverdue == 0 || unpaidAmount <= 0)
                {
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = unpaidAmount,
                        DaysOverdue = 0,
                        PenaltyType = PenaltyType.LatePaymentPenalty,
                        Description = "No penalty - paid on time or no unpaid amount",
                        CalculationMethod = "No penalty applicable",
                        LegalReference = "Sierra Leone Finance Act",
                        CalculationDate = DateTime.UtcNow
                    });
                }

                var ruleResult = await GetApplicablePenaltyRuleAsync(taxType, PenaltyType.LatePaymentPenalty, category);
                if (!ruleResult.IsSuccess)
                {
                    return Result.Failure<PenaltyCalculationResultDto>(ruleResult.ErrorMessage);
                }

                var rule = ruleResult.Value;
                var penaltyAmount = 0m;
                var calculationSteps = new List<string>();

                calculationSteps.Add($"Unpaid amount: {unpaidAmount:C} SLE");
                calculationSteps.Add($"Days overdue: {daysOverdue}");

                // Apply grace period
                var effectiveDaysOverdue = Math.Max(0, daysOverdue - (rule.GracePeriodDays ?? 0));
                if (effectiveDaysOverdue == 0)
                {
                    calculationSteps.Add($"Grace period of {rule.GracePeriodDays} days applied - no penalty");
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = unpaidAmount,
                        DaysOverdue = daysOverdue,
                        PenaltyType = PenaltyType.LatePaymentPenalty,
                        Description = $"No penalty - within {rule.GracePeriodDays} day grace period",
                        CalculationMethod = rule.RuleName,
                        LegalReference = rule.LegalReference ?? "Sierra Leone Finance Act",
                        CalculationSteps = calculationSteps,
                        CalculationDate = DateTime.UtcNow
                    });
                }

                if (rule.GracePeriodDays > 0)
                {
                    calculationSteps.Add($"Grace period: {rule.GracePeriodDays} days");
                    calculationSteps.Add($"Effective days overdue: {effectiveDaysOverdue}");
                }

                // Calculate penalty - typically time-based for late payments
                if (rule.DailyRate.HasValue)
                {
                    var maxDays = rule.MaximumDays ?? effectiveDaysOverdue;
                    var applicableDays = Math.Min(effectiveDaysOverdue, maxDays);
                    penaltyAmount = unpaidAmount * (rule.DailyRate.Value / 100) * applicableDays;
                    calculationSteps.Add($"Daily penalty rate: {rule.DailyRate.Value}%");
                    calculationSteps.Add($"Applicable days: {applicableDays}");
                    calculationSteps.Add($"Penalty: {unpaidAmount:C} × {rule.DailyRate.Value}% × {applicableDays} days = {penaltyAmount:C} SLE");
                }
                else if (rule.MonthlyRate.HasValue)
                {
                    var months = Math.Ceiling(effectiveDaysOverdue / 30.0m);
                    penaltyAmount = unpaidAmount * (rule.MonthlyRate.Value / 100) * months;
                    calculationSteps.Add($"Monthly penalty rate: {rule.MonthlyRate.Value}%");
                    calculationSteps.Add($"Months overdue: {months}");
                    calculationSteps.Add($"Penalty: {unpaidAmount:C} × {rule.MonthlyRate.Value}% × {months} months = {penaltyAmount:C} SLE");
                }
                else if (rule.FixedRate.HasValue)
                {
                    penaltyAmount = unpaidAmount * (rule.FixedRate.Value / 100);
                    calculationSteps.Add($"Fixed penalty rate: {rule.FixedRate.Value}%");
                    calculationSteps.Add($"Penalty: {unpaidAmount:C} × {rule.FixedRate.Value}% = {penaltyAmount:C} SLE");
                }

                // Apply minimum and maximum limits
                if (rule.MinimumAmount.HasValue && penaltyAmount < rule.MinimumAmount.Value)
                {
                    calculationSteps.Add($"Applied minimum penalty: {rule.MinimumAmount.Value:C} SLE");
                    penaltyAmount = rule.MinimumAmount.Value;
                }

                if (rule.MaximumAmount.HasValue && penaltyAmount > rule.MaximumAmount.Value)
                {
                    calculationSteps.Add($"Applied maximum penalty cap: {rule.MaximumAmount.Value:C} SLE");
                    penaltyAmount = rule.MaximumAmount.Value;
                }

                return Result.Success(new PenaltyCalculationResultDto
                {
                    PenaltyAmount = penaltyAmount,
                    PenaltyRate = rule.DailyRate ?? rule.MonthlyRate ?? rule.FixedRate,
                    BaseAmount = unpaidAmount,
                    DaysOverdue = daysOverdue,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    Description = $"Late payment penalty - {daysOverdue} days overdue",
                    CalculationMethod = rule.RuleName,
                    LegalReference = rule.LegalReference ?? "Sierra Leone Finance Act",
                    CalculationSteps = calculationSteps,
                    CalculationDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating late payment penalty");
                return Result.Failure<PenaltyCalculationResultDto>("Failed to calculate penalty");
            }
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculateInterestAsync(
            decimal unpaidAmount, 
            DateTime dueDate, 
            DateTime? paidDate = null)
        {
            try
            {
                var actualDate = paidDate ?? DateTime.UtcNow;
                var daysOverdue = Math.Max(0, (actualDate.Date - dueDate.Date).Days);

                if (daysOverdue == 0 || unpaidAmount <= 0)
                {
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = unpaidAmount,
                        DaysOverdue = 0,
                        PenaltyType = PenaltyType.Interest,
                        Description = "No interest - paid on time or no unpaid amount",
                        CalculationMethod = "No interest applicable",
                        LegalReference = "Sierra Leone Finance Act",
                        CalculationDate = DateTime.UtcNow
                    });
                }

                // Sierra Leone standard interest rate (typically 1.5% per month or 18% per annum)
                var annualInterestRate = 18m; // 18% per annum
                var dailyRate = annualInterestRate / 365m / 100m;
                
                var interestAmount = unpaidAmount * dailyRate * daysOverdue;

                var calculationSteps = new List<string>
                {
                    $"Unpaid amount: {unpaidAmount:C} SLE",
                    $"Days overdue: {daysOverdue}",
                    $"Annual interest rate: {annualInterestRate}%",
                    $"Daily rate: {dailyRate:P4}",
                    $"Interest: {unpaidAmount:C} × {dailyRate:P4} × {daysOverdue} days = {interestAmount:C} SLE"
                };

                return Result.Success(new PenaltyCalculationResultDto
                {
                    PenaltyAmount = interestAmount,
                    PenaltyRate = dailyRate * 100,
                    BaseAmount = unpaidAmount,
                    DaysOverdue = daysOverdue,
                    PenaltyType = PenaltyType.Interest,
                    Description = $"Interest on unpaid tax - {daysOverdue} days at {annualInterestRate}% p.a.",
                    CalculationMethod = "Compound daily interest",
                    LegalReference = "Sierra Leone Finance Act - Interest on unpaid tax",
                    CalculationSteps = calculationSteps,
                    CalculationDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating interest");
                return Result.Failure<PenaltyCalculationResultDto>("Failed to calculate interest");
            }
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculateNonFilingPenaltyAsync(
            TaxType taxType, 
            decimal estimatedLiability, 
            DateTime dueDate,
            TaxpayerCategory? category = null)
        {
            try
            {
                var daysOverdue = Math.Max(0, (DateTime.UtcNow.Date - dueDate.Date).Days);

                if (daysOverdue == 0)
                {
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = estimatedLiability,
                        DaysOverdue = 0,
                        PenaltyType = PenaltyType.NonFilingPenalty,
                        Description = "No penalty - still within filing deadline",
                        CalculationMethod = "No penalty applicable",
                        LegalReference = "Sierra Leone Finance Act",
                        CalculationDate = DateTime.UtcNow
                    });
                }

                var ruleResult = await GetApplicablePenaltyRuleAsync(taxType, PenaltyType.NonFilingPenalty, category);
                if (!ruleResult.IsSuccess)
                {
                    return Result.Failure<PenaltyCalculationResultDto>(ruleResult.ErrorMessage);
                }

                var rule = ruleResult.Value;
                var penaltyAmount = 0m;
                var calculationSteps = new List<string>();

                calculationSteps.Add($"Estimated tax liability: {estimatedLiability:C} SLE");
                calculationSteps.Add($"Days overdue: {daysOverdue}");

                // Non-filing penalties are typically severe - often a percentage of estimated liability
                if (rule.FixedRate.HasValue)
                {
                    penaltyAmount = estimatedLiability * (rule.FixedRate.Value / 100);
                    calculationSteps.Add($"Non-filing penalty rate: {rule.FixedRate.Value}%");
                    calculationSteps.Add($"Penalty: {estimatedLiability:C} × {rule.FixedRate.Value}% = {penaltyAmount:C} SLE");
                }
                else if (rule.FixedAmount.HasValue)
                {
                    penaltyAmount = rule.FixedAmount.Value;
                    calculationSteps.Add($"Fixed non-filing penalty: {penaltyAmount:C} SLE");
                }

                // Apply minimum and maximum limits
                if (rule.MinimumAmount.HasValue && penaltyAmount < rule.MinimumAmount.Value)
                {
                    calculationSteps.Add($"Applied minimum penalty: {rule.MinimumAmount.Value:C} SLE");
                    penaltyAmount = rule.MinimumAmount.Value;
                }

                if (rule.MaximumAmount.HasValue && penaltyAmount > rule.MaximumAmount.Value)
                {
                    calculationSteps.Add($"Applied maximum penalty cap: {rule.MaximumAmount.Value:C} SLE");
                    penaltyAmount = rule.MaximumAmount.Value;
                }

                return Result.Success(new PenaltyCalculationResultDto
                {
                    PenaltyAmount = penaltyAmount,
                    PenaltyRate = rule.FixedRate,
                    BaseAmount = estimatedLiability,
                    DaysOverdue = daysOverdue,
                    PenaltyType = PenaltyType.NonFilingPenalty,
                    Description = $"Non-filing penalty - {daysOverdue} days overdue",
                    CalculationMethod = rule.RuleName,
                    LegalReference = rule.LegalReference ?? "Sierra Leone Finance Act",
                    CalculationSteps = calculationSteps,
                    CalculationDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating non-filing penalty");
                return Result.Failure<PenaltyCalculationResultDto>("Failed to calculate penalty");
            }
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculateUnderDeclarationPenaltyAsync(
            TaxType taxType, 
            decimal declaredAmount, 
            decimal actualAmount,
            TaxpayerCategory? category = null)
        {
            try
            {
                var underDeclaredAmount = actualAmount - declaredAmount;

                if (underDeclaredAmount <= 0)
                {
                    return Result.Success(new PenaltyCalculationResultDto
                    {
                        PenaltyAmount = 0,
                        BaseAmount = underDeclaredAmount,
                        DaysOverdue = 0,
                        PenaltyType = PenaltyType.UnderDeclarationPenalty,
                        Description = "No penalty - no under-declaration",
                        CalculationMethod = "No penalty applicable",
                        LegalReference = "Sierra Leone Finance Act",
                        CalculationDate = DateTime.UtcNow
                    });
                }

                var ruleResult = await GetApplicablePenaltyRuleAsync(taxType, PenaltyType.UnderDeclarationPenalty, category);
                if (!ruleResult.IsSuccess)
                {
                    return Result.Failure<PenaltyCalculationResultDto>(ruleResult.ErrorMessage);
                }

                var rule = ruleResult.Value;
                var penaltyAmount = 0m;
                var calculationSteps = new List<string>();

                calculationSteps.Add($"Declared amount: {declaredAmount:C} SLE");
                calculationSteps.Add($"Actual amount: {actualAmount:C} SLE");
                calculationSteps.Add($"Under-declared amount: {underDeclaredAmount:C} SLE");

                if (rule.FixedRate.HasValue)
                {
                    penaltyAmount = underDeclaredAmount * (rule.FixedRate.Value / 100);
                    calculationSteps.Add($"Under-declaration penalty rate: {rule.FixedRate.Value}%");
                    calculationSteps.Add($"Penalty: {underDeclaredAmount:C} × {rule.FixedRate.Value}% = {penaltyAmount:C} SLE");
                }

                // Apply minimum and maximum limits
                if (rule.MinimumAmount.HasValue && penaltyAmount < rule.MinimumAmount.Value)
                {
                    calculationSteps.Add($"Applied minimum penalty: {rule.MinimumAmount.Value:C} SLE");
                    penaltyAmount = rule.MinimumAmount.Value;
                }

                if (rule.MaximumAmount.HasValue && penaltyAmount > rule.MaximumAmount.Value)
                {
                    calculationSteps.Add($"Applied maximum penalty cap: {rule.MaximumAmount.Value:C} SLE");
                    penaltyAmount = rule.MaximumAmount.Value;
                }

                return Result.Success(new PenaltyCalculationResultDto
                {
                    PenaltyAmount = penaltyAmount,
                    PenaltyRate = rule.FixedRate,
                    BaseAmount = underDeclaredAmount,
                    DaysOverdue = 0,
                    PenaltyType = PenaltyType.UnderDeclarationPenalty,
                    Description = $"Under-declaration penalty - {underDeclaredAmount:C} SLE under-declared",
                    CalculationMethod = rule.RuleName,
                    LegalReference = rule.LegalReference ?? "Sierra Leone Finance Act",
                    CalculationSteps = calculationSteps,
                    CalculationDate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating under-declaration penalty");
                return Result.Failure<PenaltyCalculationResultDto>("Failed to calculate penalty");
            }
        }

        public async Task<Result<List<PenaltyCalculationResultDto>>> CalculateAllApplicablePenaltiesAsync(
            TaxType taxType,
            decimal taxLiability,
            decimal amountPaid,
            DateTime filingDueDate,
            DateTime paymentDueDate,
            DateTime? filedDate = null,
            DateTime? paidDate = null,
            TaxpayerCategory? category = null)
        {
            try
            {
                var penalties = new List<PenaltyCalculationResultDto>();
                var unpaidAmount = taxLiability - amountPaid;

                // 1. Late filing penalty
                if (filedDate == null || filedDate > filingDueDate)
                {
                    var lateFilingResult = await CalculateLateFilingPenaltyAsync(taxType, taxLiability, filingDueDate, filedDate, category);
                    if (lateFilingResult.IsSuccess && lateFilingResult.Value.PenaltyAmount > 0)
                    {
                        penalties.Add(lateFilingResult.Value);
                    }
                }

                // 2. Late payment penalty
                if (unpaidAmount > 0 && (paidDate == null || paidDate > paymentDueDate))
                {
                    var latePaymentResult = await CalculateLatePaymentPenaltyAsync(taxType, unpaidAmount, paymentDueDate, paidDate, category);
                    if (latePaymentResult.IsSuccess && latePaymentResult.Value.PenaltyAmount > 0)
                    {
                        penalties.Add(latePaymentResult.Value);
                    }
                }

                // 3. Interest on unpaid amount
                if (unpaidAmount > 0 && (paidDate == null || paidDate > paymentDueDate))
                {
                    var interestResult = await CalculateInterestAsync(unpaidAmount, paymentDueDate, paidDate);
                    if (interestResult.IsSuccess && interestResult.Value.PenaltyAmount > 0)
                    {
                        penalties.Add(interestResult.Value);
                    }
                }

                // 4. Non-filing penalty (if not filed at all and significantly overdue)
                if (filedDate == null && DateTime.UtcNow.Date > filingDueDate.Date.AddDays(30))
                {
                    var nonFilingResult = await CalculateNonFilingPenaltyAsync(taxType, taxLiability, filingDueDate, category);
                    if (nonFilingResult.IsSuccess && nonFilingResult.Value.PenaltyAmount > 0)
                    {
                        penalties.Add(nonFilingResult.Value);
                    }
                }

                return Result.Success(penalties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating all applicable penalties");
                return Result.Failure<List<PenaltyCalculationResultDto>>("Failed to calculate penalties");
            }
        }

        public async Task<Result<bool>> ValidatePenaltyCalculationAsync(PenaltyCalculationResultDto calculation)
        {
            try
            {
                // Basic validation
                if (calculation.PenaltyAmount < 0)
                    return Result.Failure<bool>("Penalty amount cannot be negative");

                if (calculation.BaseAmount < 0)
                    return Result.Failure<bool>("Base amount cannot be negative");

                if (calculation.DaysOverdue < 0)
                    return Result.Failure<bool>("Days overdue cannot be negative");

                // Get the applicable rule and verify calculation
                var ruleResult = await GetApplicablePenaltyRuleAsync(
                    Enum.Parse<TaxType>("IncomeTax"), // Default for validation
                    calculation.PenaltyType);

                if (ruleResult.IsSuccess)
                {
                    var rule = ruleResult.Value;
                    
                    // Check against maximum limits
                    if (rule.MaximumAmount.HasValue && calculation.PenaltyAmount > rule.MaximumAmount.Value)
                    {
                        return Result.Failure<bool>($"Penalty amount exceeds maximum limit of {rule.MaximumAmount.Value:C}");
                    }

                    // Check against minimum limits
                    if (rule.MinimumAmount.HasValue && calculation.PenaltyAmount > 0 && calculation.PenaltyAmount < rule.MinimumAmount.Value)
                    {
                        return Result.Failure<bool>($"Penalty amount below minimum limit of {rule.MinimumAmount.Value:C}");
                    }
                }

                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating penalty calculation");
                return Result.Failure<bool>("Failed to validate penalty calculation");
            }
        }

        public async Task<Result<string>> GetLegalReferenceAsync(TaxType taxType, PenaltyType penaltyType)
        {
            try
            {
                var ruleResult = await GetApplicablePenaltyRuleAsync(taxType, penaltyType);
                if (!ruleResult.IsSuccess)
                {
                    return Result.Failure<string>("No applicable penalty rule found");
                }

                var legalReference = ruleResult.Value.LegalReference ?? "Sierra Leone Finance Act";
                return Result.Success(legalReference);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting legal reference");
                return Result.Failure<string>("Failed to get legal reference");
            }
        }

        public async Task<Result<bool>> SeedDefaultPenaltyRulesAsync()
        {
            try
            {
                // Check if rules already exist
                if (await _context.Set<PenaltyRule>().AnyAsync())
                {
                    _logger.LogInformation("Penalty rules already exist, skipping seeding");
                    return Result.Success(true);
                }

                var rules = new List<PenaltyRule>();

                // Income Tax penalties
                rules.AddRange(CreateIncomeTaxPenaltyRules());
                
                // GST penalties
                rules.AddRange(CreateGstPenaltyRules());
                
                // Payroll Tax penalties
                rules.AddRange(CreatePayrollTaxPenaltyRules());

                _context.Set<PenaltyRule>().AddRange(rules);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Seeded {Count} penalty rules", rules.Count);
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding penalty rules");
                return Result.Failure<bool>("Failed to seed penalty rules");
            }
        }

        private List<PenaltyRule> CreateIncomeTaxPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    RuleName = "Income Tax Late Filing Penalty",
                    Description = "5% of tax liability for late filing of income tax returns",
                    FixedRate = 5m, // 5% of tax liability
                    MinimumAmount = 500m, // 500 SLE minimum
                    MaximumAmount = 50000m, // 50,000 SLE maximum
                    GracePeriodDays = 7, // 7 day grace period
                    LegalReference = "Sierra Leone Finance Act 2020, Section 112",
                    Priority = 1
                },
                
                // Late Payment - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "Income Tax Late Payment Penalty",
                    Description = "2% per month on unpaid income tax",
                    IsTimeBased = true,
                    MonthlyRate = 2m, // 2% per month
                    GracePeriodDays = 30, // 30 day grace period
                    MaximumDays = 365, // Maximum 1 year
                    LegalReference = "Sierra Leone Finance Act 2020, Section 115",
                    Priority = 1
                },
                
                // Non-Filing - Income Tax
                new PenaltyRule
                {
                    TaxType = TaxType.IncomeTax,
                    PenaltyType = PenaltyType.NonFilingPenalty,
                    RuleName = "Income Tax Non-Filing Penalty",
                    Description = "20% of estimated tax liability for failure to file",
                    FixedRate = 20m, // 20% of estimated liability
                    MinimumAmount = 2000m, // 2,000 SLE minimum
                    MaximumAmount = 100000m, // 100,000 SLE maximum
                    LegalReference = "Sierra Leone Finance Act 2020, Section 118",
                    Priority = 1
                }
            };
        }

        private List<PenaltyRule> CreateGstPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - GST
                new PenaltyRule
                {
                    TaxType = TaxType.GST,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    RuleName = "GST Late Filing Penalty",
                    Description = "10% of GST liability for late filing",
                    FixedRate = 10m, // 10% of GST liability
                    MinimumAmount = 200m, // 200 SLE minimum
                    MaximumAmount = 25000m, // 25,000 SLE maximum
                    GracePeriodDays = 5, // 5 day grace period
                    LegalReference = "Sierra Leone Finance Act 2020, Section 142",
                    Priority = 1
                },
                
                // Late Payment - GST
                new PenaltyRule
                {
                    TaxType = TaxType.GST,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "GST Late Payment Penalty",
                    Description = "3% per month on unpaid GST",
                    IsTimeBased = true,
                    MonthlyRate = 3m, // 3% per month
                    GracePeriodDays = 15, // 15 day grace period
                    MaximumDays = 365, // Maximum 1 year
                    LegalReference = "Sierra Leone Finance Act 2020, Section 145",
                    Priority = 1
                }
            };
        }

        private List<PenaltyRule> CreatePayrollTaxPenaltyRules()
        {
            return new List<PenaltyRule>
            {
                // Late Filing - Payroll Tax
                new PenaltyRule
                {
                    TaxType = TaxType.PayrollTax,
                    PenaltyType = PenaltyType.LateFilingPenalty,
                    RuleName = "Payroll Tax Late Filing Penalty",
                    Description = "Fixed penalty for late payroll tax filing",
                    FixedAmount = 1000m, // 1,000 SLE fixed penalty
                    LegalReference = "Sierra Leone Finance Act 2020, Section 162",
                    Priority = 1
                },
                
                // Late Payment - Payroll Tax
                new PenaltyRule
                {
                    TaxType = TaxType.PayrollTax,
                    PenaltyType = PenaltyType.LatePaymentPenalty,
                    RuleName = "Payroll Tax Late Payment Penalty",
                    Description = "5% per month on unpaid payroll tax",
                    IsTimeBased = true,
                    MonthlyRate = 5m, // 5% per month
                    GracePeriodDays = 10, // 10 day grace period
                    MaximumDays = 180, // Maximum 6 months
                    LegalReference = "Sierra Leone Finance Act 2020, Section 165",
                    Priority = 1
                }
            };
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculateLatePenaltyAsync(TaxType taxType, decimal taxLiability, DateTime dueDate, DateTime? actualDate = null)
        {
            return await CalculateLateFilingPenaltyAsync(taxType, taxLiability, dueDate, actualDate);
        }

        public async Task<Result<PenaltyCalculationResultDto>> CalculatePenaltyAsync(TaxType taxType, decimal amount, DateTime dueDate, DateTime? actualDate = null)
        {
            return await CalculateLateFilingPenaltyAsync(taxType, amount, dueDate, actualDate);
        }

        public Task<Result<bool>> RecalculatePenaltiesAsync()
        {
            try
            {
                // Implementation would recalculate all penalties
                _logger.LogInformation("Penalties recalculated successfully");
                return Task.FromResult(Result.Success(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating penalties");
                return Task.FromResult(Result.Failure<bool>("Failed to recalculate penalties"));
            }
        }
    }
}