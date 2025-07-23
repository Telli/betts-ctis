using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BettsTax.Core.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserContextService _userContext;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IActivityTimelineService _activityService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            ApplicationDbContext context,
            IUserContextService userContext,
            IAuditService auditService,
            INotificationService notificationService,
            IActivityTimelineService activityService,
            UserManager<ApplicationUser> userManager,
            ILogger<MessageService> logger)
        {
            _context = context;
            _userContext = userContext;
            _auditService = auditService;
            _notificationService = notificationService;
            _activityService = activityService;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<MessageDto>> SendMessageAsync(SendMessageDto dto)
        {
            try
            {
                var senderId = _userContext.GetCurrentUserId();
                var sender = await _userManager.FindByIdAsync(senderId);
                var recipient = await _userManager.FindByIdAsync(dto.RecipientId);

                if (sender == null || recipient == null)
                {
                    return Result.Failure<MessageDto>("Sender or recipient not found");
                }

                // Validate client association if provided
                if (dto.ClientId.HasValue)
                {
                    var clientExists = await _context.Clients.AnyAsync(c => c.ClientId == dto.ClientId.Value);
                    if (!clientExists)
                    {
                        return Result.Failure<MessageDto>("Client not found");
                    }
                }

                var message = new Message
                {
                    SenderId = senderId,
                    RecipientId = dto.RecipientId,
                    ClientId = dto.ClientId,
                    TaxFilingId = dto.TaxFilingId,
                    DocumentId = dto.DocumentId,
                    ParentMessageId = dto.ParentMessageId,
                    Subject = dto.Subject,
                    Body = dto.Body,
                    Priority = dto.Priority,
                    Category = dto.Category,
                    Status = MessageStatus.Sent,
                    HasAttachments = dto.AttachmentIds.Any()
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Add attachments
                if (dto.AttachmentIds.Any())
                {
                    foreach (var docId in dto.AttachmentIds)
                    {
                        var attachment = new MessageAttachment
                        {
                            MessageId = message.MessageId,
                            DocumentId = docId
                        };
                        _context.MessageAttachments.Add(attachment);
                    }
                    await _context.SaveChangesAsync();
                }

                // Create notification
                await _notificationService.CreateAsync(
                    dto.RecipientId,
                    $"New message from {sender.FirstName} {sender.LastName}: {dto.Subject}"
                );

                // Log activity
                if (dto.ClientId.HasValue)
                {
                    await _activityService.LogCommunicationActivityAsync(
                        dto.ClientId.Value,
                        ActivityType.MessageSent,
                        $"Message sent: {dto.Subject}",
                        $"From: {sender.FirstName} {sender.LastName} to {recipient.FirstName} {recipient.LastName}"
                    );
                }

                // Audit log
                await _auditService.LogAsync(
                    "Message",
                    "Send",
                    $"Sent message to {recipient.Email}",
                    message.MessageId.ToString()
                );

                return await GetMessageAsync(message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Result.Failure<MessageDto>("Error sending message");
            }
        }

        public async Task<Result<MessageDto>> GetMessageAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Client)
                    .Include(m => m.TaxFiling)
                    .Include(m => m.Document)
                    .Include(m => m.Replies)
                    .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);

                if (message == null)
                {
                    return Result.Failure<MessageDto>("Message not found");
                }

                var currentUserId = _userContext.GetCurrentUserId();
                if (message.SenderId != currentUserId && message.RecipientId != currentUserId)
                {
                    return Result.Failure<MessageDto>("Access denied");
                }

                var dto = await MapToDto(message);
                return Result.Success<MessageDto>(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message {MessageId}", messageId);
                return Result.Failure<MessageDto>("Error retrieving message");
            }
        }

        public async Task<Result<MessageDto>> ReplyToMessageAsync(int parentMessageId, MessageReplyDto dto)
        {
            try
            {
                var parentMessage = await _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefaultAsync(m => m.MessageId == parentMessageId);

                if (parentMessage == null)
                {
                    return Result.Failure<MessageDto>("Parent message not found");
                }

                var currentUserId = _userContext.GetCurrentUserId();
                
                // Determine recipient (reply to sender if current user is recipient, otherwise reply to recipient)
                var recipientId = parentMessage.RecipientId == currentUserId ? 
                    parentMessage.SenderId : parentMessage.RecipientId;

                var sendDto = new SendMessageDto
                {
                    RecipientId = recipientId,
                    ClientId = parentMessage.ClientId,
                    TaxFilingId = parentMessage.TaxFilingId,
                    DocumentId = parentMessage.DocumentId,
                    ParentMessageId = parentMessageId,
                    Subject = $"Re: {parentMessage.Subject}",
                    Body = dto.Body,
                    Priority = parentMessage.Priority,
                    Category = parentMessage.Category,
                    AttachmentIds = dto.AttachmentIds
                };

                return await SendMessageAsync(sendDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to message {ParentMessageId}", parentMessageId);
                return Result.Failure<MessageDto>("Error sending reply");
            }
        }

        public async Task<Result> MarkAsReadAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Result.Failure("Message not found");
                }

                var currentUserId = _userContext.GetCurrentUserId();
                if (message.RecipientId != currentUserId)
                {
                    return Result.Failure("Access denied");
                }

                if (message.Status == MessageStatus.Read)
                {
                    return Result.Success();
                }

                message.Status = MessageStatus.Read;
                message.ReadDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return Result.Failure("Error updating message status");
            }
        }

        public async Task<Result> MarkAsDeliveredAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Result.Failure("Message not found");
                }

                if (message.Status != MessageStatus.Sent)
                {
                    return Result.Success();
                }

                message.Status = MessageStatus.Delivered;
                message.DeliveredDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as delivered");
                return Result.Failure("Error updating message status");
            }
        }

        public async Task<Result> ToggleStarAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Result.Failure("Message not found");
                }

                var currentUserId = _userContext.GetCurrentUserId();
                if (message.SenderId != currentUserId && message.RecipientId != currentUserId)
                {
                    return Result.Failure("Access denied");
                }

                message.IsStarred = !message.IsStarred;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling star status");
                return Result.Failure("Error updating message");
            }
        }

        public async Task<Result> ArchiveMessageAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Result.Failure("Message not found");
                }

                var currentUserId = _userContext.GetCurrentUserId();
                if (message.SenderId != currentUserId && message.RecipientId != currentUserId)
                {
                    return Result.Failure("Access denied");
                }

                message.IsArchived = true;
                message.Status = MessageStatus.Archived;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving message");
                return Result.Failure("Error archiving message");
            }
        }

        public async Task<Result> DeleteMessageAsync(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return Result.Failure("Message not found");
                }

                var currentUserId = _userContext.GetCurrentUserId();
                if (message.SenderId != currentUserId && message.RecipientId != currentUserId)
                {
                    return Result.Failure("Access denied");
                }

                message.IsDeleted = true;
                message.Status = MessageStatus.Deleted;
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return Result.Failure("Error deleting message");
            }
        }

        public async Task<Result> BulkUpdateStatusAsync(List<int> messageIds, MessageStatus status)
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var messages = await _context.Messages
                    .Where(m => messageIds.Contains(m.MessageId) && 
                               (m.SenderId == currentUserId || m.RecipientId == currentUserId))
                    .ToListAsync();

                if (messages.Count != messageIds.Count)
                {
                    return Result.Failure("One or more messages not found or access denied");
                }

                foreach (var message in messages)
                {
                    message.Status = status;
                    if (status == MessageStatus.Read)
                    {
                        message.ReadDate = DateTime.UtcNow;
                    }
                    else if (status == MessageStatus.Archived)
                    {
                        message.IsArchived = true;
                    }
                    else if (status == MessageStatus.Deleted)
                    {
                        message.IsDeleted = true;
                    }
                }

                await _context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating message status");
                return Result.Failure("Error updating messages");
            }
        }

        public async Task<Result<MessageThreadDto>> GetMessageThreadAsync(int messageId)
        {
            try
            {
                var rootMessage = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Replies)
                        .ThenInclude(r => r.Sender)
                    .Include(m => m.Replies)
                        .ThenInclude(r => r.Recipient)
                    .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);

                if (rootMessage == null)
                {
                    return Result.Failure<MessageThreadDto>("Message not found");
                }

                // If this is a reply, get the root message
                if (rootMessage.ParentMessageId.HasValue)
                {
                    rootMessage = await GetRootMessage(rootMessage.ParentMessageId.Value);
                    if (rootMessage == null)
                    {
                        return Result.Failure<MessageThreadDto>("Root message not found");
                    }
                }

                var currentUserId = _userContext.GetCurrentUserId();
                var allMessages = GetAllMessagesInThread(rootMessage);
                
                var participants = allMessages
                    .SelectMany(m => new[] { m.SenderId, m.RecipientId })
                    .Distinct()
                    .ToList();

                var hasUnread = allMessages.Any(m => 
                    m.RecipientId == currentUserId && 
                    m.Status != MessageStatus.Read && 
                    m.Status != MessageStatus.Archived && 
                    m.Status != MessageStatus.Deleted);

                var thread = new MessageThreadDto
                {
                    RootMessage = await MapToDto(rootMessage),
                    TotalReplies = allMessages.Count - 1,
                    Participants = participants,
                    LastActivityDate = allMessages.Max(m => m.SentDate),
                    HasUnreadMessages = hasUnread
                };

                return Result.Success<MessageThreadDto>(thread);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message thread");
                return Result.Failure<MessageThreadDto>("Error retrieving thread");
            }
        }

        public async Task<Result<PagedResult<MessageThreadDto>>> GetThreadsAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                // Get all root messages (no parent) for the user
                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Replies)
                    .Where(m => !m.IsDeleted && 
                               m.ParentMessageId == null &&
                               (m.SenderId == userId || m.RecipientId == userId));

                var totalCount = await query.CountAsync();
                
                var rootMessages = await query
                    .OrderByDescending(m => m.SentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var threads = new List<MessageThreadDto>();
                foreach (var root in rootMessages)
                {
                    var threadResult = await GetMessageThreadAsync(root.MessageId);
                    if (threadResult.IsSuccess)
                    {
                        threads.Add(threadResult.Value);
                    }
                }

                return Result.Success<PagedResult<MessageThreadDto>>(new PagedResult<MessageThreadDto>
                {
                    Items = threads,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting threads");
                return Result.Failure<PagedResult<MessageThreadDto>>("Error retrieving threads");
            }
        }

        public async Task<Result<PagedResult<MessageDto>>> GetInboxAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Client)
                    .Include(m => m.TaxFiling)
                    .Where(m => m.RecipientId == userId && 
                               !m.IsDeleted && 
                               !m.IsArchived &&
                               m.Status != MessageStatus.Deleted &&
                               m.Status != MessageStatus.Archived);

                var totalCount = await query.CountAsync();
                
                var messages = await query
                    .OrderByDescending(m => m.SentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<PagedResult<MessageDto>>(new PagedResult<MessageDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbox");
                return Result.Failure<PagedResult<MessageDto>>("Error retrieving inbox");
            }
        }

        public async Task<Result<PagedResult<MessageDto>>> GetSentMessagesAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Messages
                    .Include(m => m.Recipient)
                    .Include(m => m.Client)
                    .Include(m => m.TaxFiling)
                    .Where(m => m.SenderId == userId && 
                               !m.IsDeleted &&
                               m.Status != MessageStatus.Deleted);

                var totalCount = await query.CountAsync();
                
                var messages = await query
                    .OrderByDescending(m => m.SentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<PagedResult<MessageDto>>(new PagedResult<MessageDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent messages");
                return Result.Failure<PagedResult<MessageDto>>("Error retrieving sent messages");
            }
        }

        public async Task<Result<PagedResult<MessageDto>>> GetArchivedMessagesAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Client)
                    .Where(m => (m.SenderId == userId || m.RecipientId == userId) && 
                               !m.IsDeleted && 
                               m.IsArchived);

                var totalCount = await query.CountAsync();
                
                var messages = await query
                    .OrderByDescending(m => m.SentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<PagedResult<MessageDto>>(new PagedResult<MessageDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived messages");
                return Result.Failure<PagedResult<MessageDto>>("Error retrieving archived messages");
            }
        }

        public async Task<Result<PagedResult<MessageDto>>> GetStarredMessagesAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Client)
                    .Where(m => (m.SenderId == userId || m.RecipientId == userId) && 
                               !m.IsDeleted && 
                               m.IsStarred);

                var totalCount = await query.CountAsync();
                
                var messages = await query
                    .OrderByDescending(m => m.SentDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<PagedResult<MessageDto>>(new PagedResult<MessageDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting starred messages");
                return Result.Failure<PagedResult<MessageDto>>("Error retrieving starred messages");
            }
        }

        public async Task<Result<PagedResult<MessageDto>>> SearchMessagesAsync(MessageSearchDto searchDto)
        {
            try
            {
                var currentUserId = _userContext.GetCurrentUserId();
                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Client)
                    .Include(m => m.TaxFiling)
                    .Where(m => (m.SenderId == currentUserId || m.RecipientId == currentUserId) && 
                               !m.IsDeleted);

                // Apply filters
                if (!string.IsNullOrEmpty(searchDto.SearchTerm))
                {
                    query = query.Where(m => 
                        m.Subject.Contains(searchDto.SearchTerm) ||
                        m.Body.Contains(searchDto.SearchTerm));
                }

                if (searchDto.Category.HasValue)
                    query = query.Where(m => m.Category == searchDto.Category.Value);

                if (searchDto.Priority.HasValue)
                    query = query.Where(m => m.Priority == searchDto.Priority.Value);

                if (searchDto.Status.HasValue)
                    query = query.Where(m => m.Status == searchDto.Status.Value);

                if (searchDto.ClientId.HasValue)
                    query = query.Where(m => m.ClientId == searchDto.ClientId.Value);

                if (searchDto.TaxFilingId.HasValue)
                    query = query.Where(m => m.TaxFilingId == searchDto.TaxFilingId.Value);

                if (searchDto.FromDate.HasValue)
                    query = query.Where(m => m.SentDate >= searchDto.FromDate.Value);

                if (searchDto.ToDate.HasValue)
                    query = query.Where(m => m.SentDate <= searchDto.ToDate.Value);

                if (searchDto.IsStarred.HasValue)
                    query = query.Where(m => m.IsStarred == searchDto.IsStarred.Value);

                if (searchDto.HasAttachments.HasValue)
                    query = query.Where(m => m.HasAttachments == searchDto.HasAttachments.Value);

                if (!string.IsNullOrEmpty(searchDto.SenderId))
                    query = query.Where(m => m.SenderId == searchDto.SenderId);

                if (!string.IsNullOrEmpty(searchDto.RecipientId))
                    query = query.Where(m => m.RecipientId == searchDto.RecipientId);

                var totalCount = await query.CountAsync();
                
                var messages = await query
                    .OrderByDescending(m => m.SentDate)
                    .Skip((searchDto.Page - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<PagedResult<MessageDto>>(new PagedResult<MessageDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = searchDto.Page,
                    PageSize = searchDto.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages");
                return Result.Failure<PagedResult<MessageDto>>("Error searching messages");
            }
        }

        public async Task<Result<List<MessageDto>>> GetClientMessagesAsync(int clientId, string userId)
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Where(m => m.ClientId == clientId && 
                               (m.SenderId == userId || m.RecipientId == userId) &&
                               !m.IsDeleted)
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<List<MessageDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client messages");
                return Result.Failure<List<MessageDto>>("Error retrieving client messages");
            }
        }

        public async Task<Result<List<MessageDto>>> GetTaxFilingMessagesAsync(int taxFilingId, string userId)
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Where(m => m.TaxFilingId == taxFilingId && 
                               (m.SenderId == userId || m.RecipientId == userId) &&
                               !m.IsDeleted)
                    .OrderByDescending(m => m.SentDate)
                    .ToListAsync();

                var dtos = new List<MessageDto>();
                foreach (var message in messages)
                {
                    dtos.Add(await MapToDto(message));
                }

                return Result.Success<List<MessageDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tax filing messages");
                return Result.Failure<List<MessageDto>>("Error retrieving tax filing messages");
            }
        }

        public async Task<Result<List<MessageFolderDto>>> GetFolderCountsAsync(string userId)
        {
            try
            {
                var folders = new List<MessageFolderDto>();

                // Inbox
                var inboxQuery = _context.Messages
                    .Where(m => m.RecipientId == userId && 
                               !m.IsDeleted && 
                               !m.IsArchived &&
                               m.Status != MessageStatus.Deleted &&
                               m.Status != MessageStatus.Archived);

                folders.Add(new MessageFolderDto
                {
                    FolderName = "Inbox",
                    UnreadCount = await inboxQuery.CountAsync(m => m.Status != MessageStatus.Read),
                    TotalCount = await inboxQuery.CountAsync()
                });

                // Sent
                var sentQuery = _context.Messages
                    .Where(m => m.SenderId == userId && 
                               !m.IsDeleted &&
                               m.Status != MessageStatus.Deleted);

                folders.Add(new MessageFolderDto
                {
                    FolderName = "Sent",
                    UnreadCount = 0,
                    TotalCount = await sentQuery.CountAsync()
                });

                // Starred
                var starredQuery = _context.Messages
                    .Where(m => (m.SenderId == userId || m.RecipientId == userId) && 
                               !m.IsDeleted && 
                               m.IsStarred);

                folders.Add(new MessageFolderDto
                {
                    FolderName = "Starred",
                    UnreadCount = await starredQuery.CountAsync(m => m.RecipientId == userId && m.Status != MessageStatus.Read),
                    TotalCount = await starredQuery.CountAsync()
                });

                // Archived
                var archivedQuery = _context.Messages
                    .Where(m => (m.SenderId == userId || m.RecipientId == userId) && 
                               !m.IsDeleted && 
                               m.IsArchived);

                folders.Add(new MessageFolderDto
                {
                    FolderName = "Archived",
                    UnreadCount = 0,
                    TotalCount = await archivedQuery.CountAsync()
                });

                return Result.Success<List<MessageFolderDto>>(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder counts");
                return Result.Failure<List<MessageFolderDto>>("Error retrieving folder counts");
            }
        }

        public async Task<Result<int>> GetUnreadCountAsync(string userId)
        {
            try
            {
                var count = await _context.Messages
                    .CountAsync(m => m.RecipientId == userId && 
                                    !m.IsDeleted && 
                                    !m.IsArchived &&
                                    m.Status != MessageStatus.Read &&
                                    m.Status != MessageStatus.Deleted &&
                                    m.Status != MessageStatus.Archived);

                return Result.Success<int>(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Result.Failure<int>("Error retrieving unread count");
            }
        }

        public async Task<Result<List<MessageTemplateDto>>> GetMessageTemplatesAsync(MessageCategory? category = null)
        {
            try
            {
                var query = _context.MessageTemplates.Where(mt => mt.IsActive);

                if (category.HasValue)
                    query = query.Where(mt => mt.Category == category.Value);

                var templates = await query
                    .OrderBy(mt => mt.Category)
                    .ThenBy(mt => mt.Name)
                    .ToListAsync();

                var dtos = templates.Select(MapToDto).ToList();
                return Result.Success<List<MessageTemplateDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message templates");
                return Result.Failure<List<MessageTemplateDto>>("Error retrieving templates");
            }
        }

        public async Task<Result<MessageTemplateDto>> GetMessageTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.MessageTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure<MessageTemplateDto>("Template not found");
                }

                return Result.Success<MessageTemplateDto>(MapToDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message template");
                return Result.Failure<MessageTemplateDto>("Error retrieving template");
            }
        }

        public async Task<Result<MessageTemplateDto>> CreateMessageTemplateAsync(MessageTemplateDto dto)
        {
            try
            {
                var existing = await _context.MessageTemplates
                    .FirstOrDefaultAsync(mt => mt.TemplateCode == dto.TemplateCode);

                if (existing != null)
                {
                    return Result.Failure<MessageTemplateDto>("Template code already exists");
                }

                var template = new MessageTemplate
                {
                    TemplateCode = dto.TemplateCode,
                    Name = dto.Name,
                    Description = dto.Description,
                    Subject = dto.Subject,
                    Body = dto.Body,
                    Category = dto.Category,
                    AvailableVariables = JsonSerializer.Serialize(dto.AvailableVariables)
                };

                _context.MessageTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "MessageTemplate",
                    "Create",
                    $"Created template: {template.Name}",
                    template.MessageTemplateId.ToString()
                );

                return Result.Success<MessageTemplateDto>(MapToDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message template");
                return Result.Failure<MessageTemplateDto>("Error creating template");
            }
        }

        public async Task<Result<MessageTemplateDto>> UpdateMessageTemplateAsync(int templateId, MessageTemplateDto dto)
        {
            try
            {
                var template = await _context.MessageTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure<MessageTemplateDto>("Template not found");
                }

                template.Name = dto.Name;
                template.Description = dto.Description;
                template.Subject = dto.Subject;
                template.Body = dto.Body;
                template.Category = dto.Category;
                template.AvailableVariables = JsonSerializer.Serialize(dto.AvailableVariables);
                template.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "MessageTemplate",
                    "Update",
                    $"Updated template: {template.Name}",
                    template.MessageTemplateId.ToString()
                );

                return Result.Success<MessageTemplateDto>(MapToDto(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message template");
                return Result.Failure<MessageTemplateDto>("Error updating template");
            }
        }

        public async Task<Result> DeleteMessageTemplateAsync(int templateId)
        {
            try
            {
                var template = await _context.MessageTemplates.FindAsync(templateId);
                if (template == null)
                {
                    return Result.Failure("Template not found");
                }

                template.IsActive = false;
                template.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "MessageTemplate",
                    "Delete",
                    $"Deactivated template: {template.Name}",
                    template.MessageTemplateId.ToString()
                );

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message template");
                return Result.Failure("Error deleting template");
            }
        }

        public async Task<Result<SendMessageDto>> ApplyMessageTemplateAsync(ApplyMessageTemplateDto dto)
        {
            try
            {
                var template = await _context.MessageTemplates.FindAsync(dto.TemplateId);
                if (template == null)
                {
                    return Result.Failure<SendMessageDto>("Template not found");
                }

                var subject = ReplaceVariables(template.Subject, dto.Variables);
                var body = ReplaceVariables(template.Body, dto.Variables);

                var sendDto = new SendMessageDto
                {
                    RecipientId = dto.RecipientId,
                    ClientId = dto.ClientId,
                    TaxFilingId = dto.TaxFilingId,
                    Subject = subject,
                    Body = body,
                    Category = template.Category
                };

                return Result.Success<SendMessageDto>(sendDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying message template");
                return Result.Failure<SendMessageDto>("Error applying template");
            }
        }

        public async Task<Result> SendSystemMessageAsync(string recipientId, string subject, string body, 
            MessageCategory category = MessageCategory.General, MessagePriority priority = MessagePriority.Normal)
        {
            try
            {
                // Get or create system user
                var systemUser = await _userManager.FindByEmailAsync("system@bettsfirmsl.com");
                if (systemUser == null)
                {
                    systemUser = new ApplicationUser
                    {
                        UserName = "system@bettsfirmsl.com",
                        Email = "system@bettsfirmsl.com",
                        FirstName = "System",
                        LastName = "Notification",
                        EmailConfirmed = true
                    };
                    await _userManager.CreateAsync(systemUser);
                }

                var message = new Message
                {
                    SenderId = systemUser.Id,
                    RecipientId = recipientId,
                    Subject = subject,
                    Body = body,
                    Category = category,
                    Priority = priority,
                    IsSystemMessage = true,
                    SystemMessageType = category.ToString()
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Create notification
                await _notificationService.CreateAsync(recipientId, subject);

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending system message");
                return Result.Failure("Error sending system message");
            }
        }

        public async Task<Result> SendDocumentRequestMessageAsync(int clientId, List<string> documentNames, DateTime dueDate)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client?.User == null)
                {
                    return Result.Failure("Client not found");
                }

                var subject = "Documents Required for Tax Filing";
                var body = $@"Dear {client.User.FirstName} {client.User.LastName},

The following documents are required for your tax filing:

{string.Join("\n", documentNames.Select(d => $"• {d}"))}

Please upload these documents by {dueDate:MMMM dd, yyyy}.

If you have any questions, please don't hesitate to contact us.

Best regards,
The Betts Firm Team";

                return await SendSystemMessageAsync(
                    client.UserId,
                    subject,
                    body,
                    MessageCategory.DocumentRequest,
                    MessagePriority.High
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document request message");
                return Result.Failure("Error sending document request");
            }
        }

        public async Task<Result> SendDocumentRejectionMessageAsync(int documentId, string rejectionReason)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Client)
                        .ThenInclude(c => c!.User)
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document?.Client?.User == null)
                {
                    return Result.Failure("Document or client not found");
                }

                var subject = $"Document Rejected: {document.OriginalFileName}";
                var body = $@"Dear {document.Client.User.FirstName} {document.Client.User.LastName},

Your document '{document.OriginalFileName}' has been reviewed and rejected.

Reason: {rejectionReason}

Please upload a corrected version of this document as soon as possible.

Best regards,
The Betts Firm Team";

                var sendDto = new SendMessageDto
                {
                    RecipientId = document.Client.UserId,
                    ClientId = document.ClientId,
                    DocumentId = documentId,
                    Subject = subject,
                    Body = body,
                    Category = MessageCategory.DocumentReview,
                    Priority = MessagePriority.High
                };

                var result = await SendMessageAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending document rejection message");
                return Result.Failure("Error sending rejection message");
            }
        }

        public async Task<Result> SendDeadlineReminderAsync(int clientId, string deadlineDescription, DateTime dueDate)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client?.User == null)
                {
                    return Result.Failure("Client not found");
                }

                var daysUntilDue = (dueDate - DateTime.UtcNow).Days;
                var urgency = daysUntilDue <= 3 ? "URGENT: " : "";

                var subject = $"{urgency}Deadline Reminder: {deadlineDescription}";
                var body = $@"Dear {client.User.FirstName} {client.User.LastName},

This is a reminder that you have an upcoming deadline:

{deadlineDescription}
Due Date: {dueDate:MMMM dd, yyyy}
Days Remaining: {daysUntilDue}

Please ensure all requirements are met before the deadline.

Best regards,
The Betts Firm Team";

                return await SendSystemMessageAsync(
                    client.UserId,
                    subject,
                    body,
                    MessageCategory.Deadline,
                    daysUntilDue <= 3 ? MessagePriority.Urgent : MessagePriority.High
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending deadline reminder");
                return Result.Failure("Error sending reminder");
            }
        }

        public async Task<Result> SendPaymentConfirmationAsync(int paymentId)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.TaxFiling)
                        .ThenInclude(tf => tf!.Client)
                            .ThenInclude(c => c!.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment?.TaxFiling?.Client?.User == null)
                {
                    return Result.Failure("Payment or client not found");
                }

                var subject = $"Payment Confirmation - {payment.PaymentReference}";
                var body = $@"Dear {payment.TaxFiling.Client.User.FirstName} {payment.TaxFiling.Client.User.LastName},

Your payment has been successfully processed.

Payment Details:
Reference: {payment.PaymentReference}
Amount: SLE {payment.Amount:N2}
Method: {payment.Method}
Date: {payment.PaymentDate:MMMM dd, yyyy}
Tax Type: {payment.TaxFiling.TaxType}

Thank you for your payment.

Best regards,
The Betts Firm Team";

                var sendDto = new SendMessageDto
                {
                    RecipientId = payment.TaxFiling.Client.UserId,
                    ClientId = payment.TaxFiling.ClientId,
                    TaxFilingId = payment.TaxFilingId,
                    Subject = subject,
                    Body = body,
                    Category = MessageCategory.Payment,
                    Priority = MessagePriority.Normal
                };

                var result = await SendMessageAsync(sendDto);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment confirmation");
                return Result.Failure("Error sending confirmation");
            }
        }

        public async Task<Result<List<MessageNotificationDto>>> GetUnreadNotificationsAsync(string userId, int limit = 10)
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.RecipientId == userId && 
                               !m.IsDeleted && 
                               !m.IsArchived &&
                               m.Status != MessageStatus.Read &&
                               m.Status != MessageStatus.Deleted &&
                               m.Status != MessageStatus.Archived)
                    .OrderByDescending(m => m.SentDate)
                    .Take(limit)
                    .ToListAsync();

                var notifications = messages.Select(m => new MessageNotificationDto
                {
                    MessageId = m.MessageId,
                    Subject = m.Subject,
                    SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "System",
                    Preview = m.Body.Length > 100 ? m.Body.Substring(0, 100) + "..." : m.Body,
                    Priority = m.Priority,
                    SentDate = m.SentDate,
                    IsRead = false
                }).ToList();

                return Result.Success<List<MessageNotificationDto>>(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notifications");
                return Result.Failure<List<MessageNotificationDto>>("Error retrieving notifications");
            }
        }

        public async Task<Result<Dictionary<string, object>>> GetMessageStatisticsAsync(string userId, DateTime? fromDate = null)
        {
            try
            {
                var query = _context.Messages
                    .Where(m => (m.SenderId == userId || m.RecipientId == userId) && !m.IsDeleted);

                if (fromDate.HasValue)
                    query = query.Where(m => m.SentDate >= fromDate.Value);

                var stats = new Dictionary<string, object>
                {
                    ["totalMessages"] = await query.CountAsync(),
                    ["sentMessages"] = await query.CountAsync(m => m.SenderId == userId),
                    ["receivedMessages"] = await query.CountAsync(m => m.RecipientId == userId),
                    ["unreadMessages"] = await query.CountAsync(m => m.RecipientId == userId && m.Status != MessageStatus.Read),
                    ["starredMessages"] = await query.CountAsync(m => m.IsStarred),
                    ["archivedMessages"] = await query.CountAsync(m => m.IsArchived),
                    
                    ["messagesByCategory"] = await query
                        .GroupBy(m => m.Category)
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.Category.ToString(), x => (object)x.Count),
                    
                    ["messagesByPriority"] = await query
                        .GroupBy(m => m.Priority)
                        .Select(g => new { Priority = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.Priority.ToString(), x => (object)x.Count),
                    
                    ["averageResponseTime"] = await CalculateAverageResponseTimeAsync(userId, fromDate)
                };

                return Result.Success<Dictionary<string, object>>(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message statistics");
                return Result.Failure<Dictionary<string, object>>("Error retrieving statistics");
            }
        }

        // Helper methods
        private async Task<MessageDto> MapToDto(Message message)
        {
            var dto = new MessageDto
            {
                MessageId = message.MessageId,
                ParentMessageId = message.ParentMessageId,
                SenderId = message.SenderId,
                SenderName = message.Sender != null ? $"{message.Sender.FirstName} {message.Sender.LastName}" : "System",
                SenderEmail = message.Sender?.Email ?? "system@bettsfirmsl.com",
                SenderRole = message.Sender != null ? 
                    (await _userManager.GetRolesAsync(message.Sender)).FirstOrDefault() ?? "User" : "System",
                RecipientId = message.RecipientId,
                RecipientName = message.Recipient != null ? $"{message.Recipient.FirstName} {message.Recipient.LastName}" : "Unknown",
                RecipientEmail = message.Recipient?.Email ?? "",
                RecipientRole = message.Recipient != null ? 
                    (await _userManager.GetRolesAsync(message.Recipient)).FirstOrDefault() ?? "User" : "Unknown",
                ClientId = message.ClientId,
                ClientName = message.Client?.BusinessName,
                ClientNumber = message.Client?.ClientNumber,
                TaxFilingId = message.TaxFilingId,
                TaxFilingReference = message.TaxFiling?.FilingReference,
                DocumentId = message.DocumentId,
                DocumentName = message.Document?.OriginalFileName,
                Subject = message.Subject,
                Body = message.Body,
                Status = message.Status,
                Priority = message.Priority,
                Category = message.Category,
                SentDate = message.SentDate,
                DeliveredDate = message.DeliveredDate,
                ReadDate = message.ReadDate,
                IsStarred = message.IsStarred,
                IsArchived = message.IsArchived,
                HasAttachments = message.HasAttachments,
                IsSystemMessage = message.IsSystemMessage,
                ReplyCount = message.Replies?.Count ?? 0
            };

            // Load attachments if any
            if (message.HasAttachments)
            {
                var attachments = await _context.MessageAttachments
                    .Include(ma => ma.Document)
                    .Where(ma => ma.MessageId == message.MessageId)
                    .Select(ma => new DocumentDto
                    {
                        DocumentId = ma.Document.DocumentId,
                        OriginalFileName = ma.Document.OriginalFileName,
                        ContentType = ma.Document.ContentType,
                        Size = ma.Document.Size,
                        UploadedAt = ma.Document.UploadedAt
                    })
                    .ToListAsync();

                dto.Attachments = attachments;
            }

            // Load replies summary if this is a root message
            if (!message.ParentMessageId.HasValue && message.Replies?.Any() == true)
            {
                dto.Replies = message.Replies
                    .OrderBy(r => r.SentDate)
                    .Take(3) // Show first 3 replies
                    .Select(r => new MessageDto
                    {
                        MessageId = r.MessageId,
                        SenderId = r.SenderId,
                        SenderName = r.Sender != null ? $"{r.Sender.FirstName} {r.Sender.LastName}" : "System",
                        Body = r.Body.Length > 100 ? r.Body.Substring(0, 100) + "..." : r.Body,
                        SentDate = r.SentDate,
                        Status = r.Status
                    })
                    .ToList();
            }

            return dto;
        }

        private MessageTemplateDto MapToDto(MessageTemplate template)
        {
            var dto = new MessageTemplateDto
            {
                MessageTemplateId = template.MessageTemplateId,
                TemplateCode = template.TemplateCode,
                Name = template.Name,
                Description = template.Description,
                Subject = template.Subject,
                Body = template.Body,
                Category = template.Category,
                IsActive = template.IsActive
            };

            if (!string.IsNullOrEmpty(template.AvailableVariables))
            {
                try
                {
                    dto.AvailableVariables = JsonSerializer.Deserialize<Dictionary<string, string>>(template.AvailableVariables) 
                        ?? new Dictionary<string, string>();
                }
                catch
                {
                    dto.AvailableVariables = new Dictionary<string, string>();
                }
            }

            return dto;
        }

        private async Task<Message?> GetRootMessage(int messageId)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Replies)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);

            if (message == null) return null;

            if (message.ParentMessageId.HasValue)
            {
                return await GetRootMessage(message.ParentMessageId.Value);
            }

            return message;
        }

        private List<Message> GetAllMessagesInThread(Message rootMessage)
        {
            var allMessages = new List<Message> { rootMessage };
            
            if (rootMessage.Replies?.Any() == true)
            {
                foreach (var reply in rootMessage.Replies)
                {
                    allMessages.AddRange(GetAllMessagesInThread(reply));
                }
            }

            return allMessages;
        }

        private string ReplaceVariables(string template, Dictionary<string, string> variables)
        {
            var result = template;
            
            foreach (var variable in variables)
            {
                result = Regex.Replace(result, $@"\{{{variable.Key}\}}", variable.Value, RegexOptions.IgnoreCase);
            }

            return result;
        }

        private async Task<TimeSpan> CalculateAverageResponseTimeAsync(string userId, DateTime? fromDate)
        {
            try
            {
                var query = _context.Messages
                    .Where(m => m.RecipientId == userId && 
                               m.ParentMessageId.HasValue &&
                               !m.IsDeleted);

                if (fromDate.HasValue)
                    query = query.Where(m => m.SentDate >= fromDate.Value);

                var replies = await query
                    .Include(m => m.ParentMessage)
                    .ToListAsync();

                if (!replies.Any()) return TimeSpan.Zero;

                var responseTimes = replies
                    .Where(r => r.ParentMessage != null)
                    .Select(r => r.SentDate - r.ParentMessage!.SentDate)
                    .ToList();

                if (!responseTimes.Any()) return TimeSpan.Zero;

                var averageTicks = responseTimes.Average(t => t.Ticks);
                return new TimeSpan((long)averageTicks);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }
    }
}