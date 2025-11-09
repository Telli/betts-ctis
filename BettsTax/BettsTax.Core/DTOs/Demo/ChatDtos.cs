namespace BettsTax.Core.DTOs.Demo;

/// <summary>
/// Conversation overview for the messaging module.
/// </summary>
public class ChatConversationDto
{
    public int Id { get; set; }
    public int? ClientId { get; set; }
    public string Client { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public string TimestampDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
}

/// <summary>
/// Message entry in a conversation.
/// </summary>
public class ChatMessageDto
{
    public int Id { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsInternal { get; set; }
}
