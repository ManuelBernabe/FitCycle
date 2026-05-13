using Microsoft.Extensions.Logging;

namespace FitCycle.Infrastructure.Services;

public class GroqSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "llama-3.3-70b-versatile";
}

public class GroqAiService : OpenAiCompatibleService
{
    private readonly string _defaultModel;

    public GroqAiService(GroqSettings settings, ILogger<GroqAiService> logger)
        : base(ResolveKey(settings), logger)
    {
        _defaultModel = settings.Model;
    }

    private static string ResolveKey(GroqSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ApiKey)) return settings.ApiKey;
        return Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? Environment.GetEnvironmentVariable("Groq__ApiKey")
            ?? "";
    }

    public override bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    public override string ProviderName => "Groq";
    protected override string BaseUrl => "https://api.groq.com/openai/v1";
    protected override string DefaultModel => _defaultModel;
}
