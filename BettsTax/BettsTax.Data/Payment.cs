using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum PaymentStatus { Pending, Approved, Rejected, Completed }
    public enum PaymentMethod { BankTransfer, Cash, Cheque, OnlinePayment }

    public class Payment
    {
        public int PaymentId { get; set; }
        public int Id { get; set; } // Added for compatibility
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public TaxType? TaxType { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
        [NotMapped]
        public PaymentMethod PaymentMethod => Method; // Alias for compatibility
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string PaymentReference { get; set; } = string.Empty;
        public string? TransactionId { get; set; } // Added for hub compatibility
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } // Added for hub compatibility
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedById { get; set; }
        public string? RejectionReason { get; set; }
        public string ApprovalWorkflow { get; set; } = string.Empty;

        // Associate delegation fields
        public string? ProcessedByAssociateId { get; set; }
        public ApplicationUser? ProcessedByAssociate { get; set; }
        public string? ApprovedByAssociateId { get; set; }
        public ApplicationUser? ApprovedByAssociate { get; set; }
        public bool IsProcessedOnBehalf { get; set; } = false;
        public DateTime? OnBehalfProcessingDate { get; set; }

        // Navigation properties
        public Client? Client { get; set; }
        public TaxYear? TaxYear { get; set; }
        public TaxFiling? TaxFiling { get; set; }
        [NotMapped]
        public ApplicationUser? ApprovedBy { get; set; }

    // External gateway integration fields
    public string? ExternalProvider { get; set; }
    public string? ExternalTransactionId { get; set; }
    public string? ExternalReference { get; set; }
    public string? ExternalStatus { get; set; }
    public string? WebhookStatus { get; set; }
    public DateTime? InitiatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? RawInitiationResponse { get; set; }
    public string? RawLastStatusPayload { get; set; }
    public string? FailureReason { get; set; }

    // Enhanced payment tracking fields
    public string? PaymentBatchId { get; set; } // For batch processing
    public int? PaymentSequenceNumber { get; set; } // For ordering within batch
    public string? PaymentCategory { get; set; } // Tax, Penalty, Interest, Fee
    public string? PaymentSubCategory { get; set; } // Specific sub-type
    public decimal? InterestAmount { get; set; } // Interest portion
    public decimal? PenaltyAmount { get; set; } // Penalty portion
    public decimal? FeeAmount { get; set; } // Processing fee portion
    public decimal? TaxAmount { get; set; } // Actual tax portion
    public string? Currency { get; set; } = "SLE"; // Currency code
    public decimal? ExchangeRate { get; set; } // For foreign currency payments
    public decimal? OriginalAmount { get; set; } // Amount in original currency
    public string? OriginalCurrency { get; set; } // Original currency code

    // Payment reconciliation fields
    public bool IsReconciled { get; set; } = false;
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    public string? ReconciliationReference { get; set; }
    public string? BankStatementReference { get; set; }
    public DateTime? BankStatementDate { get; set; }
    public string? ReconciliationNotes { get; set; }

    // Payment retry and failure handling
    public int RetryCount { get; set; } = 0;
    public int MaxRetryAttempts { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public string? RetryStrategy { get; set; } // Exponential, Linear, Fixed
    public bool IsRetryable { get; set; } = true;
    public DateTime? LastRetryAt { get; set; }
    public string? RetryFailureReason { get; set; }

    // Payment notification and communication
    public bool NotificationSent { get; set; } = false;
    public DateTime? NotificationSentAt { get; set; }
    public string? NotificationMethod { get; set; } // Email, SMS, Push
    public bool ReminderSent { get; set; } = false;
    public DateTime? ReminderSentAt { get; set; }
    public int ReminderCount { get; set; } = 0;

    // Payment security and compliance
    public string? PaymentHash { get; set; } // For integrity verification
    public bool RequiresManualReview { get; set; } = false;
    public string? ReviewReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ComplianceNotes { get; set; }
    public bool IsSuspicious { get; set; } = false;
    public string? SuspiciousActivityReason { get; set; }

    // Payment analytics and reporting
    public string? PaymentChannel { get; set; } // Web, Mobile, API, Bulk
    public string? UserAgent { get; set; } // For web/mobile payments
    public string? IpAddress { get; set; } // For fraud detection
    public string? DeviceFingerprint { get; set; } // Device identification
    public string? PaymentSource { get; set; } // Manual, Scheduled, Automatic
    public TimeSpan? ProcessingDuration { get; set; } // Time taken to process
    public string? ProcessingNotes { get; set; }

    // Extended metadata and custom fields
    public string? ExtendedMetadata { get; set; } // JSON for additional data
    public string? CustomField1 { get; set; }
    public string? CustomField2 { get; set; }
    public string? CustomField3 { get; set; }
    public string? Tags { get; set; } // JSON array of tags
    
    // Additional navigation properties for extended fields
    public ApplicationUser? ReconciledByUser { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    }
}
