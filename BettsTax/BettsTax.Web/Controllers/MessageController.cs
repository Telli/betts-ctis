using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Models = BettsTax.Data.Models;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IUserContextService _userContext;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            IMessageService messageService, 
            IUserContextService userContext,
            ApplicationDbContext dbContext,
            ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _userContext = userContext;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var result = await _messageService.SendMessageAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("{messageId}")]
        public async Task<IActionResult> GetMessage(int messageId)
        {
            var result = await _messageService.GetMessageAsync(messageId);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("{messageId}/reply")]
        public async Task<IActionResult> ReplyToMessage(int messageId, [FromBody] MessageReplyDto dto)
        {
            var result = await _messageService.ReplyToMessageAsync(messageId, dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPut("{messageId}/read")]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            var result = await _messageService.MarkAsReadAsync(messageId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPut("{messageId}/star")]
        public async Task<IActionResult> ToggleStar(int messageId)
        {
            var result = await _messageService.ToggleStarAsync(messageId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPut("{messageId}/archive")]
        public async Task<IActionResult> ArchiveMessage(int messageId)
        {
            var result = await _messageService.ArchiveMessageAsync(messageId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var result = await _messageService.DeleteMessageAsync(messageId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPut("bulk-update")]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusDto dto)
        {
            var result = await _messageService.BulkUpdateStatusAsync(dto.MessageIds, dto.Status);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpGet("thread/{messageId}")]
        public async Task<IActionResult> GetMessageThread(int messageId)
        {
            var result = await _messageService.GetMessageThreadAsync(messageId);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("threads")]
        public async Task<IActionResult> GetThreads(int page = 1, int pageSize = 20)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetThreadsAsync(userId, page, pageSize);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("inbox")]
        public async Task<IActionResult> GetInbox(int page = 1, int pageSize = 20)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetInboxAsync(userId, page, pageSize);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("sent")]
        public async Task<IActionResult> GetSentMessages(int page = 1, int pageSize = 20)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetSentMessagesAsync(userId, page, pageSize);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("archived")]
        public async Task<IActionResult> GetArchivedMessages(int page = 1, int pageSize = 20)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetArchivedMessagesAsync(userId, page, pageSize);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("starred")]
        public async Task<IActionResult> GetStarredMessages(int page = 1, int pageSize = 20)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetStarredMessagesAsync(userId, page, pageSize);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchMessages([FromBody] MessageSearchDto searchDto)
        {
            var result = await _messageService.SearchMessagesAsync(searchDto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetClientMessages(int clientId)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetClientMessagesAsync(clientId, userId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("tax-filing/{taxFilingId}")]
        public async Task<IActionResult> GetTaxFilingMessages(int taxFilingId)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetTaxFilingMessagesAsync(taxFilingId, userId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("folders")]
        public async Task<IActionResult> GetFolderCounts()
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetFolderCountsAsync(userId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetUnreadCountAsync(userId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { count = result.Value });
        }

        [HttpGet("templates")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetMessageTemplates(MessageCategory? category = null)
        {
            var result = await _messageService.GetMessageTemplatesAsync(category);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("templates/{templateId}")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> GetMessageTemplate(int templateId)
        {
            var result = await _messageService.GetMessageTemplateAsync(templateId);
            if (!result.IsSuccess)
                return NotFound(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpPost("templates")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateMessageTemplate([FromBody] MessageTemplateDto dto)
        {
            var result = await _messageService.CreateMessageTemplateAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return CreatedAtAction(nameof(GetMessageTemplate), new { templateId = result.Value.MessageTemplateId }, result.Value);
        }

        [HttpPut("templates/{templateId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateMessageTemplate(int templateId, [FromBody] MessageTemplateDto dto)
        {
            var result = await _messageService.UpdateMessageTemplateAsync(templateId, dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpDelete("templates/{templateId}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> DeleteMessageTemplate(int templateId)
        {
            var result = await _messageService.DeleteMessageTemplateAsync(templateId);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok();
        }

        [HttpPost("templates/apply")]
        [Authorize(Roles = "Admin,SystemAdmin,Associate")]
        public async Task<IActionResult> ApplyMessageTemplate([FromBody] ApplyMessageTemplateDto dto)
        {
            var result = await _messageService.ApplyMessageTemplateAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetUnreadNotifications(int limit = 10)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetUnreadNotificationsAsync(userId, limit);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetMessageStatistics(DateTime? fromDate = null)
        {
            var userId = _userContext.GetCurrentUserId();
            var result = await _messageService.GetMessageStatisticsAsync(userId, fromDate);
            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Value);
        }

        // CONVERSATION MANAGEMENT ENDPOINTS

        /// <summary>
        /// Get conversations with optional filtering
        /// </summary>
        [HttpGet("conversations")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<IActionResult> GetConversations(
            [FromQuery] string? status = null,
            [FromQuery] string? assignedTo = null)
        {
            try
            {
                var query = _dbContext.Conversations
                    .Include(c => c.Client)
                    .Include(c => c.AssignedToUser)
                    .Include(c => c.Messages)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ConversationStatus>(status, out var statusEnum))
                {
                    query = query.Where(c => c.Status == statusEnum);
                }

                if (!string.IsNullOrEmpty(assignedTo))
                {
                    query = query.Where(c => c.AssignedTo == assignedTo);
                }

                var conversations = await query
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .Select(c => new
                    {
                        id = c.Id,
                        ClientId = c.ClientId,
                        ClientName = c.Client != null ? c.Client.CompanyName : null,
                        c.Subject,
                        Status = c.Status.ToString(),
                        AssignedTo = c.AssignedTo,
                        AssignedToName = c.AssignedToUser != null 
                            ? c.AssignedToUser.FirstName + " " + c.AssignedToUser.LastName 
                            : null,
                        UnreadCount = c.UnreadCount,
                        LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = conversations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get messages for a conversation
        /// </summary>
        [HttpGet("conversations/{conversationId}/messages")]
        public async Task<IActionResult> GetConversationMessages(int conversationId)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId();
                var userRole = _userContext.GetCurrentUserRole();

                // Verify conversation exists and user has access
                var conversation = await _dbContext.Conversations
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { success = false, message = "Conversation not found" });
                }

                // Authorization: Clients can only access their own conversations
                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || conversation.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only access your own conversations");
                    }
                }

                // Get messages, filtering internal notes for clients
                var query = _dbContext.ConversationMessages
                    .Include(m => m.Sender)
                    .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                    .AsQueryable();

                // Filter internal notes for client users
                if (userRole == "Client")
                {
                    query = query.Where(m => !m.IsInternal);
                }

                var messages = await query
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        id = m.Id,
                        ConversationId = m.ConversationId,
                        SenderId = m.SenderId,
                        SenderName = m.Sender != null ? (m.Sender.FirstName + " " + m.Sender.LastName) : "Unknown",
                        Body = m.Content,
                        IsInternal = m.IsInternal,
                        SentAt = m.SentAt
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation messages");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Send message or internal note
        /// </summary>
        [HttpPost("conversations/{conversationId}/messages")]
        public async Task<IActionResult> SendConversationMessage(int conversationId, [FromBody] SendConversationMessageDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Body))
                {
                    return BadRequest(new { success = false, message = "Message body is required" });
                }

                var userId = _userContext.GetCurrentUserId();
                var userRole = _userContext.GetCurrentUserRole();

                // Verify conversation exists
                var conversation = await _dbContext.Conversations
                    .Include(c => c.Client)
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { success = false, message = "Conversation not found" });
                }

                // Authorization: Clients cannot send internal notes
                if (userRole == "Client" && dto.IsInternal)
                {
                    return Forbid("Clients cannot send internal notes");
                }

                // Authorization: Clients can only send messages to their own conversations
                if (userRole == "Client")
                {
                    var clientId = User.FindFirst("ClientId")?.Value;
                    if (clientId == null || conversation.ClientId != int.Parse(clientId))
                    {
                        return Forbid("You can only send messages to your own conversations");
                    }
                }

                // Create message using Models.Message (from CommunicationModels.cs)
                var message = new Models.Message
                {
                    ConversationId = conversationId,
                    SenderId = userId,
                    Content = dto.Body,
                    Subject = conversation.Subject,
                    IsInternal = dto.IsInternal,
                    SentAt = DateTime.UtcNow,
                    Type = Models.MessageType.Text
                };

                _dbContext.ConversationMessages.Add(message);

                // Update conversation last message date
                conversation.LastMessageAt = DateTime.UtcNow;
                conversation.UnreadCount++;
                if (conversation.Messages != null)
                {
                    conversation.MessageCount = conversation.Messages.Count + 1;
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Message sent in conversation {ConversationId} by {UserId}, Internal: {IsInternal}", 
                    conversationId, userId, dto.IsInternal);

                return Ok(new { success = true, message = "Message sent successfully", data = new { message.Id } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending conversation message");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Assign conversation to staff member
        /// </summary>
        [HttpPost("conversations/{conversationId}/assign")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<IActionResult> AssignConversation(int conversationId, [FromBody] AssignConversationDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.UserId) || dto.UserId == "unassigned")
                {
                    dto = dto with { UserId = null! };
                }

                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { success = false, message = "Conversation not found" });
                }

                // Verify user exists and is staff/admin if assigning
                if (!string.IsNullOrEmpty(dto.UserId) && dto.UserId != "unassigned")
                {
                    var user = await _dbContext.Users
                        .Where(u => u.Id == dto.UserId)
                        .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        return BadRequest(new { success = false, message = "User not found" });
                    }

                    // Verify user is staff/admin
                    var userRoles = await _dbContext.UserRoles
                        .Where(ur => ur.UserId == dto.UserId)
                        .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                        .ToListAsync();

                    if (!userRoles.Any(r => r == "Admin" || r == "Associate" || r == "SystemAdmin"))
                    {
                        return BadRequest(new { success = false, message = "User must be a staff member or admin" });
                    }
                }

                conversation.AssignedTo = dto.UserId == "unassigned" ? null : dto.UserId;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Conversation {ConversationId} assigned to {UserId}", conversationId, dto.UserId ?? "unassigned");

                return Ok(new { success = true, message = "Conversation assigned successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning conversation");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update conversation status
        /// </summary>
        [HttpPatch("conversations/{conversationId}/status")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<IActionResult> UpdateConversationStatus(int conversationId, [FromBody] UpdateConversationStatusDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Status))
                {
                    return BadRequest(new { success = false, message = "Status is required" });
                }

                if (!Enum.TryParse<ConversationStatus>(dto.Status, out var statusEnum))
                {
                    return BadRequest(new { success = false, message = "Invalid status value" });
                }

                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { success = false, message = "Conversation not found" });
                }

                conversation.Status = statusEnum;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Conversation {ConversationId} status updated to {Status}", conversationId, dto.Status);

                return Ok(new { success = true, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation status");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get staff users for assignment
        /// </summary>
        [HttpGet("staff-users")]
        [Authorize(Roles = "Admin,Associate,SystemAdmin")]
        public async Task<IActionResult> GetStaffUsers()
        {
            try
            {
                var staffUserIds = await _dbContext.UserRoles
                    .Where(ur => _dbContext.Roles.Any(r => r.Id == ur.RoleId && 
                        (r.Name == "Admin" || r.Name == "Associate" || r.Name == "SystemAdmin")))
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .ToListAsync();

                var staffUsers = await _dbContext.Users
                    .Where(u => staffUserIds.Contains(u.Id) && u.IsActive)
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName
                    })
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                
                return Ok(new { success = true, data = staffUsers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff users");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    public record BulkUpdateStatusDto(List<int> MessageIds, MessageStatus Status);
    public record SendConversationMessageDto(string Body, bool IsInternal);
    public record AssignConversationDto(string UserId);
    public record UpdateConversationStatusDto(string Status);
}