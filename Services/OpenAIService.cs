using System.Text;
using System.Text.Json;

namespace DaycareAPI.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
            _logger = logger;
            
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> GenerateResponseAsync(string userQuery, string contextData)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "OpenAI API key not configured. Using basic pattern matching instead.";
            }

            var systemPrompt = @"You are an AI assistant for a daycare management system called MiniMinds. 
You help administrators find information about children, activities, meals, attendance, and events.
Always provide helpful, accurate responses based on the provided data.
If no relevant data is provided, politely explain what information you would need.
Keep responses concise but informative.";

            var userPrompt = $@"User Question: {userQuery}

Available Data: {contextData}

Please provide a helpful response based on the available data.";

            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            try
            {
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
                    
                    return openAIResponse?.choices?.FirstOrDefault()?.message?.content ?? 
                           "I couldn't generate a response. Please try again.";
                }
                else
                {
                    _logger.LogError("OpenAI API error: {StatusCode}", response.StatusCode);
                    return "I'm having trouble connecting to the AI service. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                return "I encountered an error processing your request. Please try again.";
            }
        }
    }

    public class OpenAIResponse
    {
        public Choice[]? choices { get; set; }
    }

    public class Choice
    {
        public Message? message { get; set; }
    }

    public class Message
    {
        public string? content { get; set; }
    }
}