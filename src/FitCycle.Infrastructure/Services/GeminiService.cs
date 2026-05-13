using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitCycle.Infrastructure.Services;

public class GeminiService : IAiService
{
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public GeminiService(IOptions<GeminiSettings> settings, ILogger<GeminiService> logger)
    {
        _apiKey = settings.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(_apiKey))
            _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                   ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                   ?? "";
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);
    public string ProviderName => "Gemini";

    public async Task<(string? Text, string? Error)> GenerateContentAsync(
        string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096)
    {
        if (!IsConfigured)
            return (null, "Gemini API key not configured");

        model ??= "gemini-2.0-flash-lite";

        var requestBody = new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt } } } },
            generationConfig = new { temperature, maxOutputTokens }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                var delay = attempt * 5;
                _logger.LogInformation("Retrying Gemini API call in {Delay}s (attempt {Attempt}/3)", delay, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _http.SendAsync(httpRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Gemini API 429 (attempt {Attempt}/3)", attempt + 1);
                    if (attempt < 2) continue;
                    return (null, "Gemini 429: rate limited");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, responseText);
                    return (null, $"Gemini error {response.StatusCode}");
                }

                var text = ExtractTextFromResponse(responseText);
                return text != null ? (text, null) : (null, "Gemini returned no text content");
            }
            catch (TaskCanceledException)
            {
                return (null, "Gemini timeout (>3 min)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini API exception");
                return (null, $"Gemini exception: {ex.Message}");
            }
        }

        return (null, "Gemini: max retries exceeded");
    }

    public async Task<(T? Result, string? Error)> GenerateStructuredAsync<T>(
        string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096) where T : class
    {
        var (text, error) = await GenerateContentAsync(prompt, model, temperature, maxOutputTokens);
        if (error != null) return (null, error);
        if (string.IsNullOrWhiteSpace(text)) return (null, "Empty response from Gemini");

        try
        {
            var result = JsonSerializer.Deserialize<T>(text, _jsonOpts);
            return (result, null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Gemini response: {Text}", text[..Math.Min(text.Length, 200)]);
            return (null, $"Invalid JSON from Gemini: {ex.Message}");
        }
    }

    private static string? ExtractTextFromResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var candidates = doc.RootElement.GetProperty("candidates");
        foreach (var candidate in candidates.EnumerateArray())
        {
            var content = candidate.GetProperty("content");
            var parts = content.GetProperty("parts");
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textProp))
                {
                    var text = textProp.GetString() ?? "";
                    return OpenAiCompatibleService.CleanJsonResponse(text);
                }
            }
        }
        return null;
    }
}
