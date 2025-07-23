using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public class OnBehalfAction
    {
        public int Id { get; set; }
        
        [Required]
        public string AssociateId { get; set; } = string.Empty;
        public ApplicationUser? Associate { get; set; }
        
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete", "Submit", etc.
        
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // "TaxFiling", "Payment", "Document"
        
        public int EntityId { get; set; }
        
        [MaxLength(2000)]
        public string? OldValues { get; set; } // JSON serialized
        
        [MaxLength(2000)]
        public string? NewValues { get; set; } // JSON serialized
        
        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        public bool ClientNotified { get; set; } = false;
        public DateTime? ClientNotificationDate { get; set; }
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(200)]
        public string? UserAgent { get; set; }
    }
}