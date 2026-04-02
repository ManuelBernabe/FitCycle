using FitCycle.Core.Models;
using System.Text.Json;

namespace FitCycle.Core.Tests;

/// <summary>
/// Tests for the last-weights pre-fill logic that runs on both backend (query)
/// and frontend (merge). Uses plain C# to test the algorithm without needing a DB.
/// </summary>
public class LastWeightsTests
{
    // Simulate the backend query: find latest workout for a day and return exercise weights
    private static List<LastWeightEntry>? GetLastWeights(List<WorkoutSession> allSessions, int userId, DayOfWeek day)
    {
        var lastSession = allSessions
            .Where(w => w.UserId == userId && w.Day == day)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefault();

        if (lastSession is null) return null;

        return lastSession.ExerciseLogs.Select(log => new LastWeightEntry
        {
            ExerciseId = log.ExerciseId,
            Weight = log.Weight,
            Reps = log.Reps,
            Sets = log.Sets,
            SetDetails = log.SetDetails
        }).ToList();
    }

    // Simulate the frontend pre-fill merge logic
    private static void ApplyLastWeights(List<ExerciseState> exercises, List<LastWeightEntry> lastWeights)
    {
        foreach (var lastEx in lastWeights)
        {
            var match = exercises.FirstOrDefault(ex => ex.ExerciseId == lastEx.ExerciseId);
            if (match == null) continue;

            List<SetDetailDto>? lastSetDetails = null;
            try { lastSetDetails = string.IsNullOrEmpty(lastEx.SetDetails) ? null : JsonSerializer.Deserialize<List<SetDetailDto>>(lastEx.SetDetails); }
            catch { }

            if (lastSetDetails is { Count: > 0 })
            {
                for (int i = 0; i < match.SetDetails.Count; i++)
                {
                    var src = i < lastSetDetails.Count ? lastSetDetails[i] : lastSetDetails[^1];
                    if (src.weight > 0) match.SetDetails[i].weight = src.weight;
                    if (src.reps > 0) match.SetDetails[i].reps = src.reps;
                }
            }
            else if (lastEx.Weight > 0)
            {
                foreach (var sd in match.SetDetails) sd.weight = lastEx.Weight;
            }
        }
    }

    [Fact]
    public void PreFill_Uses_Most_Recent_Workout_Weights()
    {
        var sessions = new List<WorkoutSession>
        {
            new() { UserId = 1, Day = DayOfWeek.Monday, CompletedAt = DateTime.UtcNow.AddDays(-14),
                ExerciseLogs = [new() { ExerciseId = 10, Weight = 30, Sets = 3, Reps = 12, SetDetails = "[{\"reps\":12,\"weight\":30}]" }]},
            new() { UserId = 1, Day = DayOfWeek.Monday, CompletedAt = DateTime.UtcNow.AddDays(-7),
                ExerciseLogs = [new() { ExerciseId = 10, Weight = 40, Sets = 3, Reps = 12, SetDetails = "[{\"reps\":12,\"weight\":40},{\"reps\":10,\"weight\":42.5},{\"reps\":8,\"weight\":45}]" }]},
        };

        var lastWeights = GetLastWeights(sessions, 1, DayOfWeek.Monday);

        Assert.NotNull(lastWeights);
        Assert.Single(lastWeights);
        Assert.Equal(40, lastWeights[0].Weight);
        Assert.Contains("42.5", lastWeights[0].SetDetails);
    }

    [Fact]
    public void PreFill_Returns_Null_When_No_Previous_Workout()
    {
        var result = GetLastWeights([], 1, DayOfWeek.Wednesday);
        Assert.Null(result);
    }

    [Fact]
    public void PreFill_Only_Matches_Correct_Day()
    {
        var sessions = new List<WorkoutSession>
        {
            new() { UserId = 1, Day = DayOfWeek.Monday, CompletedAt = DateTime.UtcNow.AddDays(-1),
                ExerciseLogs = [new() { ExerciseId = 5, Weight = 15, Sets = 3, Reps = 12, SetDetails = "[{\"reps\":12,\"weight\":15}]" }]},
        };

        Assert.Null(GetLastWeights(sessions, 1, DayOfWeek.Tuesday));
        Assert.NotNull(GetLastWeights(sessions, 1, DayOfWeek.Monday));
    }

    [Fact]
    public void PreFill_Does_Not_Cross_Users()
    {
        var sessions = new List<WorkoutSession>
        {
            new() { UserId = 1, Day = DayOfWeek.Friday, CompletedAt = DateTime.UtcNow.AddDays(-1),
                ExerciseLogs = [new() { ExerciseId = 10, Weight = 50, Sets = 3, Reps = 12, SetDetails = "[{\"reps\":12,\"weight\":50}]" }]},
        };

        Assert.Null(GetLastWeights(sessions, 2, DayOfWeek.Friday));
        Assert.NotNull(GetLastWeights(sessions, 1, DayOfWeek.Friday));
    }

    [Fact]
    public void Merge_Applies_PerSet_Weights_To_Routine_Exercises()
    {
        var exercises = new List<ExerciseState>
        {
            new() { ExerciseId = 10, SetDetails = [new() { reps = 12, weight = 0 }, new() { reps = 12, weight = 0 }, new() { reps = 12, weight = 0 }] },
        };

        var lastWeights = new List<LastWeightEntry>
        {
            new() { ExerciseId = 10, Weight = 40, SetDetails = "[{\"reps\":12,\"weight\":40},{\"reps\":10,\"weight\":42.5},{\"reps\":8,\"weight\":45}]" },
        };

        ApplyLastWeights(exercises, lastWeights);

        Assert.Equal(40, exercises[0].SetDetails[0].weight);
        Assert.Equal(42.5m, exercises[0].SetDetails[1].weight);
        Assert.Equal(45, exercises[0].SetDetails[2].weight);
        Assert.Equal(10, exercises[0].SetDetails[1].reps); // reps also carried over
    }

    [Fact]
    public void Merge_Skips_Exercises_Not_In_Routine()
    {
        var exercises = new List<ExerciseState>
        {
            new() { ExerciseId = 10, SetDetails = [new() { reps = 12, weight = 0 }] },
        };

        var lastWeights = new List<LastWeightEntry>
        {
            new() { ExerciseId = 99, Weight = 50, SetDetails = "[{\"reps\":12,\"weight\":50}]" },
        };

        ApplyLastWeights(exercises, lastWeights);

        Assert.Equal(0, exercises[0].SetDetails[0].weight); // unchanged
    }

    [Fact]
    public void Merge_Does_Not_Overwrite_With_Zero_Weight()
    {
        var exercises = new List<ExerciseState>
        {
            new() { ExerciseId = 10, SetDetails = [new() { reps = 12, weight = 35 }] },
        };

        var lastWeights = new List<LastWeightEntry>
        {
            new() { ExerciseId = 10, Weight = 0, SetDetails = "[{\"reps\":12,\"weight\":0}]" },
        };

        ApplyLastWeights(exercises, lastWeights);

        Assert.Equal(35, exercises[0].SetDetails[0].weight); // kept original
    }

    [Fact]
    public void Merge_Uses_Fallback_Weight_When_SetDetails_Empty()
    {
        var exercises = new List<ExerciseState>
        {
            new() { ExerciseId = 10, SetDetails = [new() { reps = 12, weight = 0 }, new() { reps = 12, weight = 0 }] },
        };

        var lastWeights = new List<LastWeightEntry>
        {
            new() { ExerciseId = 10, Weight = 25, SetDetails = "" },
        };

        ApplyLastWeights(exercises, lastWeights);

        Assert.Equal(25, exercises[0].SetDetails[0].weight);
        Assert.Equal(25, exercises[0].SetDetails[1].weight);
    }

    [Fact]
    public void Merge_Handles_More_Sets_In_Routine_Than_Last_Workout()
    {
        // Routine has 4 sets, last workout only had 3
        var exercises = new List<ExerciseState>
        {
            new() { ExerciseId = 10, SetDetails = [
                new() { reps = 12, weight = 0 },
                new() { reps = 12, weight = 0 },
                new() { reps = 12, weight = 0 },
                new() { reps = 12, weight = 0 },
            ]},
        };

        var lastWeights = new List<LastWeightEntry>
        {
            new() { ExerciseId = 10, Weight = 40, SetDetails = "[{\"reps\":12,\"weight\":40},{\"reps\":10,\"weight\":45},{\"reps\":8,\"weight\":50}]" },
        };

        ApplyLastWeights(exercises, lastWeights);

        Assert.Equal(40, exercises[0].SetDetails[0].weight);
        Assert.Equal(45, exercises[0].SetDetails[1].weight);
        Assert.Equal(50, exercises[0].SetDetails[2].weight);
        Assert.Equal(50, exercises[0].SetDetails[3].weight); // repeats last
    }

    // ── DTOs matching the actual data shapes ──

    record LastWeightEntry
    {
        public int ExerciseId { get; init; }
        public decimal Weight { get; init; }
        public int Reps { get; init; }
        public int Sets { get; init; }
        public string SetDetails { get; init; } = "";
    }

    class ExerciseState
    {
        public int ExerciseId { get; init; }
        public List<SetDetailDto> SetDetails { get; init; } = [];
    }

    class SetDetailDto
    {
        public int reps { get; set; }
        public decimal weight { get; set; }
    }
}
