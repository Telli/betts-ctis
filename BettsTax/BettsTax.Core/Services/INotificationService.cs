using BettsTax.Data;

namespace BettsTax.Core.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
        Task<Notification> CreateAsync(string userId, string message);
        Task<bool> MarkReadAsync(int notificationId, string userId);
        
        // SMS and Email notification methods
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendEmailAsync(string email, string subject, string body);
    }
}
