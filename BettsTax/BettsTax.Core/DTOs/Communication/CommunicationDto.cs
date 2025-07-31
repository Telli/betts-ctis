using BettsTax.Data.Models;

namespace BettsTax.Core.DTOs.Communication;

public class ConversationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ConversationType Type { get; set; }
    public ConversationStatus Status { get; set; }
    public ConversationPriority Priority { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public int? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Subject { get; set; }
    public string? Category { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public string? ClosedByName { get; set; }
    public string? CloseReason { get; set; }
    public int MessageCount { get; set; }
    public int UnreadCount { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
    public List<ConversationParticipantDto> Participants { get; set; } = new();
    public List<ConversationTagDto> Tags { get; set; } = new();
}

public class CreateConversationDto
{
    public string Title { get; set; } = string.Empty;
    public ConversationType Type { get; set; }
    public ConversationPriority Priority { get; set; } = ConversationPriority.Normal;
    public int? ClientId { get; set; }
    public string? Subject { get; set; }
    public string? Category { get; set; }
    public bool IsUrgent { get; set; } = false;
    public bool IsInternal { get; set; } = false;
    public string InitialMessage { get; set; } = string.Empty;
    public List<string> ParticipantIds { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public MessageType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public bool IsInternal { get; set; }
    public bool IsSystemMessage { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public string? EditedBy { get; set; }
    public string? EditedByName { get; set; }
    public bool IsDeleted { get; set; }
    public int? ReplyToMessageId { get; set; }
    public MessageDto? ReplyToMessage { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<MessageReactionDto> Reactions { get; set; } = new();
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class CreateMessageDto
{
    public int ConversationId { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string Content { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public bool IsInternal { get; set; } = false;
    public int? ReplyToMessageId { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
}

public class ConversationParticipantDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanModerate { get; set; }
    public DateTime? LastReadAt { get; set; }
    public int? LastReadMessageId { get; set; }
    public string NotificationPreference { get; set; } = "all";
}

public class ConversationTagDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class MessageReactionDto
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ConversationId { get; set; }
    public int? MessageId { get; set; }
}

public class CreateNotificationDto
{
    public string RecipientId { get; set; } = string.Empty;
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledFor { get; set; }
    public int? ConversationId { get; set; }
    public int? MessageId { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
}

public class ChatRoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChatRoomType Type { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? Rules { get; set; }
    public List<ChatRoomParticipantDto> Participants { get; set; } = new();
    public List<ChatMessageDto> RecentMessages { get; set; } = new();
    public bool IsParticipant { get; set; }
    public ChatRoomRole? UserRole { get; set; }
}

public class CreateChatRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChatRoomType Type { get; set; } = ChatRoomType.Public;
    public int MaxParticipants { get; set; } = 100;
    public string? WelcomeMessage { get; set; }
    public string? Rules { get; set; }
    public List<string> InitialParticipants { get; set; } = new();
}

public class ChatRoomParticipantDto
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public ChatRoomRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsMuted { get; set; }
    public DateTime? MutedUntil { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsOnline { get; set; }
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatar { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChatMessageType Type { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public int? ReplyToMessageId { get; set; }
    public ChatMessageDto? ReplyToMessage { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<string> Mentions { get; set; } = new();
}

public class CreateChatMessageDto
{
    public int ChatRoomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChatMessageType Type { get; set; } = ChatMessageType.Text;
    public int? ReplyToMessageId { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<string> Mentions { get; set; } = new();
}

public class ConversationSummaryDto
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int PendingConversations { get; set; }
    public int UrgentConversations { get; set; }
    public int UnassignedConversations { get; set; }
    public decimal AverageResponseTime { get; set; } // in hours
    public decimal AverageResolutionTime { get; set; } // in hours
    public List<ConversationStatusCount> StatusBreakdown { get; set; } = new();
    public List<ConversationPriorityCount> PriorityBreakdown { get; set; } = new();
}

public class ConversationStatusCount
{
    public ConversationStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ConversationPriorityCount
{
    public ConversationPriority Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class NotificationPreferencesDto
{
    public string UserId { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public bool PushEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public string EmailFrequency { get; set; } = "immediate"; // immediate, hourly, daily
    public List<NotificationType> DisabledTypes { get; set; } = new();
    public string QuietHoursStart { get; set; } = "22:00";
    public string QuietHoursEnd { get; set; } = "08:00";
    public bool WeekendNotifications { get; set; } = false;
}

public class ConversationSearchDto
{
    public string? Query { get; set; }
    public ConversationType? Type { get; set; }
    public ConversationStatus? Status { get; set; }
    public ConversationPriority? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public int? ClientId { get; set; }
    public string? Category { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsUrgent { get; set; }
    public bool? IsInternal { get; set; }
    public List<string> Tags { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "LastMessageAt";
    public string SortDirection { get; set; } = "desc";
}

public class ConversationStatsDto
{
    public string Period { get; set; } = string.Empty;
    public int ConversationsCreated { get; set; }
    public int ConversationsResolved { get; set; }
    public int MessagesExchanged { get; set; }
    public decimal AverageResponseTime { get; set; }
    public decimal AverageResolutionTime { get; set; }
    public int ParticipantsActive { get; set; }
    public List<ConversationTypeStats> TypeStats { get; set; } = new();
}

public class ConversationTypeStats
{
    public ConversationType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}