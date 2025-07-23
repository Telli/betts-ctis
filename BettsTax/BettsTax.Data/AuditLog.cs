namespace BettsTax.Data
{
    public enum AuditActionType
    {
        Create,
        Read,
        Update,
        Delete,
        Download,
        Upload,
        Login,
        Logout,
        AccessDenied,
        ProfileUpdate,
        DocumentAccess,
        PaymentRequest,
        ComplianceView
    }

    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // e.g. CreatePayment
        public string Entity { get; set; } = string.Empty; // e.g. Payment
        public string EntityId { get; set; } = string.Empty; // could be int or string
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Enhanced security fields for client portal
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public int? ClientId { get; set; } // For client-specific actions
        public AuditActionType ActionType { get; set; }
        public string? RequestPath { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Client? Client { get; set; }
    }
}
