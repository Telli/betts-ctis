using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public enum InvitationStatus
    {
        Pending,
        Completed,
        Expired,
        Cancelled
    }

    public class ClientInvitation
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpirationDate { get; set; }
        public bool IsUsed { get; set; } = false;
        
        [Required]
        public string InvitedByAssociateId { get; set; } = string.Empty;
        
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
        
        // Navigation properties
        public ApplicationUser InvitedByAssociate { get; set; } = null!;
    }
}