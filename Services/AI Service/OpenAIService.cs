using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VzOverFlow.Services.AIService
{
    public interface IOpenAIService
    {
        Task<string> GetChatResponseAsync(string prompt, string model = "gpt-3.5-turbo");
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly IAiConfigService _aiConfigService;
        private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAIService(HttpClient httpClient, IAiConfigService aiConfigService)
        {
            _httpClient = httpClient;
            _aiConfigService = aiConfigService;
        }

        public async Task<string> GetChatResponseAsync(string prompt, string model = "gpt-3.5-turbo")
        {
            var config = await _aiConfigService.GetActiveConfigByProviderAsync("OpenAI");
            if (config == null || string.IsNullOrEmpty(config.ApiKey))
            {
                return "OpenAI API Key not configured or active.";
            }

            var requestBody = new
            {
                model = config.ModelName ?? model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

            var response = await _httpClient.PostAsync(BaseUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"Error from OpenAI: {response.StatusCode} - {error}";
            }

            await _aiConfigService.MarkUsedAsync(config.Id);

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
    }
}