using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;

namespace BettsTax.Core.Services
{
    public interface IMessageService
    {
        // Message operations
        Task<Result<MessageDto>> SendMessageAsync(SendMessageDto dto);
        Task<Result<MessageDto>> GetMessageAsync(int messageId);
        Task<Result<MessageDto>> ReplyToMessageAsync(int parentMessageId, MessageReplyDto dto);
        Task<Result> MarkAsReadAsync(int messageId);
        Task<Result> MarkAsDeliveredAsync(int messageId);
        Task<Result> ToggleStarAsync(int messageId);
        Task<Result> ArchiveMessageAsync(int messageId);
        Task<Result> DeleteMessageAsync(int messageId);
        Task<Result> BulkUpdateStatusAsync(List<int> messageIds, MessageStatus status);
        
        // Thread operations
        Task<Result<MessageThreadDto>> GetMessageThreadAsync(int messageId);
        Task<Result<PagedResult<MessageThreadDto>>> GetThreadsAsync(string userId, int page = 1, int pageSize = 20);
        
        // Inbox operations
        Task<Result<PagedResult<MessageDto>>> GetInboxAsync(string userId, int page = 1, int pageSize = 20);
        Task<Result<PagedResult<MessageDto>>> GetSentMessagesAsync(string userId, int page = 1, int pageSize = 20);
        Task<Result<PagedResult<MessageDto>>> GetArchivedMessagesAsync(string userId, int page = 1, int pageSize = 20);
        Task<Result<PagedResult<MessageDto>>> GetStarredMessagesAsync(string userId, int page = 1, int pageSize = 20);
        
        // Search and filter
        Task<Result<PagedResult<MessageDto>>> SearchMessagesAsync(MessageSearchDto searchDto);
        Task<Result<List<MessageDto>>> GetClientMessagesAsync(int clientId, string userId);
        Task<Result<List<MessageDto>>> GetTaxFilingMessagesAsync(int taxFilingId, string userId);
        
        // Folder counts
        Task<Result<List<MessageFolderDto>>> GetFolderCountsAsync(string userId);
        Task<Result<int>> GetUnreadCountAsync(string userId);
        
        // Template operations
        Task<Result<List<MessageTemplateDto>>> GetMessageTemplatesAsync(MessageCategory? category = null);
        Task<Result<MessageTemplateDto>> GetMessageTemplateAsync(int templateId);
        Task<Result<MessageTemplateDto>> CreateMessageTemplateAsync(MessageTemplateDto dto);
        Task<Result<MessageTemplateDto>> UpdateMessageTemplateAsync(int templateId, MessageTemplateDto dto);
        Task<Result> DeleteMessageTemplateAsync(int templateId);
        Task<Result<SendMessageDto>> ApplyMessageTemplateAsync(ApplyMessageTemplateDto dto);
        
        // System messages
        Task<Result> SendSystemMessageAsync(string recipientId, string subject, string body, 
            MessageCategory category = MessageCategory.General, MessagePriority priority = MessagePriority.Normal);
        Task<Result> SendDocumentRequestMessageAsync(int clientId, List<string> documentNames, DateTime dueDate);
        Task<Result> SendDocumentRejectionMessageAsync(int documentId, string rejectionReason);
        Task<Result> SendDeadlineReminderAsync(int clientId, string deadlineDescription, DateTime dueDate);
        Task<Result> SendPaymentConfirmationAsync(int paymentId);
        
        // Notifications
        Task<Result<List<MessageNotificationDto>>> GetUnreadNotificationsAsync(string userId, int limit = 10);
        
        // Analytics
        Task<Result<Dictionary<string, object>>> GetMessageStatisticsAsync(string userId, DateTime? fromDate = null);
    }
}