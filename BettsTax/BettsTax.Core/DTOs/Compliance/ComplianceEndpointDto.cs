using System.ComponentModel.DataAnnotations;
using BettsTax.Data;
using BettsTax.Data.Models;
using ComplianceRiskLevel = BettsTax.Data.Models.ComplianceRiskLevel;

namespace BettsTax.Core.DTOs.Compliance;

public class ComplianceStatusSummaryDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public decimal OverallComplianceScore { get; set; }
    public string OverallComplianceLevel { get; set; } = string.Empty;
    public string ComplianceGrade { get; set; } = string.Empty;
    public DateTime LastCalculated { get; set; }
    
    // Summary metrics
    public int TotalFilingsRequired { get; set; }
    public int OnTimeFilings { get; set; }
    public int LateFilings { get; set; }
    public int PendingFilings { get; set; }
    public decimal TotalPenalties { get; set; }
    public decimal PotentialPenalties { get; set; }
    
    // Document status
    public int TotalDocumentsRequired { get; set; }
    public int DocumentsSubmitted { get; set; }
    public int DocumentsPending { get; set; }
    public int DocumentsRejected { get; set; }
    
    // Additional properties for compatibility
    public ComplianceLevel ComplianceLevel { get; set; }
    
    // Payment status
    public decimal TotalPaymentsDue { get; set; }
    public decimal PaymentsMade { get; set; }
    public decimal PaymentsOverdue { get; set; }
    
    // Upcoming items
    public int UpcomingDeadlinesCount { get; set; }
    public DateTime? NextDeadline { get; set; }
    public int DaysToNextDeadline { get; set; }
    
    // Risk indicators
    public ComplianceRiskLevel RiskLevel { get; set; }
    public int ActiveAlertsCount { get; set; }
    public int HighPriorityAlertsCount { get; set; }
    
    public List<ComplianceAlertDto> RecentAlerts { get; set; } = new();
    public List<UpcomingDeadlineDto> NearestDeadlines { get; set; } = new();
}

public class FilingChecklistDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public int TaxYear { get; set; }
    public DateTime GeneratedAt { get; set; }
    
    public List<FilingChecklistItemDto> ChecklistItems { get; set; } = new();
    public FilingChecklistSummaryDto Summary { get; set; } = new();
}

public class FilingChecklistItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FilingChecklistItemType ItemType { get; set; }
    public string ItemTypeName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsOverdue { get; set; }
    
    // Document-specific fields
    public int? DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public DocumentStatus? DocumentStatus { get; set; }
    public string? DocumentStatusName { get; set; }
    
    // Filing-specific fields
    public int? FilingId { get; set; }
    public BettsTax.Data.FilingStatus FilingStatus { get; set; }
    public string? FilingStatusName { get; set; }
    
    // Payment-specific fields
    public int? PaymentId { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentStatusName { get; set; }
    
    public string Notes { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
}

public class FilingChecklistSummaryDto
{
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int PendingItems { get; set; }
    public int OverdueItems { get; set; }
    public decimal CompletionPercentage { get; set; }
    public bool IsReadyForFiling { get; set; }
    public List<string> BlockingItems { get; set; } = new();
    public DateTime? EstimatedCompletionDate { get; set; }
}

public class UpcomingDeadlinesDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<UpcomingDeadlineDto> Deadlines { get; set; } = new();
    public UpcomingDeadlinesSummaryDto Summary { get; set; } = new();
}

public class UpcomingDeadlinesSummaryDto
{
    public int TotalDeadlines { get; set; }
    public int DeadlinesNext7Days { get; set; }
    public int DeadlinesNext30Days { get; set; }
    public int OverdueDeadlines { get; set; }
    public int HighPriorityDeadlines { get; set; }
    public decimal TotalEstimatedLiability { get; set; }
    public decimal TotalPotentialPenalties { get; set; }
}

public class DocumentTrackerDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<DocumentTrackerItemDto> Documents { get; set; } = new();
    public DocumentTrackerSummaryDto Summary { get; set; } = new();
    
    // Additional properties for compatibility
    public int TotalRequired { get; set; }
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Pending { get; set; }
    public int Overdue { get; set; }
    public List<DocumentStatusDto> DocumentStatuses { get; set; } = new();
}

public class DocumentTrackerItemDto
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public TaxType? TaxType { get; set; }
    public string? TaxTypeName { get; set; }
    public DocumentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public int DaysInCurrentStatus { get; set; }
    public bool IsOverdue { get; set; }
    public string? RejectionReason { get; set; }
    public int RevisionCount { get; set; }
    public List<DocumentStatusHistoryDto> StatusHistory { get; set; } = new();
}

public class DocumentStatusHistoryDto
{
    public DocumentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? Notes { get; set; }
}

public class DocumentTrackerSummaryDto
{
    public int TotalDocuments { get; set; }
    public int SubmittedDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int OverdueDocuments { get; set; }
    public decimal SubmissionRate { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal AverageReviewTime { get; set; }
    public Dictionary<string, int> DocumentsByType { get; set; } = new();
    public Dictionary<string, int> DocumentsByStatus { get; set; } = new();
}

public class DeadlineAdherenceHistoryDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<DeadlineAdherenceMonthDto> MonthlyData { get; set; } = new();
    public DeadlineAdherenceHistorySummaryDto Summary { get; set; } = new();
    
    // Additional properties for compatibility
    public decimal OverallAdherenceRate { get; set; }
    public int TotalDeadlines { get; set; }
    public int MetDeadlines { get; set; }
}

public class DeadlineAdherenceMonthDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int TotalDeadlines { get; set; }
    public int OnTimeFilings { get; set; }
    public int LateFilings { get; set; }
    public int MissedDeadlines { get; set; }
    public decimal OnTimePercentage { get; set; }
    public decimal AverageDaysEarly { get; set; }
    public decimal AverageDaysLate { get; set; }
    public decimal TotalPenalties { get; set; }
    public List<DeadlineAdherenceDetailDto> Details { get; set; } = new();
}

public class DeadlineAdherenceDetailDto
{
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? FilingDate { get; set; }
    public int DaysEarlyLate { get; set; }
    public bool IsOnTime { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string FilingStatus { get; set; } = string.Empty;
}

public class DeadlineAdherenceHistorySummaryDto
{
    public decimal OverallOnTimePercentage { get; set; }
    public decimal AverageOnTimePercentage { get; set; }
    public decimal BestMonthPercentage { get; set; }
    public decimal WorstMonthPercentage { get; set; }
    public string BestMonth { get; set; } = string.Empty;
    public string WorstMonth { get; set; } = string.Empty;
    public decimal TotalPenalties { get; set; }
    public decimal AverageMonthlyPenalties { get; set; }
    public string Trend { get; set; } = string.Empty; // "Improving", "Declining", "Stable"
    public List<string> Recommendations { get; set; } = new();
}

public class PenaltyWarningDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? FilingDate { get; set; }
    public int DaysLate { get; set; }
    public decimal TaxLiability { get; set; }
    public decimal CurrentPenalty { get; set; }
    public decimal ProjectedPenalty { get; set; }
    public decimal DailyPenaltyRate { get; set; }
    public string PenaltyType { get; set; } = string.Empty;
    public ComplianceRiskLevel Severity { get; set; }
    public string SeverityName { get; set; } = string.Empty;
    public string WarningMessage { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public bool CanBeWaived { get; set; }
    public string? WaiverConditions { get; set; }
    public DateTime CalculatedAt { get; set; }
    
    // Additional properties for compatibility
    public string RecommendedAction { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialPenalty { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class PenaltySimulationRequestDto
{
    public int ClientId { get; set; }
    public TaxType TaxType { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TaxLiability { get; set; }
    public DateTime? ProposedFilingDate { get; set; }
    public bool IncludeInterest { get; set; } = true;
    public bool IncludeAdditionalPenalties { get; set; } = true;
}

public class PenaltySimulationResultDto
{
    public PenaltySimulationRequestDto Request { get; set; } = new();
    public int DaysLate { get; set; }
    public decimal BasePenalty { get; set; }
    public decimal InterestPenalty { get; set; }
    public decimal AdditionalPenalties { get; set; }
    public decimal TotalPenalty { get; set; }
    public string PenaltyBreakdown { get; set; } = string.Empty;
    public List<PenaltySimulationScenarioDto> AlternativeScenarios { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

public class PenaltySimulationScenarioDto
{
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime FilingDate { get; set; }
    public int DaysLate { get; set; }
    public decimal TotalPenalty { get; set; }
    public decimal PenaltySavings { get; set; }
    public string Description { get; set; } = string.Empty;
}

public enum FilingChecklistItemType
{
    Document,
    Filing,
    Payment,
    Review,
    Approval,
    Other
}

public class DocumentStatusDto
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsRequired { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public string? RejectionReason { get; set; }
    public List<string> RequiredActions { get; set; } = new();
    
    // Additional property for compatibility
    public DateTime? SubmittedDate { get; set; }
}
