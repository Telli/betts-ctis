using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data.Models;

public class ReportRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(36)]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public ReportType Type { get; set; }
    
    [Required]
    public ReportFormat Format { get; set; }
    
    [Required]
    public string Parameters { get; set; } = "{}"; // JSON serialized parameters
    
    [Required]
    [StringLength(450)]
    public string RequestedByUserId { get; set; } = string.Empty;
    
    [StringLength(256)]
    public string RequestedByUserName { get; set; } = string.Empty;
    
    [Required]
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(500)]
    public string? DownloadUrl { get; set; }
    
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }
    
    public long? FileSizeBytes { get; set; }
    
    [StringLength(500)]
    public string? FileName { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation properties
    public ApplicationUser? RequestedByUser { get; set; }
}