namespace BettsTax.Data.Models;

public class PaymentScheduledRetry
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    
    // Additional property for transaction reference
    public int TransactionId { get; set; }
    public PaymentTransaction? Transaction { get; set; }
    
    public DateTime ScheduledAt { get; set; }
    public int AttemptNumber { get; set; }
    public string RetryReason { get; set; } = string.Empty;
    public string PaymentGateway { get; set; } = string.Empty;
    
    // Status property
    public string Status { get; set; } = string.Empty;
    
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessingResult { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    
    // Additional property for scheduled by
    public string ScheduledBy { get; set; } = string.Empty;
    
    // Additional properties for updates
    public DateTime? UpdatedAt { get; set; }
    public string? ProcessingNotes { get; set; }
    public string? UpdatedBy { get; set; }
}