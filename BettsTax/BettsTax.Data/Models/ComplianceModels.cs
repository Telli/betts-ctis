using BettsTax.Data;
using System.ComponentModel.DataAnnotations;
using ComplianceAlertType = BettsTax.Data.ComplianceAlertType;
using DeadlinePriority = BettsTax.Data.DeadlinePriority;

namespace BettsTax.Data.Models;


public class ComplianceDeadline
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    public TaxType TaxType { get; set; }
    
    [Required]
    public DateTime DueDate { get; set; }
    
    [Required]
    public FilingStatus Status { get; set; } = FilingStatus.Draft;
    
    public decimal EstimatedTaxLiability { get; set; }
    
    public bool DocumentsReady { get; set; } = false;
    
    public DeadlinePriority Priority { get; set; } = DeadlinePriority.Medium;
    
    [StringLength(1000)]
    public string Requirements { get; set; } = string.Empty;
    
    public DateTime? NotificationSentAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(450)]
    public string? CompletedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? CompletedByUser { get; set; }
}

public class ComplianceAlert
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    public ComplianceAlertType AlertType { get; set; }
    
    [Required]
    public ComplianceAlertSeverity Severity { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    public TaxType? TaxType { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public decimal? PenaltyAmount { get; set; }
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    [StringLength(450)]
    public string? ResolvedBy { get; set; }
    
    [StringLength(1000)]
    public string? Resolution { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? ResolvedByUser { get; set; }
}

public class ComplianceActionItem
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public CompliancePriority Priority { get; set; }
    
    public TaxType? TaxType { get; set; }
    
    [Required]
    public DateTime DueDate { get; set; }
    
    [Required]
    public ComplianceActionStatus Status { get; set; } = ComplianceActionStatus.Open;
    
    [StringLength(450)]
    public string? AssignedTo { get; set; }
    
    public decimal? EstimatedImpact { get; set; }
    
    [Required]
    [StringLength(450)]
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(1000)]
    public string? CompletionNotes { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class FinanceAct2025Rule
{
    [Key]
    [StringLength(50)]
    public string RuleId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string RuleName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Section { get; set; } = string.Empty;
    
    [Required]
    public TaxType TaxType { get; set; }
    
    [Required]
    public decimal PenaltyRate { get; set; }
    
    [Required]
    public decimal InterestRate { get; set; }
    
    public int GracePeriodDays { get; set; } = 0;
    
    public decimal MaxPenaltyPercentage { get; set; } = 100;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DateTime EffectiveDate { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ComplianceCalculation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    public DateTime CalculationDate { get; set; }
    
    [Required]
    public decimal FilingScore { get; set; }
    
    [Required]
    public decimal PaymentScore { get; set; }
    
    [Required]
    public decimal DocumentScore { get; set; }
    
    [Required]
    public decimal TimelinessScore { get; set; }
    
    [Required]
    public decimal OverallScore { get; set; }
    
    [Required]
    public ComplianceLevel Level { get; set; }
    
    [Required]
    public ComplianceRiskLevel RiskLevel { get; set; }
    
    public decimal ProjectedPenalties { get; set; }
    
    [StringLength(1000)]
    public string CalculationNotes { get; set; } = string.Empty;
    
    [StringLength(450)]
    public string CalculatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? CalculatedByUser { get; set; }
}

public class PenaltyCalculation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ClientId { get; set; }
    
    [Required]
    public TaxType TaxType { get; set; }
    
    [Required]
    public DateTime DueDate { get; set; }
    
    public DateTime? FilingDate { get; set; }
    
    public int DaysLate { get; set; }
    
    [Required]
    public decimal TaxLiability { get; set; }
    
    [Required]
    public decimal BasePenalty { get; set; }
    
    [Required]
    public decimal InterestPenalty { get; set; }
    
    public decimal AdditionalPenalties { get; set; }
    
    [Required]
    public decimal TotalPenalty { get; set; }
    
    [StringLength(1000)]
    public string PenaltyBreakdown { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ApplicableRuleId { get; set; } = string.Empty;
    
    public bool IsWaivable { get; set; } = false;
    
    [StringLength(500)]
    public string WaiverConditions { get; set; } = string.Empty;
    
    public bool IsPaid { get; set; } = false;
    
    public DateTime? PaidDate { get; set; }
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string CalculatedBy { get; set; } = string.Empty;
    
    // Navigation properties
    public Client? Client { get; set; }
    public FinanceAct2025Rule? ApplicableRule { get; set; }
    public ApplicationUser? CalculatedByUser { get; set; }
}