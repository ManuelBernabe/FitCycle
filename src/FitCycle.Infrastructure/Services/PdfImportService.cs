using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitCycle.Infrastructure.Services;

public interface IPdfImportService
{
    Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId);
}

public class PdfImportService : IPdfImportService
{
    private readonly GeminiSettings _settings;
    private readonly IRoutineRepository _repo;
    private readonly ILogger<PdfImportService> _logger;
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(3) };

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PdfImportService(IOptions<GeminiSettings> settings, IRoutineRepository repo, ILogger<PdfImportService> logger)
    {
        _settings = settings.Value;
        // Fallback: read from env var directly if config binding didn't work
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            _settings.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                            ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                            ?? "";
        _repo = repo;
        _logger = logger;
    }

    public async Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return new PdfImportResult { Success = false, Message = "API key de Gemini no configurada. Configura Gemini__ApiKey o GEMINI_API_KEY en las variables de entorno." };

        // 1. Send PDF to Gemini API
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        var (extractedJson, apiError) = await CallGeminiAsync(pdfBase64);
        if (extractedJson == null)
            return new PdfImportResult { Success = false, Message = apiError ?? "No se pudo analizar el PDF." };

        // 2. Parse Gemini's response
        PdfExtraction? extraction;
        try
        {
            extraction = JsonSerializer.Deserialize<PdfExtraction>(extractedJson, _jsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Gemini response JSON: {Json}", extractedJson[..Math.Min(extractedJson.Length, 500)]);
            return new PdfImportResult { Success = false, Message = $"Error al parsear respuesta: {ex.Message}" };
        }

        if (extraction?.Routines == null || extraction.Routines.Count == 0)
            return new PdfImportResult { Success = false, Message = "No se encontraron rutinas en el PDF." };

        // 3. Get all muscle groups and exercises
        var allMuscleGroups = _repo.GetAllMuscleGroups();
        var allExercises = _repo.GetExercises();
        var result = new PdfImportResult { Success = true, Message = "Rutinas importadas correctamente." };

        // 4. Process each day
        foreach (var dayRoutine in extraction.Routines)
        {
            var dayOfWeek = dayRoutine.DayOfWeek switch
            {
                1 => DayOfWeek.Monday,
                2 => DayOfWeek.Tuesday,
                3 => DayOfWeek.Wednesday,
                4 => DayOfWeek.Thursday,
                5 => DayOfWeek.Friday,
                _ => (DayOfWeek?)null
            };

            if (dayOfWeek == null) continue;

            var summary = new DayImportSummary
            {
                DayOfWeek = dayRoutine.DayOfWeek,
                DayName = dayOfWeek.Value.ToString(),
            };

            // Map muscle group names to IDs
            var mgIds = new List<int>();
            foreach (var mgName in dayRoutine.MuscleGroups ?? new())
            {
                var mg = allMuscleGroups.FirstOrDefault(m =>
                    string.Equals(m.Name, mgName, StringComparison.OrdinalIgnoreCase));
                if (mg != null) mgIds.Add(mg.Id);
            }

            // Process exercises
            var exerciseInputs = new List<RoutineExerciseInput>();
            var supersetMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int supersetCounter = 1;

            foreach (var pdfEx in dayRoutine.Exercises ?? new())
            {
                // Find muscle group for this exercise
                var exMg = allMuscleGroups.FirstOrDefault(m =>
                    string.Equals(m.Name, pdfEx.MuscleGroup, StringComparison.OrdinalIgnoreCase));
                var muscleGroupId = exMg?.Id ?? mgIds.FirstOrDefault();
                if (muscleGroupId == 0 && allMuscleGroups.Count > 0)
                    muscleGroupId = allMuscleGroups[0].Id;

                // Find or create exercise
                var exercise = allExercises.FirstOrDefault(e =>
                    string.Equals(e.Name, pdfEx.Name, StringComparison.OrdinalIgnoreCase));

                if (exercise == null)
                {
                    exercise = _repo.AddExercise(pdfEx.Name ?? "Ejercicio", muscleGroupId);
                    allExercises = _repo.GetExercises(); // refresh
                    summary.NewExercisesCreated++;
                }

                // Build setDetails
                var setDetails = (pdfEx.Sets ?? new()).Select(s => new
                {
                    reps = s.Reps > 0 ? s.Reps : 12,
                    weight = 0,
                    tempoPos = s.TempoPos,
                    tempoNeg = s.TempoNeg,
                    grip = s.Grip ?? "",
                }).ToList();

                if (setDetails.Count == 0)
                    setDetails = Enumerable.Range(0, 3).Select(_ => new { reps = 12, weight = 0, tempoPos = 0, tempoNeg = 0, grip = "" }).ToList();

                // Handle supersets
                int supersetGroup = 0;
                if (!string.IsNullOrWhiteSpace(pdfEx.SupersetWith))
                {
                    var key = string.Compare(pdfEx.Name, pdfEx.SupersetWith, StringComparison.OrdinalIgnoreCase) < 0
                        ? $"{pdfEx.Name}|{pdfEx.SupersetWith}"
                        : $"{pdfEx.SupersetWith}|{pdfEx.Name}";

                    if (!supersetMap.TryGetValue(key, out supersetGroup))
                    {
                        supersetGroup = supersetCounter++;
                        supersetMap[key] = supersetGroup;
                    }
                }

                exerciseInputs.Add(new RoutineExerciseInput(
                    ExerciseId: exercise.Id,
                    Sets: setDetails.Count,
                    Reps: setDetails.Count > 0 ? setDetails[0].reps : 12,
                    Weight: 0,
                    SetDetails: JsonSerializer.Serialize(setDetails),
                    SupersetGroup: supersetGroup,
                    Notes: pdfEx.Notes ?? ""
                ));

                summary.ExerciseNames.Add(pdfEx.Name ?? "?");
            }

            summary.ExerciseCount = exerciseInputs.Count;

            // Save the day routine
            try
            {
                _repo.SetDayRoutine(dayOfWeek.Value, mgIds, exerciseInputs, targetUserId);
                result.Days.Add(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set routine for day {Day}", dayOfWeek);
            }
        }

        return result;
    }

    private async Task<(string? Json, string? Error)> CallGeminiAsync(string pdfBase64)
    {
        var prompt = @"Analiza este PDF de plan de entrenamiento y extrae TODA la información en formato JSON.

IMPORTANTE: Responde SOLO con el JSON, sin texto adicional, sin markdown, sin ```json```.

Formato requerido:
{
  ""routines"": [
    {
      ""dayOfWeek"": 1,
      ""muscleGroups"": [""Pecho"", ""Tríceps""],
      ""exercises"": [
        {
          ""name"": ""Press banca"",
          ""muscleGroup"": ""Pecho"",
          ""sets"": [
            { ""reps"": 12, ""tempoPos"": 2, ""tempoNeg"": 3, ""grip"": """" }
          ],
          ""notes"": ""Instrucciones del entrenador..."",
          ""supersetWith"": null
        }
      ]
    }
  ]
}

Reglas:
- dayOfWeek: 1=Lunes, 2=Martes, 3=Miércoles, 4=Jueves, 5=Viernes
- Grupos musculares válidos: Pecho, Espalda, Hombros, Bíceps, Tríceps, Piernas, Abdominales, Glúteos
- Tipos de agarre (grip): prono, supino, neutro (o cadena vacía si no se especifica)
- Si hay varias series con distintas repeticiones, crea un objeto por cada serie en el array ""sets""
- Si todas las series tienen las mismas reps, repite el objeto tantas veces como series haya
- NO incluyas pesos (se añadirán manualmente)
- tempoPos y tempoNeg son los segundos de la fase concéntrica y excéntrica (0 si no se especifica)
- Si hay superseries, pon el nombre exacto del ejercicio pareja en ""supersetWith""
- Extrae las notas/instrucciones del entrenador para cada ejercicio
- Si el PDF tiene rutinas para varios días, extrae cada uno por separado";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "application/pdf",
                                data = pdfBase64,
                            }
                        },
                        new
                        {
                            text = prompt,
                        }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 8192,
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_settings.ApiKey}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _http.SendAsync(httpRequest);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, responseText);
                return (null, $"Gemini API error {response.StatusCode}: {responseText[..Math.Min(responseText.Length, 300)]}");
            }

            // Parse the Gemini response to extract the text content
            using var doc = JsonDocument.Parse(responseText);
            var candidates = doc.RootElement.GetProperty("candidates");
            foreach (var candidate in candidates.EnumerateArray())
            {
                var content = candidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textProp))
                    {
                        var text = textProp.GetString() ?? "";
                        // Strip markdown code fences if present
                        text = text.Trim();
                        if (text.StartsWith("```json")) text = text[7..];
                        else if (text.StartsWith("```")) text = text[3..];
                        if (text.EndsWith("```")) text = text[..^3];
                        return (text.Trim(), null);
                    }
                }
            }

            return (null, "Gemini API respondió pero sin contenido de texto.");
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Gemini API call timed out");
            return (null, "Timeout: la API de Gemini tardó demasiado (>3 min).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Gemini API");
            return (null, $"Error llamando a Gemini API: {ex.Message}");
        }
    }
}

// -- DTOs --

public class PdfExtraction
{
    public List<PdfDayRoutine> Routines { get; set; } = new();
}

public class PdfDayRoutine
{
    public int DayOfWeek { get; set; }
    public List<string> MuscleGroups { get; set; } = new();
    public List<PdfExercise> Exercises { get; set; } = new();
}

public class PdfExercise
{
    public string? Name { get; set; }
    public string? MuscleGroup { get; set; }
    public List<PdfSet> Sets { get; set; } = new();
    public string? Notes { get; set; }
    public string? SupersetWith { get; set; }
}

public class PdfSet
{
    public int Reps { get; set; }
    public int TempoPos { get; set; }
    public int TempoNeg { get; set; }
    public string? Grip { get; set; }
}

// -- Result DTOs --

public class PdfImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<DayImportSummary> Days { get; set; } = new();
}

public class DayImportSummary
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = "";
    public int ExerciseCount { get; set; }
    public int NewExercisesCreated { get; set; }
    public List<string> ExerciseNames { get; set; } = new();
}
