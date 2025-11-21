using BettsTax.Core.Services;
using BettsTax.Core.Services.Bot;
using BettsTax.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers
{
    /// <summary>
    /// Bot chat controller for RAG-based assistance
    /// Phase 3: Bot Capabilities
    /// </summary>
    [ApiController]
    [Route("api/bot")]
    [Authorize]
    public class BotController : ControllerBase
    {
        private readonly IRAGBotService _botService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<BotController> _logger;
        
        public BotController(
            IRAGBotService botService,
            IUserContextService userContext,
            ILogger<BotController> logger)
        {
            _botService = botService;
            _userContext = userContext;
            _logger = logger;
        }
        
        /// <summary>
        /// Send message to bot
        /// </summary>
        [HttpPost("chat")]
        public async Task<ActionResult<string>> Chat([FromBody] ChatRequest request)
        {
            var userId = _userContext.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var result = await _botService.ChatAsync(userId, request.ConversationId, request.Message);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Get user's conversations
        /// </summary>
        [HttpGet("conversations")]
        public async Task<ActionResult<List<BotConversation>>> GetConversations()
        {
            var userId = _userContext.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var result = await _botService.GetUserConversationsAsync(userId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Get specific conversation with messages
        /// </summary>
        [HttpGet("conversations/{conversationId}")]
        public async Task<ActionResult<BotConversation>> GetConversation(int conversationId)
        {
            var result = await _botService.GetConversationAsync(conversationId);
            return result.IsSuccess ? Ok(result.Value) : NotFound(result.ErrorMessage);
        }
        
        /// <summary>
        /// Archive conversation
        /// </summary>
        [HttpPost("conversations/{conversationId}/archive")]
        public async Task<ActionResult<bool>> ArchiveConversation(int conversationId)
        {
            var result = await _botService.ArchiveConversationAsync(conversationId);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Submit feedback for bot response
        /// </summary>
        [HttpPost("feedback")]
        public async Task<ActionResult<bool>> SubmitFeedback([FromBody] FeedbackRequest request)
        {
            var result = await _botService.SubmitFeedbackAsync(
                request.MessageId, 
                request.Rating, 
                request.Comment, 
                request.WasHelpful);
            
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
    }
    
    public class ChatRequest
    {
        public int? ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    
    public class FeedbackRequest
    {
        public int MessageId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool WasHelpful { get; set; }
    }
}
