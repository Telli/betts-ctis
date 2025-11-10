namespace BettsTax.Data
{
    public enum TaxType { IncomeTax, GST, PayrollTax, ExciseDuty, PAYE, WithholdingTax, PersonalIncomeTax, CorporateIncomeTax }
    public enum FilingStatus { Draft, Submitted, UnderReview, Approved, Rejected, Filed }

    public class TaxFiling
    {
        public int TaxFilingId { get; set; }
        public int Id { get; set; } // Added for compatibility
        public int ClientId { get; set; }
        public TaxType TaxType { get; set; }
        public int TaxYear { get; set; }
        public int TaxYearId { get; set; } // Added for compatibility
        public string FilingPeriod { get; set; } = string.Empty; // Added for compatibility
        public DateTime FilingDate { get; set; }
        public DateTime? DueDate { get; set; } // Made nullable
        public decimal Amount { get; set; } // Added for compatibility
        public decimal TaxableAmount { get; set; } // Added for compatibility
        public decimal TaxAmount { get; set; } // Added for compatibility
        public DateTime DateCreated { get; set; } = DateTime.UtcNow; // Added for compatibility
        public FilingStatus Status { get; set; } = FilingStatus.Draft;
        public decimal TaxLiability { get; set; }
        public string FilingReference { get; set; } = string.Empty;
        public string? SubmittedById { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? ReviewedById { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewComments { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Added for compatibility
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Tax authority integration fields
        public decimal? PenaltyAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public string? AdditionalData { get; set; } // JSON data for tax authority submission

        // Withholding-specific (Sierra Leone)
        public string? WithholdingTaxSubtype { get; set; }
        public bool? IsResident { get; set; }

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
        public TaxYear? TaxDeadline { get; set; } // Added navigation property

        // Tax authority integration navigation properties
        public List<TaxAuthoritySubmission> TaxAuthoritySubmissions { get; set; } = new();

        // Timestamp for when the tax filing was submitted
        public DateTime? SubmittedAt { get; set; }
    }
}