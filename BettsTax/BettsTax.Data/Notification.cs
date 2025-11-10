using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum NotificationStatus { Unread, Read }

    public class Notification
    {
        public int NotificationId { get; set; }
        [NotMapped]
        public int Id => NotificationId; // Alias for compatibility
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty; // Added for hub compatibility
        public string Type { get; set; } = "General"; // Changed to string for hub compatibility
        public string? Link { get; set; } // Added for hub compatibility
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
        public DateTime? ReadAt { get; set; } // Added for hub compatibility
        
        [NotMapped]
        public bool IsRead 
        { 
            get => Status == NotificationStatus.Read;
            set 
            {
                Status = value ? NotificationStatus.Read : NotificationStatus.Unread;
                if (value && ReadAt == null)
                {
                    ReadAt = DateTime.UtcNow;
                }
            }
        }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
