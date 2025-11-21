using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// Anthropic Claude LLM provider implementation
    /// Supports Claude 3 (Opus, Sonnet, Haiku)
    /// </summary>
    public class AnthropicProvider : ILLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly LLMProviderConfig _config;
        private readonly ILogger<AnthropicProvider> _logger;
        private const string DefaultEndpoint = "https://api.anthropic.com/v1";
        private const string AnthropicVersion = "2023-06-01";
        
        public AnthropicProvider(
            HttpClient httpClient,
            LLMProviderConfig config,
            ILogger<AnthropicProvider> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri(_config.ApiEndpoint ?? DefaultEndpoint);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
        }
        
        public async Task<Result<float[]>> GenerateEmbeddingAsync(string text)
        {
            // Anthropic doesn't provide embeddings API
            // Fall back to OpenAI or use Voyage AI
            _logger.LogWarning("Anthropic does not provide embeddings. Use OpenAI for embeddings.");
            return Result.Failure<float[]>("Anthropic does not support embeddings. Configure OpenAI for embedding generation.");
        }
        
        public async Task<Result<string>> GenerateChatCompletionAsync(
            List<ChatMessage> messages,
            string? systemPrompt = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            try
            {
                var apiMessages = new List<object>();
                
                foreach (var msg in messages)
                {
                    if (msg.Role != "system") // Anthropic handles system separately
                    {
                        apiMessages.Add(new { role = msg.Role, content = msg.Content });
                    }
                }
                
                var request = new
                {
                    model = _config.ModelName,
                    messages = apiMessages,
                    system = systemPrompt ?? "You are a helpful assistant.",
                    temperature = temperature,
                    max_tokens = maxTokens
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync("/messages", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Anthropic API error: {Error}", error);
                    return Result.Failure<string>($"Anthropic API error: {response.StatusCode}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AnthropicResponse>(responseJson);
                
                if (result?.Content == null || result.Content.Count == 0)
                {
                    return Result.Failure<string>("No response from Anthropic");
                }
                
                return Result.Success(result.Content[0].Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Anthropic chat completion");
                return Result.Failure<string>("Failed to generate response");
            }
        }
        
        public int CountTokens(string text)
        {
            // Rough approximation: 1 token ≈ 4 characters
            // For production, use Anthropic's tokenizer
            return (int)Math.Ceiling(text.Length / 4.0);
        }
        
        #region Response Models
        
        private class AnthropicResponse
        {
            public List<ContentBlock> Content { get; set; } = new();
        }
        
        private class ContentBlock
        {
            public string Text { get; set; } = string.Empty;
        }
        
        #endregion
    }
}
