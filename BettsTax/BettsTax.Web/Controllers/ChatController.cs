using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Core.DTOs.Communication;
using System.Security.Claims;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ApplicationDbContext context, ILogger<ChatController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<IEnumerable<ChatRoomDto>>> GetUserRooms()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var rooms = await _context.ChatRoomParticipants
            .Where(p => p.UserId == userId && p.IsActive)
            .Include(p => p.ChatRoom)
            .ThenInclude(r => r!.Client)
            .Select(p => new ChatRoomDto
            {
                Id = p.ChatRoom!.Id,
                Name = p.ChatRoom.Name,
                Description = p.ChatRoom.Description,
                Type = p.ChatRoom.Type.ToString(),
                IsActive = p.ChatRoom.IsActive,
                CreatedAt = p.ChatRoom.CreatedAt,
                LastActivityAt = p.ChatRoom.LastActivityAt,
                MessageCount = p.ChatRoom.MessageCount,
                CurrentParticipants = p.ChatRoom.CurrentParticipants,
                ClientId = p.ChatRoom.ClientId,
                ClientName = p.ChatRoom.Client != null ? $"{p.ChatRoom.Client.FirstName} {p.ChatRoom.Client.LastName}" : null,
                TaxYear = p.ChatRoom.TaxYear,
                TaxType = p.ChatRoom.TaxType == null ? null : p.ChatRoom.TaxType.ToString(),
                Topic = p.ChatRoom.Topic,
                UserRole = p.Role.ToString(),
                LastSeenAt = p.LastSeenAt,
                UnreadCount = p.ChatRoom.Messages.Count(m => m.SentAt > (p.LastSeenAt ?? DateTime.MinValue) && m.SenderId != userId && !m.IsDeleted)
            })
            .OrderByDescending(r => r.LastActivityAt)
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("rooms/{roomId}")]
    public async Task<ActionResult<ChatRoomDto>> GetRoom(int roomId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var participant = await _context.ChatRoomParticipants
            .Include(p => p.ChatRoom)
            .ThenInclude(r => r!.Client)
            .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && p.IsActive);

        if (participant == null) return NotFound("Room not found or access denied");

        var room = new ChatRoomDto
        {
            Id = participant.ChatRoom!.Id,
            Name = participant.ChatRoom.Name,
            Description = participant.ChatRoom.Description,
            Type = participant.ChatRoom.Type.ToString(),
            IsActive = participant.ChatRoom.IsActive,
            CreatedAt = participant.ChatRoom.CreatedAt,
            LastActivityAt = participant.ChatRoom.LastActivityAt,
            MessageCount = participant.ChatRoom.MessageCount,
            CurrentParticipants = participant.ChatRoom.CurrentParticipants,
            ClientId = participant.ChatRoom.ClientId,
            ClientName = participant.ChatRoom.Client != null ? $"{participant.ChatRoom.Client.FirstName} {participant.ChatRoom.Client.LastName}" : null,
            TaxYear = participant.ChatRoom.TaxYear,
            TaxType = participant.ChatRoom.TaxType == null ? null : participant.ChatRoom.TaxType.ToString(),
            Topic = participant.ChatRoom.Topic,
            UserRole = participant.Role.ToString(),
            LastSeenAt = participant.LastSeenAt,
            UnreadCount = participant.ChatRoom.Messages.Count(m => m.SentAt > (participant.LastSeenAt ?? DateTime.MinValue) && m.SenderId != userId && !m.IsDeleted)
        };

        return Ok(room);
    }

    [HttpGet("rooms/{roomId}/messages")]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> GetRoomMessages(
        int roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? before = null,
        [FromQuery] DateTime? after = null,
        [FromQuery] bool includeInternal = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Verify user has access to this room
        var participant = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && p.IsActive);

        if (participant == null) return NotFound("Room not found or access denied");

        // Check if user can see internal messages
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var canSeeInternal = includeInternal && (userRoles.Contains("Admin") || userRoles.Contains("Associate") || participant.Role >= ChatRoomRole.Moderator);

        var query = _context.ChatMessages
            .Where(m => m.ChatRoomId == roomId && !m.IsDeleted)
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(r => r!.Sender)
            .AsQueryable();

        // Filter by internal messages visibility
        if (!canSeeInternal)
        {
            query = query.Where(m => !m.IsPrivate);
        }

        // Apply date filters
        if (before.HasValue)
            query = query.Where(m => m.SentAt < before.Value);
        
        if (after.HasValue)
            query = query.Where(m => m.SentAt > after.Value);

        var totalCount = await query.CountAsync();
        
        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ChatRoomId = m.ChatRoomId,
                SenderId = m.SenderId,
                SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
                Content = m.Content,
                Type = m.Type.ToString(),
                SentAt = m.SentAt,
                EditedAt = m.EditedAt,
                IsDeleted = m.IsDeleted,
                IsInternal = m.IsPrivate,
                IsPinned = m.IsPinned,
                IsImportant = m.IsImportant,
                ReplyToMessage = m.ReplyToMessage != null ? new ChatMessageDto
                {
                    Id = m.ReplyToMessage.Id,
                    Content = m.ReplyToMessage.Content,
                    SenderName = m.ReplyToMessage.Sender != null ? $"{m.ReplyToMessage.Sender.FirstName} {m.ReplyToMessage.Sender.LastName}" : "Unknown",
                    SentAt = m.ReplyToMessage.SentAt
                } : null,
                RelatedTaxFilingId = m.RelatedTaxFilingId,
                RelatedPaymentId = m.RelatedPaymentId,
                RelatedDocumentId = m.RelatedDocumentId,
                TaxYear = m.TaxYear,
                TaxType = m.TaxType == null ? null : m.TaxType.ToString()
            })
            .ToListAsync();

        // Update user's last seen time for this room
        participant.LastSeenAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = new PagedResult<ChatMessageDto>
        {
            Items = messages.OrderBy(m => m.SentAt).ToList(), // Re-order chronologically for display
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return Ok(result);
    }

    [HttpPost("rooms/{roomId}/messages")]
    public async Task<ActionResult<ChatMessageDto>> SendMessage(int roomId, [FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Message content is required");

        // Verify user has permission to send messages in this room
        var participant = await _context.ChatRoomParticipants
            .Include(p => p.ChatRoom)
            .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && p.IsActive);

        if (participant == null)
            return NotFound("Room not found or you don't have permission to send messages");

        // Check if user can send internal messages
        if (request.IsInternal)
        {
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!userRoles.Contains("Admin") && !userRoles.Contains("Associate") && participant.Role < ChatRoomRole.Moderator)
            {
                return Forbid("You don't have permission to send internal messages");
            }
        }

        var message = new ChatMessage
        {
            ChatRoomId = roomId,
            SenderId = userId,
            Content = request.Content.Trim(),
            Type = ChatMessageType.Text,
            SentAt = DateTime.UtcNow,
            ReplyToMessageId = request.ReplyToMessageId,
            IsPrivate = request.IsInternal,
            IsImportant = request.IsImportant,
            RelatedTaxFilingId = request.RelatedTaxFilingId,
            RelatedPaymentId = request.RelatedPaymentId,
            RelatedDocumentId = request.RelatedDocumentId,
            TaxYear = request.TaxYear,
            TaxType = request.TaxType
        };

        _context.ChatMessages.Add(message);

        // Update room stats
        participant.ChatRoom!.MessageCount++;
        participant.ChatRoom.LastActivityAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Load the complete message with sender info
        var completeMessage = await _context.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(r => r!.Sender)
            .FirstAsync(m => m.Id == message.Id);

        var messageDto = new ChatMessageDto
        {
            Id = completeMessage.Id,
            ChatRoomId = completeMessage.ChatRoomId,
            SenderId = completeMessage.SenderId,
            SenderName = completeMessage.Sender != null ? $"{completeMessage.Sender.FirstName} {completeMessage.Sender.LastName}" : "Unknown",
            Content = completeMessage.Content,
            Type = completeMessage.Type.ToString(),
            SentAt = completeMessage.SentAt,
            IsInternal = completeMessage.IsPrivate,
            IsImportant = completeMessage.IsImportant,
            ReplyToMessage = completeMessage.ReplyToMessage != null ? new ChatMessageDto
            {
                Id = completeMessage.ReplyToMessage.Id,
                Content = completeMessage.ReplyToMessage.Content,
                SenderName = completeMessage.ReplyToMessage.Sender != null ? $"{completeMessage.ReplyToMessage.Sender.FirstName} {completeMessage.ReplyToMessage.Sender.LastName}" : "Unknown",
                SentAt = completeMessage.ReplyToMessage.SentAt
            } : null,
            RelatedTaxFilingId = completeMessage.RelatedTaxFilingId,
            RelatedPaymentId = completeMessage.RelatedPaymentId,
            RelatedDocumentId = completeMessage.RelatedDocumentId,
            TaxYear = completeMessage.TaxYear,
            TaxType = completeMessage.TaxType == null ? null : completeMessage.TaxType.ToString()
        };

        return CreatedAtAction(nameof(GetRoomMessages), new { roomId }, messageDto);
    }

    [HttpPut("messages/{messageId}")]
    public async Task<ActionResult> EditMessage(int messageId, [FromBody] EditMessageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Message content is required");

        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId && !m.IsDeleted);

        if (message == null)
            return NotFound("Message not found or you don't have permission to edit it");

        // Check if message is too old to edit (e.g., 15 minutes)
        if (DateTime.UtcNow - message.SentAt > TimeSpan.FromMinutes(15))
            return BadRequest("Message is too old to edit");

        message.Content = request.Content.Trim();
        message.EditedAt = DateTime.UtcNow;
        message.EditedBy = userId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("messages/{messageId}")]
    public async Task<ActionResult> DeleteMessage(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message == null)
            return NotFound("Message not found");

        // Check permissions - user can delete their own message or moderators can delete any
        var canDelete = message.SenderId == userId;
        if (!canDelete)
        {
            var participant = await _context.ChatRoomParticipants
                .FirstOrDefaultAsync(p => p.ChatRoomId == message.ChatRoomId && p.UserId == userId && 
                                        p.IsActive && p.Role >= ChatRoomRole.Moderator);
            canDelete = participant != null;
        }

        if (!canDelete)
            return Forbid("You don't have permission to delete this message");

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        message.DeletedBy = userId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("rooms/{roomId}/assign")]
    [Authorize(Roles = "Admin,Associate")]
    public async Task<ActionResult> AssignRoom(int roomId, [FromBody] AssignRoomRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var room = await _context.ChatRooms.FindAsync(roomId);
        if (room == null) return NotFound("Room not found");

        // Verify the user being assigned exists and has appropriate role
        var assignedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.AssignToUserId);

        if (assignedUser == null)
            return BadRequest("Assigned user not found");

        // Check user roles using UserManager if available, or check Role property
        var isValidRole = assignedUser.Role == "Admin" || assignedUser.Role == "Associate";
        if (!isValidRole)
            return BadRequest("User must be Admin or Associate to be assigned to rooms");

        // Update room assignment using metadata field (until we add proper AssignedToUserId field)
        var assignmentData = new 
        { 
            AssignedToUserId = request.AssignToUserId, 
            AssignedAt = DateTime.UtcNow, 
            AssignedBy = userId,
            Notes = request.Notes
        };
        room.Settings = System.Text.Json.JsonSerializer.Serialize(assignmentData);
        room.UpdatedAt = DateTime.UtcNow;

        // Ensure assigned user is a participant in the room
        var existingParticipant = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == request.AssignToUserId);

        if (existingParticipant == null)
        {
            var newParticipant = new ChatRoomParticipant
            {
                ChatRoomId = roomId,
                UserId = request.AssignToUserId,
                Role = ChatRoomRole.Moderator,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.ChatRoomParticipants.Add(newParticipant);
            room.CurrentParticipants++;
        }
        else if (!existingParticipant.IsActive)
        {
            existingParticipant.IsActive = true;
            existingParticipant.Role = ChatRoomRole.Moderator;
            room.CurrentParticipants++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Room {RoomId} assigned to {AssignedToUserId} by {UserId}", 
            roomId, request.AssignToUserId, userId);

        return NoContent();
    }

    [HttpGet("rooms/{roomId}/participants")]
    public async Task<ActionResult<IEnumerable<ChatParticipantDto>>> GetRoomParticipants(int roomId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Verify user has access to this room
        var userParticipant = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.ChatRoomId == roomId && p.UserId == userId && p.IsActive);

        if (userParticipant == null) return NotFound("Room not found or access denied");

        var participants = await _context.ChatRoomParticipants
            .Where(p => p.ChatRoomId == roomId && p.IsActive)
            .Include(p => p.User)
            .Select(p => new ChatParticipantDto
            {
                UserId = p.UserId,
                UserName = p.User != null ? $"{p.User.FirstName} {p.User.LastName}" : "Unknown",
                Role = p.Role.ToString(),
                JoinedAt = p.JoinedAt,
                LastSeenAt = p.LastSeenAt,
                IsOnline = p.LastSeenAt.HasValue && DateTime.UtcNow - p.LastSeenAt.Value < TimeSpan.FromMinutes(5)
            })
            .OrderBy(p => p.UserName)
            .ToListAsync();

        return Ok(participants);
    }

    [HttpPost("rooms")]
    [Authorize(Roles = "Admin,Associate")]
    public async Task<ActionResult<ChatRoomDto>> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Room name is required");

        var room = new ChatRoom
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Type = Enum.Parse<ChatRoomType>(request.Type ?? "Group"),
            IsActive = true,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            MaxParticipants = request.MaxParticipants ?? 100,
            CurrentParticipants = 1,
            ClientId = request.ClientId,
            TaxYear = request.TaxYear,
            TaxType = request.TaxType,
            RequiresApproval = request.RequiresApproval,
            IsEncrypted = request.IsEncrypted,
            Topic = request.Topic?.Trim(),
            TopicSetBy = userId,
            TopicSetAt = DateTime.UtcNow
        };

        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();

        // Add creator as owner
        var ownerParticipant = new ChatRoomParticipant
        {
            ChatRoomId = room.Id,
            UserId = userId,
            Role = ChatRoomRole.Owner,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ChatRoomParticipants.Add(ownerParticipant);
        await _context.SaveChangesAsync();

        var roomDto = new ChatRoomDto
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            Type = room.Type.ToString(),
            IsActive = room.IsActive,
            CreatedAt = room.CreatedAt,
            LastActivityAt = room.LastActivityAt,
            MessageCount = room.MessageCount,
            CurrentParticipants = room.CurrentParticipants,
            ClientId = room.ClientId,
            TaxYear = room.TaxYear,
            TaxType = room.TaxType == null ? null : room.TaxType.ToString(),
            Topic = room.Topic,
            UserRole = ownerParticipant.Role.ToString()
        };

        return CreatedAtAction(nameof(GetRoom), new { roomId = room.Id }, roomDto);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<ChatMessageDto>>> SearchMessages([FromQuery] ChatSearchDto searchParams)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Get rooms user has access to
        var userRoomIds = await _context.ChatRoomParticipants
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => p.ChatRoomId)
            .ToListAsync();

        if (!userRoomIds.Any()) return Ok(new PagedResult<ChatMessageDto>());

        var query = _context.ChatMessages
            .Where(m => userRoomIds.Contains(m.ChatRoomId) && !m.IsDeleted)
            .Include(m => m.Sender)
            .Include(m => m.ChatRoom)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(r => r!.Sender)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchParams.Query))
        {
            query = query.Where(m => m.Content.Contains(searchParams.Query));
        }

        if (searchParams.RoomId.HasValue)
        {
            query = query.Where(m => m.ChatRoomId == searchParams.RoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.SenderId))
        {
            query = query.Where(m => m.SenderId == searchParams.SenderId);
        }

        if (searchParams.FromDate.HasValue)
        {
            query = query.Where(m => m.SentAt >= searchParams.FromDate.Value);
        }

        if (searchParams.ToDate.HasValue)
        {
            query = query.Where(m => m.SentAt <= searchParams.ToDate.Value);
        }

        if (searchParams.IsInternal.HasValue)
        {
            // Check if user can see internal messages
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var canSeeInternal = userRoles.Contains("Admin") || userRoles.Contains("Associate");
            
            if (!canSeeInternal)
            {
                query = query.Where(m => !m.IsPrivate); // Hide internal messages
            }
            else if (searchParams.IsInternal.Value)
            {
                query = query.Where(m => m.IsPrivate);
            }
            else
            {
                query = query.Where(m => !m.IsPrivate);
            }
        }

        if (searchParams.IsImportant.HasValue)
        {
            query = query.Where(m => m.IsImportant == searchParams.IsImportant.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.TaxYear))
        {
            query = query.Where(m => m.TaxYear == searchParams.TaxYear);
        }

        if (searchParams.TaxType.HasValue)
        {
            query = query.Where(m => m.TaxType == searchParams.TaxType.Value);
        }

        // Apply sorting
        query = searchParams.SortBy.ToLower() switch
        {
            "sentat" => searchParams.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.SentAt) 
                : query.OrderByDescending(m => m.SentAt),
            "sender" => searchParams.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.Sender!.FirstName).ThenBy(m => m.Sender!.LastName)
                : query.OrderByDescending(m => m.Sender!.FirstName).ThenByDescending(m => m.Sender!.LastName),
            "room" => searchParams.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.ChatRoom!.Name) 
                : query.OrderByDescending(m => m.ChatRoom!.Name),
            _ => searchParams.SortDirection.ToLower() == "asc" 
                ? query.OrderBy(m => m.SentAt) 
                : query.OrderByDescending(m => m.SentAt)
        };

        var totalCount = await query.CountAsync();

        var messages = await query
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ChatRoomId = m.ChatRoomId,
                SenderId = m.SenderId,
                SenderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown",
                Content = m.Content,
                Type = m.Type.ToString(),
                SentAt = m.SentAt,
                EditedAt = m.EditedAt,
                IsDeleted = m.IsDeleted,
                IsInternal = m.IsPrivate,
                IsPinned = m.IsPinned,
                IsImportant = m.IsImportant,
                ReplyToMessage = m.ReplyToMessage != null ? new ChatMessageDto
                {
                    Id = m.ReplyToMessage.Id,
                    Content = m.ReplyToMessage.Content,
                    SenderName = m.ReplyToMessage.Sender != null ? $"{m.ReplyToMessage.Sender.FirstName} {m.ReplyToMessage.Sender.LastName}" : "Unknown",
                    SentAt = m.ReplyToMessage.SentAt
                } : null,
                RelatedTaxFilingId = m.RelatedTaxFilingId,
                RelatedPaymentId = m.RelatedPaymentId,
                RelatedDocumentId = m.RelatedDocumentId,
                TaxYear = m.TaxYear,
                TaxType = m.TaxType == null ? null : m.TaxType.ToString()
            })
            .ToListAsync();

        var result = new PagedResult<ChatMessageDto>
        {
            Items = messages,
            TotalCount = totalCount,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchParams.PageSize)
        };

        return Ok(result);
    }

    [HttpGet("audit/{messageId}")]
    [Authorize(Roles = "Admin,Associate")]
    public async Task<ActionResult<IEnumerable<ChatAuditLogDto>>> GetMessageAuditLog(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Verify message exists and user has access
        var message = await _context.ChatMessages
            .Include(m => m.ChatRoom)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null) return NotFound("Message not found");

        var userParticipant = await _context.ChatRoomParticipants
            .FirstOrDefaultAsync(p => p.ChatRoomId == message.ChatRoomId && p.UserId == userId && p.IsActive);

        if (userParticipant == null) return NotFound("Access denied");

        // Create audit log entries based on message history
        var auditLogs = new List<ChatAuditLogDto>();

        // Creation log
        auditLogs.Add(new ChatAuditLogDto
        {
            Id = 1,
            MessageId = messageId,
            Action = "Created",
            UserId = message.SenderId,
            UserName = await GetUserDisplayName(message.SenderId),
            Timestamp = message.SentAt,
            NewContent = message.Content
        });

        // Edit log
        if (message.EditedAt.HasValue && !string.IsNullOrEmpty(message.EditedBy))
        {
            auditLogs.Add(new ChatAuditLogDto
            {
                Id = 2,
                MessageId = messageId,
                Action = "Edited",
                UserId = message.EditedBy,
                UserName = await GetUserDisplayName(message.EditedBy),
                Timestamp = message.EditedAt.Value,
                NewContent = message.Content,
                Reason = "Content updated"
            });
        }

        // Deletion log
        if (message.IsDeleted && message.DeletedAt.HasValue && !string.IsNullOrEmpty(message.DeletedBy))
        {
            auditLogs.Add(new ChatAuditLogDto
            {
                Id = 3,
                MessageId = messageId,
                Action = "Deleted",
                UserId = message.DeletedBy,
                UserName = await GetUserDisplayName(message.DeletedBy),
                Timestamp = message.DeletedAt.Value,
                Reason = "Message deleted"
            });
        }

        return Ok(auditLogs.OrderBy(a => a.Timestamp));
    }

    [HttpGet("analytics")]
    [Authorize(Roles = "Admin,Associate")]
    public async Task<ActionResult<ChatAnalyticsDto>> GetChatAnalytics([FromQuery] string period = "week")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var endDate = DateTime.UtcNow;
        var startDate = period.ToLower() switch
        {
            "day" => endDate.AddDays(-1),
            "week" => endDate.AddDays(-7),
            "month" => endDate.AddMonths(-1),
            "quarter" => endDate.AddMonths(-3),
            "year" => endDate.AddYears(-1),
            _ => endDate.AddDays(-7)
        };

        // Total messages in period
        var totalMessages = await _context.ChatMessages
            .Where(m => m.SentAt >= startDate && m.SentAt <= endDate && !m.IsDeleted)
            .CountAsync();

        // Total active rooms
        var totalRooms = await _context.ChatRooms
            .Where(r => r.IsActive && r.LastActivityAt >= startDate)
            .CountAsync();

        // Active users (sent at least one message)
        var activeUsers = await _context.ChatMessages
            .Where(m => m.SentAt >= startDate && m.SentAt <= endDate && !m.IsDeleted)
            .Select(m => m.SenderId)
            .Distinct()
            .CountAsync();

        // Average response time calculation (simplified)
        var averageResponseTime = await CalculateAverageResponseTime(startDate, endDate);

        // Average messages per room
        var averageMessagesPerRoom = totalRooms > 0 ? (decimal)totalMessages / totalRooms : 0;

        // Volume stats by day
        var volumeStats = await GetChatVolumeStats(startDate, endDate);

        // Top user stats
        var userStats = await GetChatUserStats(startDate, endDate, 10);

        // Top room stats
        var roomStats = await GetChatRoomStats(startDate, endDate, 10);

        var analytics = new ChatAnalyticsDto
        {
            Period = period,
            TotalMessages = totalMessages,
            TotalRooms = totalRooms,
            ActiveUsers = activeUsers,
            AverageResponseTime = averageResponseTime,
            AverageMessagesPerRoom = averageMessagesPerRoom,
            VolumeStats = volumeStats,
            UserStats = userStats,
            RoomStats = roomStats
        };

        return Ok(analytics);
    }

    private async Task<string> GetUserDisplayName(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";
    }

    private async Task<decimal> CalculateAverageResponseTime(DateTime startDate, DateTime endDate)
    {
        // Simplified response time calculation
        // In a real implementation, you'd track conversation threads and calculate actual response times
        var messages = await _context.ChatMessages
            .Where(m => m.SentAt >= startDate && m.SentAt <= endDate && !m.IsDeleted && m.ReplyToMessageId.HasValue)
            .Include(m => m.ReplyToMessage)
            .ToListAsync();

        if (!messages.Any()) return 0;

        var responseTimes = messages
            .Where(m => m.ReplyToMessage != null)
            .Select(m => (decimal)(m.SentAt - m.ReplyToMessage!.SentAt).TotalMinutes)
            .Where(rt => rt > 0 && rt < 1440) // Filter out unrealistic response times (> 24 hours)
            .ToList();

        return responseTimes.Any() ? responseTimes.Average() : 0;
    }

    private async Task<List<ChatVolumeStats>> GetChatVolumeStats(DateTime startDate, DateTime endDate)
    {
        var stats = await _context.ChatMessages
            .Where(m => m.SentAt >= startDate && m.SentAt <= endDate && !m.IsDeleted)
            .GroupBy(m => m.SentAt.Date)
            .Select(g => new ChatVolumeStats
            {
                Date = g.Key,
                MessageCount = g.Count(),
                ActiveUsers = g.Select(m => m.SenderId).Distinct().Count(),
                ActiveRooms = g.Select(m => m.ChatRoomId).Distinct().Count()
            })
            .OrderBy(s => s.Date)
            .ToListAsync();

        return stats;
    }

    private async Task<List<ChatUserStats>> GetChatUserStats(DateTime startDate, DateTime endDate, int limit)
    {
        var stats = await _context.ChatMessages
            .Where(m => m.SentAt >= startDate && m.SentAt <= endDate && !m.IsDeleted)
            .Include(m => m.Sender)
            .GroupBy(m => m.SenderId)
            .Select(g => new ChatUserStats
            {
                UserId = g.Key,
                UserName = g.First().Sender != null ? $"{g.First().Sender!.FirstName} {g.First().Sender!.LastName}" : "Unknown",
                MessagesSent = g.Count(),
                RoomsParticipated = g.Select(m => m.ChatRoomId).Distinct().Count(),
                LastActive = g.Max(m => m.SentAt),
                AverageResponseTime = 0 // Would need more complex calculation
            })
            .OrderByDescending(s => s.MessagesSent)
            .Take(limit)
            .ToListAsync();

        return stats;
    }

    private async Task<List<ChatRoomStats>> GetChatRoomStats(DateTime startDate, DateTime endDate, int limit)
    {
        var stats = await _context.ChatMessages
            .Where(m => m.SentAt >= startDate && m.SentAt <= endDate && !m.IsDeleted)
            .Include(m => m.ChatRoom)
            .GroupBy(m => m.ChatRoomId)
            .Select(g => new ChatRoomStats
            {
                RoomId = g.Key,
                RoomName = g.First().ChatRoom!.Name,
                MessageCount = g.Count(),
                ParticipantCount = g.Select(m => m.SenderId).Distinct().Count(),
                LastActivity = g.Max(m => m.SentAt),
                AverageResponseTime = 0 // Would need more complex calculation
            })
            .OrderByDescending(s => s.MessageCount)
            .Take(limit)
            .ToListAsync();

        return stats;
    }
}

// Request/Response DTOs
public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = false;
    public bool IsImportant { get; set; } = false;
    public int? ReplyToMessageId { get; set; }
    public int? RelatedTaxFilingId { get; set; }
    public int? RelatedPaymentId { get; set; }
    public int? RelatedDocumentId { get; set; }
    public string? TaxYear { get; set; }
    public TaxType? TaxType { get; set; }
}

public class EditMessageRequest
{
    public string Content { get; set; } = string.Empty;
}

public class AssignRoomRequest
{
    public string AssignToUserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateRoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; } = "Group";
    public int? MaxParticipants { get; set; } = 100;
    public int? ClientId { get; set; }
    public string? TaxYear { get; set; }
    public TaxType? TaxType { get; set; }
    public bool RequiresApproval { get; set; } = false;
    public bool IsEncrypted { get; set; } = false;
    public string? Topic { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
