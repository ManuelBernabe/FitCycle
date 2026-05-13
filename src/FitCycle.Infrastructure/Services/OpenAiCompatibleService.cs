using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FitCycle.Infrastructure.Services;

/// <summary>
/// Base class for providers that use the OpenAI Chat Completions API shape
/// (Groq, OpenRouter, Mistral, DeepSeek, etc.).
/// </summary>
public abstract class OpenAiCompatibleService : IAiService
{
    protected readonly string ApiKey;
    protected readonly ILogger Logger;
    protected static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    protected OpenAiCompatibleService(string apiKey, ILogger logger)
    {
        ApiKey = apiKey;
        Logger = logger;
    }

    public abstract bool IsConfigured { get; }
    public abstract string ProviderName { get; }
    protected abstract string BaseUrl { get; }
    protected abstract string DefaultModel { get; }

    /// <summary>Optional extra headers (e.g. OpenRouter requires HTTP-Referer/X-Title).</summary>
    protected virtual void ConfigureHeaders(HttpRequestMessage request) { }

    public async Task<(string? Text, string? Error)> GenerateContentAsync(
        string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096)
    {
        if (!IsConfigured)
            return (null, $"{ProviderName} API key not configured");

        model ??= DefaultModel;

        // Detect if the caller expects a JSON response and turn on the provider's
        // native JSON mode so the model doesn't wrap with prose or markdown.
        var wantsJson = prompt.Contains("JSON", StringComparison.OrdinalIgnoreCase);

        object requestBody = wantsJson
            ? new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature,
                max_tokens = maxOutputTokens,
                response_format = new { type = "json_object" },
            }
            : new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature,
                max_tokens = maxOutputTokens,
            };

        var json = JsonSerializer.Serialize(requestBody);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                var delay = attempt * 5;
                Logger.LogInformation("Retrying {Provider} API call in {Delay}s (attempt {Attempt}/3)", ProviderName, delay, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Add("Authorization", $"Bearer {ApiKey}");
                ConfigureHeaders(httpRequest);

                var response = await Http.SendAsync(httpRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    Logger.LogWarning("{Provider} API 429 (attempt {Attempt}/3)", ProviderName, attempt + 1);
                    if (attempt < 2) continue;
                    return (null, $"{ProviderName} 429: rate limited");
                }

                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("{Provider} API error {Status}: {Body}", ProviderName, response.StatusCode, responseText);
                    return (null, $"{ProviderName} error {response.StatusCode}");
                }

                var text = ExtractTextFromResponse(responseText);
                return text != null ? (text, null) : (null, $"{ProviderName} returned no text content");
            }
            catch (TaskCanceledException)
            {
                return (null, $"{ProviderName} timeout (>3 min)");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{Provider} API exception", ProviderName);
                return (null, $"{ProviderName} exception: {ex.Message}");
            }
        }

        return (null, $"{ProviderName}: max retries exceeded");
    }

    public async Task<(T? Result, string? Error)> GenerateStructuredAsync<T>(
        string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096) where T : class
    {
        var (text, error) = await GenerateContentAsync(prompt, model, temperature, maxOutputTokens);
        if (error != null) return (null, error);
        if (string.IsNullOrWhiteSpace(text)) return (null, $"Empty response from {ProviderName}");

        try
        {
            var result = JsonSerializer.Deserialize<T>(text, JsonOpts);
            return (result, null);
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Failed to deserialize {Provider} response: {Text}", ProviderName, text[..Math.Min(text.Length, 200)]);
            return (null, $"Invalid JSON from {ProviderName}: {ex.Message}");
        }
    }

    private static string? ExtractTextFromResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("choices", out var choices)) return null;
        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var message)) continue;
            if (!message.TryGetProperty("content", out var content)) continue;
            var text = content.GetString() ?? "";
            return CleanJsonResponse(text);
        }
        return null;
    }

    /// <summary>
    /// Strips markdown fences and extracts the largest top-level JSON object/array
    /// from a free-form LLM response. Falls back to the trimmed input.
    /// </summary>
    public static string CleanJsonResponse(string text)
    {
        text = text.Trim();
        // Strip markdown fences
        if (text.StartsWith("```json")) text = text[7..];
        else if (text.StartsWith("```")) text = text[3..];
        if (text.EndsWith("```")) text = text[..^3];
        text = text.Trim();

        // If still wrapped in prose, find first '{' or '[' and matching close
        var firstObj = text.IndexOf('{');
        var firstArr = text.IndexOf('[');
        int firstBracket;
        char openChar, closeChar;
        if (firstObj < 0 && firstArr < 0) return text;
        if (firstObj < 0 || (firstArr >= 0 && firstArr < firstObj))
        { firstBracket = firstArr; openChar = '['; closeChar = ']'; }
        else
        { firstBracket = firstObj; openChar = '{'; closeChar = '}'; }

        if (firstBracket == 0 && text.EndsWith(closeChar.ToString())) return text;

        // Walk the string with a simple bracket counter (respects strings)
        int depth = 0;
        bool inString = false;
        bool escape = false;
        for (int i = firstBracket; i < text.Length; i++)
        {
            var c = text[i];
            if (escape) { escape = false; continue; }
            if (c == '\\') { escape = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;
            if (c == openChar) depth++;
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0) return text.Substring(firstBracket, i - firstBracket + 1);
            }
        }
        // Unbalanced — return slice from first bracket onwards (might still be parseable if truncated)
        return text.Substring(firstBracket);
    }
}
