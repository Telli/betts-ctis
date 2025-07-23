using BettsTax.Data;

namespace BettsTax.Core.DTOs
{
    public class MessageDto
    {
        public int MessageId { get; set; }
        public int? ParentMessageId { get; set; }
        
        // Sender info
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        
        // Recipient info
        public string RecipientId { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientRole { get; set; } = string.Empty;
        
        // Client association
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientNumber { get; set; }
        
        // Tax filing association
        public int? TaxFilingId { get; set; }
        public string? TaxFilingReference { get; set; }
        
        // Document association
        public int? DocumentId { get; set; }
        public string? DocumentName { get; set; }
        
        // Message content
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        
        // Message metadata
        public MessageStatus Status { get; set; }
        public MessagePriority Priority { get; set; }
        public MessageCategory Category { get; set; }
        
        // Timestamps
        public DateTime SentDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }
        
        // Flags
        public bool IsStarred { get; set; }
        public bool IsArchived { get; set; }
        public bool HasAttachments { get; set; }
        public bool IsSystemMessage { get; set; }
        
        // Threading
        public int ReplyCount { get; set; }
        public List<MessageDto> Replies { get; set; } = new List<MessageDto>();
        
        // Attachments
        public List<DocumentDto> Attachments { get; set; } = new List<DocumentDto>();
    }

    public class SendMessageDto
    {
        public string RecipientId { get; set; } = string.Empty;
        public int? ClientId { get; set; }
        public int? TaxFilingId { get; set; }
        public int? DocumentId { get; set; }
        public int? ParentMessageId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        public MessageCategory Category { get; set; } = MessageCategory.General;
        public List<int> AttachmentIds { get; set; } = new List<int>();
    }

    public class MessageReplyDto
    {
        public string Body { get; set; } = string.Empty;
        public List<int> AttachmentIds { get; set; } = new List<int>();
    }

    public class MessageThreadDto
    {
        public MessageDto RootMessage { get; set; } = null!;
        public int TotalReplies { get; set; }
        public List<string> Participants { get; set; } = new List<string>();
        public DateTime LastActivityDate { get; set; }
        public bool HasUnreadMessages { get; set; }
    }

    public class MessageFolderDto
    {
        public string FolderName { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class MessageSearchDto
    {
        public string? SearchTerm { get; set; }
        public MessageCategory? Category { get; set; }
        public MessagePriority? Priority { get; set; }
        public MessageStatus? Status { get; set; }
        public int? ClientId { get; set; }
        public int? TaxFilingId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsStarred { get; set; }
        public bool? HasAttachments { get; set; }
        public string? SenderId { get; set; }
        public string? RecipientId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class MessageTemplateDto
    {
        public int MessageTemplateId { get; set; }
        public string TemplateCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public MessageCategory Category { get; set; }
        public Dictionary<string, string> AvailableVariables { get; set; } = new Dictionary<string, string>();
        public bool IsActive { get; set; }
    }

    public class ApplyMessageTemplateDto
    {
        public int TemplateId { get; set; }
        public string RecipientId { get; set; } = string.Empty;
        public int? ClientId { get; set; }
        public int? TaxFilingId { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }

    public class MessageNotificationDto
    {
        public int MessageId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public MessagePriority Priority { get; set; }
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
    }
}