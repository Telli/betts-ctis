using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public class SystemSetting
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; } = "General";
        
        public bool IsEncrypted { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string UpdatedByUserId { get; set; } = string.Empty;
        
        // Navigation property
        public ApplicationUser UpdatedByUser { get; set; } = null!;
    }
}