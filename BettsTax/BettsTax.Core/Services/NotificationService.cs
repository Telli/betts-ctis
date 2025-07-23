using BettsTax.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Notification> CreateAsync(string userId, string message)
        {
            var n = new Notification { UserId = userId, Message = message };
            _db.Notifications.Add(n);
            await _db.SaveChangesAsync();
            return n;
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task<bool> MarkReadAsync(int notificationId, string userId)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId);
            if (n == null) return false;
            n.Status = NotificationStatus.Read;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
