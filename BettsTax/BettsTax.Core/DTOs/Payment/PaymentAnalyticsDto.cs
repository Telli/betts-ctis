using BettsTax.Data.Models;
using BettsTax.Data;
using PaymentTransactionStatus = BettsTax.Data.Models.PaymentTransactionStatus;

namespace BettsTax.Core.DTOs.Payment;

// Main dashboard DTO
public class PaymentDashboardDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedTransactions { get; set; }
    public decimal CompletedAmount { get; set; }
    public int FailedTransactions { get; set; }
    public decimal FailedAmount { get; set; }
    public int PendingTransactions { get; set; }
    public decimal PendingAmount { get; set; }
    public int RefundedTransactions { get; set; }
    public decimal RefundedAmount { get; set; }
    public double SuccessRate { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public int OrangeMoneyTransactions { get; set; }
    public decimal OrangeMoneyAmount { get; set; }
    public int AfricellMoneyTransactions { get; set; }
    public decimal AfricellMoneyAmount { get; set; }
    public int HighRiskTransactions { get; set; }
    public int BlockedTransactions { get; set; }
    public int ManualReviewRequired { get; set; }
    public List<DailyTrendDto> DailyTrends { get; set; } = new();
    public List<GatewayBreakdownDto> GatewayBreakdown { get; set; } = new();
    public Dictionary<PaymentTransactionStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<int, int> HourlyDistribution { get; set; } = new();
}

// Trend analysis DTOs
public class PaymentTrendDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedTransactions { get; set; }
    public decimal CompletedAmount { get; set; }
    public int FailedTransactions { get; set; }
    public double SuccessRate { get; set; }
}

public class DailyTrendDto
{
    public DateTime Date { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedTransactions { get; set; }
}

public enum PaymentTrendInterval
{
    Daily,
    Weekly,
    Monthly
}

// Gateway performance DTOs
public class GatewayPerformanceDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayName { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedTransactions { get; set; }
    public decimal CompletedAmount { get; set; }
    public int FailedTransactions { get; set; }
    public decimal FailedAmount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageProcessingTime { get; set; }
    public double MarketShare { get; set; }
    public double UptimePercentage { get; set; }
}

public class GatewayBreakdownDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayName { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
}

// Sierra Leone compliance DTOs
public class SierraLeoneComplianceDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalTaxRevenue { get; set; }
    public decimal IncomeTaxRevenue { get; set; }
    public decimal GstRevenue { get; set; }
    public decimal PayrollTaxRevenue { get; set; }
    public decimal ExciseDutyRevenue { get; set; }
    public decimal LargeTaxpayerRevenue { get; set; }
    public decimal MediumTaxpayerRevenue { get; set; }
    public decimal SmallTaxpayerRevenue { get; set; }
    public decimal MicroTaxpayerRevenue { get; set; }
    public double MobileMoneyPercentage { get; set; }
    public double OrangeMoneyPercentage { get; set; }
    public double AfricellMoneyPercentage { get; set; }
    public int FreetownTransactions { get; set; }
    public int ProvinceTransactions { get; set; }
    public double AverageComplianceScore { get; set; }
    public Dictionary<string, int> ComplianceDistribution { get; set; } = new();
    public decimal FinanceAct2025Penalties { get; set; }
    public double DeadlineComplianceRate { get; set; }
    public double DigitalPaymentAdoptionRate { get; set; }
    public double MobileMoneyGrowthRate { get; set; }
    public double GdpContributionEstimate { get; set; }
    public double TaxEfficiencyRatio { get; set; }
}

public class TaxTypeRevenueDto
{
    public string TaxType { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePayment { get; set; }
    public double Percentage { get; set; }
    public List<decimal> MonthlyTrend { get; set; } = new();
}

public class TaxpayerCategoryAnalysisDto
{
    public string Category { get; set; } = string.Empty;
    public int TaxpayerCount { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePaymentAmount { get; set; }
    public double AveragePaymentsPerTaxpayer { get; set; }
    public double RevenuePercentage { get; set; }
    public double ComplianceScore { get; set; }
    public string PreferredPaymentMethod { get; set; } = string.Empty;
    public double GrowthRate { get; set; }
}

// Client analytics DTOs
public class ClientPaymentAnalyticsDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TaxpayerCategory { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
    public int CompletedTransactions { get; set; }
    public decimal CompletedAmount { get; set; }
    public int FailedTransactions { get; set; }
    public int PendingTransactions { get; set; }
    public double SuccessRate { get; set; }
    public decimal AverageTransactionAmount { get; set; }
    public string PreferredPaymentMethod { get; set; } = string.Empty;
    public string PaymentFrequency { get; set; } = string.Empty;
    public Dictionary<string, decimal> TaxTypes { get; set; } = new();
    public double ComplianceScore { get; set; }
    public double OnTimePaymentRate { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public Dictionary<string, double> PaymentMethodDistribution { get; set; } = new();
}

public class TopPayingClientDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TaxpayerCategory { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AveragePaymentAmount { get; set; }
    public DateTime LastPaymentDate { get; set; }
    public string PreferredPaymentMethod { get; set; } = string.Empty;
}

// Export and reporting DTOs
public class PaymentAnalyticsExportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? ClientId { get; set; }
    public string Format { get; set; } = "JSON"; // JSON, CSV, Excel
}

public class PaymentAnalyticsReportDto
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PaymentReportType ReportType { get; set; }
    public string ExecutiveSummary { get; set; } = string.Empty;
    public PaymentDashboardDto? Dashboard { get; set; }
    public List<PaymentTrendDto>? Trends { get; set; }
    public List<GatewayPerformanceDto>? GatewayPerformance { get; set; }
    public SierraLeoneComplianceDto? ComplianceMetrics { get; set; }
    public List<TaxTypeRevenueDto>? TaxTypeBreakdown { get; set; }
    public List<TaxpayerCategoryAnalysisDto>? CategoryAnalysis { get; set; }
    public ClientPaymentAnalyticsDto? ClientAnalytics { get; set; }
}

public class PaymentAnalyticsReportRequestDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int? ClientId { get; set; }
    public PaymentReportType ReportType { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public enum PaymentReportType
{
    Comprehensive,
    Performance,
    Compliance,
    ClientSpecific
}

// Security and retry statistics DTOs
public class SecurityStatsDto
{
    public int TotalSecurityIncidents { get; set; }
    public int HighRiskTransactions { get; set; }
    public int BlockedTransactions { get; set; }
    public int ManualReviewRequired { get; set; }
    public int FraudRuleTriggered { get; set; }
    public Dictionary<SecurityRiskLevel, int> RiskLevelDistribution { get; set; } = new();
    public List<string> TopRiskFactors { get; set; } = new();
}

public class RetryStatisticsDto
{
    public int TotalRetryAttempts { get; set; }
    public int SuccessfulRetries { get; set; }
    public int FailedRetries { get; set; }
    public double AverageRetryDuration { get; set; }
    public double RetrySuccessRate { get; set; }
    public Dictionary<PaymentGatewayType, GatewayRetryStats> GatewayRetryStats { get; set; } = new();
    public int DeadLetterQueueSize { get; set; }
    public Dictionary<PaymentGatewayType, string> CircuitBreakerStates { get; set; } = new();
}

public class GatewayRetryStats
{
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public double AverageDuration { get; set; }
}

// Circuit breaker DTOs
public class CircuitBreakerStatusDto
{
    public PaymentGatewayType GatewayType { get; set; }
    public string GatewayTypeName { get; set; } = string.Empty;
    public CircuitBreakerStatus State { get; set; }
    public string StateName { get; set; } = string.Empty;
    public int FailureCount { get; set; }
    public DateTime? LastFailureAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public bool IsHealthy { get; set; }
}