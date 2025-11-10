using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class TaxFilingDto
    {
        public int TaxFilingId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public TaxType TaxType { get; set; }
        public int TaxYear { get; set; }
        public DateTime FilingDate { get; set; }
        public DateTime DueDate { get; set; }
        public FilingStatus Status { get; set; }
        public decimal TaxLiability { get; set; }
        public string FilingReference { get; set; } = string.Empty;
        public string? SubmittedById { get; set; }
        public string? SubmittedByName { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? ReviewedById { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewComments { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public List<DocumentDto> Documents { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
        
        // Computed properties
        public bool IsOverdue => DueDate < DateTime.UtcNow && Status != FilingStatus.Filed;
        public int DaysUntilDue => (DueDate.Date - DateTime.UtcNow.Date).Days;
        public decimal TotalPaid => Payments.Where(p => p.Status == PaymentStatus.Approved).Sum(p => p.Amount);
        public decimal Balance => TaxLiability - TotalPaid;
        // Withholding-specific (optional)
        public string? WithholdingTaxSubtype { get; set; }
        public bool? IsResident { get; set; }
    }

    public class CreateTaxFilingDto
    {
        public int ClientId { get; set; }
        public TaxType TaxType { get; set; }
        public int TaxYear { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TaxLiability { get; set; }
        public string? FilingReference { get; set; }
        // Extended (optional)
        public string? FilingPeriod { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? PenaltyAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public string? AdditionalData { get; set; }
        // Withholding-specific (optional)
        public string? WithholdingTaxSubtype { get; set; }
        public bool? IsResident { get; set; }
    }

    public class UpdateTaxFilingDto
    {
        public TaxType? TaxType { get; set; }
        public int? TaxYear { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? TaxLiability { get; set; }
        public string? FilingReference { get; set; }
        public string? ReviewComments { get; set; }
        // Extended (optional)
        public string? FilingPeriod { get; set; }
        public decimal? TaxableAmount { get; set; }
        public decimal? PenaltyAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public string? AdditionalData { get; set; }
        // Withholding-specific (optional)
        public string? WithholdingTaxSubtype { get; set; }
        public bool? IsResident { get; set; }
    }

    public class ReviewTaxFilingDto
    {
        public FilingStatus Status { get; set; }
        public string? ReviewComments { get; set; }
    }
}