using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public string? RejectionReason { get; set; }
        public string ApprovalWorkflow { get; set; } = string.Empty;
        public TaxType? TaxType { get; set; }
    }

}
