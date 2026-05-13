using FitCycle.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitCycle.Core.Tests;

public class AiFallbackTests
{
    private class FakeAi : IAiService
    {
        public string ProviderName { get; }
        public bool IsConfigured { get; }
        private readonly string? _text;
        private readonly string? _error;
        public int CallCount;

        public FakeAi(string name, bool configured, string? text, string? error)
        {
            ProviderName = name;
            IsConfigured = configured;
            _text = text;
            _error = error;
        }

        public Task<(string? Text, string? Error)> GenerateContentAsync(string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096)
        {
            CallCount++;
            return Task.FromResult((_text, _error));
        }

        public Task<(T? Result, string? Error)> GenerateStructuredAsync<T>(string prompt, string? model = null, double temperature = 0.1, int maxOutputTokens = 4096) where T : class
            => Task.FromResult<(T?, string?)>((null, _error));
    }

    private static AiServiceWithFallback Create(params IAiService[] providers)
        => new(providers, NullLogger<AiServiceWithFallback>.Instance);

    [Fact]
    public async Task Returns_Error_When_No_Provider_Configured()
    {
        var ai = Create(new FakeAi("A", configured: false, text: null, error: null));
        var (text, error) = await ai.GenerateContentAsync("test");
        Assert.Null(text);
        Assert.Contains("No AI providers", error);
    }

    [Fact]
    public async Task Uses_First_Configured_Provider_When_It_Succeeds()
    {
        var p1 = new FakeAi("P1", true, "result-1", null);
        var p2 = new FakeAi("P2", true, "result-2", null);
        var ai = Create(p1, p2);
        var (text, error) = await ai.GenerateContentAsync("test");
        Assert.Equal("result-1", text);
        Assert.Null(error);
        Assert.Equal(1, p1.CallCount);
        Assert.Equal(0, p2.CallCount);
    }

    [Fact]
    public async Task Falls_Back_To_Second_Provider_When_First_Fails()
    {
        var p1 = new FakeAi("P1", true, null, "rate limited");
        var p2 = new FakeAi("P2", true, "rescued", null);
        var ai = Create(p1, p2);
        var (text, error) = await ai.GenerateContentAsync("test");
        Assert.Equal("rescued", text);
        Assert.Null(error);
        Assert.Equal(1, p1.CallCount);
        Assert.Equal(1, p2.CallCount);
    }

    [Fact]
    public async Task Skips_Unconfigured_Providers()
    {
        var p1 = new FakeAi("P1", false, "should-not-call", null);
        var p2 = new FakeAi("P2", true, "ok", null);
        var ai = Create(p1, p2);
        var (text, _) = await ai.GenerateContentAsync("test");
        Assert.Equal("ok", text);
        Assert.Equal(0, p1.CallCount);
        Assert.Equal(1, p2.CallCount);
    }

    [Fact]
    public async Task Returns_Error_When_All_Providers_Fail()
    {
        var p1 = new FakeAi("P1", true, null, "boom");
        var p2 = new FakeAi("P2", true, null, "kaput");
        var ai = Create(p1, p2);
        var (text, error) = await ai.GenerateContentAsync("test");
        Assert.Null(text);
        Assert.Contains("All AI providers failed", error);
        Assert.Contains("kaput", error);
    }

    [Fact]
    public void ProviderName_Lists_Only_Configured_Providers()
    {
        var ai = Create(
            new FakeAi("Groq", true, null, null),
            new FakeAi("OpenRouter", false, null, null),
            new FakeAi("Gemini", true, null, null));
        Assert.Equal("Groq → Gemini", ai.ProviderName);
    }
}
