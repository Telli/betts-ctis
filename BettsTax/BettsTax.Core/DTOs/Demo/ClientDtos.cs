namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Represents a client entry for listings and dashboard views.
/// </summary>
public class ClientSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tin { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ComplianceScore { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}
