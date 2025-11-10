using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data.Models;

public class TaxPenaltyRule
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TaxType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string TaxpayerCategory { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ViolationType { get; set; } = string.Empty; // Late Filing, Late Payment, etc.
    
    [Required]
    [MaxLength(50)]
    public string PenaltyType { get; set; } = "Percentage"; // Percentage, Fixed, Mixed
    
    [Required]
    public decimal PenaltyRate { get; set; }
    
    public decimal Rate { get; set; } // Alias for PenaltyRate
    
    public decimal? FixedPenaltyAmount { get; set; }
    
    public decimal? FixedAmount { get; set; } // Alias for FixedPenaltyAmount
    
    public int? MinDaysLate { get; set; } = 0;
    
    public int? MaxDaysLate { get; set; } = 365;
    
    public decimal? MinTaxAmount { get; set; }
    
    public decimal? MaxTaxAmount { get; set; }
    
    public int MaxPenaltyDays { get; set; } = 365;
    
    public int Priority { get; set; } = 1;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiryDate { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    // Formula enhancements
    [MaxLength(2000)]
    public string? CalculationFormula { get; set; } // Custom formula for complex calculations
    
    [MaxLength(1000)]
    public string? FormulaVariables { get; set; } // JSON object defining variables used in formula
    
    public bool UseCustomFormula { get; set; } = false;
    
    // Additional penalty calculation parameters
    public decimal? DailyInterestRate { get; set; } // For compound interest calculations
    
    public bool CompoundDaily { get; set; } = false;
    
    public decimal? CapPercentage { get; set; } // Maximum penalty as percentage of tax amount
    
    public decimal? MinimumPenalty { get; set; } // Minimum penalty amount
    
    public decimal? MaximumPenalty { get; set; } // Maximum penalty amount
    
    // Grace period and escalation
    public int GracePeriodDays { get; set; } = 0;
    
    public bool HasEscalation { get; set; } = false;
    
    [MaxLength(2000)]
    public string? EscalationRules { get; set; } // JSON array of escalation tiers
    
    // Conditional logic
    [MaxLength(1000)]
    public string? Conditions { get; set; } // JSON object for conditional penalty application
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(450)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation properties
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}