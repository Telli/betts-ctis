using BettsTax.Data;

namespace BettsTax.Core.DTOs.Compliance;

public class ComplianceOverviewDto
{
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string TIN { get; set; } = string.Empty;
    public decimal OverallComplianceScore { get; set; }
    public ComplianceLevel ComplianceLevel { get; set; }
    public string ComplianceGrade { get; set; } = string.Empty;
    public DateTime LastCalculated { get; set; }
    public List<TaxTypeComplianceDto> TaxTypeCompliance { get; set; } = new();
    public List<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
    public List<ComplianceAlertDto> ActiveAlerts { get; set; } = new();
    public ComplianceRiskAssessmentDto RiskAssessment { get; set; } = new();
}

public class TaxTypeComplianceDto
{
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public ComplianceLevel Level { get; set; }
    public int TotalFilingsRequired { get; set; }
    public int OnTimeFilings { get; set; }
    public int LateFilings { get; set; }
    public int MissedDeadlines { get; set; }
    public decimal TotalPenalties { get; set; }
    public DateTime LastFilingDate { get; set; }
    public DateTime NextDeadline { get; set; }
    public int DaysUntilDeadline { get; set; }
    public bool IsOverdue { get; set; }
}

public class UpcomingDeadlineDto
{
    public int Id { get; set; }
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysRemaining { get; set; }
    public ComplianceRiskLevel Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public FilingStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal EstimatedTaxLiability { get; set; }
    public bool DocumentsReady { get; set; }
    public bool IsOverdue { get; set; }
    public decimal PotentialPenalty { get; set; }
    public string Requirements { get; set; } = string.Empty;
}

public class ComplianceAlertDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string AlertTypeName { get; set; } = string.Empty;
    public ComplianceAlertSeverity Severity { get; set; }
    public string SeverityName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TaxType? TaxType { get; set; }
    public string? TaxTypeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? PenaltyAmount { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? Resolution { get; set; }
}

public class ComplianceRiskAssessmentDto
{
    public ComplianceRiskLevel RiskLevel { get; set; }
    public string RiskLevelName { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    public List<ComplianceRiskFactorDto> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public decimal ProjectedPenalties { get; set; }
    public DateTime NextReviewDate { get; set; }
}

public class ComplianceRiskFactorDto
{
    public string Factor { get; set; } = string.Empty;
    public ComplianceRiskLevel Impact { get; set; }
    public string ImpactName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? LastOccurrence { get; set; }
    public int Frequency { get; set; }
}

public class PenaltyCalculationDto
{
    public int ClientId { get; set; }
    public TaxType TaxType { get; set; }
    public string TaxTypeName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? FilingDate { get; set; }
    public int DaysLate { get; set; }
    public decimal TaxLiability { get; set; }
    public decimal BasePenalty { get; set; }
    public decimal InterestPenalty { get; set; }
    public decimal AdditionalPenalties { get; set; }
    public decimal TotalPenalty { get; set; }
    public string PenaltyBreakdown { get; set; } = string.Empty;
    public BettsTax.Data.Models.FinanceAct2025Rule ApplicableRule { get; set; } = new();
    public bool IsWaivable { get; set; }
    public string WaiverConditions { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}


public class ComplianceDashboardDto
{
    public ComplianceOverviewDto Overview { get; set; } = new();
    public List<TaxTypeComplianceDto> TaxTypeBreakdown { get; set; } = new();
    public List<UpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
    public List<ComplianceAlertDto> ActiveAlerts { get; set; } = new();
    public ComplianceTrendDto Trends { get; set; } = new();
    public List<PenaltyCalculationDto> RecentPenalties { get; set; } = new();
    public ComplianceActionPlanDto ActionPlan { get; set; } = new();
}

public class ComplianceTrendDto
{
    public List<ComplianceTrendDataPoint> ComplianceScoreHistory { get; set; } = new();
    public List<ComplianceTrendDataPoint> PenaltyHistory { get; set; } = new();
    public List<ComplianceTrendDataPoint> FilingTimelinessHistory { get; set; } = new();
    public ComplianceProjectionDto Projection { get; set; } = new();
}

public class ComplianceTrendDataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public TaxType? TaxType { get; set; }
    public string? Category { get; set; }
}

public class ComplianceProjectionDto
{
    public decimal ProjectedComplianceScore { get; set; }
    public decimal ProjectedPenalties { get; set; }
    public ComplianceLevel ProjectedLevel { get; set; }
    public DateTime ProjectionDate { get; set; }
    public string ProjectionBasis { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}

public class ComplianceActionPlanDto
{
    public List<ComplianceActionItemDto> ActionItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ComplianceRiskLevel OverallPriority { get; set; }
    public DateTime TargetCompletionDate { get; set; }
}

public class ComplianceActionItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceRiskLevel Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public TaxType? TaxType { get; set; }
    public string? TaxTypeName { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public decimal? EstimatedImpact { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNotes { get; set; }
}

// Note: Enums are defined in BettsTax.Data namespace to avoid circular dependencies