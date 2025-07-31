using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data.Models;

public class ExciseDutyRate
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int TaxYear { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductCategory { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    public decimal Rate { get; set; }
    
    public decimal? MinimumTax { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string RateType { get; set; } = "Percentage"; // Percentage, Fixed, Mixed
    
    [Required]
    [MaxLength(50)]
    public string UnitOfMeasure { get; set; } = "Unit"; // Unit, Litre, Kilogram, etc.
    
    public bool IsActive { get; set; } = true;
    
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiryDate { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
}