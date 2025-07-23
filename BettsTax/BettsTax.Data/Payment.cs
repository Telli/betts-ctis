namespace BettsTax.Data
{
    public enum PaymentStatus { Pending, Approved, Rejected }
    public enum PaymentMethod { BankTransfer, Cash, Cheque, OnlinePayment }

    public class Payment
    {
        public int PaymentId { get; set; }
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string PaymentReference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
        public ApplicationUser? ApprovedBy { get; set; }
    }
}
