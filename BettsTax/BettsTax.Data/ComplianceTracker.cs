using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum ComplianceStatus
    {
        Compliant,
        AtRisk,
        NonCompliant,
        PenaltyApplied,
        UnderReview,
        Exempted
    }

    public enum ComplianceRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum PenaltyType
    {
        LateFilingPenalty,
        LatePaymentPenalty,
        UnderDeclarationPenalty,
        NonFilingPenalty,
        Interest,
        AdministrativePenalty,
        CriminalPenalty
    }

    public enum ComplianceActionType
    {
        FileReturn,
        MakePayment,
        SubmitDocuments,
        RespondToNotice,
        AttendHearing,
        PayPenalty,
        RequestExtension,
        RegisterForTax,
        UpdateInformation
    }

    public enum ComplianceAlertSeverity
    {
        Info,
        Warning,
        Critical,
        Urgent
    }

    // Main compliance tracking entity for each client
    public class ComplianceTracker
    {
        public int ComplianceTrackerId { get; set; }
        
        public int ClientId { get; set; }
        public int TaxYearId { get; set; }
        public TaxType TaxType { get; set; }
        
        // Overall compliance status
        public ComplianceStatus Status { get; set; } = ComplianceStatus.Compliant;
        public ComplianceRiskLevel RiskLevel { get; set; } = ComplianceRiskLevel.Low;
        public decimal ComplianceScore { get; set; } = 100m; // 0-100 scale
        
        // Key dates
        public DateTime FilingDueDate { get; set; }
        public DateTime PaymentDueDate { get; set; }
        public DateTime? FiledDate { get; set; }
        public DateTime? PaidDate { get; set; }
        
        // Status tracking
        public bool IsFilingRequired { get; set; } = true;
        public bool IsPaymentRequired { get; set; } = true;
        public bool IsFilingComplete { get; set; } = false;
        public bool IsPaymentComplete { get; set; } = false;
        public bool IsDocumentationComplete { get; set; } = false;
        
        // Penalty tracking
        public bool HasPenalties { get; set; } = false;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPenaltiesOwed { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPenaltiesPaid { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingPenalties => TotalPenaltiesOwed - TotalPenaltiesPaid;
        
        // Days overdue
        public int DaysOverdueForFiling => IsFilingComplete ? 0 : Math.Max(0, (DateTime.UtcNow.Date - FilingDueDate.Date).Days);
        public int DaysOverdueForPayment => IsPaymentComplete ? 0 : Math.Max(0, (DateTime.UtcNow.Date - PaymentDueDate.Date).Days);
        
        // Amounts
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxLiability { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingBalance => TaxLiability - AmountPaid;
        
        // Extensions and exemptions
        public bool HasExtension { get; set; } = false;
        public DateTime? ExtendedDueDate { get; set; }
        public bool HasExemption { get; set; } = false;
        [MaxLength(500)]
        public string? ExemptionReason { get; set; }
        
        // Metadata
        [MaxLength(1000)]
        public string? Notes { get; set; }
        [MaxLength(2000)]
        public string? ComplianceDetails { get; set; } // JSON with detailed compliance info
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Client? Client { get; set; }
        public TaxYear? TaxYear { get; set; }
        public List<CompliancePenalty> Penalties { get; set; } = new();
        public List<ComplianceAlert> Alerts { get; set; } = new();
        public List<ComplianceAction> Actions { get; set; } = new();
    }

    // Penalty calculations and tracking
    public class CompliancePenalty
    {
        public int CompliancePenaltyId { get; set; }
        
        public int ComplianceTrackerId { get; set; }
        public PenaltyType PenaltyType { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount => Amount - AmountPaid;
        
        // Calculation details
        [Column(TypeName = "decimal(5,4)")]
        public decimal? PenaltyRate { get; set; } // As percentage
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BaseAmount { get; set; } // Amount penalty is calculated on
        public int? DaysOverdue { get; set; }
        
        // Status
        public bool IsPaid { get; set; } = false;
        public bool IsWaived { get; set; } = false;
        [MaxLength(500)]
        public string? WaiverReason { get; set; }
        
        // Dates
        public DateTime PenaltyDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? WaivedDate { get; set; }
        
        [MaxLength(1000)]
        public string? CalculationDetails { get; set; } // JSON with calculation breakdown
        
        // Navigation properties
        public ComplianceTracker? ComplianceTracker { get; set; }
    }

    // Compliance alerts and notifications
    public class ComplianceAlert
    {
        public int ComplianceAlertId { get; set; }
        
        public int ComplianceTrackerId { get; set; }
        public ComplianceAlertSeverity Severity { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        // Alert details
        public DateTime AlertDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsRead { get; set; } = false;
        public bool IsActionRequired { get; set; } = true;
        
        // Auto-generated or manual
        public bool IsSystemGenerated { get; set; } = true;
        [MaxLength(100)]
        public string? GeneratedBy { get; set; }
        
        [MaxLength(500)]
        public string? ActionUrl { get; set; } // Link to resolve the alert
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON with additional alert data
        
        public DateTime? ReadDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        
        // Navigation properties
        public ComplianceTracker? ComplianceTracker { get; set; }
    }

    // Required compliance actions
    public class ComplianceAction
    {
        public int ComplianceActionId { get; set; }
        
        public int ComplianceTrackerId { get; set; }
        public ComplianceActionType ActionType { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        // Priority and timing
        public int Priority { get; set; } = 1; // 1 = highest priority
        public DateTime DueDate { get; set; }
        public bool IsOverdue => DateTime.UtcNow.Date > DueDate.Date && !IsCompleted;
        public int DaysUntilDue => (DueDate.Date - DateTime.UtcNow.Date).Days;
        
        // Status
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedDate { get; set; }
        [MaxLength(500)]
        public string? CompletionNotes { get; set; }
        
        // System or manual action
        public bool IsSystemGenerated { get; set; } = true;
        [MaxLength(100)]
        public string? AssignedTo { get; set; } // UserId of responsible person
        
        [MaxLength(500)]
        public string? ActionUrl { get; set; } // Link to complete the action
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON with action-specific data
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ComplianceTracker? ComplianceTracker { get; set; }
    }

    // Sierra Leone Finance Act penalty rates and rules
    public class PenaltyRule
    {
        public int PenaltyRuleId { get; set; }
        
        public TaxType TaxType { get; set; }
        public PenaltyType PenaltyType { get; set; }
        public TaxpayerCategory? TaxpayerCategory { get; set; } // null means applies to all
        
        [Required]
        [MaxLength(200)]
        public string RuleName { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        
        // Penalty calculation
        [Column(TypeName = "decimal(5,4)")]
        public decimal? FixedRate { get; set; } // Fixed percentage
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixedAmount { get; set; } // Fixed amount
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumAmount { get; set; }
        
        // Time-based calculation
        public bool IsTimeBased { get; set; } = false;
        [Column(TypeName = "decimal(5,4)")]
        public decimal? DailyRate { get; set; }
        [Column(TypeName = "decimal(5,4)")]
        public decimal? MonthlyRate { get; set; }
        public int? GracePeriodDays { get; set; } = 0;
        public int? MaximumDays { get; set; }
        
        // Conditions
        public int? ThresholdDays { get; set; } // Penalty applies after X days
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ThresholdAmount { get; set; } // Penalty applies if amount > X
        
        // Effective period
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } = 1; // Lower number = higher priority
        
        [MaxLength(2000)]
        public string? CalculationFormula { get; set; } // JSON with calculation logic
        [MaxLength(500)]
        public string? LegalReference { get; set; } // Reference to Finance Act section
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    // Compliance insights and recommendations
    public class ComplianceInsight
    {
        public int ComplianceInsightId { get; set; }
        
        public int? ClientId { get; set; } // null for system-wide insights
        public int? ComplianceTrackerId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Recommendation { get; set; } = string.Empty;
        
        // Insight metadata
        public ComplianceRiskLevel RiskLevel { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PotentialSavings { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PotentialPenalty { get; set; }
        
        // Categorization
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty; // "Tax Planning", "Penalty Avoidance", etc.
        [MaxLength(100)]
        public string? Tags { get; set; } // Comma-separated tags
        
        // Status
        public bool IsActive { get; set; } = true;
        public bool IsRead { get; set; } = false;
        public bool IsImplemented { get; set; } = false;
        public DateTime? ImplementedDate { get; set; }
        
        // System generated or manual
        public bool IsSystemGenerated { get; set; } = true;
        [MaxLength(100)]
        public string? GeneratedBy { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        
        [MaxLength(2000)]
        public string? Metadata { get; set; } // JSON with additional insight data
        
        // Navigation properties
        public Client? Client { get; set; }
        public ComplianceTracker? ComplianceTracker { get; set; }
    }
}