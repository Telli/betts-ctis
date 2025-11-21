using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data
{
    public enum MessageStatus
    {
        Sent,
        Delivered,
        Read,
        Archived,
        Deleted
    }

    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum MessageCategory
    {
        General,
        DocumentRequest,
        DocumentReview,
        TaxFiling,
        Payment,
        Deadline,
        Compliance,
        Other
    }

    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        // Threading support
        public int? ParentMessageId { get; set; }
        [ForeignKey("ParentMessageId")]
        public virtual Message? ParentMessage { get; set; }
        public virtual ICollection<Message> Replies { get; set; } = new List<Message>();

        // Participants
        [Required]
        [MaxLength(450)]
        public string SenderId { get; set; } = string.Empty;
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; } = null!;

        [Required]
        [MaxLength(450)]
        public string RecipientId { get; set; } = string.Empty;
        [ForeignKey("RecipientId")]
        public virtual ApplicationUser Recipient { get; set; } = null!;

        // Client association
        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        // Conversation association
        public int? ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public virtual Models.Conversation? Conversation { get; set; }

        // Tax filing association
        public int? TaxFilingId { get; set; }
        [ForeignKey("TaxFilingId")]
        public virtual TaxFiling? TaxFiling { get; set; }

        // Document association
        public int? DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        // Message content
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        // Message metadata
        public MessageStatus Status { get; set; } = MessageStatus.Sent;
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        public MessageCategory Category { get; set; } = MessageCategory.General;

        // Timestamps
        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }

        // Flags
        public bool IsStarred { get; set; }
        public bool IsArchived { get; set; }
        public bool IsDeleted { get; set; }
        public bool HasAttachments { get; set; }
        public bool IsInternal { get; set; } // Internal notes not visible to clients

        // System generated
        public bool IsSystemMessage { get; set; }
        public string? SystemMessageType { get; set; }

        // Attachments (comma-separated document IDs)
        public string? AttachmentIds { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }

    public class MessageAttachment
    {
        [Key]
        public int MessageAttachmentId { get; set; }

        public int MessageId { get; set; }
        [ForeignKey("MessageId")]
        public virtual Message Message { get; set; } = null!;

        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; } = null!;

        public DateTime AttachedDate { get; set; } = DateTime.UtcNow;
    }

    public class MessageTemplate
    {
        [Key]
        public int MessageTemplateId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public MessageCategory Category { get; set; }

        // Variables available in template (JSON)
        public string? AvailableVariables { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}