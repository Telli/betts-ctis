using BettsTax.Data;

namespace BettsTax.Core.DTOs.Reports;

public class ReportRequestDto
{
    public int Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportFormat Format { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string RequestedByUserId { get; set; } = string.Empty;
    public string RequestedByUserName { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public long? FileSizeBytes { get; set; }
}

public class CreateReportRequestDto
{
    public ReportType Type { get; set; }
    public ReportFormat Format { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class ReportDataDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class TaxFilingReportDataDto : ReportDataDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public int TaxYear { get; set; }
    public List<TaxFilingReportItem> Filings { get; set; } = new();
    public ReportSummary Summary { get; set; } = new();
}

public class TaxFilingReportItem
{
    public int TaxFilingId { get; set; }
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public DateTime FilingDate { get; set; }
    public DateTime DueDate { get; set; }
    public FilingStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal TaxLiability { get; set; }
    public string FilingReference { get; set; } = string.Empty;
    public int DaysFromDue { get; set; }
    public bool IsOnTime => DaysFromDue <= 0;
    public string ComplianceStatus => IsOnTime ? "On Time" : $"{Math.Abs(DaysFromDue)} days late";
}

public class PaymentHistoryReportDataDto : ReportDataDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<PaymentReportItem> Payments { get; set; } = new();
    public ReportSummary Summary { get; set; } = new();
}

public class PaymentReportItem
{
    public int PaymentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string FormattedAmount { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public TaxType? TaxType { get; set; }
    public string? TaxTypeName { get; set; }
    public int? TaxYear { get; set; }
}

public class ComplianceReportDataDto : ReportDataDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OverallComplianceScore { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; }
    public List<ComplianceReportItem> Items { get; set; } = new();
    public List<PenaltyReportItem> Penalties { get; set; } = new();
    public ReportSummary Summary { get; set; } = new();
}

public class ComplianceReportItem
{
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public int TotalFilings { get; set; }
    public int OnTimeFilings { get; set; }
    public int LateFilings { get; set; }
    public int MissedDeadlines { get; set; }
    public decimal ComplianceRate { get; set; }
    public string ComplianceGrade { get; set; } = string.Empty;
}

public class PenaltyReportItem
{
    public DateTime IncurredDate { get; set; }
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal PenaltyAmount { get; set; }
    public string FormattedAmount { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
}

public class ClientActivityReportDataDto : ReportDataDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<ClientActivityReportItem> Activities { get; set; } = new();
    public Dictionary<string, int> ActivityCounts { get; set; } = new();
    public ReportSummary Summary { get; set; } = new();
}

public class ClientActivityReportItem
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
    public int TotalActivities { get; set; }
    public int DocumentsUploaded { get; set; }
    public int PaymentsMade { get; set; }
    public int TaxFilingsSubmitted { get; set; }
    public int LoginCount { get; set; }
    public string EngagementLevel { get; set; } = string.Empty;
}

public class ReportSummary
{
    public int TotalRecords { get; set; }
    public decimal TotalAmount { get; set; }
    public string FormattedTotalAmount { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

// Note: Enums are defined in BettsTax.Data.Enums