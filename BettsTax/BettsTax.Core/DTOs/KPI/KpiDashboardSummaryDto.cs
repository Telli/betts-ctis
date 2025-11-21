using System.Collections.Generic;

namespace BettsTax.Core.DTOs.KPI;

public class KpiDashboardSummaryDto
{
    public KpiDashboardInternalSummaryDto Internal { get; set; } = new();
    public KpiDashboardClientSummaryDto Client { get; set; } = new();
}

public class KpiDashboardInternalSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public string RevenueCurrency { get; set; } = "SLE";
    public decimal? RevenueChangePercentage { get; set; }
    public int ActiveClients { get; set; }
    public int TotalClients { get; set; }
    public decimal ComplianceRate { get; set; }
    public decimal PaymentCompletionRate { get; set; }
    public decimal DocumentSubmissionRate { get; set; }
    public double AverageFilingTimelinessDays { get; set; }
    public double AverageProcessingTimeDays { get; set; }
    public decimal ClientEngagementRate { get; set; }
    public string ReferencePeriodLabel { get; set; } = string.Empty;
}

public class KpiDashboardClientSummaryDto
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public decimal AverageComplianceScore { get; set; }
    public double AverageFilingTimeDays { get; set; }
    public string? TopPerformerName { get; set; }
    public decimal TopPerformerComplianceScore { get; set; }
    public List<KpiClientSegmentPerformanceDto> Segments { get; set; } = new();
}

public class KpiClientSegmentPerformanceDto
{
    public string Segment { get; set; } = string.Empty;
    public decimal ComplianceRate { get; set; }
    public int ClientCount { get; set; }
}
