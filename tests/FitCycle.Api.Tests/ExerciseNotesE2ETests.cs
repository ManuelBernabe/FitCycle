using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FitCycle.Api.Tests;

/// <summary>
/// E2E tests for the personal per-exercise notes: the workout page loads them all at
/// mount (GET /exercise-notes) and upserts/deletes via PUT /exercise-notes exactly as
/// these tests do. Notes are keyed by exercise NAME so they survive PDF re-imports.
/// </summary>
public class ExerciseNotesE2ETests : IClassFixture<FitCycleApiFactory>
{
    private readonly FitCycleApiFactory _factory;

    public ExerciseNotesE2ETests(FitCycleApiFactory factory) => _factory = factory;

    private async Task<HttpClient> LoginAsync()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login",
            new { username = "admin", password = "Admin123!" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<Dictionary<string, string>> GetNotes(HttpClient client)
    {
        var res = await client.GetAsync("/exercise-notes");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray().ToDictionary(
            n => n.GetProperty("exerciseName").GetString()!,
            n => n.GetProperty("note").GetString()!);
    }

    [Fact]
    public async Task Note_Upsert_Update_And_DeleteOnEmpty_Roundtrip()
    {
        var client = await LoginAsync();

        // Create
        var put = await client.PutAsJsonAsync("/exercise-notes",
            new { exerciseName = "Hip Thrust Barra Olímpica", note = "Banco en posición 3, pad grueso" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var notes = await GetNotes(client);
        Assert.Equal("Banco en posición 3, pad grueso", notes["Hip Thrust Barra Olímpica"]);

        // Update — case-insensitive name match must hit the SAME row, not create a duplicate.
        put = await client.PutAsJsonAsync("/exercise-notes",
            new { exerciseName = "hip thrust barra olímpica", note = "Subir a pad doble" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        notes = await GetNotes(client);
        var match = notes.Where(kv => kv.Key.Equals("Hip Thrust Barra Olímpica", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(match);
        Assert.Equal("Subir a pad doble", match[0].Value);

        // Empty note deletes the row
        put = await client.PutAsJsonAsync("/exercise-notes",
            new { exerciseName = "Hip Thrust Barra Olímpica", note = "" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        notes = await GetNotes(client);
        Assert.DoesNotContain(notes.Keys, k => k.Equals("Hip Thrust Barra Olímpica", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Note_WithoutExerciseName_IsRejected()
    {
        var client = await LoginAsync();
        var put = await client.PutAsJsonAsync("/exercise-notes", new { exerciseName = "  ", note = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task Notes_RequireAuthentication()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/exercise-notes");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
