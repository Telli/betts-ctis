namespace BettsTax.Web.Models.Entities;

/// <summary>
/// User entity
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Admin, Staff, Client
    public int? ClientId { get; set; }
    public bool IsDemo { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation property
    public Client? Client { get; set; }
}
