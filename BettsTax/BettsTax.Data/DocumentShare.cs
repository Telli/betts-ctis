using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public class DocumentShare
    {
        public int Id { get; set; }
        
        public int DocumentId { get; set; }
        public Document? Document { get; set; }
        
        [Required]
        public string SharedWithUserId { get; set; } = string.Empty;
        public ApplicationUser? SharedWithUser { get; set; }
        
        [Required]
        public string SharedByUserId { get; set; } = string.Empty;
        public ApplicationUser? SharedByUser { get; set; }
        
        public DateTime SharedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        
        public DocumentSharePermission Permission { get; set; }
        public bool IsActive { get; set; } = true;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}