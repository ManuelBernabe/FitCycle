using Microsoft.Extensions.Logging;

namespace FitCycle.Infrastructure.Services;

public class OpenRouterSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "meta-llama/llama-3.3-70b-instruct:free";
    public string SiteUrl { get; set; } = "https://fitcycle.app";
    public string SiteName { get; set; } = "FitCycle";
}

public class OpenRouterAiService : OpenAiCompatibleService
{
    private readonly string _defaultModel;
    private readonly string _siteUrl;
    private readonly string _siteName;

    public OpenRouterAiService(OpenRouterSettings settings, ILogger<OpenRouterAiService> logger)
        : base(ResolveKey(settings), logger)
    {
        _defaultModel = settings.Model;
        _siteUrl = settings.SiteUrl;
        _siteName = settings.SiteName;
    }

    private static string ResolveKey(OpenRouterSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ApiKey)) return settings.ApiKey;
        return Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? Environment.GetEnvironmentVariable("OpenRouter__ApiKey")
            ?? "";
    }

    public override bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    public override string ProviderName => "OpenRouter";
    protected override string BaseUrl => "https://openrouter.ai/api/v1";
    protected override string DefaultModel => _defaultModel;

    protected override void ConfigureHeaders(HttpRequestMessage request)
    {
        // Optional but recommended by OpenRouter for analytics/leaderboard
        request.Headers.TryAddWithoutValidation("HTTP-Referer", _siteUrl);
        request.Headers.TryAddWithoutValidation("X-Title", _siteName);
    }
}
