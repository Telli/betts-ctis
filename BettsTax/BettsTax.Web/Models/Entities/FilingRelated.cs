namespace BettsTax.Web.Models.Entities;

/// <summary>
/// Filing schedule entity (rows in the filing schedule)
/// </summary>
public class FilingSchedule
{
    public int Id { get; set; }
    public int FilingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Taxable { get; set; }
    public int Order { get; set; } // Display order

    // Navigation property
    public Filing Filing { get; set; } = null!;
}

/// <summary>
/// Filing document entity (documents attached to a filing)
/// </summary>
public class FilingDocument
{
    public int Id { get; set; }
    public int FilingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? FilePath { get; set; }

    // Navigation property
    public Filing Filing { get; set; } = null!;
}

/// <summary>
/// Filing history entity (audit trail)
/// </summary>
public class FilingHistory
{
    public int Id { get; set; }
    public int FilingId { get; set; }
    public DateTime Date { get; set; }
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Created, Updated, Reviewed, Submitted
    public string Changes { get; set; } = string.Empty;

    // Navigation property
    public Filing Filing { get; set; } = null!;
}
