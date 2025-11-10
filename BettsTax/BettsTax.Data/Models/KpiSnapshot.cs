using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data.Models;

public class KpiSnapshot
{
    [Key]
    public int Id { get; set; }
    
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
    
    public int TotalClients { get; set; }
    
    public decimal ClientComplianceRate { get; set; }
    
    public decimal TaxFilingTimeliness { get; set; }
    
    public decimal PaymentCompletionRate { get; set; }
    
    public decimal DocumentSubmissionCompliance { get; set; }
    
    public decimal ClientEngagementRate { get; set; }
    
    // Client-specific metrics
    public decimal OnTimePaymentPercentage { get; set; }
    
    public decimal FilingTimelinessAverage { get; set; } // Average days early (negative = early)
    
    public decimal DocumentReadinessRate { get; set; }
    
    // Alert flags
    public bool ComplianceRateBelowThreshold { get; set; }
    
    public bool FilingTimelinessBelowThreshold { get; set; }
    
    public bool PaymentCompletionBelowThreshold { get; set; }
    
    public bool DocumentSubmissionBelowThreshold { get; set; }
    
    public bool EngagementBelowThreshold { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    [StringLength(2000)]
    public string? ExtendedMetrics { get; set; } // JSON for additional metrics
    
    // Navigation properties
    public ApplicationUser? CreatedByUser { get; set; }
}

public class ClientKpiMetrics
{
    [Key]
    public int Id { get; set; }
    
    public int ClientId { get; set; }
    
    public int KpiSnapshotId { get; set; }
    
    public decimal OnTimePaymentPercentage { get; set; }
    
    public decimal FilingTimelinessAverage { get; set; } // Days early/late
    
    public decimal DocumentReadinessRate { get; set; }
    
    public decimal EngagementScore { get; set; }
    
    public int CompletedDocuments { get; set; }
    
    public int PendingDocuments { get; set; }
    
    public int RejectedDocuments { get; set; }
    
    public DateTime LastActivity { get; set; }
    
    public int LoginCount30Days { get; set; }
    
    public int MeaningfulEvents30Days { get; set; }
    
    // Navigation properties
    public Client? Client { get; set; }
    public KpiSnapshot? KpiSnapshot { get; set; }
}

public class KpiAlert
{
    [Key]
    public int Id { get; set; }
    
    public int? KpiSnapshotId { get; set; }
    
    public int? ClientId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string AlertType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;
    
    public decimal? ThresholdValue { get; set; }
    
    public decimal? ActualValue { get; set; }
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    [StringLength(450)]
    public string? ResolvedBy { get; set; }
    
    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? NotificationSentAt { get; set; }
    
    public bool NotificationSent { get; set; } = false;
    
    // Navigation properties
    public KpiSnapshot? KpiSnapshot { get; set; }
    public Client? Client { get; set; }
    public ApplicationUser? ResolvedByUser { get; set; }
}

public enum AlertSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
