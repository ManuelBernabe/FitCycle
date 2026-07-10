using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FitCycle.Api.Tests;

/// <summary>
/// End-to-end regression tests for the "weights reset to 0 after finishing a workout"
/// bug: they drive the real HTTP pipeline exactly like the PWA does — login, POST
/// /workouts with the same payload shape finishWorkout() builds, then GET
/// /workouts/last-weights/{day} the way the workout page prefill does on re-entry.
/// </summary>
public class WorkoutWeightsE2ETests : IClassFixture<FitCycleApiFactory>
{
    private readonly FitCycleApiFactory _factory;

    public WorkoutWeightsE2ETests(FitCycleApiFactory factory) => _factory = factory;

    private async Task<HttpClient> LoginAsync()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login",
            new { username = "admin", password = "Admin123!" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Builds one exercise entry with the exact shape finishWorkout() sends.</summary>
    private static object BuildExercise(int exerciseId, string name, string muscleGroup,
        params (int reps, decimal weight)[] sets)
    {
        var setDetails = sets
            .Select(s => new { reps = s.reps, weight = s.weight, tempoPos = 0, tempoNeg = 0, grip = "" })
            .ToArray();
        return new
        {
            exerciseId,
            exerciseName = name,
            sets = sets.Length,
            reps = sets[0].reps,
            weight = sets.Max(s => s.weight),
            muscleGroupName = muscleGroup,
            setDetails = JsonSerializer.Serialize(setDetails),
        };
    }

    private static object BuildWorkout(int day, DateTime completedAt, params object[] exercises) => new
    {
        day,
        startedAt = completedAt.AddMinutes(-50).ToString("o"),
        completedAt = completedAt.ToString("o"),
        exercises,
    };

    private static async Task<JsonDocument> GetLastWeights(HttpClient client, int day)
    {
        var res = await client.GetAsync($"/workouts/last-weights/{day}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    }

    private static JsonElement FindExercise(JsonDocument lastWeights, int exerciseId)
    {
        foreach (var ex in lastWeights.RootElement.GetProperty("exercises").EnumerateArray())
            if (ex.GetProperty("exerciseId").GetInt32() == exerciseId)
                return ex;
        throw new Xunit.Sdk.XunitException($"exerciseId {exerciseId} not found in last-weights response");
    }

    private static decimal[] SetWeights(JsonElement exercise)
    {
        var raw = exercise.GetProperty("setDetails").GetString();
        Assert.False(string.IsNullOrEmpty(raw));
        using var doc = JsonDocument.Parse(raw!);
        return doc.RootElement.EnumerateArray()
            .Select(s => s.GetProperty("weight").GetDecimal())
            .ToArray();
    }

    [Fact]
    public async Task Piernas_FinishWorkout_ThenReenter_EveryTypedWeightComesBack()
    {
        var client = await LoginAsync();
        const int day = 1; // Lunes = Piernas

        // Sentadilla with progressive decimal loads, Prensa flat, Zancadas with a
        // 0.5 kg microplate and one set the user skipped (weight 0).
        var post = await client.PostAsJsonAsync("/workouts", BuildWorkout(day,
            new DateTime(2026, 7, 6, 18, 30, 0, DateTimeKind.Utc),
            BuildExercise(21, "Sentadilla", "Piernas", (12, 80m), (10, 85.5m), (8, 90m)),
            BuildExercise(22, "Prensa", "Piernas", (15, 120.5m), (15, 120.5m), (15, 120.5m), (15, 120.5m)),
            BuildExercise(25, "Zancadas", "Piernas", (20, 0.5m), (20, 0m))));
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        // Re-enter the day — this is the exact call the workout page prefill makes.
        using var lastWeights = await GetLastWeights(client, day);

        var sentadilla = FindExercise(lastWeights, 21);
        Assert.Equal(90m, sentadilla.GetProperty("weight").GetDecimal());
        Assert.Equal(new[] { 80m, 85.5m, 90m }, SetWeights(sentadilla));

        var prensa = FindExercise(lastWeights, 22);
        Assert.Equal(new[] { 120.5m, 120.5m, 120.5m, 120.5m }, SetWeights(prensa));

        var zancadas = FindExercise(lastWeights, 25);
        Assert.Equal(new[] { 0.5m, 0m }, SetWeights(zancadas));

        // A second re-entry (the user backing out and coming back) must be identical.
        using var again = await GetLastWeights(client, day);
        Assert.Equal(new[] { 80m, 85.5m, 90m }, SetWeights(FindExercise(again, 21)));
    }

    [Fact]
    public async Task LastWeights_ReturnsTheMostRecentSessionOfTheDay()
    {
        var client = await LoginAsync();
        const int day = 2;

        var first = await client.PostAsJsonAsync("/workouts", BuildWorkout(day,
            new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc),
            BuildExercise(23, "Extensión de cuádriceps", "Piernas", (15, 40m), (15, 40m), (15, 40m))));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/workouts", BuildWorkout(day,
            new DateTime(2026, 7, 7, 18, 0, 0, DateTimeKind.Utc),
            BuildExercise(23, "Extensión de cuádriceps", "Piernas", (15, 45m), (15, 47.5m), (15, 50m))));
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        using var lastWeights = await GetLastWeights(client, day);
        Assert.Equal(new[] { 45m, 47.5m, 50m }, SetWeights(FindExercise(lastWeights, 23)));
    }

    [Fact]
    public async Task FractionalWeights_SurviveTheFullRoundtripExactly()
    {
        var client = await LoginAsync();
        const int day = 3;

        // Every microplate increment the gym has — these must come back bit-exact,
        // not truncated (the es-ES "0,5" → 0 truncation was a real historical bug).
        var post = await client.PostAsJsonAsync("/workouts", BuildWorkout(day,
            new DateTime(2026, 7, 8, 19, 0, 0, DateTimeKind.Utc),
            BuildExercise(24, "Curl femoral", "Piernas", (12, 0.5m), (12, 1.25m), (12, 2.5m), (12, 12.5m))));
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        using var lastWeights = await GetLastWeights(client, day);
        Assert.Equal(new[] { 0.5m, 1.25m, 2.5m, 12.5m }, SetWeights(FindExercise(lastWeights, 24)));
    }

    [Fact]
    public async Task SaveWorkout_ReportsPr_OnlyWhenWeightBeatsHistory()
    {
        var client = await LoginAsync();
        const int day = 5;

        static async Task<JsonElement> PostAndGetPrs(HttpClient c, object payload)
        {
            var res = await c.PostAsJsonAsync("/workouts", payload);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("prs").Clone();
        }

        var prs1 = await PostAndGetPrs(client, BuildWorkout(day,
            new DateTime(2026, 6, 26, 18, 0, 0, DateTimeKind.Utc),
            BuildExercise(30, "Hip thrust", "Glúteos", (10, 30m))));
        Assert.Contains(prs1.EnumerateArray(), p => p.GetProperty("exerciseId").GetInt32() == 30);

        var prs2 = await PostAndGetPrs(client, BuildWorkout(day,
            new DateTime(2026, 7, 3, 18, 0, 0, DateTimeKind.Utc),
            BuildExercise(30, "Hip thrust", "Glúteos", (10, 35m))));
        var pr = Assert.Single(prs2.EnumerateArray().Where(p => p.GetProperty("exerciseId").GetInt32() == 30));
        Assert.Equal(30m, pr.GetProperty("previousMax").GetDecimal());
        Assert.Equal(35m, pr.GetProperty("newMax").GetDecimal());

        var prs3 = await PostAndGetPrs(client, BuildWorkout(day,
            new DateTime(2026, 7, 9, 18, 0, 0, DateTimeKind.Utc),
            BuildExercise(30, "Hip thrust", "Glúteos", (10, 20m))));
        Assert.DoesNotContain(prs3.EnumerateArray(), p => p.GetProperty("exerciseId").GetInt32() == 30);
    }

    [Fact]
    public async Task LastWeights_ForADayNeverTrained_ReturnsEmptyExercises()
    {
        var client = await LoginAsync();
        using var lastWeights = await GetLastWeights(client, 6);
        Assert.Empty(lastWeights.RootElement.GetProperty("exercises").EnumerateArray());
    }
}
