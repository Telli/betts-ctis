using BettsTax.Shared;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// LLM provider abstraction for RAG bot
    /// Supports OpenAI, Anthropic, and Gemini
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>
        /// Generate text embedding for RAG
        /// </summary>
        Task<Result<float[]>> GenerateEmbeddingAsync(string text);
        
        /// <summary>
        /// Generate chat completion
        /// </summary>
        Task<Result<string>> GenerateChatCompletionAsync(
            List<ChatMessage> messages,
            string? systemPrompt = null,
            double temperature = 0.7,
            int maxTokens = 1000);
        
        /// <summary>
        /// Count tokens in text
        /// </summary>
        int CountTokens(string text);
    }
    
    /// <summary>
    /// Chat message for LLM
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
        public string Content { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// LLM provider configuration
    /// </summary>
    public class LLMProviderConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1000;
    }
}
