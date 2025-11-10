using System.ComponentModel.DataAnnotations;
using BettsTax.Data;
using BettsTax.Data.Models.Security;

namespace BettsTax.Data.Models;

public class CaseIssue
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string CaseNumber { get; set; } = string.Empty;
    
    public int ClientId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string IssueType { get; set; } = string.Empty;
    
    [Required]
    public CasePriority Priority { get; set; } = CasePriority.Medium;
    
    [Required]
    public CaseStatus Status { get; set; } = CaseStatus.Open;
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(450)]
    public string? AssignedToUserId { get; set; }
    
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResolvedAt { get; set; }
    
    [StringLength(450)]
    public string? ResolvedByUserId { get; set; }
    
    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate.Value && Status != CaseStatus.Resolved;
    
    [StringLength(100)]
    public string? Category { get; set; }
    
    [StringLength(50)]
    public string? Severity { get; set; }
    
    public int? RelatedTaxFilingId { get; set; }
    
    public int? RelatedPaymentId { get; set; }
    
    public int? RelatedDocumentId { get; set; }
    
    [StringLength(2000)]
    public string? InternalNotes { get; set; }
    
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(450)]
    public string? LastUpdatedByUserId { get; set; }
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? ResolvedByUser { get; set; }
    public ApplicationUser? LastUpdatedByUser { get; set; }
    public TaxFiling? RelatedTaxFiling { get; set; }
    public Payment? RelatedPayment { get; set; }
    public Document? RelatedDocument { get; set; }
    
    public List<CaseComment> Comments { get; set; } = new();
    public List<CaseAttachment> Attachments { get; set; } = new();
}

public class CaseComment
{
    [Key]
    public int Id { get; set; }
    
    public int CaseIssueId { get; set; }
    
    [Required]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
    
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsInternal { get; set; } = false;
    
    // Navigation properties
    public CaseIssue? CaseIssue { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class CaseAttachment
{
    [Key]
    public int Id { get; set; }
    
    public int CaseIssueId { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ContentType { get; set; }
    
    public long FileSize { get; set; }
    
    [StringLength(450)]
    public string UploadedByUserId { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public CaseIssue? CaseIssue { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
}

public enum CasePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum CaseStatus
{
    Open = 0,
    InProgress = 1,
    PendingClient = 2,
    PendingInternal = 3,
    Resolved = 4,
    Closed = 5,
    Cancelled = 6
}
