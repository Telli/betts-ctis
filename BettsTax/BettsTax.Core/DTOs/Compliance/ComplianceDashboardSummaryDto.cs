namespace BettsTax.Core.DTOs.Compliance;

public class ComplianceOverviewSummaryDto
{
    public int TotalClients { get; set; }
    public int Compliant { get; set; }
    public int AtRisk { get; set; }
    public int Overdue { get; set; }
    public decimal AverageScore { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int TotalAlerts { get; set; }
}

public class ComplianceDashboardFilterDto
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Type { get; set; }
    public int? ClientId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ComplianceDashboardItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime DueDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Priority { get; set; } = "medium";
    public decimal Penalty { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int TaxYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal? ComplianceScore { get; set; }
    public int DaysOverdue { get; set; }
    public int Alerts { get; set; }
    public string TaxType { get; set; } = string.Empty;
}

public class ComplianceTaxTypeBreakdownDto
{
    public string TaxType { get; set; } = string.Empty;
    public int ClientCount { get; set; }
    public decimal ComplianceRate { get; set; }
    public decimal AverageScore { get; set; }
    public decimal OutstandingAmount { get; set; }
}

public class FilingChecklistMatrixRowDto
{
    public string TaxType { get; set; } = string.Empty;
    public QuarterStatusDto Status { get; set; } = new();
}

public class QuarterStatusDto
{
    public string Q1 { get; set; } = "n/a";
    public string Q2 { get; set; } = "n/a";
    public string Q3 { get; set; } = "n/a";
    public string Q4 { get; set; } = "n/a";
}

public class PenaltyWarningSummaryDto
{
    public string Type { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal EstimatedAmount { get; set; }
    public int DaysOverdue { get; set; }
    public int? FilingId { get; set; }
    public int? PaymentId { get; set; }
}

public class DocumentRequirementProgressDto
{
    public string Name { get; set; } = string.Empty;
    public int Required { get; set; }
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Progress { get; set; }
}

public class ComplianceTimelineEventDto
{
    public DateTime Date { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Status { get; set; } = "success";
    public string? Details { get; set; }
}
