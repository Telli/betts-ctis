using BettsTax.Core.DTOs.Tax;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Interface for Tax Calculation Engine Service
/// Provides comprehensive tax calculations for Sierra Leone Finance Act 2025
/// </summary>
public interface ITaxCalculationEngineService
{
    // Income Tax Calculations
    Task<IncomeTaxCalculationDto> CalculateIncomeTaxAsync(IncomeTaxCalculationRequestDto request);
    Task<List<IncomeTaxRateDto>> GetIncomeTaxRatesAsync(int taxYear, string taxpayerCategory);

    // GST Calculations
    Task<GstCalculationDto> CalculateGstAsync(GstCalculationRequestDto request);
    Task<GstRateDto> GetGstRateAsync(int taxYear, bool isExport = false);

    // Payroll Tax Calculations
    Task<PayrollTaxCalculationDto> CalculatePayrollTaxAsync(PayrollTaxCalculationRequestDto request);

    // Excise Duty Calculations
    Task<ExciseDutyCalculationDto> CalculateExciseDutyAsync(ExciseDutyCalculationRequestDto request);
    Task<List<ExciseDutyRateDto>> GetExciseDutyRatesAsync(int taxYear, string productCategory);

    // Penalty Calculations
    Task<TaxPenaltyCalculationDto> CalculateLatePenaltiesAsync(decimal taxAmount, DateTime dueDate, DateTime actualDate, string taxType);
    Task<List<TaxPenaltyRuleDto>> GetPenaltyRulesAsync(string taxType);

    // Tax Rate Management
    Task<TaxRateDto> CreateTaxRateAsync(CreateTaxRateDto request, string createdBy);
    Task<TaxRateDto> UpdateTaxRateAsync(int rateId, CreateTaxRateDto request, string updatedBy);
    Task<List<TaxRateDto>> GetTaxRatesAsync(int taxYear, string? taxType = null);

    // Comprehensive Tax Assessment
    Task<ComprehensiveTaxAssessmentDto> PerformComprehensiveTaxAssessmentAsync(ComprehensiveTaxAssessmentRequestDto request);
    Task<TaxComplianceScoreDto> CalculateComplianceScoreAsync(int clientId, int taxYear);
    Task<List<TaxComplianceIssueDto>> IdentifyComplianceIssuesAsync(int clientId, int taxYear);
}