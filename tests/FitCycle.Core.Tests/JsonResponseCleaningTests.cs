using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

public class JsonResponseCleaningTests
{
    [Fact]
    public void Strips_Markdown_Fences_With_Json_Tag()
    {
        var raw = "```json\n{\"a\":1}\n```";
        Assert.Equal("{\"a\":1}", OpenAiCompatibleService.CleanJsonResponse(raw));
    }

    [Fact]
    public void Strips_Plain_Markdown_Fences()
    {
        var raw = "```\n{\"a\":1}\n```";
        Assert.Equal("{\"a\":1}", OpenAiCompatibleService.CleanJsonResponse(raw));
    }

    [Fact]
    public void Extracts_Json_From_Prose_Wrapper()
    {
        var raw = "Here is the JSON:\n{\"routines\":[{\"dayOfWeek\":1}]}\nLet me know if you need anything else.";
        var cleaned = OpenAiCompatibleService.CleanJsonResponse(raw);
        Assert.StartsWith("{", cleaned);
        Assert.EndsWith("}", cleaned);
        Assert.DoesNotContain("Here is", cleaned);
        Assert.DoesNotContain("Let me know", cleaned);
    }

    [Fact]
    public void Handles_Nested_Braces_Correctly()
    {
        var raw = "Prefix {\"a\":{\"b\":{\"c\":1}}} suffix";
        Assert.Equal("{\"a\":{\"b\":{\"c\":1}}}", OpenAiCompatibleService.CleanJsonResponse(raw));
    }

    [Fact]
    public void Respects_Braces_Inside_Strings()
    {
        var raw = "{\"name\":\"Press {banca}\",\"sets\":3}";
        Assert.Equal(raw, OpenAiCompatibleService.CleanJsonResponse(raw));
    }

    [Fact]
    public void Handles_Top_Level_Array()
    {
        var raw = "Some prose [{\"x\":1},{\"x\":2}] more prose";
        Assert.Equal("[{\"x\":1},{\"x\":2}]", OpenAiCompatibleService.CleanJsonResponse(raw));
    }

    [Fact]
    public void Returns_Trimmed_Text_When_No_Json_Found()
    {
        var raw = "  Just plain text, no JSON here.  ";
        Assert.Equal("Just plain text, no JSON here.", OpenAiCompatibleService.CleanJsonResponse(raw));
    }

    [Fact]
    public void Handles_Escaped_Quotes_In_Strings()
    {
        var raw = "{\"note\":\"He said \\\"hi\\\"\"}";
        Assert.Equal(raw, OpenAiCompatibleService.CleanJsonResponse(raw));
    }
}
