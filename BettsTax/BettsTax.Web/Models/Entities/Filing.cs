namespace BettsTax.Web.Models.Entities;

/// <summary>
/// Filing entity
/// </summary>
public class Filing
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string TaxType { get; set; } = string.Empty; // GST, VAT, Corporate Tax
    public string Period { get; set; } = string.Empty; // Q1 2025, FY 2024
    public string Status { get; set; } = string.Empty; // Draft, In Progress, Submitted, Approved
    public decimal? TotalSales { get; set; }
    public decimal? TaxableSales { get; set; }
    public decimal? GstRate { get; set; }
    public decimal? OutputTax { get; set; }
    public decimal? InputTaxCredit { get; set; }
    public decimal? NetGstPayable { get; set; }
    public string? Notes { get; set; }
    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Navigation properties
    public Client Client { get; set; } = null!;
    public ICollection<FilingSchedule> Schedules { get; set; } = new List<FilingSchedule>();
    public ICollection<FilingDocument> Documents { get; set; } = new List<FilingDocument>();
    public ICollection<FilingHistory> History { get; set; } = new List<FilingHistory>();
}
