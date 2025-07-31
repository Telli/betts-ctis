using BettsTax.Data;

namespace BettsTax.Core.DTOs.KPI;

public class InternalKPIDto
{
    public decimal ClientComplianceRate { get; set; }
    public double AverageFilingTimeliness { get; set; }
    public decimal PaymentCompletionRate { get; set; }
    public decimal DocumentSubmissionCompliance { get; set; }
    public decimal ClientEngagementRate { get; set; }
    public List<TrendDataPoint> ComplianceTrend { get; set; } = new();
    public List<TaxTypeMetrics> TaxTypeBreakdown { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string Period { get; set; } = string.Empty;
}

public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class TaxTypeMetrics
{
    public TaxType TaxType { get; set; }
    public int TotalFilings { get; set; }
    public int OnTimeFilings { get; set; }
    public decimal ComplianceRate { get; set; }
    public decimal TotalAmount { get; set; }
    public int ClientCount { get; set; }
}