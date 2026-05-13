using Microsoft.Extensions.Logging;

namespace FitCycle.Infrastructure.Services;

/// <summary>
/// Tries each configured provider in order. If a call fails (returns Error),
/// automatically retries with the next provider. The first provider with
/// IsConfigured=true is the primary; the rest are fallbacks.
/// </summary>
public class AiServiceWithFallback : IAiService
{
    private readonly IReadOnlyList<IAiService> _providers;
    private readonly ILogger<AiServiceWithFallback> _logger;

    public AiServiceWithFallback(IEnumerable<IAiService> providers, ILogger<AiServiceWithFallback> logger)
    {
        _providers = providers.ToList();
        _logger = logger;
    }

    public bool IsConfigured => _providers.Any(p => p.IsConfigured);

    public string ProviderName
    {
        get
        {
            var names = _providers.Where(p => p.IsConfigured).Select(p => p.ProviderName);
            return string.Join(" → ", names);
        }
    }

    public async Task<(string? Text, string? Error)> GenerateContentAsync(
        string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096)
    {
        var configured = _providers.Where(p => p.IsConfigured).ToList();
        if (configured.Count == 0) return (null, "No AI providers configured");

        string? lastError = null;
        foreach (var provider in configured)
        {
            _logger.LogInformation("Trying AI provider: {Provider}", provider.ProviderName);
            // Don't forward Gemini-specific model name to OpenAI-style providers (and vice versa)
            var modelToUse = ShouldForwardModel(provider, model) ? model : null;
            var (text, error) = await provider.GenerateContentAsync(prompt, modelToUse, temperature, maxOutputTokens);
            if (error == null && !string.IsNullOrWhiteSpace(text))
                return (text, null);

            _logger.LogWarning("{Provider} failed: {Error} — trying next provider", provider.ProviderName, error);
            lastError = $"{provider.ProviderName}: {error}";
        }

        return (null, $"All AI providers failed. Last error: {lastError}");
    }

    public async Task<(T? Result, string? Error)> GenerateStructuredAsync<T>(
        string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096) where T : class
    {
        var configured = _providers.Where(p => p.IsConfigured).ToList();
        if (configured.Count == 0) return (null, "No AI providers configured");

        string? lastError = null;
        foreach (var provider in configured)
        {
            _logger.LogInformation("Trying AI provider: {Provider}", provider.ProviderName);
            var modelToUse = ShouldForwardModel(provider, model) ? model : null;
            var (result, error) = await provider.GenerateStructuredAsync<T>(prompt, modelToUse, temperature, maxOutputTokens);
            if (error == null && result != null)
                return (result, null);

            _logger.LogWarning("{Provider} failed: {Error} — trying next provider", provider.ProviderName, error);
            lastError = $"{provider.ProviderName}: {error}";
        }

        return (null, $"All AI providers failed. Last error: {lastError}");
    }

    private static bool ShouldForwardModel(IAiService provider, string? model)
    {
        if (string.IsNullOrWhiteSpace(model)) return false;
        // Only forward model names that match the provider family
        return (provider.ProviderName == "Gemini" && model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase))
            || (provider.ProviderName == "Groq" && (model.Contains("llama", StringComparison.OrdinalIgnoreCase) || model.Contains("mixtral", StringComparison.OrdinalIgnoreCase) || model.Contains("qwen", StringComparison.OrdinalIgnoreCase)))
            || (provider.ProviderName == "OpenRouter" && model.Contains("/"));
    }
}
