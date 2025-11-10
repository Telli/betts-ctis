using BettsTax.Data;

namespace BettsTax.Core.DTOs.Reports;

// Document Submission Report DTOs
public class DocumentSubmissionReportDataDto : ReportDataDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DocumentSubmissionReportItem> Documents { get; set; } = new();
    public DocumentSubmissionSummary Summary { get; set; } = new();
}

public class DocumentSubmissionReportItem
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReviewedBy { get; set; } = string.Empty;
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewNotes { get; set; }
    public bool IsOnTime => DueDate.HasValue && SubmittedDate <= DueDate.Value;
    public int? DaysFromDue => DueDate.HasValue ? (int)(SubmittedDate - DueDate.Value).TotalDays : null;
    public string ComplianceStatus => IsOnTime ? "On Time" : DaysFromDue.HasValue ? $"{Math.Abs(DaysFromDue.Value)} days {(DaysFromDue > 0 ? "late" : "early")}" : "No Due Date";
}

public class DocumentSubmissionSummary
{
    public int TotalDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int OnTimeSubmissions { get; set; }
    public int LateSubmissions { get; set; }
    public decimal OnTimePercentage { get; set; }
    public decimal ApprovalRate { get; set; }
    public double AverageProcessingDays { get; set; }
}

// Tax Calendar Summary Report DTOs
public class TaxCalendarReportDataDto : ReportDataDto
{
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public int TaxYear { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<TaxCalendarReportItem> Deadlines { get; set; } = new();
    public TaxCalendarSummary Summary { get; set; } = new();
}

public class TaxCalendarReportItem
{
    public DateTime DueDate { get; set; }
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public FilingStatus? Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? CompletedDate { get; set; }
    public bool IsOverdue => DateTime.UtcNow > DueDate && Status != FilingStatus.Submitted;
    public int DaysUntilDue => (int)(DueDate - DateTime.UtcNow).TotalDays;
    public string UrgencyLevel => DaysUntilDue < 0 ? "Overdue" : DaysUntilDue <= 7 ? "Urgent" : DaysUntilDue <= 30 ? "Due Soon" : "Upcoming";
}

public class TaxCalendarSummary
{
    public int TotalDeadlines { get; set; }
    public int CompletedDeadlines { get; set; }
    public int OverdueDeadlines { get; set; }
    public int UpcomingDeadlines { get; set; }
    public int UrgentDeadlines { get; set; }
    public decimal CompletionRate { get; set; }
    public Dictionary<TaxType, int> DeadlinesByTaxType { get; set; } = new();
}

// Client Compliance Overview Report DTOs
public class ClientComplianceOverviewReportDataDto : ReportDataDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<ClientComplianceOverviewItem> Clients { get; set; } = new();
    public ComplianceOverviewSummary Summary { get; set; } = new();
}

public class ClientComplianceOverviewItem
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public decimal OverallComplianceScore { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; }
    public int TotalFilings { get; set; }
    public int OnTimeFilings { get; set; }
    public int LateFilings { get; set; }
    public int TotalPayments { get; set; }
    public int OnTimePayments { get; set; }
    public decimal TotalPenalties { get; set; }
    public DateTime LastActivity { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> ComplianceIssues { get; set; } = new();
}

public class ComplianceOverviewSummary
{
    public int TotalClients { get; set; }
    public int HighRiskClients { get; set; }
    public int MediumRiskClients { get; set; }
    public int LowRiskClients { get; set; }
    public decimal AverageComplianceScore { get; set; }
    public decimal TotalPenaltiesIssued { get; set; }
    public int TotalOverdueItems { get; set; }
    public Dictionary<ComplianceLevel, int> ClientsByComplianceLevel { get; set; } = new();
}

// Revenue Report DTOs
public class RevenueReportDataDto : ReportDataDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<RevenueReportItem> RevenueItems { get; set; } = new();
    public RevenueSummary Summary { get; set; } = new();
}

public class RevenueReportItem
{
    public DateTime Date { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
}

public class RevenueSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal CollectedRevenue { get; set; }
    public decimal PendingRevenue { get; set; }
    public decimal RefundedRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public Dictionary<TaxType, decimal> RevenueByTaxType { get; set; } = new();
    public Dictionary<PaymentMethod, decimal> RevenueByMethod { get; set; } = new();
    public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();
}

// Case Management Report DTOs (requires CaseIssue model)
public class CaseManagementReportDataDto : ReportDataDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<CaseManagementReportItem> Cases { get; set; } = new();
    public CaseManagementSummary Summary { get; set; } = new();
}

public class CaseManagementReportItem
{
    public int CaseId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DaysOpen => ResolvedDate.HasValue ? (int)(ResolvedDate.Value - CreatedDate).TotalDays : (int)(DateTime.UtcNow - CreatedDate).TotalDays;
    public bool IsOverdue { get; set; }
}

public class CaseManagementSummary
{
    public int TotalCases { get; set; }
    public int OpenCases { get; set; }
    public int ResolvedCases { get; set; }
    public int OverdueCases { get; set; }
    public double AverageResolutionDays { get; set; }
    public Dictionary<string, int> CasesByPriority { get; set; } = new();
    public Dictionary<string, int> CasesByStatus { get; set; } = new();
    public Dictionary<string, int> CasesByType { get; set; } = new();
}

// Enhanced Activity Report DTOs
public class EnhancedClientActivityReportDataDto : ReportDataDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? ClientFilter { get; set; }
    public string? ActivityTypeFilter { get; set; }
    public List<EnhancedClientActivityReportItem> Activities { get; set; } = new();
    public EnhancedActivitySummary Summary { get; set; } = new();
}

public class EnhancedClientActivityReportItem
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
    public int MessagesExchanged { get; set; }
    public decimal EngagementScore { get; set; }
    public string EngagementLevel { get; set; } = string.Empty;
    public List<string> RecentActivities { get; set; } = new();
}

public class EnhancedActivitySummary
{
    public int TotalClients { get; set; }
    public int ActiveClients { get; set; }
    public int InactiveClients { get; set; }
    public decimal AverageEngagementScore { get; set; }
    public int TotalActivities { get; set; }
    public Dictionary<string, int> ActivitiesByType { get; set; } = new();
    public Dictionary<string, int> ClientsByEngagementLevel { get; set; } = new();
}
