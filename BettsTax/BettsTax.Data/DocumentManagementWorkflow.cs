using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    /// <summary>
    /// Document submission workflow entity - tracks document submissions and approvals
    /// </summary>
    public class DocumentSubmissionWorkflow
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty; // Invoice, Receipt, Contract, etc.

        [Required]
        public DocumentSubmissionStatus Status { get; set; } = DocumentSubmissionStatus.Submitted;

        [Required]
        public DateTime SubmittedAt { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? RejectedAt { get; set; }

        [MaxLength(450)]
        public string? SubmittedBy { get; set; }

        [MaxLength(450)]
        public string? VerifiedBy { get; set; }

        [MaxLength(450)]
        public string? ApprovedBy { get; set; }

        [MaxLength(450)]
        public string? RejectedBy { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        [MaxLength(1000)]
        public string? VerificationNotes { get; set; }

        public int VersionNumber { get; set; } = 1;

        public bool RequiresVerification { get; set; } = true;

        public bool RequiresApproval { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Document? Document { get; set; }
        public Client? Client { get; set; }
        public List<DocumentSubmissionStep> SubmissionSteps { get; set; } = new();
        public List<DocumentVerificationResult> VerificationResults { get; set; } = new();
    }

    /// <summary>
    /// Document submission step entity - tracks each step in the submission workflow
    /// </summary>
    public class DocumentSubmissionStep
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid DocumentSubmissionWorkflowId { get; set; }

        [Required]
        [MaxLength(100)]
        public string StepType { get; set; } = string.Empty; // "Submission", "Verification", "Approval", "Rejection"

        [Required]
        public DocumentSubmissionStepStatus Status { get; set; } = DocumentSubmissionStepStatus.Pending;

        [MaxLength(450)]
        public string? AssignedTo { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(450)]
        public string? CompletedBy { get; set; }

        // Navigation properties
        public DocumentSubmissionWorkflow? DocumentSubmission { get; set; }
    }

    /// <summary>
    /// Document verification result entity - tracks verification checks
    /// </summary>
    public class DocumentVerificationResult
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid DocumentSubmissionWorkflowId { get; set; }

        [Required]
        [MaxLength(100)]
        public string VerificationType { get; set; } = string.Empty; // "Format", "Content", "Authenticity", "Compliance"

        [Required]
        public VerificationResultStatus Status { get; set; } = VerificationResultStatus.Pending;

        [MaxLength(1000)]
        public string? Findings { get; set; }

        [MaxLength(1000)]
        public string? Recommendations { get; set; }

        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? VerifiedBy { get; set; }

        // Navigation properties
        public DocumentSubmissionWorkflow? DocumentSubmission { get; set; }
    }

    /// <summary>
    /// Document version control entity - tracks document versions
    /// </summary>
    public class DocumentVersionControl
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        [MaxLength(100)]
        public string FileHash { get; set; } = string.Empty; // SHA256 hash for integrity

        [MaxLength(1000)]
        public string? ChangeDescription { get; set; }

        [MaxLength(450)]
        public string? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Document? Document { get; set; }
    }

    /// <summary>
    /// Document submission status enum
    /// </summary>
    public enum DocumentSubmissionStatus
    {
        Submitted = 0,
        UnderVerification = 1,
        VerificationPassed = 2,
        VerificationFailed = 3,
        UnderApproval = 4,
        Approved = 5,
        Rejected = 6,
        Resubmitted = 7
    }

    /// <summary>
    /// Document submission step status enum
    /// </summary>
    public enum DocumentSubmissionStepStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Skipped = 4
    }

    /// <summary>
    /// Verification result status enum
    /// </summary>
    public enum VerificationResultStatus
    {
        Pending = 0,
        Passed = 1,
        Failed = 2,
        PartiallyPassed = 3,
        RequiresReview = 4
    }
}

