using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data.Models;

public class TaxRate
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int TaxYear { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TaxType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string TaxpayerCategory { get; set; } = string.Empty;
    
    public decimal MinIncome { get; set; }
    public decimal? MaxIncome { get; set; }
    
    [Required]
    public decimal Rate { get; set; }
    
    public decimal? FixedAmount { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiryDate { get; set; }
    
    [MaxLength(450)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
    
    [MaxLength(100)]
    public string? Description { get; set; }
}