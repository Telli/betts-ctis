using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public class AssociatePermissionAuditLog
    {
        public int Id { get; set; }
        
        [Required]
        public string AssociateId { get; set; } = string.Empty;
        public ApplicationUser? Associate { get; set; }
        
        public int? ClientId { get; set; }
        public Client? Client { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // "Grant", "Revoke", "Update", "Expire"
        
        [MaxLength(50)]
        public string? PermissionArea { get; set; }
        
        public AssociatePermissionLevel? OldLevel { get; set; }
        public AssociatePermissionLevel? NewLevel { get; set; }
        
        [Required]
        public string ChangedByAdminId { get; set; } = string.Empty;
        public ApplicationUser? ChangedByAdmin { get; set; }
        
        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Reason { get; set; }
        
        [MaxLength(1000)]
        public string? Details { get; set; } // JSON serialized additional details
        
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        
        [MaxLength(200)]
        public string? UserAgent { get; set; }
    }
}