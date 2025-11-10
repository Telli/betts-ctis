using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data;
using BettsTax.Data.Models;
using System.Security.Claims;

namespace BettsTax.Web.Hubs;

[Authorize]
public class NotificationsHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(
        ApplicationDbContext context,
        ILogger<NotificationsHub> logger)
    {
        _context = context;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            _logger.LogWarning("User connected to NotificationsHub without valid identifier");
            return;
        }

        _logger.LogInformation("User {UserId} connected to NotificationsHub", userId);

        // Join user to their personal notification group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Send current unread count on connection
        var unreadCount = await GetUnreadNotificationCount(userId);
        await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _logger.LogInformation("User {UserId} disconnected from NotificationsHub", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    public async Task MarkAsRead(int notificationId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                await Clients.Caller.SendAsync("Error", "Notification not found");
                return;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send updated unread count
                var unreadCount = await GetUnreadNotificationCount(userId);
                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);

                _logger.LogInformation("Notification {NotificationId} marked as read by {UserId}", notificationId, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to mark notification as read");
        }
    }

    /// <summary>
    /// Mark all notifications as read for current user
    /// </summary>
    public async Task MarkAllAsRead()
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Send updated unread count (should be 0)
            await Clients.Caller.SendAsync("UpdateUnreadCount", 0);

            _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to mark notifications as read");
        }
    }

    /// <summary>
    /// Delete a notification
    /// </summary>
    public async Task DeleteNotification(int notificationId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                await Clients.Caller.SendAsync("Error", "Notification not found");
                return;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            // Send updated unread count
            var unreadCount = await GetUnreadNotificationCount(userId);
            await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);

            _logger.LogInformation("Notification {NotificationId} deleted by {UserId}", notificationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to delete notification");
        }
    }

    /// <summary>
    /// Send a notification to a specific user (called from backend services)
    /// </summary>
    public async Task SendNotificationToUser(string targetUserId, string title, string message, string type = "Info", string? link = null)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            // Check if user has permission to send notifications (Admin or Associate only)
            var userRoles = Context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
            if (!userRoles.Contains("Admin") && !userRoles.Contains("Associate"))
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to send notifications");
                return;
            }

            // Create the notification in database
            var notification = new Notification
            {
                UserId = targetUserId,
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send to the target user via SignalR
            var notificationData = new
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Link = notification.Link,
                CreatedAt = notification.CreatedAt,
                IsRead = false
            };

            await Clients.Group($"user_{targetUserId}").SendAsync("ReceiveNotification", notificationData);

            // Update unread count for target user
            var unreadCount = await GetUnreadNotificationCount(targetUserId);
            await Clients.Group($"user_{targetUserId}").SendAsync("UpdateUnreadCount", unreadCount);

            _logger.LogInformation("Notification sent to user {TargetUserId} by {UserId}", targetUserId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {TargetUserId}", targetUserId);
            await Clients.Caller.SendAsync("Error", "Failed to send notification");
        }
    }

    /// <summary>
    /// Get unread notification count for a user
    /// </summary>
    private async Task<int> GetUnreadNotificationCount(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    /// <summary>
    /// Helper method to send notification (can be called from other services via IHubContext)
    /// </summary>
    public static async Task SendNotification(
        IHubContext<NotificationsHub> hubContext,
        ApplicationDbContext context,
        string userId,
        string title,
        string message,
        string type = "Info",
        string? link = null)
    {
        // Create notification in database
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Send to user via SignalR
        var notificationData = new
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Link = notification.Link,
            CreatedAt = notification.CreatedAt,
            IsRead = false
        };

        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notificationData);

        // Update unread count
        var unreadCount = await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
        
        await hubContext.Clients.Group($"user_{userId}")
            .SendAsync("UpdateUnreadCount", unreadCount);
    }
}
