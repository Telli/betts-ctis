using System.ComponentModel.DataAnnotations;

namespace BettsTax.Data
{
    public enum RegistrationType
    {
        InviteLink,
        SelfRegistration
    }

    public enum RegistrationStatus
    {
        Started,
        InProgress,
        Completed,
        Abandoned
    }

    public enum RegistrationSource
    {
        Invitation,
        SelfRegistration,
        AdminCreated
    }

    public class ClientRegistration
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string RegistrationToken { get; set; } = string.Empty;
        
        public RegistrationType Type { get; set; }
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Started;
        
        public string? CompletionData { get; set; } // JSON storage for form data
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }
        
        public int? CompletedClientId { get; set; }
        
        // Navigation property
        public Client? CompletedClient { get; set; }
    }
}