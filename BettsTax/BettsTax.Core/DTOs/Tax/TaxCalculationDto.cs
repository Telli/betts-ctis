namespace BettsTax.Core.DTOs.Tax;

// Income Tax DTOs
public class IncomeTaxCalculationRequestDto
{
    public int TaxYear { get; set; }
    public string TaxpayerCategory { get; set; } = string.Empty; // Individual, Large, Medium, Small, Micro
    public decimal GrossIncome { get; set; }
    public decimal Deductions { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class IncomeTaxCalculationDto
{
    public int TaxYear { get; set; }
    public string TaxpayerCategory { get; set; } = string.Empty;
    public decimal GrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal IncomeTaxDue { get; set; }
    public decimal MinimumTax { get; set; }
    public List<TaxBracketCalculationDto> TaxBrackets { get; set; } = new();
    public TaxPenaltyCalculationDto Penalties { get; set; } = new();
    public decimal TotalAmountDue { get; set; }
}

public class TaxBracketCalculationDto
{
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

public class IncomeTaxRateDto
{
    public int Id { get; set; }
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public string Description { get; set; } = string.Empty;
}

// GST DTOs
public class GstCalculationRequestDto
{
    public int TaxYear { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TaxableSupplies { get; set; }
    public decimal ExemptSupplies { get; set; }
    public decimal ZeroRatedSupplies { get; set; }
    public decimal InputTax { get; set; }
    public bool IsExport { get; set; }
    public bool IsImport { get; set; }
    public decimal ImportValue { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? FilingDate { get; set; }
}

public class GstCalculationDto
{
    public int TaxYear { get; set; }
    public decimal GrossSales { get; set; }
    public decimal TaxableSupplies { get; set; }
    public decimal ExemptSupplies { get; set; }
    public decimal ZeroRatedSupplies { get; set; }
    public decimal InputTax { get; set; }
    public decimal OutputGst { get; set; }
    public decimal ReverseChargeGst { get; set; }
    public decimal NetGstLiability { get; set; }
    public decimal GstRate { get; set; }
    public TaxPenaltyCalculationDto Penalties { get; set; } = new();
    public decimal TotalAmountDue { get; set; }
}

public class GstRateDto
{
    public int Id { get; set; }
    public decimal Rate { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
}

// Payroll Tax DTOs
public class PayrollTaxCalculationRequestDto
{
    public int TaxYear { get; set; }
    public decimal TotalPayroll { get; set; }
    public List<PayrollTaxEmployeeRequestDto> Employees { get; set; } = new();
    public DateTime? DueDate { get; set; }
    public DateTime? RemittanceDate { get; set; }
}

public class PayrollTaxEmployeeRequestDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AnnualSalary { get; set; }
}

public class PayrollTaxCalculationDto
{
    public int TaxYear { get; set; }
    public decimal TotalPayroll { get; set; }
    public decimal PayrollTaxDue { get; set; }
    public decimal SkillsDevelopmentLevy { get; set; }
    public List<PayrollTaxEmployeeDto> EmployeeContributions { get; set; } = new();
    public TaxPenaltyCalculationDto Penalties { get; set; } = new();
    public decimal TotalAmountDue { get; set; }
}

public class PayrollTaxEmployeeDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AnnualSalary { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal PayeTax { get; set; }
}

// Excise Duty DTOs
public class ExciseDutyCalculationRequestDto
{
    public int TaxYear { get; set; }
    public string ProductCategory { get; set; } = string.Empty; // Tobacco, Alcohol, Fuel, etc.
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public List<ExciseDutyItemRequestDto> Items { get; set; } = new();
    public DateTime? DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class ExciseDutyItemRequestDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
}

public class ExciseDutyCalculationDto
{
    public int TaxYear { get; set; }
    public string ProductCategory { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public decimal TotalExciseDuty { get; set; }
    public List<ExciseDutyItemDto> ExciseDutyItems { get; set; } = new();
    public TaxPenaltyCalculationDto Penalties { get; set; } = new();
    public decimal TotalAmountDue { get; set; }
}

public class ExciseDutyItemDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public decimal ExciseRate { get; set; }
    public string RateType { get; set; } = string.Empty; // Specific or Ad Valorem
    public decimal ExciseDuty { get; set; }
}

public class ExciseDutyRateDto
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string RateType { get; set; } = string.Empty; // Specific or Ad Valorem
    public string UnitOfMeasure { get; set; } = string.Empty;
}

// Penalty DTOs
public class TaxPenaltyCalculationDto
{
    public string TaxType { get; set; } = string.Empty;
    public decimal TaxAmount { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime ActualDate { get; set; }
    public int DaysLate { get; set; }
    public decimal TotalPenalty { get; set; }
    public List<PenaltyItemDto> PenaltyItems { get; set; } = new();
}

public class PenaltyItemDto
{
    public string PenaltyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public DateTime AppliedDate { get; set; }
}

public class TaxPenaltyRuleDto
{
    public int Id { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string PenaltyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal? FixedAmount { get; set; }
    public int? MinDaysLate { get; set; }
    public int? MaxDaysLate { get; set; }
    public decimal? MinTaxAmount { get; set; }
    public decimal? MaxTaxAmount { get; set; }
    public int Priority { get; set; }
}

// Tax Rate Management DTOs
public class TaxRateDto
{
    public int Id { get; set; }
    public int TaxYear { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string TaxpayerCategory { get; set; } = string.Empty;
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CreateTaxRateDto
{
    public int TaxYear { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public string TaxpayerCategory { get; set; } = string.Empty;
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    public decimal Rate { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
}

// Tax Allowance DTOs
public class TaxAllowanceDto
{
    public int Id { get; set; }
    public string AllowanceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Percentage { get; set; }
}

// Comprehensive Tax Assessment DTOs
public class ComprehensiveTaxAssessmentRequestDto
{
    public int ClientId { get; set; }
    public int TaxYear { get; set; }
    public string TaxpayerCategory { get; set; } = string.Empty;
    
    // Income Tax
    public decimal GrossIncome { get; set; }
    public decimal Deductions { get; set; }
    
    // GST
    public decimal GrossSales { get; set; }
    public decimal TaxableSupplies { get; set; }
    public decimal ExemptSupplies { get; set; }
    public decimal InputTax { get; set; }
    
    // Payroll Tax
    public decimal TotalPayroll { get; set; }
    public List<PayrollTaxEmployeeRequestDto> Employees { get; set; } = new();
    
    // Excise Duty
    public List<ExciseDutyItemRequestDto> ExciseDutyItems { get; set; } = new();
    
    // Deadlines
    public DateTime? IncomeTaxDueDate { get; set; }
    public DateTime? GstDueDate { get; set; }
    public DateTime? PayrollTaxDueDate { get; set; }
    public DateTime? ExciseDutyDueDate { get; set; }
}

public class ComprehensiveTaxAssessmentDto
{
    public int ClientId { get; set; }
    public int TaxYear { get; set; }
    public string TaxpayerCategory { get; set; } = string.Empty;
    public DateTime AssessmentDate { get; set; }
    
    public IncomeTaxCalculationDto IncomeTax { get; set; } = new();
    public GstCalculationDto Gst { get; set; } = new();
    public PayrollTaxCalculationDto PayrollTax { get; set; } = new();
    public ExciseDutyCalculationDto ExciseDuty { get; set; } = new();
    
    public decimal TotalTaxLiability { get; set; }
    public decimal TotalPenalties { get; set; }
    public decimal GrandTotal { get; set; }
    
    public List<TaxComplianceIssueDto> ComplianceIssues { get; set; } = new();
    public TaxComplianceScoreDto ComplianceScore { get; set; } = new();
}

public class TaxComplianceIssueDto
{
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
}

public class TaxComplianceScoreDto
{
    public decimal Score { get; set; } // 0-100
    public string Grade { get; set; } = string.Empty; // A, B, C, D, F
    public string Description { get; set; } = string.Empty;
    public List<string> PositiveFactors { get; set; } = new();
    public List<string> ImprovementAreas { get; set; } = new();
}