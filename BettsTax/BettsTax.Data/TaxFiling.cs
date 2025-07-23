namespace BettsTax.Data
{
    public enum TaxType { IncomeTax, GST, PayrollTax, ExciseDuty }
    public enum FilingStatus { Draft, Submitted, UnderReview, Approved, Rejected, Filed }

    public class TaxFiling
    {
        public int TaxFilingId { get; set; }
        public int ClientId { get; set; }
        public TaxType TaxType { get; set; }
        public int TaxYear { get; set; }
        public DateTime FilingDate { get; set; }
        public DateTime DueDate { get; set; }
        public FilingStatus Status { get; set; } = FilingStatus.Draft;
        public decimal TaxLiability { get; set; }
        public string FilingReference { get; set; } = string.Empty;
        public string? SubmittedById { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? ReviewedById { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewComments { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Associate delegation fields
        public string? CreatedByAssociateId { get; set; }
        public ApplicationUser? CreatedByAssociate { get; set; }
        public string? LastModifiedByAssociateId { get; set; }
        public ApplicationUser? LastModifiedByAssociate { get; set; }
        public bool IsCreatedOnBehalf { get; set; } = false;
        public DateTime? OnBehalfActionDate { get; set; }

        // Navigation properties
        public Client? Client { get; set; }
        public ApplicationUser? SubmittedBy { get; set; }
        public ApplicationUser? ReviewedBy { get; set; }
        public List<Document> Documents { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();
    }
}