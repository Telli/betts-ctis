using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text.Json;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// RAG (Retrieval-Augmented Generation) bot service
    /// Phase 3: Bot Capabilities with RAG
    /// </summary>
    public interface IRAGBotService
    {
        Task<Result<string>> ChatAsync(string userId, int? conversationId, string userMessage);
        Task<Result<List<BotConversation>>> GetUserConversationsAsync(string userId);
        Task<Result<BotConversation>> GetConversationAsync(int conversationId);
        Task<Result<bool>> ArchiveConversationAsync(int conversationId);
        Task<Result<bool>> SubmitFeedbackAsync(int messageId, int rating, string? comment, bool wasHelpful);
    }
    
    public class RAGBotService : IRAGBotService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILLMProviderFactory _providerFactory;
        private readonly ILogger<RAGBotService> _logger;
        private const int MaxConversationHistory = 10; // Last N messages for context
        
        public RAGBotService(
            ApplicationDbContext context,
            ILLMProviderFactory providerFactory,
            ILogger<RAGBotService> logger)
        {
            _context = context;
            _providerFactory = providerFactory;
            _logger = logger;
        }
        
        public async Task<Result<string>> ChatAsync(string userId, int? conversationId, string userMessage)
        {
            try
            {
                // Get or create conversation
                BotConversation conversation;
                if (conversationId.HasValue)
                {
                    conversation = await _context.Set<BotConversation>()
                        .Include(c => c.Messages)
                        .FirstOrDefaultAsync(c => c.BotConversationId == conversationId.Value && c.UserId == userId);
                    
                    if (conversation == null)
                    {
                        return Result.Failure<string>("Conversation not found");
                    }
                }
                else
                {
                    conversation = new BotConversation
                    {
                        UserId = userId,
                        Title = TruncateText(userMessage, 50),
                        StartedDate = DateTime.UtcNow,
                        LastMessageDate = DateTime.UtcNow
                    };
                    _context.Set<BotConversation>().Add(conversation);
                    await _context.SaveChangesAsync();
                }
                
                // Get active configuration
                var config = await _context.Set<BotConfiguration>()
                    .FirstOrDefaultAsync(c => c.IsActive && c.IsDefault);
                
                if (config == null)
                {
                    return Result.Failure<string>("No active bot configuration found");
                }
                
                // Get LLM provider
                var provider = await _providerFactory.GetProviderAsync(config);
                
                // Generate embedding for user query
                var embeddingResult = await provider.GenerateEmbeddingAsync(userMessage);
                if (!embeddingResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to generate embedding, proceeding without RAG: {Error}", 
                        embeddingResult.ErrorMessage);
                }
                
                // Retrieve relevant context from knowledge base
                List<DocumentChunk> relevantChunks = new();
                if (embeddingResult.IsSuccess)
                {
                    relevantChunks = await RetrieveRelevantChunksAsync(
                        embeddingResult.Value, 
                        config.TopK, 
                        config.SimilarityThreshold);
                }
                
                // Build context from retrieved chunks
                var context = BuildContext(relevantChunks);
                
                // Get conversation history
                var history = await GetConversationHistoryAsync(conversation.BotConversationId);
                
                // Build messages for LLM
                var messages = new List<ChatMessage>();
                
                // Add conversation history
                foreach (var msg in history)
                {
                    messages.Add(new ChatMessage
                    {
                        Role = msg.Role,
                        Content = msg.Content
                    });
                }
                
                // Add current user message with context
                var userMessageWithContext = string.IsNullOrEmpty(context)
                    ? userMessage
                    : $"Context from knowledge base:\n{context}\n\nUser question: {userMessage}";
                
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = userMessageWithContext
                });
                
                // Generate response
                var responseResult = await provider.GenerateChatCompletionAsync(
                    messages,
                    config.SystemPrompt,
                    config.Temperature,
                    config.MaxTokens);
                
                if (!responseResult.IsSuccess)
                {
                    return Result.Failure<string>(responseResult.ErrorMessage);
                }
                
                // Save user message
                var userMsg = new BotMessage
                {
                    BotConversationId = conversation.BotConversationId,
                    Role = "user",
                    Content = userMessage,
                    TokenCount = provider.CountTokens(userMessage),
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BotMessage>().Add(userMsg);
                
                // Save assistant response
                var assistantMsg = new BotMessage
                {
                    BotConversationId = conversation.BotConversationId,
                    Role = "assistant",
                    Content = responseResult.Value,
                    RetrievedChunks = relevantChunks.Any() 
                        ? JsonSerializer.Serialize(relevantChunks.Select(c => c.DocumentChunkId).ToList())
                        : null,
                    TokenCount = provider.CountTokens(responseResult.Value),
                    Timestamp = DateTime.UtcNow,
                    Provider = config.Provider,
                    ModelName = config.ModelName
                };
                _context.Set<BotMessage>().Add(assistantMsg);
                
                // Update conversation
                conversation.LastMessageDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                return Result.Success(responseResult.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bot chat");
                return Result.Failure<string>("Failed to generate response");
            }
        }
        
        public async Task<Result<List<BotConversation>>> GetUserConversationsAsync(string userId)
        {
            try
            {
                var conversations = await _context.Set<BotConversation>()
                    .Where(c => c.UserId == userId && !c.IsArchived)
                    .OrderByDescending(c => c.LastMessageDate)
                    .Take(50)
                    .ToListAsync();
                
                return Result.Success(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user conversations");
                return Result.Failure<List<BotConversation>>("Failed to retrieve conversations");
            }
        }
        
        public async Task<Result<BotConversation>> GetConversationAsync(int conversationId)
        {
            try
            {
                var conversation = await _context.Set<BotConversation>()
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.BotConversationId == conversationId);
                
                if (conversation == null)
                {
                    return Result.Failure<BotConversation>("Conversation not found");
                }
                
                return Result.Success(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation");
                return Result.Failure<BotConversation>("Failed to retrieve conversation");
            }
        }
        
        public async Task<Result<bool>> ArchiveConversationAsync(int conversationId)
        {
            try
            {
                var conversation = await _context.Set<BotConversation>()
                    .FirstOrDefaultAsync(c => c.BotConversationId == conversationId);
                
                if (conversation == null)
                {
                    return Result.Failure<bool>("Conversation not found");
                }
                
                conversation.IsArchived = true;
                await _context.SaveChangesAsync();
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving conversation");
                return Result.Failure<bool>("Failed to archive conversation");
            }
        }
        
        public async Task<Result<bool>> SubmitFeedbackAsync(int messageId, int rating, string? comment, bool wasHelpful)
        {
            try
            {
                var message = await _context.Set<BotMessage>()
                    .Include(m => m.Conversation)
                    .FirstOrDefaultAsync(m => m.BotMessageId == messageId);
                
                if (message == null)
                {
                    return Result.Failure<bool>("Message not found");
                }
                
                var feedback = new BotFeedback
                {
                    BotMessageId = messageId,
                    UserId = message.Conversation!.UserId,
                    Rating = rating,
                    Comment = comment,
                    WasHelpful = wasHelpful,
                    Timestamp = DateTime.UtcNow
                };
                
                _context.Set<BotFeedback>().Add(feedback);
                await _context.SaveChangesAsync();
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback");
                return Result.Failure<bool>("Failed to submit feedback");
            }
        }
        
        #region Private Helper Methods
        
        private async Task<List<DocumentChunk>> RetrieveRelevantChunksAsync(
            float[] queryEmbedding, 
            int topK, 
            double similarityThreshold)
        {
            try
            {
                var vector = new Vector(queryEmbedding);
                
                // Use pgvector cosine similarity search
                var chunks = await _context.Set<DocumentChunk>()
                    .Include(c => c.KnowledgeDocument)
                    .Where(c => c.KnowledgeDocument!.IsActive && c.Embedding != null)
                    .OrderBy(c => c.Embedding!.CosineDistance(vector))
                    .Take(topK * 2) // Get more candidates for filtering
                    .ToListAsync();
                
                // Filter by similarity threshold (cosine distance: 0 = identical, 2 = opposite)
                // Convert to similarity: similarity = 1 - (distance / 2)
                var relevantChunks = chunks
                    .Where(c => c.Embedding != null)
                    .Select(c => new { Chunk = c, Distance = c.Embedding!.CosineDistance(vector) })
                    .Where(x => (1 - (x.Distance / 2)) >= similarityThreshold)
                    .OrderBy(x => x.Distance)
                    .Take(topK)
                    .Select(x => x.Chunk)
                    .ToList();
                
                _logger.LogInformation("Retrieved {Count} relevant chunks from knowledge base", 
                    relevantChunks.Count);
                
                return relevantChunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relevant chunks");
                return new List<DocumentChunk>();
            }
        }
        
        private string BuildContext(List<DocumentChunk> chunks)
        {
            if (!chunks.Any())
            {
                return string.Empty;
            }
            
            var contextParts = new List<string>();
            
            foreach (var chunk in chunks)
            {
                var source = chunk.KnowledgeDocument?.Title ?? "Unknown";
                contextParts.Add($"[Source: {source}]\n{chunk.Content}");
            }
            
            return string.Join("\n\n---\n\n", contextParts);
        }
        
        private async Task<List<BotMessage>> GetConversationHistoryAsync(int conversationId)
        {
            return await _context.Set<BotMessage>()
                .Where(m => m.BotConversationId == conversationId)
                .OrderByDescending(m => m.Timestamp)
                .Take(MaxConversationHistory)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
        
        private string TruncateText(string text, int maxLength)
        {
            if (text.Length <= maxLength)
            {
                return text;
            }
            
            return text.Substring(0, maxLength) + "...";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Factory for creating LLM providers
    /// </summary>
    public interface ILLMProviderFactory
    {
        Task<ILLMProvider> GetProviderAsync(BotConfiguration config);
    }
    
    public class LLMProviderFactory : ILLMProviderFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LLMProviderFactory> _logger;
        private readonly IEncryptionService _encryptionService;
        
        public LLMProviderFactory(
            IHttpClientFactory httpClientFactory,
            ILogger<LLMProviderFactory> logger,
            IEncryptionService encryptionService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _encryptionService = encryptionService;
        }
        
        public async Task<ILLMProvider> GetProviderAsync(BotConfiguration config)
        {
            var httpClient = _httpClientFactory.CreateClient("LLMProvider");
            
            var providerConfig = new LLMProviderConfig
            {
                ApiKey = await _encryptionService.DecryptAsync(config.ApiKeyEncrypted),
                ApiEndpoint = config.ApiEndpoint,
                ModelName = config.ModelName,
                Temperature = config.Temperature,
                MaxTokens = config.MaxTokens
            };
            
            return config.Provider switch
            {
                LLMProvider.OpenAI => new OpenAIProvider(httpClient, providerConfig, 
                    _logger as ILogger<OpenAIProvider> ?? throw new InvalidOperationException()),
                LLMProvider.Anthropic => new AnthropicProvider(httpClient, providerConfig, 
                    _logger as ILogger<AnthropicProvider> ?? throw new InvalidOperationException()),
                LLMProvider.Gemini => new GeminiProvider(httpClient, providerConfig, 
                    _logger as ILogger<GeminiProvider> ?? throw new InvalidOperationException()),
                _ => throw new NotSupportedException($"Provider {config.Provider} not supported")
            };
        }
    }
    
    /// <summary>
    /// Encryption service for API keys
    /// </summary>
    public interface IEncryptionService
    {
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string cipherText);
    }
}
