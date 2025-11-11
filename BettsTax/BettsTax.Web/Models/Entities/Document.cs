namespace BettsTax.Web.Models.Entities;

/// <summary>
/// Document entity
/// </summary>
public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Tax Return, Financial Statement, VAT Report, Payroll
    public int ClientId { get; set; }
    public int Year { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Pending Review, Approved, Rejected
    public string? FilePath { get; set; } // Path to stored file
    public long FileSize { get; set; } // File size in bytes
    public bool IsDemo { get; set; }

    // Navigation property
    public Client Client { get; set; } = null!;
}
