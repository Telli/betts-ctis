namespace BettsTax.Core.DTOs.KPI;

/// <summary>
/// KPI metrics DTO
/// </summary>
public class KpiMetricsDto
{
    public decimal ComplianceRate { get; set; }
    public decimal AvgTimeliness { get; set; }
    public decimal PaymentCompletion { get; set; }
    public decimal DocSubmission { get; set; }
    public decimal EngagementRate { get; set; }
}

/// <summary>
/// Monthly trend DTO
/// </summary>
public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Compliance { get; set; }
    public decimal Timeliness { get; set; }
    public decimal Payments { get; set; }
}

/// <summary>
/// Client performance DTO
/// </summary>
public class ClientPerformanceDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Score { get; set; }
}
