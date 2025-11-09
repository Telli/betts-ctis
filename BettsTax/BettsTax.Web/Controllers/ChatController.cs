using System.Linq;
using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

/// <summary>
/// Messaging endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IDemoDataService _demoDataService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IDemoDataService demoDataService,
        ILogger<ChatController> logger)
    {
        _demoDataService = demoDataService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve active conversations.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations()
    {
        try
        {
            var conversations = await _demoDataService.GetChatConversationsAsync();
            return Ok(new { success = true, data = conversations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve conversations");
            return StatusCode(500, new { success = false, message = "Failed to load conversations" });
        }
    }

    /// <summary>
    /// Retrieve messages for a conversation.
    /// </summary>
    [HttpGet("conversations/{conversationId:int}/messages")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages([FromRoute] int conversationId)
    {
        try
        {
            var conversation = await _demoDataService.GetChatConversationsAsync();
            if (!conversation.Any(c => c.Id == conversationId))
            {
                return NotFound(new { success = false, message = "Conversation not found" });
            }

            var messages = await _demoDataService.GetChatMessagesAsync(conversationId);
            return Ok(new { success = true, data = messages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve messages for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { success = false, message = "Failed to load messages" });
        }
    }
}
