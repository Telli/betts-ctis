using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public class AssociatePermissionTemplate
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public List<AssociatePermissionRule> Rules { get; set; } = new();
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string CreatedByAdminId { get; set; } = string.Empty;
        public ApplicationUser? CreatedByAdmin { get; set; }
        
        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;
        
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class AssociatePermissionRule
    {
        public int Id { get; set; }
        
        public int TemplateId { get; set; }
        public AssociatePermissionTemplate? Template { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PermissionArea { get; set; } = string.Empty;
        
        public AssociatePermissionLevel Level { get; set; }
        
        public decimal? AmountThreshold { get; set; } // For payment permissions
        public bool RequiresApproval { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}