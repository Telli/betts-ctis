using BettsTax.Core.DTOs.Communication;
using BettsTax.Data.Models;

namespace BettsTax.Core.Services.Interfaces;

public interface IConversationService
{
    // Conversation Management
    Task<ConversationDto> CreateConversationAsync(CreateConversationDto request, string userId);
    Task<ConversationDto?> GetConversationAsync(int conversationId, string userId);
    Task<List<ConversationDto>> GetConversationsAsync(ConversationSearchDto search, string userId);
    Task<bool> UpdateConversationAsync(int conversationId, ConversationDto conversation, string userId);
    Task<bool> CloseConversationAsync(int conversationId, string reason, string userId);
    Task<bool> ReopenConversationAsync(int conversationId, string userId);
    Task<bool> AssignConversationAsync(int conversationId, string assignedToId, string userId);
    Task<bool> UpdatePriorityAsync(int conversationId, ConversationPriority priority, string userId);
    Task<bool> AddTagAsync(int conversationId, string tag, string userId);
    Task<bool> RemoveTagAsync(int conversationId, string tag, string userId);
    
    // Message Management
    Task<MessageDto> SendMessageAsync(CreateMessageDto message, string userId);
    Task<List<MessageDto>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 50, string? userId = null);
    Task<bool> EditMessageAsync(int messageId, string content, string userId);
    Task<bool> DeleteMessageAsync(int messageId, string userId);
    Task<bool> MarkMessageAsReadAsync(int messageId, string userId);
    Task<bool> MarkConversationAsReadAsync(int conversationId, string userId);
    Task<bool> AddReactionAsync(int messageId, string reaction, string userId);
    Task<bool> RemoveReactionAsync(int messageId, string reaction, string userId);
    
    // Participant Management
    Task<bool> AddParticipantAsync(int conversationId, string participantId, ParticipantRole role, string userId);
    Task<bool> RemoveParticipantAsync(int conversationId, string participantId, string userId);
    Task<bool> UpdateParticipantRoleAsync(int conversationId, string participantId, ParticipantRole role, string userId);
    Task<bool> UpdateNotificationPreferenceAsync(int conversationId, string participantId, string preference, string userId);
    
    // Analytics and Statistics
    Task<ConversationSummaryDto> GetConversationSummaryAsync(string? userId = null);
    Task<List<ConversationStatsDto>> GetConversationStatsAsync(DateTime fromDate, DateTime toDate, string? userId = null);
    Task<List<ConversationDto>> GetMyConversationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<ConversationDto>> GetUnreadConversationsAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}

public interface IChatService
{
    // Chat Room Management
    Task<ChatRoomDto> CreateChatRoomAsync(CreateChatRoomDto request, string userId);
    Task<ChatRoomDto?> GetChatRoomAsync(int roomId, string userId);
    Task<List<ChatRoomDto>> GetChatRoomsAsync(string userId, ChatRoomType? type = null);
    Task<List<ChatRoomDto>> GetPublicChatRoomsAsync();
    Task<bool> UpdateChatRoomAsync(int roomId, ChatRoomDto room, string userId);
    Task<bool> DeleteChatRoomAsync(int roomId, string userId);
    
    // Chat Room Participation
    Task<bool> JoinChatRoomAsync(int roomId, string userId);
    Task<bool> LeaveChatRoomAsync(int roomId, string userId);
    Task<bool> InviteUserAsync(int roomId, string inviteeId, string userId);
    Task<bool> KickUserAsync(int roomId, string kickUserId, string userId);
    Task<bool> MuteUserAsync(int roomId, string muteUserId, DateTime? mutedUntil, string userId);
    Task<bool> UnmuteUserAsync(int roomId, string muteUserId, string userId);
    Task<bool> PromoteUserAsync(int roomId, string promoteUserId, ChatRoomRole role, string userId);
    
    // Chat Messages
    Task<ChatMessageDto> SendChatMessageAsync(CreateChatMessageDto message, string userId);
    Task<List<ChatMessageDto>> GetChatMessagesAsync(int roomId, int page = 1, int pageSize = 50, string? userId = null);
    Task<bool> EditChatMessageAsync(int messageId, string content, string userId);
    Task<bool> DeleteChatMessageAsync(int messageId, string userId);
    Task<bool> UpdateLastSeenAsync(int roomId, string userId);
    
    // Real-time Features
    Task<bool> UserJoinedRoomAsync(int roomId, string userId);
    Task<bool> UserLeftRoomAsync(int roomId, string userId);
    Task<List<string>> GetOnlineUsersAsync(int roomId);
    Task<bool> SendTypingIndicatorAsync(int roomId, string userId, bool isTyping);
}

public interface ICommunicationNotificationService
{
    // Notification Management
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto notification);
    Task<List<NotificationDto>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId);
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(int notificationId, string userId);
    
    // Notification Delivery
    Task<bool> SendEmailNotificationAsync(string recipientId, string subject, string content, NotificationPriority priority = NotificationPriority.Normal);
    Task<bool> SendSmsNotificationAsync(string recipientId, string content, NotificationPriority priority = NotificationPriority.Normal);
    Task<bool> SendPushNotificationAsync(string recipientId, string title, string content, NotificationPriority priority = NotificationPriority.Normal);
    Task<bool> SendInAppNotificationAsync(string recipientId, string title, string content, NotificationPriority priority = NotificationPriority.Normal);
    
    // Bulk Notifications
    Task<bool> SendBulkNotificationAsync(List<string> recipientIds, NotificationType type, string subject, string content, NotificationChannel channel);
    Task<bool> SendNotificationToRoleAsync(string role, NotificationType type, string subject, string content, NotificationChannel channel);
    Task<bool> SendSystemAnnouncementAsync(string title, string content, NotificationPriority priority = NotificationPriority.Normal);
    
    // Template Management
    Task<bool> ProcessTemplatedNotificationAsync(string templateName, string recipientId, Dictionary<string, string> variables, NotificationChannel channel);
    Task<string> RenderTemplateAsync(string templateName, Dictionary<string, string> variables, NotificationChannel channel);
    
    // Notification Preferences
    Task<NotificationPreferencesDto> GetNotificationPreferencesAsync(string userId);
    Task<bool> UpdateNotificationPreferencesAsync(string userId, NotificationPreferencesDto preferences);
    Task<bool> ShouldSendNotificationAsync(string userId, NotificationType type, NotificationChannel channel);
    
    // Queue Management
    Task ProcessNotificationQueueAsync();
    Task<int> GetPendingNotificationCountAsync();
    Task<List<NotificationDto>> GetFailedNotificationsAsync(int page = 1, int pageSize = 20);
    Task<bool> RetryFailedNotificationAsync(int notificationId);
}

public interface IRealTimeService
{
    // Connection Management
    Task<bool> ConnectUserAsync(string userId, string connectionId);
    Task<bool> DisconnectUserAsync(string connectionId);
    Task<List<string>> GetUserConnectionsAsync(string userId);
    Task<bool> IsUserOnlineAsync(string userId);
    Task<List<string>> GetOnlineUsersAsync();
    
    // Message Broadcasting
    Task SendToUserAsync(string userId, string method, object data);
    Task SendToUsersAsync(List<string> userIds, string method, object data);
    Task SendToGroupAsync(string groupName, string method, object data);
    Task SendToAllAsync(string method, object data);
    
    // Group Management
    Task<bool> AddToGroupAsync(string connectionId, string groupName);
    Task<bool> RemoveFromGroupAsync(string connectionId, string groupName);
    Task<bool> AddUserToGroupAsync(string userId, string groupName);
    Task<bool> RemoveUserFromGroupAsync(string userId, string groupName);
    
    // Conversation-specific Broadcasting
    Task NotifyConversationUpdate(int conversationId, object data);
    Task NotifyNewMessage(int conversationId, MessageDto message);
    Task NotifyMessageUpdate(int conversationId, int messageId, object data);
    Task NotifyTypingIndicator(int conversationId, string userId, bool isTyping);
    
    // Chat Room Broadcasting
    Task NotifyChatRoomUpdate(int roomId, object data);
    Task NotifyNewChatMessage(int roomId, ChatMessageDto message);
    Task NotifyUserJoined(int roomId, string userId, string userName);
    Task NotifyUserLeft(int roomId, string userId, string userName);
    Task NotifyChatTypingIndicator(int roomId, string userId, bool isTyping);
    
    // Presence Management
    Task UpdateUserPresenceAsync(string userId, string status = "online");
    Task<string> GetUserPresenceAsync(string userId);
    Task<Dictionary<string, string>> GetUsersPresenceAsync(List<string> userIds);
}

public interface IFileService
{
    // File Upload/Download
    Task<AttachmentDto> UploadFileAsync(Stream fileStream, string fileName, string contentType, string uploadedBy);
    Task<Stream> DownloadFileAsync(string fileUrl);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<string> GetFileUrlAsync(string fileName);
    Task<AttachmentDto> GetFileInfoAsync(string fileUrl);
    
    // Image Processing
    Task<string> GenerateThumbnailAsync(string imageUrl, int width = 200, int height = 200);
    Task<bool> IsImageAsync(string contentType);
    Task<bool> IsVideoAsync(string contentType);
    
    // File Validation
    Task<bool> ValidateFileAsync(string fileName, string contentType, long size);
    Task<List<string>> GetAllowedFileTypesAsync();
    Task<long> GetMaxFileSizeAsync();
    
    // Cleanup
    Task CleanupOrphanedFilesAsync();
    Task<long> GetTotalStorageUsedAsync();
    Task<long> GetUserStorageUsedAsync(string userId);
}

public interface ICommunicationAnalyticsService
{
    // Conversation Analytics
    Task<Dictionary<string, object>> GetConversationAnalyticsAsync(DateTime fromDate, DateTime toDate);
    Task<List<ConversationStatsDto>> GetConversationTrendsAsync(DateTime fromDate, DateTime toDate, string groupBy = "day");
    Task<Dictionary<string, int>> GetConversationsByTypeAsync(DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, decimal>> GetResponseTimeAnalyticsAsync(DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, int>> GetUserActivityAsync(DateTime fromDate, DateTime toDate);
    
    // Message Analytics
    Task<int> GetTotalMessagesAsync(DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, int>> GetMessagesByTypeAsync(DateTime fromDate, DateTime toDate);
    Task<List<object>> GetMessageVolumeAsync(DateTime fromDate, DateTime toDate, string groupBy = "hour");
    Task<Dictionary<string, int>> GetMostActiveUsersAsync(DateTime fromDate, DateTime toDate, int limit = 10);
    
    // Notification Analytics
    Task<Dictionary<string, object>> GetNotificationAnalyticsAsync(DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, int>> GetNotificationsByChannelAsync(DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, decimal>> GetNotificationDeliveryRatesAsync(DateTime fromDate, DateTime toDate);
    Task<List<object>> GetNotificationVolumeAsync(DateTime fromDate, DateTime toDate, string groupBy = "hour");
    
    // Performance Metrics
    Task<Dictionary<string, object>> GetPerformanceMetricsAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetAverageResponseTimeAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetAverageResolutionTimeAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetCustomerSatisfactionScoreAsync(DateTime fromDate, DateTime toDate);
    
    // Export and Reporting
    Task<byte[]> ExportConversationDataAsync(DateTime fromDate, DateTime toDate, string format = "csv");
    Task<byte[]> ExportAnalyticsReportAsync(DateTime fromDate, DateTime toDate, string reportType);
}