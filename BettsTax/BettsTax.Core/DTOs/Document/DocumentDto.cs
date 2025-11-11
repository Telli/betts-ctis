namespace BettsTax.Core.DTOs.Document;

/// <summary>
/// Document DTO
/// </summary>
public class DocumentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public int Year { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public int Version { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string UploadDate { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Upload document DTO
/// </summary>
public class UploadDocumentDto
{
    public string Type { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string TaxType { get; set; } = string.Empty;
    public int Year { get; set; }
}
