using BettsTax.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification> CreateAsync(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Status = NotificationStatus.Unread,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<bool> MarkReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            notification.Status = NotificationStatus.Read;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            // TODO: Implement SMS sending logic using an SMS provider
            // For now, return true to indicate success
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            // TODO: Implement email sending logic using an email provider
            // For now, return true to indicate success
            await Task.CompletedTask;
            return true;
        }
        
        public async Task<bool> SendNotificationAsync(string userId, string message, string notificationType)
        {
            // Example implementation: Create a notification and send an email or SMS based on the type
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Status = NotificationStatus.Unread,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            if (notificationType == "Email")
            {
                // Call SendEmailAsync (assuming email is fetched from user profile)
                return await SendEmailAsync("user@example.com", "Notification", message);
            }
            else if (notificationType == "SMS")
            {
                // Call SendSmsAsync (assuming phone number is fetched from user profile)
                return await SendSmsAsync("+1234567890", message);
            }

            return true;
        }
    }
}