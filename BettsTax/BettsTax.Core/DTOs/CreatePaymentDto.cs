using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class CreatePaymentDto
    {
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    public class ApprovePaymentDto
    {
        public string? Comments { get; set; }
    }

    public class RejectPaymentDto
    {
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class ReconcilePaymentDto
    {
        public string? ReconciliationReference { get; set; }
        public string? BankStatementReference { get; set; }
        public DateTime? BankStatementDate { get; set; }
        public string? Notes { get; set; }
        public bool MarkAsCompleted { get; set; } = true;
    }

    public class UploadPaymentEvidenceDto
    {
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public DocumentCategory Category { get; set; } = DocumentCategory.Other;
        public string Description { get; set; } = "Payment evidence";
    }
}
