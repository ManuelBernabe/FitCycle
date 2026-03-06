using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Data;
using FitCycle.Infrastructure.Entities;
using FitCycle.Infrastructure.Repositories;
using FitCycle.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core SQLite — support Railway volume mount via DATA_DIR env var
var dataDir = Environment.GetEnvironmentVariable("DATA_DIR");
if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
    Directory.CreateDirectory(dataDir);

// DATA_DIR takes priority over appsettings.json to ensure Railway volume is used
var connStr = !string.IsNullOrEmpty(dataDir)
    ? $"Data Source={Path.Combine(dataDir, "fitcycle.db")}"
    : (builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=fitcycle.db");
builder.Services.AddDbContext<FitCycleDbContext>(options =>
    options.UseSqlite(connStr));

builder.Services.AddScoped<IRoutineRepository, SqliteRoutineRepository>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IAuthService, AuthService>();

// Email
var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();

// Gemini (Google AI for PDF import)
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddScoped<IPdfImportService, PdfImportService>();

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
        policy.RequireRole("Admin", "Superuser", "SuperUserMaster"));
    options.AddPolicy("SuperuserOnly", policy =>
        policy.RequireRole("Superuser", "SuperUserMaster"));
    options.AddPolicy("SuperUserMasterOnly", policy =>
        policy.RequireRole("SuperUserMaster"));
});

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitCycleDbContext>();

    // If DB already has tables but the InitialCreate migration isn't recorded,
    // mark only the InitialCreate as applied so Migrate() doesn't try to recreate tables.
    // Subsequent migrations (adding columns, etc.) must always run normally.
    try
    {
        var applied = db.Database.GetAppliedMigrations().ToList();
        if (applied.Count == 0)
        {
            // Check if tables already exist (DB was created outside of EF migrations)
            var tableExists = db.Database.ExecuteSqlRaw("SELECT 1 FROM Users LIMIT 1") >= 0;
            // Mark only the InitialCreate migration as applied
            var pending = db.Database.GetPendingMigrations().ToList();
            var initial = pending.FirstOrDefault(m => m.Contains("InitialCreate"));
            if (initial != null)
            {
                db.Database.ExecuteSqlRaw(
                    "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1})",
                    initial, "8.0.11");
            }
        }
    }
    catch { /* Table may not exist yet — Migrate() will handle it */ }

    db.Database.Migrate();

    // Promote seeded admin (id=1) to SuperUserMaster if still Superuser
    var adminUser = db.Users.FirstOrDefault(u => u.Id == 1 && u.Role == UserRole.Superuser);
    if (adminUser != null)
    {
        adminUser.Role = UserRole.SuperUserMaster;
        db.SaveChanges();
    }

    // Update placeholder images to real ones for known exercises
    var repo = scope.ServiceProvider.GetRequiredService<IRoutineRepository>();
    if (repo is SqliteRoutineRepository sqliteRepo)
        sqliteRepo.UpdatePlaceholderImages();

    // Auto-backup: create daily backup if none exists for today
    try
    {
        var backupConnStr = db.Database.GetConnectionString() ?? "";
        var dbMatch = System.Text.RegularExpressions.Regex.Match(backupConnStr, @"Data Source=(.+?)(?:;|$)");
        var dbFilePath = dbMatch.Success ? dbMatch.Groups[1].Value : "fitcycle.db";

        if (File.Exists(dbFilePath))
        {
            var backupDir = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "backups");
            Directory.CreateDirectory(backupDir);
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            if (!Directory.GetFiles(backupDir, $"fitcycle_{today}*.db").Any())
            {
                db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");
                var backupPath = Path.Combine(backupDir, $"fitcycle_{today}_{DateTime.UtcNow:HHmmss}.db");
                File.Copy(dbFilePath, backupPath);

                // Keep max 10 backups
                var allBackups = Directory.GetFiles(backupDir, "fitcycle_*.db").OrderBy(f => f).ToArray();
                if (allBackups.Length > 10)
                {
                    foreach (var old in allBackups.Take(allBackups.Length - 10))
                        File.Delete(old);
                }
            }
        }
    }
    catch { /* Non-critical: don't prevent app from starting */ }
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
app.MapPost("/auth/register", async (RegisterRequest request, IAuthService auth, IEmailService emailService, EmailSettings emailCfg, ILogger<Program> logger) =>
{
    try
    {
        var result = await auth.RegisterAsync(request);

        // Fire-and-forget activation email
        var activationUrl = $"{emailCfg.AppBaseUrl}/auth/activate?token={result.ActivationToken}";
        _ = Task.Run(async () =>
        {
            try { await emailService.SendActivationEmailAsync(request.Email, request.Username, activationUrl); }
            catch (Exception ex) { logger.LogError(ex, "Failed to send activation email to {Email}", request.Email); }
        });

        return Results.Ok(new { message = result.Message });
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
    catch (UnauthorizedAccessException ex)
    {
        return Results.Json(new { error = ex.Message }, statusCode: 401);
    }
})
.WithName("Login")
.WithOpenApi()
.AllowAnonymous();

app.MapGet("/auth/activate", async (string token, IAuthService auth) =>
{
    var success = await auth.ActivateAsync(token);
    var html = success
        ? @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""><title>Cuenta Activada</title></head>
<body style=""margin:0;padding:0;background:#f3f0fc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;"">
<div style=""background:#fff;border-radius:16px;padding:40px;text-align:center;max-width:400px;box-shadow:0 4px 24px rgba(0,0,0,0.1);"">
<div style=""font-size:48px;font-weight:bold;color:#512BD4;letter-spacing:2px;margin-bottom:8px;"">FC</div>
<div style=""font-size:48px;margin:16px 0;"">&#10003;</div>
<h1 style=""color:#28a745;font-size:24px;margin:0 0 12px;"">Cuenta activada</h1>
<p style=""color:#555;font-size:16px;line-height:1.6;"">Tu cuenta ha sido activada correctamente. Ya puedes iniciar sesión.</p>
<a href=""/"" style=""display:inline-block;margin-top:20px;background:#512BD4;color:#fff;padding:12px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Ir a FitCycle</a>
</div></body></html>"
        : @"<!DOCTYPE html><html><head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""><title>Error de Activación</title></head>
<body style=""margin:0;padding:0;background:#f3f0fc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;"">
<div style=""background:#fff;border-radius:16px;padding:40px;text-align:center;max-width:400px;box-shadow:0 4px 24px rgba(0,0,0,0.1);"">
<div style=""font-size:48px;font-weight:bold;color:#512BD4;letter-spacing:2px;margin-bottom:8px;"">FC</div>
<div style=""font-size:48px;margin:16px 0;"">&#10007;</div>
<h1 style=""color:#dc3545;font-size:24px;margin:0 0 12px;"">Enlace inválido o expirado</h1>
<p style=""color:#555;font-size:16px;line-height:1.6;"">El enlace de activación no es válido o ha expirado. Solicita uno nuevo desde la pantalla de login.</p>
<a href=""/"" style=""display:inline-block;margin-top:20px;background:#512BD4;color:#fff;padding:12px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Ir a FitCycle</a>
</div></body></html>";
    return Results.Content(html, "text/html");
})
.WithName("ActivateAccount")
.WithOpenApi()
.AllowAnonymous();

app.MapPost("/auth/resend-activation", async (ResendActivationRequest request, IAuthService auth, IEmailService emailService, EmailSettings emailCfg, ILogger<Program> logger) =>
{
    var token = await auth.ResendActivationAsync(request.Email);
    if (token != null)
    {
        var activationUrl = $"{emailCfg.AppBaseUrl}/auth/activate?token={token}";
        _ = Task.Run(async () =>
        {
            try { await emailService.SendActivationEmailAsync(request.Email, "", activationUrl); }
            catch (Exception ex) { logger.LogError(ex, "Failed to resend activation email to {Email}", request.Email); }
        });
    }
    // Always return OK to prevent email enumeration
    return Results.Ok(new { message = "Si el email está registrado, recibirás un enlace de activación." });
})
.WithName("ResendActivation")
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

// -- Impersonar usuario (solo Superuser) --
app.MapPost("/auth/impersonate/{userId}", async (int userId, IAuthService auth) =>
{
    try
    {
        var result = await auth.ImpersonateAsync(userId);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ImpersonateUser")
.WithOpenApi()
.RequireAuthorization("SuperUserMasterOnly");

// -- Gestión de usuarios (SuperUserMaster only) --
app.MapGet("/users", async (IAuthService auth) =>
{
    var users = await auth.GetAllUsersAsync();
    return Results.Ok(users);
})
.WithName("GetUsers")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove");

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
.RequireAuthorization("SuperUserMasterOnly");

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
.RequireAuthorization("SuperUserMasterOnly");

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
.RequireAuthorization("SuperUserMasterOnly");

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
.RequireAuthorization("SuperUserMasterOnly");

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

// -- Importar rutinas desde PDF (solo Superuser) --
app.MapPost("/routines/import-pdf", async (HttpRequest request, IPdfImportService importService, ILogger<Program> logger) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var file = form.Files["pdf"];
        if (file == null || file.Length == 0)
            return Results.BadRequest(new { error = "No se proporcionó archivo PDF." });

        if (file.Length > 10 * 1024 * 1024)
            return Results.BadRequest(new { error = "El archivo excede el límite de 10 MB." });

        if (!int.TryParse(form["userId"].ToString(), out var targetUserId) || targetUserId <= 0)
            return Results.BadRequest(new { error = "userId inválido." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var result = await importService.ImportFromPdfAsync(ms.ToArray(), targetUserId);

        return result.Success ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error importing PDF");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ImportPdfRoutine")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove")
.DisableAntiforgery();

// -- Debug: ver texto extraído del PDF --
app.MapPost("/routines/debug-pdf", async (HttpRequest request, IPdfImportService importService) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files["pdf"];
    if (file == null || file.Length == 0)
        return Results.BadRequest(new { error = "No se proporcionó archivo PDF." });

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var text = importService.ExtractTextFromPdf(ms.ToArray());
    var linesList = text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
    var diaLines = linesList.Where(l =>
        System.Text.RegularExpressions.Regex.IsMatch(l, @"D[IÍ]A", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        .Select(l => l.Trim()).ToList();

    return Results.Ok(new
    {
        totalLines = linesList.Count,
        totalChars = text.Length,
        diaLines,
        first100Lines = linesList.Take(100).Select(l => l.Trim()).ToList(),
        fullText = text.Length <= 20000 ? text : text[..20000] + "... (truncated)"
    });
})
.WithName("DebugPdfText")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove")
.DisableAntiforgery();

// -- Copiar rutinas de un usuario a otro (solo Superuser) --
app.MapPost("/routines/copy", (CopyRoutinesRequest req, IRoutineRepository repo) =>
{
    if (req.SourceUserId == req.TargetUserId)
        return Results.BadRequest(new { error = "El usuario origen y destino no pueden ser el mismo." });

    var sourceWeek = repo.GetWeekRoutine(req.SourceUserId);
    var days = sourceWeek?.Days ?? [];

    if (days.Count == 0)
        return Results.BadRequest(new { error = "El usuario origen no tiene rutinas." });

    int copiedDays = 0;
    foreach (var day in days)
    {
        var muscleGroupIds = day.MuscleGroups.Select(g => g.Id).ToList();
        var exercises = day.Exercises.Select(e => new RoutineExerciseInput(
            e.ExerciseId, e.Sets, e.Reps, e.Weight,
            e.SetDetails ?? "", e.SupersetGroup, e.Notes ?? ""
        )).ToList();

        if (muscleGroupIds.Count > 0 || exercises.Count > 0)
        {
            repo.SetDayRoutine(day.Day, muscleGroupIds, exercises, req.TargetUserId);
            copiedDays++;
        }
    }

    return Results.Ok(new { success = true, message = $"Se copiaron {copiedDays} días de rutinas." });
})
.WithName("CopyRoutines")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove");

// -- Plantillas de rutinas (solo Superuser) --
app.MapGet("/templates", (FitCycleDbContext db) =>
{
    var templates = db.RoutineTemplates
        .OrderByDescending(t => t.CreatedAt)
        .Select(t => new
        {
            t.Id, t.Name, t.Description, t.CreatedAt, t.CreatedByUserId,
            t.RoutineDataJson
        })
        .ToList();
    return Results.Ok(templates);
})
.WithName("GetTemplates")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove");

app.MapPost("/templates", (SaveTemplateRequest req, IRoutineRepository repo, FitCycleDbContext db, ClaimsPrincipal user) =>
{
    var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var week = repo.GetWeekRoutine(req.SourceUserId);
    var days = week?.Days ?? [];

    if (!days.Any(d => d.Exercises.Count > 0))
        return Results.BadRequest(new { error = "El usuario no tiene rutinas configuradas." });

    var json = JsonSerializer.Serialize(week);

    var template = new RoutineTemplateEntity
    {
        Name = req.Name,
        Description = req.Description ?? "",
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = currentUserId,
        RoutineDataJson = json
    };

    db.RoutineTemplates.Add(template);
    db.SaveChanges();

    return Results.Ok(new { success = true, id = template.Id, message = "Plantilla guardada." });
})
.WithName("SaveTemplate")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove");

app.MapDelete("/templates/{id}", (int id, FitCycleDbContext db) =>
{
    var template = db.RoutineTemplates.Find(id);
    if (template is null)
        return Results.NotFound(new { error = "Plantilla no encontrada." });

    db.RoutineTemplates.Remove(template);
    db.SaveChanges();
    return Results.Ok(new { success = true, message = "Plantilla eliminada." });
})
.WithName("DeleteTemplate")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove");

app.MapPost("/templates/{id}/apply", (int id, ApplyTemplateRequest req, FitCycleDbContext db, IRoutineRepository repo) =>
{
    var template = db.RoutineTemplates.Find(id);
    if (template is null)
        return Results.NotFound(new { error = "Plantilla no encontrada." });

    var week = JsonSerializer.Deserialize<WeekRoutine>(template.RoutineDataJson);
    if (week?.Days is null || week.Days.Count == 0)
        return Results.BadRequest(new { error = "La plantilla no contiene rutinas." });

    int copiedDays = 0;
    foreach (var day in week.Days)
    {
        var muscleGroupIds = day.MuscleGroups.Select(g => g.Id).ToList();
        var exercises = day.Exercises.Select(e =>
        {
            // Ensure exercise exists in DB, find by name or create
            var exerciseId = e.ExerciseId;
            if (!db.Exercises.Any(ex => ex.Id == exerciseId))
            {
                var existing = db.Exercises.FirstOrDefault(ex => ex.Name == e.ExerciseName);
                if (existing != null)
                {
                    exerciseId = existing.Id;
                }
                else
                {
                    var mgId = muscleGroupIds.FirstOrDefault();
                    if (mgId == 0) mgId = 1;
                    var newEx = repo.AddExercise(e.ExerciseName, mgId);
                    exerciseId = newEx.Id;
                }
            }
            return new RoutineExerciseInput(exerciseId, e.Sets, e.Reps, e.Weight,
                e.SetDetails ?? "", e.SupersetGroup, e.Notes ?? "");
        }).ToList();

        if (muscleGroupIds.Count > 0 || exercises.Count > 0)
        {
            repo.SetDayRoutine(day.Day, muscleGroupIds, exercises, req.TargetUserId);
            copiedDays++;
        }
    }

    return Results.Ok(new { success = true, message = $"Plantilla aplicada: {copiedDays} días." });
})
.WithName("ApplyTemplate")
.WithOpenApi()
.RequireAuthorization("AdminOrAbove");

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

app.MapGet("/admin/download-db", (FitCycleDbContext db) =>
{
    var connStr = db.Database.GetConnectionString() ?? "";
    var match = System.Text.RegularExpressions.Regex.Match(connStr, @"Data Source=(.+?)(?:;|$)");
    var dbPath = match.Success ? match.Groups[1].Value : "fitcycle.db";

    if (!File.Exists(dbPath))
        return Results.NotFound(new { error = $"BD no encontrada en: {dbPath} (conn: {connStr})" });

    // Flush WAL to main DB file so download includes all data
    db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");

    var bytes = File.ReadAllBytes(dbPath);
    return Results.File(bytes, "application/octet-stream", "fitcycle.db");
})
.WithName("DownloadDatabase")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

// -- Admin: SQL Query Console --
app.MapPost("/admin/query", (SqlQueryRequest req, FitCycleDbContext db) =>
{
    var sql = req.Query?.Trim() ?? "";
    if (string.IsNullOrEmpty(sql))
        return Results.BadRequest(new { error = "Query vacía." });

    // Only allow SELECT and PRAGMA
    var upper = sql.ToUpperInvariant();
    if (!upper.StartsWith("SELECT") && !upper.StartsWith("PRAGMA") && !upper.StartsWith("WITH"))
        return Results.BadRequest(new { error = "Solo se permiten consultas SELECT, PRAGMA o WITH." });

    // Block dangerous keywords even in subqueries
    var blocked = new[] { "DROP ", "DELETE ", "UPDATE ", "INSERT ", "ALTER ", "CREATE ", "ATTACH ", "DETACH " };
    if (blocked.Any(b => upper.Contains(b)))
        return Results.BadRequest(new { error = "Query contiene operaciones no permitidas." });

    try
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        var columns = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();
        var rows = new List<List<object?>>();
        var maxRows = 500;
        while (reader.Read() && rows.Count < maxRows)
        {
            var row = new List<object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
            rows.Add(row);
        }

        return Results.Ok(new { columns, rows, rowCount = rows.Count, truncated = rows.Count >= maxRows });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("AdminQuery")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

// -- Admin: Backups --
app.MapGet("/admin/backups", (FitCycleDbContext db) =>
{
    var connStr = db.Database.GetConnectionString() ?? "";
    var dbMatch = System.Text.RegularExpressions.Regex.Match(connStr, @"Data Source=(.+?)(?:;|$)");
    var dbFilePath = dbMatch.Success ? dbMatch.Groups[1].Value : "fitcycle.db";
    var backupDir = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "backups");

    if (!Directory.Exists(backupDir))
        return Results.Ok(new { backups = Array.Empty<object>() });

    var backups = Directory.GetFiles(backupDir, "fitcycle_*.db")
        .Select(f => new FileInfo(f))
        .OrderByDescending(f => f.Name)
        .Select(f => new { name = f.Name, size = f.Length, date = f.LastWriteTimeUtc })
        .ToList();

    return Results.Ok(new { backups });
})
.WithName("ListBackups")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapPost("/admin/backup", (FitCycleDbContext db) =>
{
    var connStr = db.Database.GetConnectionString() ?? "";
    var dbMatch = System.Text.RegularExpressions.Regex.Match(connStr, @"Data Source=(.+?)(?:;|$)");
    var dbFilePath = dbMatch.Success ? dbMatch.Groups[1].Value : "fitcycle.db";

    if (!File.Exists(dbFilePath))
        return Results.NotFound(new { error = "BD no encontrada." });

    var backupDir = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "backups");
    Directory.CreateDirectory(backupDir);

    db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");
    var backupName = $"fitcycle_{DateTime.UtcNow:yyyyMMdd}_{DateTime.UtcNow:HHmmss}.db";
    var backupPath = Path.Combine(backupDir, backupName);
    File.Copy(dbFilePath, backupPath);

    // Keep max 10 backups
    var allBackups = Directory.GetFiles(backupDir, "fitcycle_*.db").OrderBy(f => f).ToArray();
    if (allBackups.Length > 10)
    {
        foreach (var old in allBackups.Take(allBackups.Length - 10))
            File.Delete(old);
    }

    var info = new FileInfo(backupPath);
    return Results.Ok(new { success = true, name = backupName, size = info.Length, message = "Backup creado." });
})
.WithName("CreateBackup")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapPost("/admin/restore/{name}", (string name, FitCycleDbContext db) =>
{
    // Validate filename to prevent path traversal
    if (name.Contains("..") || name.Contains('/') || name.Contains('\\') || !name.EndsWith(".db"))
        return Results.BadRequest(new { error = "Nombre de backup inválido." });

    var connStr = db.Database.GetConnectionString() ?? "";
    var dbMatch = System.Text.RegularExpressions.Regex.Match(connStr, @"Data Source=(.+?)(?:;|$)");
    var dbFilePath = dbMatch.Success ? dbMatch.Groups[1].Value : "fitcycle.db";
    var backupDir = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "backups");
    var backupPath = Path.Combine(backupDir, name);

    if (!File.Exists(backupPath))
        return Results.NotFound(new { error = "Backup no encontrado." });

    try
    {
        // Create pre-restore backup
        db.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");
        var preRestoreName = $"fitcycle_prerestore_{DateTime.UtcNow:yyyyMMdd}_{DateTime.UtcNow:HHmmss}.db";
        File.Copy(dbFilePath, Path.Combine(backupDir, preRestoreName));

        // Close EF connection before overwriting
        db.Database.CloseConnection();

        // Delete WAL/SHM files if present
        var walPath = dbFilePath + "-wal";
        var shmPath = dbFilePath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);

        // Overwrite DB with backup
        File.Copy(backupPath, dbFilePath, overwrite: true);

        return Results.Ok(new { success = true, message = $"BD restaurada desde {name}. Se creó backup previo: {preRestoreName}" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = $"Error al restaurar: {ex.Message}" });
    }
})
.WithName("RestoreBackup")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapGet("/admin/backup/download/{name}", (string name, FitCycleDbContext db) =>
{
    if (name.Contains("..") || name.Contains('/') || name.Contains('\\') || !name.EndsWith(".db"))
        return Results.BadRequest(new { error = "Nombre inválido." });

    var connStr = db.Database.GetConnectionString() ?? "";
    var dbMatch = System.Text.RegularExpressions.Regex.Match(connStr, @"Data Source=(.+?)(?:;|$)");
    var dbFilePath = dbMatch.Success ? dbMatch.Groups[1].Value : "fitcycle.db";
    var backupDir = Path.Combine(Path.GetDirectoryName(dbFilePath) ?? ".", "backups");
    var backupPath = Path.Combine(backupDir, name);

    if (!File.Exists(backupPath))
        return Results.NotFound(new { error = "Backup no encontrado." });

    var bytes = File.ReadAllBytes(backupPath);
    return Results.File(bytes, "application/octet-stream", name);
})
.WithName("DownloadBackup")
.WithOpenApi()
.RequireAuthorization("SuperuserOnly");

app.MapFallbackToFile("index.html");

app.Run();

record SqlQueryRequest(string Query);
record CreateExerciseRequest(string Name, int MuscleGroupId);
record ExerciseInput(int ExerciseId, int Sets, int Reps);
record UpdateDayRoutineRequest(List<int> MuscleGroupIds, List<RoutineExerciseInput>? Exercises);
record SaveWorkoutExerciseInput(int ExerciseId, string ExerciseName, int Sets, int Reps, decimal Weight, string MuscleGroupName, string SetDetails = "");
record SaveWorkoutRequest(DayOfWeek Day, DateTime StartedAt, DateTime CompletedAt, List<SaveWorkoutExerciseInput> Exercises);
record CopyRoutinesRequest(int SourceUserId, int TargetUserId);
record SaveTemplateRequest(string Name, string? Description, int SourceUserId);
record ApplyTemplateRequest(int TargetUserId);
record SaveMeasurementRequest(
    DateTime? MeasuredAt = null,
    decimal? Weight = null, decimal? Height = null,
    decimal? Chest = null, decimal? Waist = null, decimal? Hips = null,
    decimal? BicepLeft = null, decimal? BicepRight = null,
    decimal? ThighLeft = null, decimal? ThighRight = null,
    decimal? CalfLeft = null, decimal? CalfRight = null,
    decimal? Neck = null, decimal? BodyFat = null, string? Notes = null);
