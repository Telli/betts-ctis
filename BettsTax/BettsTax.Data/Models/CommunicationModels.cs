using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettsTax.Data.Models;

public class Conversation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public ConversationType Type { get; set; }
    
    [Required]
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    
    [Required]
    public ConversationPriority Priority { get; set; } = ConversationPriority.Normal;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    [StringLength(450)]
    public string? AssignedTo { get; set; }
    
    public int? ClientId { get; set; }
    
    [StringLength(100)]
    public string? Subject { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    public bool IsUrgent { get; set; } = false;
    
    public bool IsInternal { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastMessageAt { get; set; }
    
    public DateTime? ClosedAt { get; set; }
    
    [StringLength(450)]
    public string? ClosedBy { get; set; }
    
    [StringLength(500)]
    public string? CloseReason { get; set; }
    
    public int MessageCount { get; set; } = 0;
    
    public int UnreadCount { get; set; } = 0;
    
    [StringLength(2000)]
    public string? Metadata { get; set; } // JSON for additional data
    
    // Navigation properties
    public Client? Client { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
    public ApplicationUser? ClosedByUser { get; set; }
    public List<Message> Messages { get; set; } = new();
    public List<ConversationParticipant> Participants { get; set; } = new();
    public List<ConversationTag> Tags { get; set; } = new();
}

public class Message
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ConversationId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string SenderId { get; set; } = string.Empty;
    
    [Required]
    public MessageType Type { get; set; } = MessageType.Text;
    
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Subject { get; set; }
    
    public bool IsInternal { get; set; } = false;
    
    public bool IsSystemMessage { get; set; } = false;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EditedAt { get; set; }
    
    [StringLength(450)]
    public string? EditedBy { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    [StringLength(450)]
    public string? DeletedBy { get; set; }
    
    public int? ReplyToMessageId { get; set; }
    
    [StringLength(1000)]
    public string? Attachments { get; set; } // JSON array of attachment info
    
    [StringLength(2000)]
    public string? Metadata { get; set; } // JSON for additional data
    
    // Navigation properties
    public Conversation? Conversation { get; set; }
    public ApplicationUser? Sender { get; set; }
    public ApplicationUser? EditedByUser { get; set; }
    public ApplicationUser? DeletedByUser { get; set; }
    public Message? ReplyToMessage { get; set; }
    public List<MessageRead> ReadReceipts { get; set; } = new();
    public List<MessageReaction> Reactions { get; set; } = new();
}

public class ConversationParticipant
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ConversationId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LeftAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool CanRead { get; set; } = true;
    
    public bool CanWrite { get; set; } = true;
    
    public bool CanModerate { get; set; } = false;
    
    public DateTime? LastReadAt { get; set; }
    
    public int? LastReadMessageId { get; set; }
    
    [StringLength(50)]
    public string? NotificationPreference { get; set; } = "all"; // all, mentions, none
    
    // Navigation properties
    public Conversation? Conversation { get; set; }
    public ApplicationUser? User { get; set; }
    public Message? LastReadMessage { get; set; }
}

public class MessageRead
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int MessageId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Message? Message { get; set; }
    public ApplicationUser? User { get; set; }
}

public class MessageReaction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int MessageId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Reaction { get; set; } = string.Empty; // emoji or reaction type
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Message? Message { get; set; }
    public ApplicationUser? User { get; set; }
}

public class ConversationTag
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ConversationId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Tag { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? Color { get; set; }
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Conversation? Conversation { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public class NotificationTemplate
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    [StringLength(4000)]
    public string EmailTemplate { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string SmsTemplate { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string PushTemplate { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(1000)]
    public string? Variables { get; set; } // JSON array of template variables
    
    // Navigation properties
    public ApplicationUser? CreatedByUser { get; set; }
}

public class NotificationQueue
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(450)]
    public string RecipientId { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? RecipientEmail { get; set; }
    
    [StringLength(20)]
    public string? RecipientPhone { get; set; }
    
    [Required]
    public NotificationType Type { get; set; }
    
    [Required]
    public NotificationChannel Channel { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    [StringLength(4000)]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    
    [Required]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    public DateTime? ScheduledFor { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentAt { get; set; }
    
    public int AttemptCount { get; set; } = 0;
    
    public int MaxAttempts { get; set; } = 3;
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    [StringLength(100)]
    public string? ExternalId { get; set; } // Provider-specific ID
    
    [StringLength(2000)]
    public string? Metadata { get; set; } // JSON for additional data
    
    // Related entities
    public int? ConversationId { get; set; }
    public int? MessageId { get; set; }
    
    // Navigation properties
    public ApplicationUser? Recipient { get; set; }
    public Conversation? Conversation { get; set; }
    public Message? Message { get; set; }
}

public class ChatRoom
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public ChatRoomType Type { get; set; } = ChatRoomType.Public;
    
    public bool IsActive { get; set; } = true;
    
    [StringLength(450)]
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public int MaxParticipants { get; set; } = 100;
    
    public int CurrentParticipants { get; set; } = 0;
    
    [StringLength(500)]
    public string? WelcomeMessage { get; set; }
    
    [StringLength(2000)]
    public string? Rules { get; set; }
    
    [StringLength(1000)]
    public string? Settings { get; set; } // JSON for room settings
    
    // Enhanced features for tax system
    public int? ClientId { get; set; } // Link to specific client for client-specific rooms
    
    [StringLength(50)]
    public string? TaxYear { get; set; } // For year-specific discussions
    
    public TaxType? TaxType { get; set; } // For tax-type specific rooms
    
    public bool IsArchived { get; set; } = false;
    
    public DateTime? ArchivedAt { get; set; }
    
    [StringLength(450)]
    public string? ArchivedBy { get; set; }
    
    public bool RequiresApproval { get; set; } = false; // For joining the room
    
    public bool IsEncrypted { get; set; } = false; // For sensitive discussions
    
    public DateTime? LastActivityAt { get; set; }
    
    public int MessageCount { get; set; } = 0;
    
    [StringLength(200)]
    public string? Topic { get; set; } // Current discussion topic
    
    [StringLength(450)]
    public string? TopicSetBy { get; set; }
    
    public DateTime? TopicSetAt { get; set; }
    
    // Navigation properties
    public ApplicationUser? CreatedByUser { get; set; }
    public ApplicationUser? ArchivedByUser { get; set; }
    public ApplicationUser? TopicSetByUser { get; set; }
    public Client? Client { get; set; }
    public List<ChatRoomParticipant> Participants { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
    public List<ChatRoomInvitation> Invitations { get; set; } = new();
}

public class ChatRoomParticipant
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ChatRoomId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public ChatRoomRole Role { get; set; } = ChatRoomRole.Member;
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LeftAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsMuted { get; set; } = false;
    
    public DateTime? MutedUntil { get; set; }
    
    public DateTime? LastSeenAt { get; set; }
    
    // Navigation properties
    public ChatRoom? ChatRoom { get; set; }
    public ApplicationUser? User { get; set; }
}

public class ChatMessage
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ChatRoomId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string SenderId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(2000)] // Increased from 1000 for longer messages
    public string Content { get; set; } = string.Empty;
    
    [Required]
    public ChatMessageType Type { get; set; } = ChatMessageType.Text;
    
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? EditedAt { get; set; }
    
    [StringLength(450)]
    public string? EditedBy { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    [StringLength(450)]
    public string? DeletedBy { get; set; }
    
    public int? ReplyToMessageId { get; set; }
    
    [StringLength(1000)]
    public string? Attachments { get; set; } // JSON array of attachment info
    
    [StringLength(500)]
    public string? Mentions { get; set; } // JSON array of mentioned user IDs
    
    // Enhanced features for tax system
    public bool IsPinned { get; set; } = false;
    
    public DateTime? PinnedAt { get; set; }
    
    [StringLength(450)]
    public string? PinnedBy { get; set; }
    
    public bool IsImportant { get; set; } = false; // For highlighting important messages
    
    public bool IsPrivate { get; set; } = false; // For moderator-only visibility
    
    public int? ThreadId { get; set; } // For threaded conversations
    
    public bool StartsThread { get; set; } = false;
    
    public int ThreadMessageCount { get; set; } = 0;
    
    public DateTime? LastThreadActivity { get; set; }
    
    [StringLength(100)]
    public string? MessageHash { get; set; } // For message integrity verification
    
    [StringLength(1000)]
    public string? Metadata { get; set; } // JSON for additional data (reactions, etc.)
    
    // Tax-specific fields
    public int? RelatedTaxFilingId { get; set; }
    
    public int? RelatedPaymentId { get; set; }
    
    public int? RelatedDocumentId { get; set; }
    
    [StringLength(50)]
    public string? TaxYear { get; set; }
    
    public TaxType? TaxType { get; set; }
    
    // Navigation properties
    public ChatRoom? ChatRoom { get; set; }
    public ApplicationUser? Sender { get; set; }
    public ApplicationUser? EditedByUser { get; set; }
    public ApplicationUser? DeletedByUser { get; set; }
    public ApplicationUser? PinnedByUser { get; set; }
    public ChatMessage? ReplyToMessage { get; set; }
    public ChatMessage? ThreadParent { get; set; }
    public List<ChatMessage> ThreadReplies { get; set; } = new();
    public List<ChatMessageReaction> Reactions { get; set; } = new();
    public List<ChatMessageRead> ReadReceipts { get; set; } = new();
    public TaxFiling? RelatedTaxFiling { get; set; }
    public Payment? RelatedPayment { get; set; }
    public Document? RelatedDocument { get; set; }
}

public class ChatRoomInvitation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ChatRoomId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string InvitedUserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(450)]
    public string InvitedBy { get; set; } = string.Empty;
    
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ExpiresAt { get; set; }
    
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    
    public DateTime? RespondedAt { get; set; }
    
    [StringLength(500)]
    public string? Message { get; set; }
    
    // Navigation properties
    public ChatRoom? ChatRoom { get; set; }
    public ApplicationUser? InvitedUser { get; set; }
    public ApplicationUser? InvitedByUser { get; set; }
}

public class ChatMessageReaction
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ChatMessageId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Reaction { get; set; } = string.Empty; // emoji or reaction type
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ChatMessage? ChatMessage { get; set; }
    public ApplicationUser? User { get; set; }
}

public class ChatMessageRead
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int ChatMessageId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;
    
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ChatMessage? ChatMessage { get; set; }
    public ApplicationUser? User { get; set; }
}

// Enums for Communication System
public enum ConversationType
{
    Support = 0,
    Consultation = 1,
    Notification = 2,
    Internal = 3,
    Emergency = 4,
    Feedback = 5
}

public enum ConversationStatus
{
    Active = 0,
    Pending = 1,
    Resolved = 2,
    Closed = 3,
    Escalated = 4,
    OnHold = 5
}

public enum ConversationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
    Critical = 4
}

public enum MessageType
{
    Text = 0,
    File = 1,
    Image = 2,
    Document = 3,
    System = 4,
    StatusUpdate = 5
}

public enum ParticipantRole
{
    Participant = 0,
    Moderator = 1,
    Admin = 2,
    Owner = 3
}

public enum NotificationType
{
    General = 0,
    TaxReminder = 1,
    PaymentConfirmation = 2,
    DocumentRequest = 3,
    ComplianceAlert = 4,
    SystemMaintenance = 5,
    ChatMessage = 6,
    ConversationUpdate = 7
}

public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Push = 2,
    InApp = 3
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Delivered = 3,
    Read = 4,
    Cancelled = 5
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum ChatRoomType
{
    Public = 0,
    Private = 1,
    DirectMessage = 2,
    Group = 3,
    Support = 4
}

public enum ChatRoomRole
{
    Member = 0,
    Moderator = 1,
    Admin = 2,
    Owner = 3
}

public enum ChatMessageType
{
    Text = 0,
    Image = 1,
    File = 2,
    System = 3,
    Join = 4,
    Leave = 5,
    Document = 6,
    TaxFiling = 7,
    Payment = 8,
    Reminder = 9,
    Alert = 10
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
    Cancelled = 4
}