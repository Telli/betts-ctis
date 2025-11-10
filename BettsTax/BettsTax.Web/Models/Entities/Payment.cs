namespace BettsTax.Web.Models.Entities;

/// <summary>
/// Payment entity
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string TaxType { get; set; } = string.Empty; // VAT, Corporate Tax, PAYE, etc.
    public string Period { get; set; } = string.Empty; // Q1 2025, December 2024, etc.
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty; // Bank Transfer, Check, Direct Debit
    public string Status { get; set; } = string.Empty; // Completed, Pending, Processing
    public DateTime Date { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public bool IsDemo { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Client Client { get; set; } = null!;
}
