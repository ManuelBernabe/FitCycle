using System.Net.Http.Json;
using System.Text.Json;
using FitCycle.Core.Models;

namespace FitCycle.App.Services;

public interface IRoutineService
{
    Task<IReadOnlyList<MuscleGroup>> GetMuscleGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Exercise>> GetExercisesAsync(int? muscleGroupId = null, CancellationToken ct = default);
    Task<Exercise> CreateExerciseAsync(string name, int muscleGroupId, CancellationToken ct = default);
    Task<WeekRoutine> GetWeekRoutineAsync(CancellationToken ct = default);
    Task<DayRoutine> GetDayRoutineAsync(DayOfWeek day, CancellationToken ct = default);
    Task<DayRoutine> UpdateDayRoutineAsync(DayOfWeek day, List<int> muscleGroupIds, List<ExerciseInputDto> exercises, CancellationToken ct = default);
}

public record ExerciseInputDto(int ExerciseId, int Sets, int Reps);

public class RoutineService(HttpClient http) : IRoutineService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http = http;

    public async Task<IReadOnlyList<MuscleGroup>> GetMuscleGroupsAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync("/musclegroups", ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<MuscleGroup[]>(s, JsonOptions, ct);
        return data ?? [];
    }

    public async Task<IReadOnlyList<Exercise>> GetExercisesAsync(int? muscleGroupId = null, CancellationToken ct = default)
    {
        var url = muscleGroupId.HasValue ? $"/exercises?muscleGroupId={muscleGroupId}" : "/exercises";
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<Exercise[]>(s, JsonOptions, ct);
        return data ?? [];
    }

    public async Task<Exercise> CreateExerciseAsync(string name, int muscleGroupId, CancellationToken ct = default)
    {
        var content = JsonContent.Create(new { Name = name, MuscleGroupId = muscleGroupId });
        using var resp = await _http.PostAsync("/exercises", content, ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<Exercise>(s, JsonOptions, ct);
        return data!;
    }

    public async Task<WeekRoutine> GetWeekRoutineAsync(CancellationToken ct = default)
    {
        using var resp = await _http.GetAsync("/routines", ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<WeekRoutine>(s, JsonOptions, ct);
        return data ?? new WeekRoutine();
    }

    public async Task<DayRoutine> GetDayRoutineAsync(DayOfWeek day, CancellationToken ct = default)
    {
        var dayInt = (int)day;
        using var resp = await _http.GetAsync($"/routines/{dayInt}", ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<DayRoutine>(s, JsonOptions, ct);
        return data ?? new DayRoutine { Day = day };
    }

    public async Task<DayRoutine> UpdateDayRoutineAsync(DayOfWeek day, List<int> muscleGroupIds, List<ExerciseInputDto> exercises, CancellationToken ct = default)
    {
        var dayInt = (int)day;
        var body = new
        {
            MuscleGroupIds = muscleGroupIds,
            Exercises = exercises.Select(e => new { e.ExerciseId, e.Sets, e.Reps }).ToList()
        };
        var content = JsonContent.Create(body);
        using var resp = await _http.PutAsync($"/routines/{dayInt}", content, ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<DayRoutine>(s, JsonOptions, ct);
        return data ?? new DayRoutine { Day = day };
    }
}
