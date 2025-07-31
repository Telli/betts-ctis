using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models;

[Table("KPIMetrics")]
public class KPIMetric
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string MetricName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    public DateTime CalculatedAt { get; set; }

    [Required]
    [StringLength(20)]
    public string Period { get; set; } = string.Empty; // Daily, Weekly, Monthly

    public int? ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }

    [StringLength(500)]
    public string? Metadata { get; set; } // JSON for additional data

    [StringLength(50)]
    public string? Category { get; set; } // Compliance, Financial, Engagement

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}