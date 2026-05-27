using System.Text.Json;
using FitCycle.Infrastructure.Repositories;

namespace FitCycle.Core.Tests;

public class SetDetailsNormalizationTests
{
    [Fact]
    public void Valid_Json_SetDetails_Drives_Sets_Reps_Weight()
    {
        var json = "[{\"reps\":12,\"weight\":40,\"tempoPos\":2,\"tempoNeg\":3,\"grip\":\"\"},{\"reps\":10,\"weight\":42.5,\"tempoPos\":0,\"tempoNeg\":0,\"grip\":\"\"},{\"reps\":8,\"weight\":45,\"tempoPos\":0,\"tempoNeg\":0,\"grip\":\"\"}]";

        var (outJson, sets, reps, weight) =
            SqliteRoutineRepository.NormalizeSetDetails(json, legacySets: 1, legacyReps: 99, legacyWeight: 999m);

        Assert.Equal(3, sets); // Derived from setDetails.length, NOT legacySets
        Assert.Equal(12, reps); // First set's reps
        Assert.Equal(45m, weight); // Max weight across sets
        Assert.Contains("\"reps\":12", outJson);
    }

    [Fact]
    public void Empty_Json_Falls_Back_To_Scalars()
    {
        var (json, sets, reps, weight) =
            SqliteRoutineRepository.NormalizeSetDetails("", legacySets: 4, legacyReps: 10, legacyWeight: 50m);

        Assert.Equal(4, sets);
        Assert.Equal(10, reps);
        Assert.Equal(50m, weight);
        // Synthesized JSON must be a valid array of 4 entries the frontend can read back.
        var parsed = JsonSerializer.Deserialize<List<SqliteRoutineRepository.SetEntry>>(json)!;
        Assert.Equal(4, parsed.Count);
        Assert.All(parsed, s => Assert.Equal(10, s.reps));
        Assert.All(parsed, s => Assert.Equal(50m, s.weight));
    }

    [Fact]
    public void Malformed_Json_Falls_Back_Instead_Of_Throwing()
    {
        var (json, sets, _, _) =
            SqliteRoutineRepository.NormalizeSetDetails("not-json{{{", legacySets: 3, legacyReps: 12, legacyWeight: 0m);

        Assert.Equal(3, sets);
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public void Sets_Default_To_Three_When_Both_Json_And_Scalar_Are_Empty()
    {
        var (json, sets, reps, weight) =
            SqliteRoutineRepository.NormalizeSetDetails(null, legacySets: 0, legacyReps: 0, legacyWeight: 0m);

        Assert.Equal(3, sets);
        Assert.Equal(12, reps);
        Assert.Equal(0m, weight);
        Assert.Contains("\"reps\":12", json);
    }

    [Fact]
    public void Negative_Or_Zero_Values_Inside_Json_Are_Sanitized()
    {
        var json = "[{\"reps\":0,\"weight\":-5,\"tempoPos\":-1,\"tempoNeg\":-2,\"grip\":null}]";
        var (outJson, sets, reps, weight) =
            SqliteRoutineRepository.NormalizeSetDetails(json, 0, 0, 0);

        Assert.Equal(1, sets);
        Assert.Equal(12, reps); // 0 reps got bumped to default 12
        Assert.Equal(0m, weight); // Negative weight clamped to 0
        Assert.DoesNotContain("\"reps\":0", outJson);
        Assert.DoesNotContain("\"weight\":-", outJson);
    }

    [Fact]
    public void Excessive_Sets_Are_Clamped()
    {
        var (_, sets, _, _) =
            SqliteRoutineRepository.NormalizeSetDetails(null, legacySets: 9999, legacyReps: 12, legacyWeight: 0m);
        Assert.True(sets <= 30, $"Expected sets <= 30, got {sets}");
    }
}
