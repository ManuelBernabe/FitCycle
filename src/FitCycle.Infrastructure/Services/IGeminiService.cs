namespace FitCycle.Infrastructure.Services;

public interface IGeminiService
{
    /// <summary>Send a prompt to Gemini and get raw text back. Returns (text, error).</summary>
    Task<(string? Text, string? Error)> GenerateContentAsync(string prompt, string model = "gemini-2.0-flash-lite", double temperature = 0.1, int maxOutputTokens = 4096);

    /// <summary>Send a prompt and deserialize the JSON response. Returns (result, error).</summary>
    Task<(T? Result, string? Error)> GenerateStructuredAsync<T>(string prompt, string model = "gemini-2.0-flash-lite", double temperature = 0.1, int maxOutputTokens = 4096) where T : class;

    /// <summary>Whether the Gemini API key is configured.</summary>
    bool IsConfigured { get; }
}
