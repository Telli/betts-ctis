using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// OpenAI LLM provider implementation
    /// Supports GPT-4, GPT-3.5, and text-embedding-ada-002
    /// </summary>
    public class OpenAIProvider : ILLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly LLMProviderConfig _config;
        private readonly ILogger<OpenAIProvider> _logger;
        private const string DefaultEndpoint = "https://api.openai.com/v1";
        
        public OpenAIProvider(
            HttpClient httpClient,
            LLMProviderConfig config,
            ILogger<OpenAIProvider> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri(_config.ApiEndpoint ?? DefaultEndpoint);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }
        
        public async Task<Result<float[]>> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var request = new
                {
                    input = text,
                    model = "text-embedding-ada-002"
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync("/embeddings", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI embedding error: {Error}", error);
                    return Result.Failure<float[]>($"OpenAI API error: {response.StatusCode}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseJson);
                
                if (result?.Data == null || result.Data.Count == 0)
                {
                    return Result.Failure<float[]>("No embedding returned from OpenAI");
                }
                
                return Result.Success(result.Data[0].Embedding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OpenAI embedding");
                return Result.Failure<float[]>("Failed to generate embedding");
            }
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
                
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    apiMessages.Add(new { role = "system", content = systemPrompt });
                }
                
                foreach (var msg in messages)
                {
                    apiMessages.Add(new { role = msg.Role, content = msg.Content });
                }
                
                var request = new
                {
                    model = _config.ModelName,
                    messages = apiMessages,
                    temperature = temperature,
                    max_tokens = maxTokens
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                var response = await _httpClient.PostAsync("/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI chat completion error: {Error}", error);
                    return Result.Failure<string>($"OpenAI API error: {response.StatusCode}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);
                
                if (result?.Choices == null || result.Choices.Count == 0)
                {
                    return Result.Failure<string>("No response from OpenAI");
                }
                
                return Result.Success(result.Choices[0].Message.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OpenAI chat completion");
                return Result.Failure<string>("Failed to generate response");
            }
        }
        
        public int CountTokens(string text)
        {
            // Rough approximation: 1 token ≈ 4 characters
            // For production, use tiktoken library
            return (int)Math.Ceiling(text.Length / 4.0);
        }
        
        #region Response Models
        
        private class OpenAIEmbeddingResponse
        {
            public List<EmbeddingData> Data { get; set; } = new();
        }
        
        private class EmbeddingData
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
        
        private class OpenAIChatResponse
        {
            public List<Choice> Choices { get; set; } = new();
        }
        
        private class Choice
        {
            public Message Message { get; set; } = new();
        }
        
        private class Message
        {
            public string Content { get; set; } = string.Empty;
        }
        
        #endregion
    }
}
