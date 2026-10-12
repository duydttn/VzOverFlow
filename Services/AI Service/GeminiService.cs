using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VzOverFlow.Services.AIService
{
    public interface IGeminiService
    {
        Task<float[]> GetEmbeddingAsync(string text);
    }

    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAiConfigService _aiConfigService;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent";

        public GeminiService(HttpClient httpClient, IAiConfigService aiConfigService)
        {
            _httpClient = httpClient;
            _aiConfigService = aiConfigService;
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var config = await _aiConfigService.GetActiveConfigByProviderAsync("Gemini");
            if (config == null || string.IsNullOrEmpty(config.ApiKey))
            {
                return Array.Empty<float>();
            }

            var requestBody = new
            {
                content = new
                {
                    parts = new[]
                    {
                        new { text = text }
                    }
                }
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}?key={config.ApiKey}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<float>();
            }

            await _aiConfigService.MarkUsedAsync(config.Id);

            var responseString = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseString);

            return geminiResponse?.Embedding?.Values ?? Array.Empty<float>();
        }

        private class GeminiEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public EmbeddingContent? Embedding { get; set; }
        }

        private class EmbeddingContent
        {
            [JsonPropertyName("values")]
            public float[]? Values { get; set; }
        }
    }
}