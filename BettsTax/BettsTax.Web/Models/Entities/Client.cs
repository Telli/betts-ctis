namespace BettsTax.Web.Models.Entities;

/// <summary>
/// Client entity
/// </summary>
public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tin { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty; // Large, Medium, Small
    public string Industry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active, Under Review, Suspended
    public decimal ComplianceScore { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public bool IsDemo { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Filing> Filings { get; set; } = new List<Filing>();
}
