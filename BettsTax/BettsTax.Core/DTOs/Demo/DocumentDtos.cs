namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Represents metadata about a supporting document stored in the system.
/// </summary>
public class DocumentRecordDto
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public int Year { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
