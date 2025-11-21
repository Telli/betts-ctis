using BettsTax.Shared;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// Google Gemini LLM provider implementation
    /// Supports Gemini Pro and Gemini Pro Vision
    /// </summary>
    public class GeminiProvider : ILLMProvider
    {
        private readonly HttpClient _httpClient;
        private readonly LLMProviderConfig _config;
        private readonly ILogger<GeminiProvider> _logger;
        private const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta";
        
        public GeminiProvider(
            HttpClient httpClient,
            LLMProviderConfig config,
            ILogger<GeminiProvider> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            
            _httpClient.BaseAddress = new Uri(_config.ApiEndpoint ?? DefaultEndpoint);
        }
        
        public async Task<Result<float[]>> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var request = new
                {
                    model = "models/embedding-001",
                    content = new { parts = new[] { new { text } } }
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                var url = $"/models/embedding-001:embedContent?key={_config.ApiKey}";
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini embedding error: {Error}", error);
                    return Result.Failure<float[]>($"Gemini API error: {response.StatusCode}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseJson);
                
                if (result?.Embedding?.Values == null)
                {
                    return Result.Failure<float[]>("No embedding returned from Gemini");
                }
                
                return Result.Success(result.Embedding.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Gemini embedding");
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
                var contents = new List<object>();
                
                // Add system prompt as first user message if provided
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    contents.Add(new
                    {
                        role = "user",
                        parts = new[] { new { text = systemPrompt } }
                    });
                    contents.Add(new
                    {
                        role = "model",
                        parts = new[] { new { text = "Understood. I'll follow these instructions." } }
                    });
                }
                
                foreach (var msg in messages)
                {
                    var role = msg.Role == "assistant" ? "model" : "user";
                    contents.Add(new
                    {
                        role,
                        parts = new[] { new { text = msg.Content } }
                    });
                }
                
                var request = new
                {
                    contents,
                    generationConfig = new
                    {
                        temperature,
                        maxOutputTokens = maxTokens
                    }
                };
                
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");
                
                var url = $"/models/{_config.ModelName}:generateContent?key={_config.ApiKey}";
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {Error}", error);
                    return Result.Failure<string>($"Gemini API error: {response.StatusCode}");
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GeminiResponse>(responseJson);
                
                if (result?.Candidates == null || result.Candidates.Count == 0)
                {
                    return Result.Failure<string>("No response from Gemini");
                }
                
                var textPart = result.Candidates[0].Content.Parts.FirstOrDefault();
                if (textPart == null)
                {
                    return Result.Failure<string>("Empty response from Gemini");
                }
                
                return Result.Success(textPart.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Gemini chat completion");
                return Result.Failure<string>("Failed to generate response");
            }
        }
        
        public int CountTokens(string text)
        {
            // Rough approximation: 1 token ≈ 4 characters
            return (int)Math.Ceiling(text.Length / 4.0);
        }
        
        #region Response Models
        
        private class GeminiEmbeddingResponse
        {
            public EmbeddingData Embedding { get; set; } = new();
        }
        
        private class EmbeddingData
        {
            public float[] Values { get; set; } = Array.Empty<float>();
        }
        
        private class GeminiResponse
        {
            public List<Candidate> Candidates { get; set; } = new();
        }
        
        private class Candidate
        {
            public Content Content { get; set; } = new();
        }
        
        private class Content
        {
            public List<Part> Parts { get; set; } = new();
        }
        
        private class Part
        {
            public string Text { get; set; } = string.Empty;
        }
        
        #endregion
    }
}
