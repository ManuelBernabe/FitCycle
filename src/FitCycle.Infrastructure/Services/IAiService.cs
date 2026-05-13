namespace FitCycle.Infrastructure.Services;

/// <summary>
/// Provider-agnostic LLM interface. Implementations: Groq, OpenRouter, Gemini.
/// The model parameter is optional — each implementation has its own default.
/// </summary>
public interface IAiService
{
    /// <summary>Send a prompt and get raw text back. Returns (text, error).</summary>
    Task<(string? Text, string? Error)> GenerateContentAsync(string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096);

    /// <summary>Send a prompt and deserialize the JSON response. Returns (result, error).</summary>
    Task<(T? Result, string? Error)> GenerateStructuredAsync<T>(string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096) where T : class;

    /// <summary>Whether this provider is ready to use (API key configured).</summary>
    bool IsConfigured { get; }

    /// <summary>Display name for logging.</summary>
    string ProviderName { get; }
}
