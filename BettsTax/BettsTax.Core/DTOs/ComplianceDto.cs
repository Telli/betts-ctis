using BettsTax.Data;
using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.DTOs
{
    public class ComplianceTrackerDto
    {
        public int ComplianceTrackerId { get; set; }
        public int ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientNumber { get; set; }
        public int TaxYearId { get; set; }
        public int TaxYear { get; set; }
        public TaxType TaxType { get; set; }
        public string TaxTypeName { get; set; } = string.Empty;
        
        // Status and risk
        public ComplianceStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public ComplianceRiskLevel RiskLevel { get; set; }
        public decimal ComplianceScore { get; set; }
        public string RiskLevelDescription { get; set; } = string.Empty;
        
        // Key dates and status
        public DateTime FilingDueDate { get; set; }
        public DateTime PaymentDueDate { get; set; }
        public DateTime? FiledDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? ExtendedDueDate { get; set; }
        
        // Completion status
        public bool IsFilingRequired { get; set; }
        public bool IsPaymentRequired { get; set; }
        public bool IsFilingComplete { get; set; }
        public bool IsPaymentComplete { get; set; }
        public bool IsDocumentationComplete { get; set; }
        public bool HasExtension { get; set; }
        public bool HasExemption { get; set; }
        public string? ExemptionReason { get; set; }
        
        // Overdue tracking
        public int DaysOverdueForFiling { get; set; }
        public int DaysOverdueForPayment { get; set; }
        public bool IsOverdue => DaysOverdueForFiling > 0 || DaysOverdueForPayment > 0;
        
        // Financial amounts
        public decimal TaxLiability { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal TotalPenaltiesOwed { get; set; }
        public decimal TotalPenaltiesPaid { get; set; }
        public decimal OutstandingPenalties { get; set; }
        
        // Alerts and actions
        public int ActiveAlertsCount { get; set; }
        public int CriticalAlertsCount { get; set; }
        public int PendingActionsCount { get; set; }
        public int OverdueActionsCount { get; set; }
        
        // Summary information
        public List<ComplianceAlertSummaryDto> RecentAlerts { get; set; } = new();
        public List<ComplianceActionSummaryDto> UpcomingActions { get; set; } = new();
        public List<string> ComplianceIssues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        
        public DateTime LastUpdated { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ComplianceAlertDto
    {
        public int ComplianceAlertId { get; set; }
        public int ComplianceTrackerId { get; set; }
        public ComplianceAlertSeverity Severity { get; set; }
        public string SeverityDescription { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        public DateTime AlertDate { get; set; }
        public DateTime? DueDate { get; set; }
        public int? DaysUntilDue { get; set; }
        public bool IsActive { get; set; }
        public bool IsRead { get; set; }
        public bool IsActionRequired { get; set; }
        public bool IsOverdue { get; set; }
        
        public string? ActionUrl { get; set; }
        public string? ActionButtonText { get; set; }
        public bool IsSystemGenerated { get; set; }
        public string? GeneratedBy { get; set; }
        
        public DateTime? ReadDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        
        // Related client information
        public string? ClientName { get; set; }
        public TaxType? TaxType { get; set; }
        public string? TaxTypeName { get; set; }
    }

    public class ComplianceAlertSummaryDto
    {
        public int ComplianceAlertId { get; set; }
        public ComplianceAlertSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime AlertDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsActionRequired { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class ComplianceActionDto
    {
        public int ComplianceActionId { get; set; }
        public int ComplianceTrackerId { get; set; }
        public ComplianceActionType ActionType { get; set; }
        public string ActionTypeDescription { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public int Priority { get; set; }
        public string PriorityDescription { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysUntilDue { get; set; }
        
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? CompletionNotes { get; set; }
        
        public string? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
        public string? ActionUrl { get; set; }
        public bool IsSystemGenerated { get; set; }
        
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Related client information
        public string? ClientName { get; set; }
        public TaxType? TaxType { get; set; }
        public string? TaxTypeName { get; set; }
    }

    public class ComplianceActionSummaryDto
    {
        public int ComplianceActionId { get; set; }
        public ComplianceActionType ActionType { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int Priority { get; set; }
        public bool IsOverdue { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class CompliancePenaltyDto
    {
        public int CompliancePenaltyId { get; set; }
        public int ComplianceTrackerId { get; set; }
        public PenaltyType PenaltyType { get; set; }
        public string PenaltyTypeDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        
        // Calculation details
        public decimal? PenaltyRate { get; set; }
        public decimal? BaseAmount { get; set; }
        public int? DaysOverdue { get; set; }
        public string? CalculationBreakdown { get; set; }
        
        public bool IsPaid { get; set; }
        public bool IsWaived { get; set; }
        public string? WaiverReason { get; set; }
        
        public DateTime PenaltyDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? WaivedDate { get; set; }
        
        // Related client information
        public string? ClientName { get; set; }
        public TaxType? TaxType { get; set; }
        public string? TaxTypeName { get; set; }
    }

    public class ComplianceInsightDto
    {
        public int ComplianceInsightId { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public int? ComplianceTrackerId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        
        public ComplianceRiskLevel RiskLevel { get; set; }
        public string RiskLevelDescription { get; set; } = string.Empty;
        public decimal? PotentialSavings { get; set; }
        public decimal? PotentialPenalty { get; set; }
        
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        
        public bool IsActive { get; set; }
        public bool IsRead { get; set; }
        public bool IsImplemented { get; set; }
        public DateTime? ImplementedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        
        public bool IsSystemGenerated { get; set; }
        public string? GeneratedBy { get; set; }
    }

    public class ComplianceDashboardDto
    {
        // Overall statistics
        public int TotalClients { get; set; }
        public int CompliantClients { get; set; }
        public int AtRiskClients { get; set; }
        public int NonCompliantClients { get; set; }
        public decimal OverallComplianceRate { get; set; }
        
        // Financial overview
        public decimal TotalTaxLiability { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalPenaltiesOwed { get; set; }
        public decimal TotalPenaltiesPaid { get; set; }
        public decimal TotalOutstandingPenalties { get; set; }
        
        // Alerts and actions
        public int TotalActiveAlerts { get; set; }
        public int CriticalAlerts { get; set; }
        public int WarningAlerts { get; set; }
        public int TotalPendingActions { get; set; }
        public int OverdueActions { get; set; }
        
        // Breakdown by tax type
        public Dictionary<TaxType, ComplianceStatsByTaxType> StatsByTaxType { get; set; } = new();
        
        // Recent activity
        public List<ComplianceAlertDto> RecentAlerts { get; set; } = new();
        public List<ComplianceActionDto> UpcomingActions { get; set; } = new();
        public List<ComplianceInsightDto> ActiveInsights { get; set; } = new();
        
        // Trends (last 12 months)
        public List<ComplianceTrendDto> ComplianceTrends { get; set; } = new();
        public List<PenaltyTrendDto> PenaltyTrends { get; set; } = new();
        
        // Risk analysis
        public List<RiskAnalysisDto> RiskAnalysis { get; set; } = new();
        
        public DateTime LastUpdated { get; set; }
    }

    public class ComplianceStatsByTaxType
    {
        public TaxType TaxType { get; set; }
        public int TotalClients { get; set; }
        public int CompliantClients { get; set; }
        public int NonCompliantClients { get; set; }
        public decimal ComplianceRate { get; set; }
        public decimal TotalLiability { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalPenalties { get; set; }
    }

    public class ComplianceTrendDto
    {
        public DateTime Period { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal ComplianceRate { get; set; }
        public int TotalClients { get; set; }
        public int CompliantClients { get; set; }
        public int NonCompliantClients { get; set; }
    }

    public class PenaltyTrendDto
    {
        public DateTime Period { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        public decimal TotalPenalties { get; set; }
        public decimal PenaltiesPaid { get; set; }
        public int ClientsWithPenalties { get; set; }
    }

    public class RiskAnalysisDto
    {
        public string RiskCategory { get; set; } = string.Empty;
        public ComplianceRiskLevel RiskLevel { get; set; }
        public int AffectedClients { get; set; }
        public decimal PotentialImpact { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }

    // Request DTOs
    public class UpdateComplianceStatusDto
    {
        [Required]
        public int ComplianceTrackerId { get; set; }
        
        public ComplianceStatus? Status { get; set; }
        public bool? IsFilingComplete { get; set; }
        public bool? IsPaymentComplete { get; set; }
        public bool? IsDocumentationComplete { get; set; }
        public DateTime? FiledDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateComplianceAlertDto
    {
        [Required]
        public int ComplianceTrackerId { get; set; }
        
        [Required]
        public ComplianceAlertSeverity Severity { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public DateTime? DueDate { get; set; }
        public bool IsActionRequired { get; set; } = true;
        public string? ActionUrl { get; set; }
    }

    public class CreateComplianceActionDto
    {
        [Required]
        public int ComplianceTrackerId { get; set; }
        
        [Required]
        public ComplianceActionType ActionType { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime DueDate { get; set; }
        
        public int Priority { get; set; } = 1;
        public string? AssignedTo { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class CalculatePenaltyDto
    {
        [Required]
        public TaxType TaxType { get; set; }
        
        [Required]
        public PenaltyType PenaltyType { get; set; }
        
        [Required]
        public decimal BaseAmount { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        
        public DateTime? ActualDate { get; set; }
        public TaxpayerCategory? TaxpayerCategory { get; set; }
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }

    public class PenaltyCalculationResultDto
    {
        public decimal PenaltyAmount { get; set; }
        public decimal? PenaltyRate { get; set; }
        public decimal BaseAmount { get; set; }
        public int DaysOverdue { get; set; }
        public PenaltyType PenaltyType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CalculationMethod { get; set; } = string.Empty;
        public string LegalReference { get; set; } = string.Empty;
        public List<string> CalculationSteps { get; set; } = new();
        public DateTime CalculationDate { get; set; }
    }

    public class ComplianceFilterDto
    {
        public ComplianceStatus? Status { get; set; }
        public ComplianceRiskLevel? RiskLevel { get; set; }
        public TaxType? TaxType { get; set; }
        public int? TaxYearId { get; set; }
        public bool? HasPenalties { get; set; }
        public bool? IsOverdue { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
    }
}