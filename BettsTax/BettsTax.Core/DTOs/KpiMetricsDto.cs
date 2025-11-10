namespace BettsTax.Core.DTOs;

/// <summary>
/// Aggregate KPI metrics for admin dashboard (refreshed periodically).
/// </summary>
public class KpiMetricsDto
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    // Core compliance KPIs
    public int TotalClients { get; set; }
    public decimal ClientComplianceRate { get; set; } // % compliant clients
    public decimal TaxFilingTimeliness { get; set; }   // % filings on time
    public decimal PaymentCompletionRate { get; set; } // % payments approved
    public decimal DocumentSubmissionCompliance { get; set; } // % required docs submitted (placeholder)
    public decimal ClientEngagementRate { get; set; } // % active clients in last 30 days (placeholder)

    // Threshold breach flags (simple evaluation for now)
    public bool ComplianceRateBelowThreshold { get; set; }
    public bool FilingTimelinessBelowThreshold { get; set; }
    public bool PaymentCompletionBelowThreshold { get; set; }
    public bool DocumentSubmissionBelowThreshold { get; set; }
    public bool EngagementBelowThreshold { get; set; }
}
