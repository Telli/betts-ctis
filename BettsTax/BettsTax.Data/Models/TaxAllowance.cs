using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data.Models;

public class TaxAllowance
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int TaxYear { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string AllowanceType { get; set; } = string.Empty; // Personal, Dependent, Disability, etc.
    
    [Required]
    [MaxLength(100)]
    public string TaxType { get; set; } = string.Empty; // Income Tax, etc.
    
    [Required]
    [MaxLength(50)]
    public string TaxpayerCategory { get; set; } = string.Empty;
    
    [Required]
    public decimal Amount { get; set; }
    
    public decimal? Percentage { get; set; }
    
    public decimal? MaxAmount { get; set; }
    
    public decimal? MinIncome { get; set; }
    
    public decimal? MaxIncome { get; set; }
    
    public bool IsDeductible { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiryDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(450)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? Conditions { get; set; } // JSON for complex conditions
}