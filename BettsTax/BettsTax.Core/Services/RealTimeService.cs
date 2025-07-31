using BettsTax.Core.DTOs.Communication;
using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BettsTax.Core.Services;

public class RealTimeService : IRealTimeService
{
    private readonly ILogger<RealTimeService> _logger;
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, string> _connectionUsers = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _groupConnections = new();
    private readonly ConcurrentDictionary<string, string> _userPresence = new();
    private readonly IHubContext<CommunicationHub> _hubContext;

    public RealTimeService(ILogger<RealTimeService> logger, IHubContext<CommunicationHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<bool> ConnectUserAsync(string userId, string connectionId)
    {
        try
        {
            // Add connection to user mapping
            _userConnections.AddOrUpdate(userId, 
                new HashSet<string> { connectionId },
                (key, existing) => { existing.Add(connectionId); return existing; });

            // Add user to connection mapping
            _connectionUsers.TryAdd(connectionId, userId);

            // Update user presence
            await UpdateUserPresenceAsync(userId, "online");

            // Add user to their personal group
            await _hubContext.Groups.AddToGroupAsync(connectionId, $"user_{userId}");

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, connectionId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting user {UserId} with connection {ConnectionId}", userId, connectionId);
            return false;
        }
    }

    public async Task<bool> DisconnectUserAsync(string connectionId)
    {
        try
        {
            if (_connectionUsers.TryRemove(connectionId, out var userId))
            {
                // Remove connection from user's connections
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(connectionId);
                    
                    // If no more connections, update presence to offline
                    if (connections.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);
                        await UpdateUserPresenceAsync(userId, "offline");
                    }
                }

                // Remove from all groups
                var groupsToRemove = _groupConnections
                    .Where(kvp => kvp.Value.Contains(connectionId))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var groupName in groupsToRemove)
                {
                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
                    _groupConnections[groupName].Remove(connectionId);
                }

                _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, connectionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting connection {ConnectionId}", connectionId);
            return false;
        }
    }

    public async Task<List<string>> GetUserConnectionsAsync(string userId)
    {
        return _userConnections.TryGetValue(userId, out var connections) 
            ? connections.ToList() 
            : new List<string>();
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
    }

    public async Task<List<string>> GetOnlineUsersAsync()
    {
        return _userConnections.Keys.ToList();
    }

    public async Task SendToUserAsync(string userId, string method, object data)
    {
        try
        {
            await _hubContext.Clients.Group($"user_{userId}").SendAsync(method, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to user {UserId}", userId);
        }
    }

    public async Task SendToUsersAsync(List<string> userIds, string method, object data)
    {
        try
        {
            var groups = userIds.Select(id => $"user_{id}").ToList();
            await _hubContext.Clients.Groups(groups).SendAsync(method, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to multiple users");
        }
    }

    public async Task SendToGroupAsync(string groupName, string method, object data)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync(method, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to group {GroupName}", groupName);
        }
    }

    public async Task SendToAllAsync(string method, object data)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(method, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to all clients");
        }
    }

    public async Task<bool> AddToGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await _hubContext.Groups.AddToGroupAsync(connectionId, groupName);
            
            _groupConnections.AddOrUpdate(groupName,
                new HashSet<string> { connectionId },
                (key, existing) => { existing.Add(connectionId); return existing; });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection {ConnectionId} to group {GroupName}", connectionId, groupName);
            return false;
        }
    }

    public async Task<bool> RemoveFromGroupAsync(string connectionId, string groupName)
    {
        try
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
            
            if (_groupConnections.TryGetValue(groupName, out var connections))
            {
                connections.Remove(connectionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId} from group {GroupName}", connectionId, groupName);
            return false;
        }
    }

    public async Task<bool> AddUserToGroupAsync(string userId, string groupName)
    {
        try
        {
            var connections = await GetUserConnectionsAsync(userId);
            foreach (var connectionId in connections)
            {
                await AddToGroupAsync(connectionId, groupName);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to group {GroupName}", userId, groupName);
            return false;
        }
    }

    public async Task<bool> RemoveUserFromGroupAsync(string userId, string groupName)
    {
        try
        {
            var connections = await GetUserConnectionsAsync(userId);
            foreach (var connectionId in connections)
            {
                await RemoveFromGroupAsync(connectionId, groupName);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from group {GroupName}", userId, groupName);
            return false;
        }
    }

    public async Task NotifyConversationUpdate(int conversationId, object data)
    {
        await SendToGroupAsync($"conversation_{conversationId}", "ConversationUpdated", new { conversationId, data });
    }

    public async Task NotifyNewMessage(int conversationId, MessageDto message)
    {
        await SendToGroupAsync($"conversation_{conversationId}", "NewMessage", message);
    }

    public async Task NotifyMessageUpdate(int conversationId, int messageId, object data)
    {
        await SendToGroupAsync($"conversation_{conversationId}", "MessageUpdated", new { messageId, data });
    }

    public async Task NotifyTypingIndicator(int conversationId, string userId, bool isTyping)
    {
        await SendToGroupAsync($"conversation_{conversationId}", "TypingIndicator", new { userId, isTyping });
    }

    public async Task NotifyChatRoomUpdate(int roomId, object data)
    {
        await SendToGroupAsync($"chatroom_{roomId}", "ChatRoomUpdated", new { roomId, data });
    }

    public async Task NotifyNewChatMessage(int roomId, ChatMessageDto message)
    {
        await SendToGroupAsync($"chatroom_{roomId}", "NewChatMessage", message);
    }

    public async Task NotifyUserJoined(int roomId, string userId, string userName)
    {
        await SendToGroupAsync($"chatroom_{roomId}", "UserJoined", new { userId, userName });
    }

    public async Task NotifyUserLeft(int roomId, string userId, string userName)
    {
        await SendToGroupAsync($"chatroom_{roomId}", "UserLeft", new { userId, userName });
    }

    public async Task NotifyChatTypingIndicator(int roomId, string userId, bool isTyping)
    {
        await SendToGroupAsync($"chatroom_{roomId}", "ChatTypingIndicator", new { userId, isTyping });
    }

    public async Task UpdateUserPresenceAsync(string userId, string status = "online")
    {
        _userPresence.AddOrUpdate(userId, status, (key, oldValue) => status);
        
        // Notify all users about presence change
        await SendToAllAsync("PresenceUpdated", new { userId, status, timestamp = DateTime.UtcNow });
    }

    public async Task<string> GetUserPresenceAsync(string userId)
    {
        return _userPresence.TryGetValue(userId, out var status) ? status : "offline";
    }

    public async Task<Dictionary<string, string>> GetUsersPresenceAsync(List<string> userIds)
    {
        var presence = new Dictionary<string, string>();
        
        foreach (var userId in userIds)
        {
            presence[userId] = await GetUserPresenceAsync(userId);
        }
        
        return presence;
    }
}

// SignalR Hub for real-time communication
public class CommunicationHub : Hub
{
    private readonly IRealTimeService _realTimeService;
    private readonly ILogger<CommunicationHub> _logger;

    public CommunicationHub(IRealTimeService realTimeService, ILogger<CommunicationHub> logger)
    {
        _realTimeService = realTimeService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await _realTimeService.ConnectUserAsync(userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _realTimeService.DisconnectUserAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}", Context.UserIdentifier, conversationId);
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", Context.UserIdentifier, conversationId);
    }

    public async Task JoinChatRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chatroom_{roomId}");
        await _realTimeService.NotifyUserJoined(roomId, Context.UserIdentifier!, Context.User?.Identity?.Name ?? "Unknown");
    }

    public async Task LeaveChatRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chatroom_{roomId}");
        await _realTimeService.NotifyUserLeft(roomId, Context.UserIdentifier!, Context.User?.Identity?.Name ?? "Unknown");
    }

    public async Task SendTypingIndicator(int conversationId, bool isTyping)
    {
        if (!string.IsNullOrEmpty(Context.UserIdentifier))
        {
            await _realTimeService.NotifyTypingIndicator(conversationId, Context.UserIdentifier, isTyping);
        }
    }

    public async Task SendChatTypingIndicator(int roomId, bool isTyping)
    {
        if (!string.IsNullOrEmpty(Context.UserIdentifier))
        {
            await _realTimeService.NotifyChatTypingIndicator(roomId, Context.UserIdentifier, isTyping);
        }
    }

    public async Task UpdatePresence(string status)
    {
        if (!string.IsNullOrEmpty(Context.UserIdentifier))
        {
            await _realTimeService.UpdateUserPresenceAsync(Context.UserIdentifier, status);
        }
    }
}