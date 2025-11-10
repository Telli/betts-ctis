namespace BettsTax.Core.DTOs.Client;

/// <summary>
/// Client DTO
/// </summary>
public class ClientDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tin { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

/// <summary>
/// Create client DTO
/// </summary>
public class CreateClientDto
{
    public string Name { get; set; } = string.Empty;
    public string Tin { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
}

/// <summary>
/// Update client DTO
/// </summary>
public class UpdateClientDto
{
    public string? Name { get; set; }
    public string? Tin { get; set; }
    public string? Segment { get; set; }
    public string? Industry { get; set; }
    public string? Status { get; set; }
    public string? AssignedTo { get; set; }
}
