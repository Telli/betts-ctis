using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models;

[Table("ComplianceScores")]
public class ComplianceScore
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client Client { get; set; } = null!;

    [Required]
    public int TaxYear { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal OverallScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal FilingScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PaymentScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DocumentScore { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TimelinessScore { get; set; }

    public ComplianceLevel Level { get; set; }

    public DateTime CalculatedAt { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(500)]
    public string? RecommendedActions { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties for trend analysis
    public List<KPIMetric> RelatedMetrics { get; set; } = new();
}