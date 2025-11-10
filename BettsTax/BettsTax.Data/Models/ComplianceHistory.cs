using System.ComponentModel.DataAnnotations;
using BettsTax.Data; // Update to use the unified ComplianceStatus

namespace BettsTax.Data.Models;

public class ComplianceHistory
{
    [Key]
    public int Id { get; set; }
    
    public int ClientId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string TaxYear { get; set; } = string.Empty;
    
    public TaxType TaxType { get; set; }
    
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    
    // Filing compliance metrics
    public bool FilingCompleted { get; set; }
    
    public DateTime? FilingDueDate { get; set; }
    
    public DateTime? ActualFilingDate { get; set; }
    
    public int FilingDaysEarly { get; set; } // Negative means late
    
    public bool FilingOnTime => ActualFilingDate.HasValue && FilingDueDate.HasValue && 
                                ActualFilingDate.Value <= FilingDueDate.Value;
    
    // Payment compliance metrics
    public bool PaymentCompleted { get; set; }
    
    public DateTime? PaymentDueDate { get; set; }
    
    public DateTime? ActualPaymentDate { get; set; }
    
    public decimal AmountDue { get; set; }
    
    public decimal AmountPaid { get; set; }
    
    public decimal OutstandingBalance => AmountDue - AmountPaid;
    
    public bool PaymentOnTime => ActualPaymentDate.HasValue && PaymentDueDate.HasValue && 
                                 ActualPaymentDate.Value <= PaymentDueDate.Value;
    
    // Document compliance metrics
    public int RequiredDocuments { get; set; }
    
    public int SubmittedDocuments { get; set; }
    
    public int ApprovedDocuments { get; set; }
    
    public int RejectedDocuments { get; set; }
    
    public decimal DocumentComplianceRate => RequiredDocuments > 0 ? 
        (decimal)ApprovedDocuments / RequiredDocuments * 100 : 100;
    
    // Penalty information
    public decimal PenaltyAmount { get; set; }
    
    public decimal InterestAmount { get; set; }
    
    public decimal TotalPenalties => PenaltyAmount + InterestAmount;
    
    public bool HasPenalties => TotalPenalties > 0;
    
    // Compliance score (0-100)
    public decimal ComplianceScore { get; set; }
    
    public ComplianceGrade ComplianceGrade { get; set; }
    
    // Risk assessment
    public ComplianceRiskLevel RiskLevel { get; set; }
    
    [StringLength(1000)]
    public string? RiskFactors { get; set; } // JSON array of risk factors
    
    // Engagement metrics
    public int ClientLoginCount { get; set; }
    
    public int DocumentUploads { get; set; }
    
    public int MessageExchanges { get; set; }
    
    public DateTime? LastClientActivity { get; set; }
    
    // Status tracking
    public ComplianceStatus Status { get; set; }
    
    [StringLength(2000)]
    public string? Notes { get; set; }
    
    [StringLength(1000)]
    public string? ActionItems { get; set; } // JSON array of action items
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    [StringLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
    public List<ComplianceHistoryEvent> Events { get; set; } = new();
}

public class ComplianceHistoryEvent
{
    [Key]
    public int Id { get; set; }
    
    public int ComplianceHistoryId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty; // Filing, Payment, Document, Penalty, etc.
    
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public DateTime EventDate { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public bool IsCompliant { get; set; }
    
    public decimal? Amount { get; set; }
    
    public decimal? PenaltyAmount { get; set; }
    
    [StringLength(50)]
    public string? Status { get; set; }
    
    [StringLength(1000)]
    public string? Details { get; set; } // JSON for additional event details
    
    // Related entity references
    public int? RelatedTaxFilingId { get; set; }
    public int? RelatedPaymentId { get; set; }
    public int? RelatedDocumentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    // Navigation properties
    public ComplianceHistory? ComplianceHistory { get; set; }
    public TaxFiling? RelatedTaxFiling { get; set; }
    public Payment? RelatedPayment { get; set; }
    public Document? RelatedDocument { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public enum ComplianceGrade
{
    A = 0,  // 90-100%
    B = 1,  // 80-89%
    C = 2,  // 70-79%
    D = 3,  // 60-69%
    F = 4   // Below 60%
}

public enum ComplianceRiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
