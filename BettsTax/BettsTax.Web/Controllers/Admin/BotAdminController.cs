using BettsTax.Core.Services.Bot;
using BettsTax.Data;
using BettsTax.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Web.Controllers.Admin
{
    /// <summary>
    /// Admin controller for bot configuration and knowledge base management
    /// Phase 3: Bot Capabilities - Admin
    /// </summary>
    [ApiController]
    [Route("api/admin/bot")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public class BotAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDocumentProcessingService _documentService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<BotAdminController> _logger;
        
        public BotAdminController(
            ApplicationDbContext context,
            IDocumentProcessingService documentService,
            IEncryptionService encryptionService,
            ILogger<BotAdminController> logger)
        {
            _context = context;
            _documentService = documentService;
            _encryptionService = encryptionService;
            _logger = logger;
        }
        
        #region Bot Configuration
        
        /// <summary>
        /// Get all bot configurations
        /// </summary>
        [HttpGet("configurations")]
        public async Task<ActionResult<List<BotConfiguration>>> GetConfigurations()
        {
            var configs = await _context.Set<BotConfiguration>()
                .OrderByDescending(c => c.IsDefault)
                .ThenByDescending(c => c.CreatedDate)
                .ToListAsync();
            
            return Ok(configs);
        }
        
        /// <summary>
        /// Get bot configuration by ID
        /// </summary>
        [HttpGet("configurations/{id}")]
        public async Task<ActionResult<BotConfiguration>> GetConfiguration(int id)
        {
            var config = await _context.Set<BotConfiguration>()
                .FirstOrDefaultAsync(c => c.BotConfigurationId == id);
            
            if (config == null)
            {
                return NotFound();
            }
            
            return Ok(config);
        }
        
        /// <summary>
        /// Create bot configuration
        /// </summary>
        [HttpPost("configurations")]
        public async Task<ActionResult<BotConfiguration>> CreateConfiguration([FromBody] BotConfigurationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var config = new BotConfiguration
            {
                Name = request.Name,
                Provider = request.Provider,
                ModelName = request.ModelName,
                ApiKeyEncrypted = await _encryptionService.EncryptAsync(request.ApiKey),
                ApiEndpoint = request.ApiEndpoint,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                TopK = request.TopK,
                SimilarityThreshold = request.SimilarityThreshold,
                SystemPrompt = request.SystemPrompt,
                IsActive = request.IsActive,
                IsDefault = request.IsDefault,
                CreatedById = User.Identity?.Name ?? "System",
                CreatedDate = DateTime.UtcNow
            };
            
            // If setting as default, unset other defaults
            if (config.IsDefault)
            {
                var existingDefaults = await _context.Set<BotConfiguration>()
                    .Where(c => c.IsDefault)
                    .ToListAsync();
                
                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }
            
            _context.Set<BotConfiguration>().Add(config);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetConfiguration), new { id = config.BotConfigurationId }, config);
        }
        
        /// <summary>
        /// Update bot configuration
        /// </summary>
        [HttpPut("configurations/{id}")]
        public async Task<ActionResult<BotConfiguration>> UpdateConfiguration(int id, [FromBody] BotConfigurationRequest request)
        {
            var config = await _context.Set<BotConfiguration>()
                .FirstOrDefaultAsync(c => c.BotConfigurationId == id);
            
            if (config == null)
            {
                return NotFound();
            }
            
            config.Name = request.Name;
            config.Provider = request.Provider;
            config.ModelName = request.ModelName;
            
            if (!string.IsNullOrEmpty(request.ApiKey))
            {
                config.ApiKeyEncrypted = await _encryptionService.EncryptAsync(request.ApiKey);
            }
            
            config.ApiEndpoint = request.ApiEndpoint;
            config.Temperature = request.Temperature;
            config.MaxTokens = request.MaxTokens;
            config.TopK = request.TopK;
            config.SimilarityThreshold = request.SimilarityThreshold;
            config.SystemPrompt = request.SystemPrompt;
            config.IsActive = request.IsActive;
            config.IsDefault = request.IsDefault;
            config.UpdatedById = User.Identity?.Name;
            config.UpdatedDate = DateTime.UtcNow;
            
            // If setting as default, unset other defaults
            if (config.IsDefault)
            {
                var existingDefaults = await _context.Set<BotConfiguration>()
                    .Where(c => c.IsDefault && c.BotConfigurationId != id)
                    .ToListAsync();
                
                foreach (var existing in existingDefaults)
                {
                    existing.IsDefault = false;
                }
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(config);
        }
        
        /// <summary>
        /// Delete bot configuration
        /// </summary>
        [HttpDelete("configurations/{id}")]
        public async Task<ActionResult> DeleteConfiguration(int id)
        {
            var config = await _context.Set<BotConfiguration>()
                .FirstOrDefaultAsync(c => c.BotConfigurationId == id);
            
            if (config == null)
            {
                return NotFound();
            }
            
            _context.Set<BotConfiguration>().Remove(config);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        
        #endregion
        
        #region Knowledge Base Management
        
        /// <summary>
        /// Get all knowledge documents
        /// </summary>
        [HttpGet("documents")]
        public async Task<ActionResult<List<KnowledgeDocument>>> GetDocuments([FromQuery] string? category = null)
        {
            var result = await _documentService.GetDocumentsAsync(category);
            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Upload knowledge document
        /// </summary>
        [HttpPost("documents")]
        public async Task<ActionResult<KnowledgeDocument>> UploadDocument([FromBody] DocumentUploadRequest request)
        {
            var userId = User.Identity?.Name ?? "System";
            var result = await _documentService.UploadDocumentAsync(
                request.Title,
                request.Content,
                request.Category,
                request.Tags,
                userId);
            
            return result.IsSuccess 
                ? CreatedAtAction(nameof(GetDocuments), result.Value) 
                : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Delete knowledge document
        /// </summary>
        [HttpDelete("documents/{id}")]
        public async Task<ActionResult> DeleteDocument(int id)
        {
            var result = await _documentService.DeleteDocumentAsync(id);
            return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
        }
        
        /// <summary>
        /// Get embedding job status
        /// </summary>
        [HttpGet("jobs/{jobId}")]
        public async Task<ActionResult<EmbeddingJob>> GetJobStatus(int jobId)
        {
            var result = await _documentService.GetEmbeddingJobStatusAsync(jobId);
            return result.IsSuccess ? Ok(result.Value) : NotFound(result.ErrorMessage);
        }
        
        /// <summary>
        /// Reprocess document embeddings
        /// </summary>
        [HttpPost("documents/{id}/reprocess")]
        public async Task<ActionResult> ReprocessDocument(int id)
        {
            var result = await _documentService.ProcessDocumentAsync(id);
            return result.IsSuccess ? Ok() : BadRequest(result.ErrorMessage);
        }
        
        #endregion
        
        #region Analytics
        
        /// <summary>
        /// Get bot usage statistics
        /// </summary>
        [HttpGet("analytics/usage")]
        public async Task<ActionResult<object>> GetUsageStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            
            var totalConversations = await _context.Set<BotConversation>()
                .Where(c => c.StartedDate >= start && c.StartedDate <= end)
                .CountAsync();
            
            var totalMessages = await _context.Set<BotMessage>()
                .Where(m => m.Timestamp >= start && m.Timestamp <= end)
                .CountAsync();
            
            var avgMessagesPerConversation = totalConversations > 0 
                ? (double)totalMessages / totalConversations 
                : 0;
            
            var feedbackStats = await _context.Set<BotFeedback>()
                .Where(f => f.Timestamp >= start && f.Timestamp <= end)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFeedback = g.Count(),
                    AverageRating = g.Average(f => f.Rating),
                    HelpfulPercentage = g.Count(f => f.WasHelpful) * 100.0 / g.Count()
                })
                .FirstOrDefaultAsync();
            
            return Ok(new
            {
                totalConversations,
                totalMessages,
                avgMessagesPerConversation,
                feedback = feedbackStats ?? new { TotalFeedback = 0, AverageRating = 0.0, HelpfulPercentage = 0.0 }
            });
        }
        
        /// <summary>
        /// Get popular topics from conversations
        /// </summary>
        [HttpGet("analytics/topics")]
        public async Task<ActionResult<List<object>>> GetPopularTopics([FromQuery] int limit = 10)
        {
            var conversations = await _context.Set<BotConversation>()
                .OrderByDescending(c => c.LastMessageDate)
                .Take(1000)
                .Select(c => c.Title)
                .ToListAsync();
            
            // Simple word frequency analysis
            var wordCounts = new Dictionary<string, int>();
            var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };
            
            foreach (var title in conversations)
            {
                var words = title.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (!stopWords.Contains(word) && word.Length > 3)
                    {
                        wordCounts[word] = wordCounts.GetValueOrDefault(word, 0) + 1;
                    }
                }
            }
            
            var topTopics = wordCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(limit)
                .Select(kvp => new { topic = kvp.Key, count = kvp.Value })
                .ToList<object>();
            
            return Ok(topTopics);
        }
        
        #endregion
    }
    
    public class BotConfigurationRequest
    {
        public string Name { get; set; } = string.Empty;
        public LLMProvider Provider { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
        public int TopK { get; set; } = 5;
        public double SimilarityThreshold { get; set; } = 0.7;
        public string SystemPrompt { get; set; } = "You are a helpful tax compliance assistant for Sierra Leone.";
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }
    
    public class DocumentUploadRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}
