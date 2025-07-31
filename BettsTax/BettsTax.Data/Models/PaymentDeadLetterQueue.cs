namespace BettsTax.Data.Models;

public class PaymentDeadLetterQueue
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    
    // Additional properties for transaction reference
    public int TransactionId { get; set; }
    public string OriginalTransactionReference { get; set; } = string.Empty;
    
    public string OriginalMessage { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty; // Payment, Refund, Notification
    public string FailureReason { get; set; } = string.Empty;
    
    // Reason property (alias for FailureReason)
    public string Reason { get; set; } = string.Empty;
    
    public int ProcessingAttempts { get; set; }
    
    // RetryAttempts property (alias for ProcessingAttempts)
    public int RetryAttempts { get; set; }
    
    public DateTime FirstAttemptAt { get; set; }
    public DateTime LastAttemptAt { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? LastStackTrace { get; set; }
    
    // Additional transaction data property
    public string? TransactionData { get; set; }
    
    public bool RequiresManualReview { get; set; }
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    public string Status { get; set; } = "Pending"; // Pending, InReview, Resolved, Discarded
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    
    // ProcessingNotes property (alias for ReviewNotes)
    public string? ProcessingNotes { get; set; }
    
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}