using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public class AssociateClientPermission
    {
        public int Id { get; set; }
        
        [Required]
        public string AssociateId { get; set; } = string.Empty;
        public ApplicationUser? Associate { get; set; }
        
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PermissionArea { get; set; } = string.Empty; // "TaxFilings", "Payments", "Documents", etc.
        
        public AssociatePermissionLevel Level { get; set; }
        
        public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        
        [Required]
        public string GrantedByAdminId { get; set; } = string.Empty;
        public ApplicationUser? GrantedByAdmin { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public decimal? AmountThreshold { get; set; } // For payment permissions
        public bool RequiresApproval { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}