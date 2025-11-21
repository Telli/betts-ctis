using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    /// <summary>
    /// Payment approval request entity - tracks payment approval workflows
    /// </summary>
    public class PaymentApprovalRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(450)]
        public string RequestedBy { get; set; } = string.Empty;

        [Required]
        public PaymentApprovalStatus Status { get; set; } = PaymentApprovalStatus.Pending;

        [MaxLength(1000)]
        public string? Comments { get; set; }

        [Column(TypeName = "TEXT")]
        public string ApprovalChain { get; set; } = "[]"; // JSON array of approval levels

        [Column(TypeName = "TEXT")]
        public string ApprovalHistory { get; set; } = "[]"; // JSON array of approval records

        public int CurrentApprovalLevel { get; set; } = 0;

        [MaxLength(450)]
        public string? CurrentApproverId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(450)]
        public string? CompletedBy { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        // Navigation properties
        public Payment? Payment { get; set; }
        public ApplicationUser? Requester { get; set; }
        public ApplicationUser? CurrentApprover { get; set; }
        public ApplicationUser? Completer { get; set; }
        public List<PaymentApprovalStep> ApprovalSteps { get; set; } = new();
    }

    /// <summary>
    /// Individual approval step in the payment approval workflow
    /// </summary>
    public class PaymentApprovalStep
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ApprovalRequestId { get; set; }

        [Required]
        public int ApprovalLevel { get; set; } // 1, 2, 3, etc.

        [Required]
        [MaxLength(100)]
        public string ApprovalRole { get; set; } = string.Empty; // "Associate", "Manager", "Director"

        [Required]
        [MaxLength(450)]
        public string ApproverId { get; set; } = string.Empty;

        [Required]
        public PaymentApprovalStepStatus Status { get; set; } = PaymentApprovalStepStatus.Pending;

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        // Navigation properties
        public PaymentApprovalRequest? ApprovalRequest { get; set; }
        public ApplicationUser? Approver { get; set; }
    }

    /// <summary>
    /// Payment approval threshold configuration
    /// </summary>
    public class PaymentApprovalThreshold
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxAmount { get; set; }

        [Required]
        [Column(TypeName = "TEXT")]
        public string ApprovalChain { get; set; } = "[]"; // JSON array of roles required

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(450)]
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Approval status enum
    /// </summary>
    public enum PaymentApprovalStatus
    {
        Pending = 0,
        InProgress = 1,
        Approved = 2,
        Rejected = 3,
        Cancelled = 4,
        Expired = 5
    }

    /// <summary>
    /// Individual approval step status enum
    /// </summary>
    public enum PaymentApprovalStepStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Skipped = 3,
        Delegated = 4
    }
}

