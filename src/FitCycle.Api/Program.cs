using System.Reflection;
using System.Security.Claims;
using System.Text;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Data;
using FitCycle.Infrastructure.Repositories;
using FitCycle.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core SQLite
builder.Services.AddDbContext<FitCycleDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=fitcycle.db"));

builder.Services.AddScoped<IRoutineRepository, SqliteRoutineRepository>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IAuthService, AuthService>();

// Email
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrAbove", policy =>
        policy.RequireRole("Admin", "Superuser"));
    options.AddPolicy("SuperuserOnly", policy =>
        policy.RequireRole("Superuser"));
});

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitCycleDbContext>();
    db.Database.Migrate();

    // Update placeholder images to real ones for known exercises
    var repo = scope.ServiceProvider.GetRequiredService<IRoutineRepository>();
    if (repo is SqliteRoutineRepository sqliteRepo)
        sqliteRepo.UpdatePlaceholderImages();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.RoutePrefix = string.Empty);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    utc = DateTime.UtcNow
}))
.WithName("Health")
.WithOpenApi()
.AllowAnonymous();

app.MapGet("/version", () =>
{
    var v = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    return Results.Ok(new { version = v });
})
.WithName("Version")
.WithOpenApi()
.AllowAnonymous();

// -- Autenticación --
app.MapPost("/auth/register", async (RegisterRequest request, IAuthService auth, IEmailService emailService, ILogger<Program> logger) =>
{
    try
    {
        var result = await auth.RegisterAsync(request);

        // Fire-and-forget welcome email
        _ = Task.Run(async () =>
        {
            try { await emailService.SendWelcomeEmailAsync(request.Email, request.Username); }
            catch (Exception ex) { logger.LogError(ex, "Failed to send welcome email to {Email}", request.Email); }
        });

        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("Register")
.WithOpenApi()
.AllowAnonymous();

app.MapPost("/auth/login", async (LoginRequest request, IAuthService auth) =>
{
    try
    {
        var result = await auth.LoginAsync(request);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Json(new { error = "Credenciales inválidas." }, statusCode: 401);
    }
})
.WithName("Login")
.WithOpenApi()
.AllowAnonymous();

app.MapPost("/auth/refresh", async (RefreshRequest request, IAuthService auth) =>
{
    try
    {
        var result = await auth.RefreshTokenAsync(request);
        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Json(new { error = "Refresh token inválido o expirado." }, statusCode: 401);
    }
})
.WithName("RefreshToken")
.WithOpenApi()
.AllowAnonymous();

app.MapGet("/auth/me", async (ClaimsPrincipal user, IAuthService auth) =>
{
    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var info = await auth.GetUserInfoAsync(userId);
    return info is null ? Results.NotFound() : Results.Ok(info);
})
.WithName("GetCurrentUser")
.WithOpenApi()
.RequireAuthorization();

// -- Gestión de usuarios (Superuser) --
app.MapGet("/users", async (IAuthService auth) =>
{
    var users = await auth.GetAllUsersAsync();
    return Results.Ok(users);
})
.WithName("GetUsers")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapPost("/users", async (CreateUserRequest request, IAuthService auth) =>
{
    try
    {
        var user = await auth.CreateUserAsync(request);
        return Results.Created($"/users/{user.Id}", user);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateUser")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapPut("/users/{id}", async (int id, UpdateUserRequest request, IAuthService auth) =>
{
    try
    {
        var user = await auth.UpdateUserAsync(id, request);
        return Results.Ok(user);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateUser")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapDelete("/users/{id}", async (int id, ClaimsPrincipal user, IAuthService auth) =>
{
    var currentUserIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (currentUserIdClaim is null || !int.TryParse(currentUserIdClaim, out var currentUserId))
        return Results.Unauthorized();

    try
    {
        var deleted = await auth.DeleteUserAsync(id, currentUserId);
        return deleted ? Results.Ok(new { message = "Usuario eliminado." }) : Results.NotFound();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("DeleteUser")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapPut("/users/{id}/password", async (int id, ResetPasswordRequest request, IAuthService auth) =>
{
    try
    {
        await auth.ResetPasswordAsync(id, request.NewPassword);
        return Results.Ok(new { message = "Contraseña actualizada." });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ResetPassword")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

// -- Grupos musculares --
app.MapGet("/musclegroups", (IRoutineRepository repo) =>
{
    return Results.Ok(repo.GetAllMuscleGroups());
})
.WithName("GetMuscleGroups")
.WithOpenApi()
.RequireAuthorization();

// -- Ejercicios --
app.MapGet("/exercises", (int? muscleGroupId, IRoutineRepository repo) =>
{
    return Results.Ok(repo.GetExercises(muscleGroupId));
})
.WithName("GetExercises")
.WithOpenApi()
.RequireAuthorization();

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
.WithOpenApi()
.RequireAuthorization();

// -- Rutina semanal --
app.MapGet("/routines", (IRoutineRepository repo, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    return Results.Ok(repo.GetWeekRoutine(userId));
})
.WithName("GetWeekRoutine")
.WithOpenApi()
.RequireAuthorization();

// -- Rutina de un día --
app.MapGet("/routines/{day}", (DayOfWeek day, IRoutineRepository repo, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    return Results.Ok(repo.GetDayRoutine(day, userId));
})
.WithName("GetDayRoutine")
.WithOpenApi()
.RequireAuthorization();

// -- Actualizar rutina de un día --
app.MapPut("/routines/{day}", (DayOfWeek day, UpdateDayRoutineRequest request, IRoutineRepository repo, ClaimsPrincipal user) =>
{
    try
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var exercises = request.Exercises ?? [];
        var result = repo.SetDayRoutine(day, request.MuscleGroupIds, exercises, userId);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateDayRoutine")
.WithOpenApi()
.RequireAuthorization();

// -- Historial de entrenamientos --
app.MapPost("/workouts", (SaveWorkoutRequest request, FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var session = new WorkoutSession
    {
        UserId = userId,
        Day = request.Day,
        StartedAt = request.StartedAt,
        CompletedAt = request.CompletedAt,
        ExerciseLogs = request.Exercises.Select(e => new WorkoutExerciseLog
        {
            ExerciseId = e.ExerciseId,
            ExerciseName = e.ExerciseName,
            Sets = e.Sets,
            Reps = e.Reps,
            Weight = e.Weight,
            MuscleGroupName = e.MuscleGroupName,
            SetDetails = e.SetDetails ?? string.Empty
        }).ToList()
    };

    db.WorkoutSessions.Add(session);
    db.SaveChanges();
    return Results.Created($"/workouts/{session.Id}", session);
})
.WithName("SaveWorkout")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/workouts", (FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var sessions = db.WorkoutSessions
        .Where(w => w.UserId == userId)
        .Include(s => s.ExerciseLogs)
        .OrderByDescending(s => s.CompletedAt)
        .Take(50)
        .ToList();
    return Results.Ok(sessions);
})
.WithName("GetWorkouts")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/workouts/stats", (FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var sessions = db.WorkoutSessions
        .Where(w => w.UserId == userId)
        .Include(s => s.ExerciseLogs)
        .OrderByDescending(s => s.CompletedAt)
        .ToList();

    var totalWorkouts = sessions.Count;
    var totalSets = sessions.SelectMany(s => s.ExerciseLogs).Sum(l => l.Sets);
    var totalReps = sessions.SelectMany(s => s.ExerciseLogs).Sum(l => l.Sets * l.Reps);

    // Workouts per week (last 4 weeks)
    var now = DateTime.UtcNow;
    var weeksAgo4 = now.AddDays(-28);
    var recentSessions = sessions.Where(s => s.CompletedAt >= weeksAgo4).ToList();
    var weeklyData = Enumerable.Range(0, 4).Select(weekOffset =>
    {
        var weekStart = now.AddDays(-(weekOffset + 1) * 7);
        var weekEnd = now.AddDays(-weekOffset * 7);
        return new
        {
            week = weekOffset == 0 ? "Esta semana" : $"Hace {weekOffset + 1} sem.",
            count = recentSessions.Count(s => s.CompletedAt >= weekStart && s.CompletedAt < weekEnd)
        };
    }).Reverse().ToList();

    // Most frequent exercises
    var topExercises = sessions
        .SelectMany(s => s.ExerciseLogs)
        .GroupBy(l => l.ExerciseName)
        .Select(g => new { name = g.Key, count = g.Count() })
        .OrderByDescending(x => x.count)
        .Take(5)
        .ToList();

    // Weight progression for top 10 exercises by frequency
    var allLogs = sessions.SelectMany(s => s.ExerciseLogs).ToList();
    var top10Exercises = allLogs
        .GroupBy(l => l.ExerciseName)
        .OrderByDescending(g => g.Count())
        .Take(10)
        .Select(g => g.Key)
        .ToList();

    var weightProgress = top10Exercises.Select(exerciseName =>
    {
        var entries = sessions
            .Where(s => s.ExerciseLogs.Any(l => l.ExerciseName == exerciseName && l.Weight > 0))
            .OrderBy(s => s.CompletedAt)
            .Select(s => new
            {
                date = s.CompletedAt,
                weight = s.ExerciseLogs
                    .Where(l => l.ExerciseName == exerciseName && l.Weight > 0)
                    .Max(l => l.Weight)
            })
            .ToList();

        return new { exerciseName, entries };
    })
    .Where(x => x.entries.Count > 0)
    .ToList();

    return Results.Ok(new
    {
        totalWorkouts,
        totalSets,
        totalReps,
        weeklyData,
        topExercises,
        weightProgress
    });
})
.WithName("GetWorkoutStats")
.WithOpenApi()
.RequireAuthorization();

app.MapGet("/workouts/exercise/{exerciseId}/progress", (int exerciseId, FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var logs = db.WorkoutExerciseLogs
        .Include(l => l.WorkoutSession)
        .Where(l => l.ExerciseId == exerciseId && l.Weight > 0 && l.WorkoutSession!.UserId == userId)
        .OrderBy(l => l.WorkoutSession!.CompletedAt)
        .Select(l => new { date = l.WorkoutSession!.CompletedAt, weight = l.Weight, sets = l.Sets, reps = l.Reps })
        .ToList();
    return Results.Ok(logs);
})
.WithName("GetExerciseProgress")
.WithOpenApi()
.RequireAuthorization();

// -- Medidas corporales --
app.MapGet("/measurements", (FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var measurements = db.BodyMeasurements
        .Where(m => m.UserId == userId)
        .OrderByDescending(m => m.MeasuredAt)
        .Take(50)
        .ToList();
    return Results.Ok(measurements);
})
.WithName("GetMeasurements")
.WithOpenApi()
.RequireAuthorization();

app.MapPost("/measurements", (SaveMeasurementRequest request, FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var measurement = new BodyMeasurement
    {
        UserId = userId,
        MeasuredAt = request.MeasuredAt ?? DateTime.UtcNow,
        Weight = request.Weight,
        Height = request.Height,
        Chest = request.Chest,
        Waist = request.Waist,
        Hips = request.Hips,
        BicepLeft = request.BicepLeft,
        BicepRight = request.BicepRight,
        ThighLeft = request.ThighLeft,
        ThighRight = request.ThighRight,
        CalfLeft = request.CalfLeft,
        CalfRight = request.CalfRight,
        Neck = request.Neck,
        BodyFat = request.BodyFat,
        Notes = request.Notes
    };
    db.BodyMeasurements.Add(measurement);
    db.SaveChanges();
    return Results.Created($"/measurements/{measurement.Id}", measurement);
})
.WithName("SaveMeasurement")
.WithOpenApi()
.RequireAuthorization();

app.MapDelete("/measurements/{id}", (int id, FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var measurement = db.BodyMeasurements.FirstOrDefault(m => m.Id == id && m.UserId == userId);
    if (measurement is null) return Results.NotFound();
    db.BodyMeasurements.Remove(measurement);
    db.SaveChanges();
    return Results.Ok(new { message = "Medida eliminada." });
})
.WithName("DeleteMeasurement")
.WithOpenApi()
.RequireAuthorization();

// -- Webhook de deploy (Railway llama aquí al terminar) --
app.MapPost("/webhook/deploy", async (HttpRequest req, IEmailService emailService, ILogger<Program> logger) =>
{
    // Validate webhook secret
    var secret = req.Headers["X-Webhook-Secret"].FirstOrDefault()
              ?? req.Query["secret"].FirstOrDefault();
    if (!string.IsNullOrEmpty(emailSettings.WebhookSecret) &&
        secret != emailSettings.WebhookSecret)
    {
        return Results.Unauthorized();
    }

    var status = req.Query["status"].FirstOrDefault() ?? "SUCCESS";
    var env = req.Query["env"].FirstOrDefault() ?? "production";

    try
    {
        await emailService.SendDeployNotificationAsync(status, env);
        logger.LogInformation("Deploy webhook processed: {Status} ({Env})", status, env);
        return Results.Ok(new { message = $"Notification sent: {status}" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send deploy notification");
        return Results.Ok(new { message = "Webhook received but email failed", error = ex.Message });
    }
})
.WithName("DeployWebhook")
.WithOpenApi()
.AllowAnonymous();

app.MapFallbackToFile("index.html");

app.Run();

record CreateExerciseRequest(string Name, int MuscleGroupId);
record ExerciseInput(int ExerciseId, int Sets, int Reps);
record UpdateDayRoutineRequest(List<int> MuscleGroupIds, List<RoutineExerciseInput>? Exercises);
record SaveWorkoutExerciseInput(int ExerciseId, string ExerciseName, int Sets, int Reps, decimal Weight, string MuscleGroupName, string SetDetails = "");
record SaveWorkoutRequest(DayOfWeek Day, DateTime StartedAt, DateTime CompletedAt, List<SaveWorkoutExerciseInput> Exercises);
record SaveMeasurementRequest(
    DateTime? MeasuredAt = null,
    decimal? Weight = null, decimal? Height = null,
    decimal? Chest = null, decimal? Waist = null, decimal? Hips = null,
    decimal? BicepLeft = null, decimal? BicepRight = null,
    decimal? ThighLeft = null, decimal? ThighRight = null,
    decimal? CalfLeft = null, decimal? CalfRight = null,
    decimal? Neck = null, decimal? BodyFat = null, string? Notes = null);
