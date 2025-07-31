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
}