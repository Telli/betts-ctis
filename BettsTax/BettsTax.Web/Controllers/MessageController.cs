using BettsTax.Core.DTOs;
using BettsTax.Core.Services;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IUserContextService _userContext;

        public MessageController(IMessageService messageService, IUserContextService userContext)
        {
            _messageService = messageService;
            _userContext = userContext;
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
    }

    public record BulkUpdateStatusDto(List<int> MessageIds, MessageStatus Status);
}