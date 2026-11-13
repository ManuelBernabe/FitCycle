using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FitCycle.Api.Tests;

/// <summary>
/// E2E tests for the Apple Watch remote control: the phone publishes its live workout
/// (PUT /watch/session), the watch's Shortcut drives it with a personal token
/// (/watch/cmd/{action}?k=...), and the phone picks the changes up (GET /watch/session).
/// </summary>
public class WatchControlE2ETests : IClassFixture<FitCycleApiFactory>
{
    private readonly FitCycleApiFactory _factory;

    public WatchControlE2ETests(FitCycleApiFactory factory) => _factory = factory;

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

    private static async Task<string> NewWatchToken(HttpClient client)
    {
        var res = await client.PostAsync("/me/watch/token", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    // Bench press 2 sets → Row 1 set. Cursor on the first set.
    private static object Snapshot(string key, int cursor, long baseRevision, decimal benchWeight = 60m) => new
    {
        sessionKey = key,
        dayNum = 1,
        cursor,
        restSeconds = 90,
        baseRevision,
        steps = new object[]
        {
            new { ex = 0, set = 0, name = "Press banca", setNo = 1, totalSets = 2, reps = 10, weight = benchWeight, lastOfExercise = false },
            new { ex = 0, set = 1, name = "Press banca", setNo = 2, totalSets = 2, reps = 8, weight = benchWeight, lastOfExercise = true },
            new { ex = 1, set = 0, name = "Remo", setNo = 1, totalSets = 1, reps = 12, weight = 40m, lastOfExercise = true },
        },
    };

    private static async Task<JsonElement> GetView(HttpClient client)
    {
        var res = await client.GetAsync("/watch/session");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("view").Clone();
    }

    [Fact]
    public async Task Watch_Drives_Session_And_Phone_Catches_Up()
    {
        var phone = await LoginAsync();
        var token = await NewWatchToken(phone);
        var watch = _factory.CreateClient();

        var put = await phone.PutAsJsonAsync("/watch/session", Snapshot("s1", 0, 0));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        // Status: plain text with exercise + set line (Spanish decimal comma)
        var text = await watch.GetStringAsync($"/watch/cmd/status?k={token}");
        Assert.Contains("Press banca", text);
        Assert.Contains("Serie 1/2 · 10 × 60 kg", text);

        // +2.5 kg → 62,5
        text = await watch.GetStringAsync($"/watch/cmd/plus?k={token}");
        Assert.Contains("62,5 kg", text);

        // Set done → rest between sets of the same exercise
        text = await watch.GetStringAsync($"/watch/cmd/next?k={token}");
        Assert.Contains("Descansa 1:30", text);
        Assert.Contains("Serie 2/2", text);

        var view = await GetView(phone);
        Assert.True(view.GetProperty("pending").GetBoolean());
        Assert.Equal(0, view.GetProperty("ex").GetInt32());
        Assert.Equal(1, view.GetProperty("set").GetInt32());
        Assert.True(view.GetProperty("restRemaining").GetInt32() > 80);
        var edit = Assert.Single(view.GetProperty("weightEdits").EnumerateArray());
        Assert.Equal(0, edit.GetProperty("set").GetInt32());
        Assert.Equal(62.5m, edit.GetProperty("weight").GetDecimal());
        var revision = view.GetProperty("revision").GetInt64();

        // A phone publish built on an OLD revision is rejected while watch changes are pending
        var stale = await phone.PutAsJsonAsync("/watch/session", Snapshot("s1", 0, revision - 1));
        using (var doc = JsonDocument.Parse(await stale.Content.ReadAsStringAsync()))
            Assert.False(doc.RootElement.GetProperty("accepted").GetBoolean());

        // After applying, the phone republishes on the current revision → accepted, pending cleared
        var ok = await phone.PutAsJsonAsync("/watch/session", Snapshot("s1", 1, revision, 62.5m));
        using (var doc = JsonDocument.Parse(await ok.Content.ReadAsStringAsync()))
            Assert.True(doc.RootElement.GetProperty("accepted").GetBoolean());
        view = await GetView(phone);
        Assert.False(view.GetProperty("pending").GetBoolean());
        Assert.Empty(view.GetProperty("weightEdits").EnumerateArray());

        // Completing the exercise's LAST set → next exercise, no rest (same rule as the phone)
        text = await watch.GetStringAsync($"/watch/cmd/next?k={token}");
        Assert.DoesNotContain("Descansa", text);
        Assert.Contains("Remo", text);

        // Next on the final step → finished message; prev undoes it
        text = await watch.GetStringAsync($"/watch/cmd/next?k={token}");
        Assert.Contains("Finalizar", text);
        Assert.True((await GetView(phone)).GetProperty("finished").GetBoolean());
        text = await watch.GetStringAsync($"/watch/cmd/prev?k={token}");
        Assert.Contains("Remo", text);

        // JSON format for richer shortcuts
        var json = await watch.GetStringAsync($"/watch/cmd/status?k={token}&format=json");
        using (var doc = JsonDocument.Parse(json))
        {
            Assert.Equal("Remo", doc.RootElement.GetProperty("exercise").GetString());
            Assert.True(doc.RootElement.GetProperty("hasSession").GetBoolean());
        }

        // Manual rest from the watch (no advance) → JSON carries the seconds for the watch timer
        json = await watch.GetStringAsync($"/watch/cmd/rest?k={token}&format=json");
        using (var doc = JsonDocument.Parse(json))
        {
            Assert.Equal(90, doc.RootElement.GetProperty("rest").GetInt32());
            Assert.Equal("Remo", doc.RootElement.GetProperty("exercise").GetString());
        }
        view = await GetView(phone);
        Assert.True(view.GetProperty("restRemaining").GetInt32() > 80);

        // Rest started on the PHONE is reported by its publish → "status" hands the
        // remaining seconds to the watch timer
        revision = view.GetProperty("revision").GetInt64();
        var withRest = await phone.PutAsJsonAsync("/watch/session", new
        {
            sessionKey = "s1", dayNum = 1, cursor = 2, restSeconds = 90, baseRevision = revision, restRemaining = 45,
            steps = new object[]
            {
                new { ex = 0, set = 0, name = "Press banca", setNo = 1, totalSets = 2, reps = 10, weight = 62.5m, lastOfExercise = false },
                new { ex = 0, set = 1, name = "Press banca", setNo = 2, totalSets = 2, reps = 8, weight = 60m, lastOfExercise = true },
                new { ex = 1, set = 0, name = "Remo", setNo = 1, totalSets = 1, reps = 12, weight = 40m, lastOfExercise = true },
            },
        });
        using (var doc = JsonDocument.Parse(await withRest.Content.ReadAsStringAsync()))
            Assert.True(doc.RootElement.GetProperty("accepted").GetBoolean());
        json = await watch.GetStringAsync($"/watch/cmd/status?k={token}&format=json");
        using (var doc = JsonDocument.Parse(json))
        {
            var rest = doc.RootElement.GetProperty("rest").GetInt32();
            Assert.InRange(rest, 40, 45);
            Assert.Contains("Descansa 0:4", doc.RootElement.GetProperty("text").GetString());
        }

        // Finishing on the phone ends the session
        await phone.DeleteAsync("/watch/session");
        text = await watch.GetStringAsync($"/watch/cmd/status?k={token}");
        Assert.Contains("No hay ningún entreno", text);
    }

    [Fact]
    public async Task Invalid_Or_Revoked_Token_Is_Rejected()
    {
        var phone = await LoginAsync();
        var watch = _factory.CreateClient();

        var res = await watch.GetAsync("/watch/cmd/status?k=not-a-real-token");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        res = await watch.GetAsync("/watch/cmd/status");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);

        var oldToken = await NewWatchToken(phone);
        var newToken = await NewWatchToken(phone); // regenerating revokes the old one
        Assert.Equal(HttpStatusCode.Unauthorized, (await watch.GetAsync($"/watch/cmd/status?k={oldToken}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await watch.GetAsync($"/watch/cmd/status?k={newToken}")).StatusCode);

        // Header auth works too
        var req = new HttpRequestMessage(HttpMethod.Post, "/watch/cmd/status");
        req.Headers.Add("X-Watch-Token", newToken);
        Assert.Equal(HttpStatusCode.OK, (await watch.SendAsync(req)).StatusCode);

        // Status endpoint reflects it; delete disables
        using (var doc = JsonDocument.Parse(await phone.GetStringAsync("/me/watch")))
            Assert.True(doc.RootElement.GetProperty("enabled").GetBoolean());
        await phone.DeleteAsync("/me/watch/token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await watch.GetAsync($"/watch/cmd/status?k={newToken}")).StatusCode);
        using (var doc = JsonDocument.Parse(await phone.GetStringAsync("/me/watch")))
            Assert.False(doc.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task Watch_Endpoints_Require_Jwt_For_Phone_Side()
    {
        var anon = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/watch/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsync("/me/watch/token", null)).StatusCode);
    }
}
