using System.Reflection;
using FitCycle.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IRoutineRepository, InMemoryRoutineRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.RoutePrefix = string.Empty);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    utc = DateTime.UtcNow
}))
.WithName("Health")
.WithOpenApi();

app.MapGet("/version", () =>
{
    var v = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    return Results.Ok(new { version = v });
})
.WithName("Version")
.WithOpenApi();

// -- Grupos musculares --
app.MapGet("/musclegroups", (IRoutineRepository repo) =>
{
    return Results.Ok(repo.GetAllMuscleGroups());
})
.WithName("GetMuscleGroups")
.WithOpenApi();

// -- Ejercicios --
app.MapGet("/exercises", (int? muscleGroupId, IRoutineRepository repo) =>
{
    return Results.Ok(repo.GetExercises(muscleGroupId));
})
.WithName("GetExercises")
.WithOpenApi();

app.MapPost("/exercises", (CreateExerciseRequest request, IRoutineRepository repo) =>
{
    try
    {
        var exercise = repo.AddExercise(request.Name, request.MuscleGroupId);
        return Results.Created($"/exercises/{exercise.Id}", exercise);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateExercise")
.WithOpenApi();

// -- Rutina semanal --
app.MapGet("/routines", (IRoutineRepository repo) =>
{
    return Results.Ok(repo.GetWeekRoutine());
})
.WithName("GetWeekRoutine")
.WithOpenApi();

// -- Rutina de un día --
app.MapGet("/routines/{day}", (DayOfWeek day, IRoutineRepository repo) =>
{
    return Results.Ok(repo.GetDayRoutine(day));
})
.WithName("GetDayRoutine")
.WithOpenApi();

// -- Actualizar rutina de un día --
app.MapPut("/routines/{day}", (DayOfWeek day, UpdateDayRoutineRequest request, IRoutineRepository repo) =>
{
    try
    {
        var exercises = request.Exercises ?? [];
        var result = repo.SetDayRoutine(day, request.MuscleGroupIds, exercises);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateDayRoutine")
.WithOpenApi();

app.Run();

record CreateExerciseRequest(string Name, int MuscleGroupId);
record ExerciseInput(int ExerciseId, int Sets, int Reps);
record UpdateDayRoutineRequest(List<int> MuscleGroupIds, List<RoutineExerciseInput>? Exercises);
