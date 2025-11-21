using BettsTax.Data;

namespace BettsTax.Core.DTOs.KPI;

public class ClientKPIDto
{
    public double MyFilingTimeliness { get; set; }
    public decimal OnTimePaymentPercentage { get; set; }
    public decimal DocumentReadinessScore { get; set; }
    public decimal ComplianceScore { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; }
    public List<DeadlineMetric> UpcomingDeadlines { get; set; } = new();
    public List<TrendDataPoint> FilingHistory { get; set; } = new();
    public List<TrendDataPoint> PaymentHistory { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

public class DeadlineMetric
{
    public int Id { get; set; }
    public TaxType TaxType { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysRemaining { get; set; }
    public DeadlinePriority Priority { get; set; }
    public FilingStatus Status { get; set; }
    public decimal? EstimatedAmount { get; set; }
    public bool DocumentsReady { get; set; }
}

public class KPIAlertDto
{
    public int Id { get; set; }
    public KPIAlertType AlertType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public KPIAlertSeverity Severity { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
}

public enum DeadlinePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum KPIAlertType
{
    ComplianceThreshold = 1,
    FilingOverdue = 2,
    PaymentDelayed = 3,
    DocumentMissing = 4,
    ClientInactive = 5,
    UpcomingDeadline = 6
}

public enum KPIAlertSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}