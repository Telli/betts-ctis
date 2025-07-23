using BettsTax.Data;

namespace BettsTax.Core.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
        Task<Notification> CreateAsync(string userId, string message);
        Task<bool> MarkReadAsync(int notificationId, string userId);
    }
}
