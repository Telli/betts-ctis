using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Core.Services.Interfaces;
using System.Security.Claims;

namespace BettsTax.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly IConversationService _communicationService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        ApplicationDbContext context,
        IConversationService communicationService,
        ILogger<ChatHub> logger)
    {
        _context = context;
        _communicationService = communicationService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId == null)
        {
            _logger.LogWarning("User connected without valid identifier");
            return;
        }

        _logger.LogInformation("User {UserId} connected to ChatHub", userId);

        // Join user to their personal group for direct messages
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Join user to all their active chat rooms
        var userRooms = await _context.ChatRoomParticipants
            .Where(p => p.UserId == userId && p.IsActive)
            .Include(p => p.ChatRoom)
            .Select(p => p.ChatRoom!.Id)
            .ToListAsync();

        foreach (var roomId in userRooms)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
        }

        // Update user's last seen status
        await UpdateUserPresence(userId, true);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
            await UpdateUserPresence(userId, false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(int roomId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        // Verify user has access to this room
        var participant = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && p.IsActive);

        if (participant == null)
        {
            _logger.LogWarning("User {UserId} attempted to join room {RoomId} without permission", userId, roomId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
        
        // Update last seen for this room
        participant.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
    }

    public async Task LeaveRoom(int roomId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
        _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
    }

    public async Task SendMessage(int roomId, string content, bool isInternal = false, int? replyToMessageId = null)
    {
        var userId = Context.UserIdentifier;
        if (userId == null || string.IsNullOrWhiteSpace(content)) return;

        try
        {
            // Verify user has permission to send messages in this room
            var participant = await _context.ChatRoomParticipants
                .Include(p => p.ChatRoom)
                .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && p.IsActive);

            if (participant == null)
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to send messages in this room");
                return;
            }

            // Check if user can send internal messages
            if (isInternal)
            {
                var userRoles = Context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
                if (!userRoles.Contains("Admin") && !userRoles.Contains("Associate") && participant.Role < ChatRoomRole.Moderator)
                {
                    await Clients.Caller.SendAsync("Error", "You don't have permission to send internal messages");
                    return;
                }
            }

            // Create the message
            var message = new ChatMessage
            {
                ChatRoomId = roomId,
                SenderId = userId,
                Content = content.Trim(),
                Type = ChatMessageType.Text,
                SentAt = DateTime.UtcNow,
                ReplyToMessageId = replyToMessageId,
                IsPrivate = isInternal
            };

            _context.ChatMessages.Add(message);

            // Update room stats
            participant.ChatRoom!.MessageCount++;
            participant.ChatRoom.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Load the complete message with sender info
            var completeMessage = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                .ThenInclude(r => r!.Sender)
                .FirstAsync(m => m.Id == message.Id);

            // Prepare message data for clients
            var messageData = new
            {
                Id = completeMessage.Id,
                ChatRoomId = completeMessage.ChatRoomId,
                SenderId = completeMessage.SenderId,
                SenderName = completeMessage.Sender?.FirstName + " " + completeMessage.Sender?.LastName,
                Content = completeMessage.Content,
                SentAt = completeMessage.SentAt,
                IsPrivate = completeMessage.IsPrivate,
                ReplyToMessage = completeMessage.ReplyToMessage != null ? new
                {
                    Id = completeMessage.ReplyToMessage.Id,
                    Content = completeMessage.ReplyToMessage.Content,
                    SenderName = completeMessage.ReplyToMessage.Sender?.FirstName + " " + completeMessage.ReplyToMessage.Sender?.LastName
                } : null
            };

            // Send to appropriate recipients
            if (isInternal)
            {
                // Send only to moderators and admins
                var moderatorIds = await _context.ChatRoomParticipants
                    .Where(p => p.ChatRoomId == roomId && p.IsActive && 
                               (p.Role >= ChatRoomRole.Moderator))
                    .Select(p => p.UserId)
                    .ToListAsync();

                var adminUsers = await _context.Users
                    .Where(u => u.Role == "Admin" || u.Role == "Associate")
                    .Select(u => u.Id)
                    .ToListAsync();

                var recipients = moderatorIds.Union(adminUsers).Distinct().ToList();

                foreach (var recipientId in recipients)
                {
                    await Clients.Group($"user_{recipientId}").SendAsync("ReceiveMessage", messageData);
                }
            }
            else
            {
                // Send to all room participants
                await Clients.Group($"room_{roomId}").SendAsync("ReceiveMessage", messageData);
            }

            _logger.LogInformation("Message sent by {UserId} to room {RoomId} (Internal: {IsInternal})", 
                userId, roomId, isInternal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message from {UserId} to room {RoomId}", userId, roomId);
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    public async Task EditMessage(int messageId, string newContent)
    {
        var userId = Context.UserIdentifier;
        if (userId == null || string.IsNullOrWhiteSpace(newContent)) return;

        try
        {
            var message = await _context.ChatMessages
                .Include(m => m.ChatRoom)
                .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId && !m.IsDeleted);

            if (message == null)
            {
                await Clients.Caller.SendAsync("Error", "Message not found or you don't have permission to edit it");
                return;
            }

            // Check if message is too old to edit (e.g., 15 minutes)
            if (DateTime.UtcNow - message.SentAt > TimeSpan.FromMinutes(15))
            {
                await Clients.Caller.SendAsync("Error", "Message is too old to edit");
                return;
            }

            message.Content = newContent.Trim();
            message.EditedAt = DateTime.UtcNow;
            message.EditedBy = userId;

            await _context.SaveChangesAsync();

            var editData = new
            {
                MessageId = messageId,
                NewContent = message.Content,
                EditedAt = message.EditedAt
            };

            // Notify room participants about the edit
            if (message.IsPrivate)
            {
                // Send only to moderators and admins
                var moderatorIds = await _context.ChatRoomParticipants
                    .Where(p => p.ChatRoomId == message.ChatRoomId && p.IsActive && 
                               p.Role >= ChatRoomRole.Moderator)
                    .Select(p => p.UserId)
                    .ToListAsync();

                foreach (var recipientId in moderatorIds)
                {
                    await Clients.Group($"user_{recipientId}").SendAsync("MessageEdited", editData);
                }
            }
            else
            {
                await Clients.Group($"room_{message.ChatRoomId}").SendAsync("MessageEdited", editData);
            }

            _logger.LogInformation("Message {MessageId} edited by {UserId}", messageId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId} by {UserId}", messageId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to edit message");
        }
    }

    public async Task DeleteMessage(int messageId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            var message = await _context.ChatMessages
                .Include(m => m.ChatRoom)
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

            if (message == null)
            {
                await Clients.Caller.SendAsync("Error", "Message not found");
                return;
            }

            // Check permissions - user can delete their own message or moderators can delete any
            var canDelete = message.SenderId == userId;
            if (!canDelete)
            {
                var participant = await _context.ChatRoomParticipants
                    .FirstOrDefaultAsync(p => p.ChatRoomId == message.ChatRoomId && p.UserId == userId && 
                                            p.IsActive && p.Role >= ChatRoomRole.Moderator);
                canDelete = participant != null;
            }

            if (!canDelete)
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to delete this message");
                return;
            }

            message.IsDeleted = true;
            message.DeletedAt = DateTime.UtcNow;
            message.DeletedBy = userId;

            await _context.SaveChangesAsync();

            var deleteData = new { MessageId = messageId };

            // Notify room participants about the deletion
            if (message.IsPrivate)
            {
                var moderatorIds = await _context.ChatRoomParticipants
                    .Where(p => p.ChatRoomId == message.ChatRoomId && p.IsActive && 
                               p.Role >= ChatRoomRole.Moderator)
                    .Select(p => p.UserId)
                    .ToListAsync();

                foreach (var recipientId in moderatorIds)
                {
                    await Clients.Group($"user_{recipientId}").SendAsync("MessageDeleted", deleteData);
                }
            }
            else
            {
                await Clients.Group($"room_{message.ChatRoomId}").SendAsync("MessageDeleted", deleteData);
            }

            _logger.LogInformation("Message {MessageId} deleted by {UserId}", messageId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message {MessageId} by {UserId}", messageId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to delete message");
        }
    }

    public async Task StartTyping(int roomId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        var userName = Context.User?.Identity?.Name ?? "Unknown User";
        
        await Clients.OthersInGroup($"room_{roomId}").SendAsync("UserTyping", new { UserId = userId, UserName = userName, RoomId = roomId });
    }

    public async Task StopTyping(int roomId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        await Clients.OthersInGroup($"room_{roomId}").SendAsync("UserStoppedTyping", new { UserId = userId, RoomId = roomId });
    }

    public async Task AssignRoom(int roomId, string assignToUserId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        try
        {
            // Check if user has permission to assign rooms (Admin/Associate or room moderator)
            var userRoles = Context.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList() ?? new List<string>();
            var canAssign = userRoles.Contains("Admin") || userRoles.Contains("Associate");

            if (!canAssign)
            {
                var participant = await _context.ChatRoomParticipants
                    .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && 
                                            p.IsActive && p.Role >= ChatRoomRole.Moderator);
                canAssign = participant != null;
            }

            if (!canAssign)
            {
                await Clients.Caller.SendAsync("Error", "You don't have permission to assign rooms");
                return;
            }

            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            // Update room assignment - assuming we add AssignedToUserId field
            // This would require a migration to add the field to ChatRoom
            // For now, we'll use the metadata field to store assignment info
            var assignmentData = new { AssignedToUserId = assignToUserId, AssignedAt = DateTime.UtcNow, AssignedBy = userId };
            room.Settings = System.Text.Json.JsonSerializer.Serialize(assignmentData);
            room.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify room participants about the assignment
            await Clients.Group($"room_{roomId}").SendAsync("RoomAssigned", new 
            { 
                RoomId = roomId, 
                AssignedToUserId = assignToUserId, 
                AssignedBy = userId 
            });

            _logger.LogInformation("Room {RoomId} assigned to {AssignedToUserId} by {UserId}", roomId, assignToUserId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning room {RoomId} by {UserId}", roomId, userId);
            await Clients.Caller.SendAsync("Error", "Failed to assign room");
        }
    }

    private async Task UpdateUserPresence(string userId, bool isOnline)
    {
        try
        {
            // Update user's last seen in all their active room participations
            var participations = await _context.ChatRoomParticipants
                .Where(p => p.UserId == userId && p.IsActive)
                .ToListAsync();

            foreach (var participation in participations)
            {
                participation.LastSeenAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Notify other users about presence change
            var presenceData = new { UserId = userId, IsOnline = isOnline, LastSeen = DateTime.UtcNow };
            
            foreach (var participation in participations)
            {
                await Clients.OthersInGroup($"room_{participation.ChatRoomId}")
                    .SendAsync("UserPresenceChanged", presenceData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating presence for user {UserId}", userId);
        }
    }
}
