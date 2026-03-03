using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

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
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            _settings.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                            ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                            ?? "";
        _repo = repo;
        _logger = logger;
    }

    public async Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId)
    {
        // 1. Extract text from PDF locally using PdfPig
        string pdfText;
        try
        {
            pdfText = ExtractTextFromPdf(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            return new PdfImportResult { Success = false, Message = $"Error al leer el PDF: {ex.Message}" };
        }

        if (string.IsNullOrWhiteSpace(pdfText))
            return new PdfImportResult { Success = false, Message = "No se pudo extraer texto del PDF." };

        _logger.LogInformation("Extracted {Chars} characters from PDF", pdfText.Length);

        // 2. Try Gemini API first, fall back to local parser if it fails
        PdfExtraction? extraction = null;
        string? parseError = null;

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            var (extractedJson, apiError) = await CallGeminiWithTextAsync(pdfText);
            if (extractedJson != null)
            {
                try
                {
                    extraction = JsonSerializer.Deserialize<PdfExtraction>(extractedJson, _jsonOpts);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Gemini JSON, falling back to local parser");
                    parseError = ex.Message;
                }
            }
            else
            {
                _logger.LogWarning("Gemini API failed: {Error}. Falling back to local parser.", apiError);
                parseError = apiError;
            }
        }
        else
        {
            _logger.LogInformation("No Gemini API key configured, using local parser");
        }

        // 3. Fallback: local regex parser
        if (extraction?.Routines == null || extraction.Routines.Count == 0)
        {
            _logger.LogInformation("Using local regex parser for PDF text");
            try
            {
                extraction = LocalPdfParser.Parse(pdfText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local parser also failed");
                return new PdfImportResult { Success = false, Message = parseError ?? $"Error al parsear PDF: {ex.Message}" };
            }
        }

        if (extraction?.Routines == null || extraction.Routines.Count == 0)
        {
            // Include extracted text lines containing "DÍA" for debugging
            var errLines = pdfText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var errDiaLines = errLines.Where(l => l.Contains("DÍA", StringComparison.OrdinalIgnoreCase) || l.Contains("DIA", StringComparison.OrdinalIgnoreCase))
                .Select(l => l.Trim().Length > 80 ? l.Trim()[..80] : l.Trim()).Take(10);
            var firstLines = errLines.Take(15).Select(l => l.Trim().Length > 60 ? l.Trim()[..60] : l.Trim());
            var debugText = $"Líneas con DÍA: [{string.Join(" | ", errDiaLines)}]. Primeras líneas: [{string.Join(" | ", firstLines)}]";
            return new PdfImportResult { Success = false, Message = $"No se encontraron rutinas. {debugText[..Math.Min(debugText.Length, 600)]}" };
        }

        // 4. Get all muscle groups and exercises
        var allMuscleGroups = _repo.GetAllMuscleGroups();
        var allExercises = _repo.GetExercises();

        // Debug: include found days and DÍA lines in message
        var foundDays = extraction.Routines.Select(r => $"Día{r.DayOfWeek}({r.Exercises.Count}ej)").ToList();
        var allTextLines = pdfText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var diaLines = allTextLines
            .Where(l => l.Contains("DÍA", StringComparison.OrdinalIgnoreCase) || l.Contains("DIA", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Trim().Length > 70 ? l.Trim()[..70] : l.Trim())
            .ToList();
        var debugMsg = $"Encontradas: {string.Join(", ", foundDays)}. Líneas DÍA: [{string.Join(" | ", diaLines)}]";
        var result = new PdfImportResult { Success = true, Message = debugMsg[..Math.Min(debugMsg.Length, 600)] };

        // 5. Process each day
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

            var mgIds = new List<int>();
            foreach (var mgName in dayRoutine.MuscleGroups ?? new())
            {
                var mg = allMuscleGroups.FirstOrDefault(m =>
                    string.Equals(m.Name, mgName, StringComparison.OrdinalIgnoreCase));
                if (mg != null) mgIds.Add(mg.Id);
            }

            var exerciseInputs = new List<RoutineExerciseInput>();
            var supersetMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int supersetCounter = 1;

            foreach (var pdfEx in dayRoutine.Exercises ?? new())
            {
                var exMg = allMuscleGroups.FirstOrDefault(m =>
                    string.Equals(m.Name, pdfEx.MuscleGroup, StringComparison.OrdinalIgnoreCase));
                var muscleGroupId = exMg?.Id ?? mgIds.FirstOrDefault();
                if (muscleGroupId == 0 && allMuscleGroups.Count > 0)
                    muscleGroupId = allMuscleGroups[0].Id;

                var exercise = allExercises.FirstOrDefault(e =>
                    string.Equals(e.Name, pdfEx.Name, StringComparison.OrdinalIgnoreCase));

                if (exercise == null)
                {
                    exercise = _repo.AddExercise(pdfEx.Name ?? "Ejercicio", muscleGroupId);
                    allExercises = _repo.GetExercises();
                    summary.NewExercisesCreated++;
                }

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

    private string ExtractTextFromPdf(byte[] pdfBytes)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            sb.AppendLine($"--- Página {page.Number} ---");

            // Group words into lines by Y-coordinate (vertical position)
            var lines = new List<(double y, List<(double x, string text)> words)>();

            foreach (var word in words)
            {
                var y = Math.Round(word.BoundingBox.Bottom, 1);
                var existingLine = lines.FirstOrDefault(l => Math.Abs(l.y - y) < 3);

                if (existingLine.words != null)
                {
                    existingLine.words.Add((word.BoundingBox.Left, word.Text));
                }
                else
                {
                    lines.Add((y, new List<(double x, string text)> { (word.BoundingBox.Left, word.Text) }));
                }
            }

            // Sort lines top-to-bottom (higher Y = higher on page in PDF coords)
            foreach (var line in lines.OrderByDescending(l => l.y))
            {
                var sortedWords = line.words.OrderBy(w => w.x).Select(w => w.text);
                sb.AppendLine(string.Join(" ", sortedWords));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<(string? Json, string? Error)> CallGeminiWithTextAsync(string pdfText)
    {
        if (pdfText.Length > 30000)
            pdfText = pdfText[..30000];

        var prompt = @"Analiza este texto extraído de un PDF de plan de entrenamiento y extrae TODA la información en formato JSON.

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
- dayOfWeek: 1=Lunes(DÍA 1), 2=Martes(DÍA 2), 3=Miércoles(DÍA 3), 4=Jueves(DÍA 4), 5=Viernes(DÍA 5)
- Grupos musculares válidos: Pecho, Espalda, Hombros, Bíceps, Tríceps, Piernas, Abdominales, Glúteos
- Mapea: PECTORAL→Pecho, ESPALDA→Espalda, HOMBRO(S)→Hombros, BÍCEPS→Bíceps, TRÍCEPS→Tríceps, CUADRICEPS/FEMORAL/ABDUCTOR/ADUCTOR/GEMELO→Piernas
- Tipos de agarre (grip): prono, supino, neutro (o vacío)
- Crea un objeto por cada serie en ""sets"" con sus reps
- tempoPos y tempoNeg: segundos de fase concéntrica/excéntrica (0 si no se especifica)
- supersetWith: nombre exacto del ejercicio pareja (null si no hay)
- Nombres de ejercicios en Title Case
- Extrae notas/instrucciones del entrenador

TEXTO DEL PDF:
" + pdfText;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 16384,
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={_settings.ApiKey}";

        // Retry up to 3 times with backoff for 429 errors
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                var delay = attempt * 5; // 5s, 10s
                _logger.LogInformation("Retrying Gemini API call in {Delay}s (attempt {Attempt}/3)", delay, attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _http.SendAsync(httpRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Gemini API 429 (attempt {Attempt}/3)", attempt + 1);
                    if (attempt < 2) continue; // retry
                    return (null, "Gemini API: demasiadas solicitudes (429). Se usará el parser local.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, responseText);
                    return (null, $"Gemini API error {response.StatusCode}: {responseText[..Math.Min(responseText.Length, 300)]}");
                }

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
                            text = text.Trim();
                            if (text.StartsWith("```json")) text = text[7..];
                            else if (text.StartsWith("```")) text = text[3..];
                            if (text.EndsWith("```")) text = text[..^3];
                            return (text.Trim(), null);
                        }
                    }
                }

                return (null, "Gemini respondió sin contenido de texto.");
            }
            catch (TaskCanceledException)
            {
                return (null, "Timeout: Gemini tardó demasiado (>3 min).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Gemini API");
                return (null, $"Error llamando a Gemini: {ex.Message}");
            }
        }

        return (null, "Gemini API: agotados los reintentos.");
    }
}

// -- Local PDF Parser (fallback when Gemini is unavailable) --

public static class LocalPdfParser
{
    private static readonly Dictionary<string, string> MuscleGroupMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PECTORAL"] = "Pecho", ["PECHO"] = "Pecho", ["CHEST"] = "Pecho",
        ["ESPALDA"] = "Espalda", ["DORSAL"] = "Espalda", ["BACK"] = "Espalda",
        ["HOMBRO"] = "Hombros", ["HOMBROS"] = "Hombros", ["DELTOIDES"] = "Hombros",
        ["BÍCEPS"] = "Bíceps", ["BICEPS"] = "Bíceps",
        ["TRÍCEPS"] = "Tríceps", ["TRICEPS"] = "Tríceps",
        ["PIERNA"] = "Piernas", ["PIERNAS"] = "Piernas",
        ["CUADRICEPS"] = "Piernas", ["CUÁDRICEPS"] = "Piernas",
        ["FEMORAL"] = "Piernas", ["ISQUIOTIBIAL"] = "Piernas",
        ["ABDUCTOR"] = "Piernas", ["ADUCTOR"] = "Piernas",
        ["GEMELO"] = "Piernas", ["GEMELOS"] = "Piernas", ["PANTORRILLA"] = "Piernas",
        ["ABDOMINAL"] = "Abdominales", ["ABDOMINALES"] = "Abdominales", ["ABS"] = "Abdominales",
        ["GLÚTEO"] = "Glúteos", ["GLÚTEOS"] = "Glúteos", ["GLUTEO"] = "Glúteos", ["GLUTEOS"] = "Glúteos",
        ["BRAZO"] = "Bíceps", ["BRAZOS"] = "Bíceps",
    };

    // Regex for day headers — matches anywhere in line, not just at start
    // Patterns: "PECTORAL-DÍA 1", "DÍA 2 ESPALDA+ FEMORAL", "DÍA3---CUADRICEPS"
    private static readonly Regex DayHeaderRegex = new(
        @"(?:([A-ZÁÉÍÓÚÑ\s\+]+?)\s*[-–—]+\s*)?D[IÍ]A\s*[-–—]*\s*(\d+)\s*[-–—]*\s*([A-ZÁÉÍÓÚÑ\s\+\(\)]*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Reps patterns: "4*15*12*10*8", "15 12 10 8", "REPS 15 12 10 8"
    private static readonly Regex RepsShorthand = new(
        @"(\d+)\s*\*\s*(\d+(?:\s*\*\s*\d+)*)", RegexOptions.Compiled);

    private static readonly Regex RepsLine = new(
        @"(?:REPS?|REPETICION(?:ES)?)\s*[:\s]*(\d[\d\s,*]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TempoPos = new(
        @"(?:FASE\s+POSITIVA|TEMPO\s+POS|CONC[EÉ]NTRIC[AO])\s*[:\s]*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TempoNeg = new(
        @"(?:FASE\s+NEGATIVA|TEMPO\s+NEG|EXC[EÉ]NTRIC[AO])\s*[:\s]*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SeriesCount = new(
        @"(?:SERIES?|SETS?)\s*[:\s]*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RestLine = new(
        @"(?:TIEMPO\s+DE\s+)?DESCANSO\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keywords that are NOT exercise names
    private static readonly HashSet<string> TableKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SERIE", "SERIES", "REPS", "REPETICIONES", "FASE", "POSITIVA", "NEGATIVA",
        "TEMPO", "DESCANSO", "REST", "AGARRE", "GRIP", "PESO", "WEIGHT",
        "PÁGINA", "PAGE", "PLAN", "ENTRENAMIENTO", "TRAINING", "NOTA", "NOTAS",
        "TIEMPO", "SEG", "SEGUNDOS", "MIN", "MINUTOS", "KG", "LBS",
    };

    // Words that signal instructional/note text, not exercise names
    private static readonly HashSet<string> NoteStartWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOVILIDAD", "HAZ", "POR", "SIEMPRE", "AUMENTANDO", "VAMOS",
        "RECUERDA", "IMPORTANTE", "NOTA", "NOTAS", "REALIZA", "MANTÉN",
        "INTENTA", "ASEGÚRATE", "CUIDADO", "EVITA", "NO", "SI", "CUANDO",
        "TIEMPO", "DESCANSO", "1º", "2º", "3º", "4º", "5º",
    };

    public static PdfExtraction Parse(string pdfText)
    {
        var extraction = new PdfExtraction();
        var lines = pdfText.Split('\n').Select(l => l.Trim()).ToList();

        // Find all day boundaries
        var dayBoundaries = new List<(int lineIndex, int dayNum, List<string> muscleGroups)>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            // Skip page separators and very short lines
            if (line.StartsWith("---") || line.Length < 3) continue;

            var match = DayHeaderRegex.Match(line);

            if (match.Success)
            {
                var dayNum = int.Parse(match.Groups[2].Value);
                if (dayNum < 1 || dayNum > 7) continue;

                // Avoid false matches on random numbers — require "DÍA" keyword
                var diaIdx = line.IndexOf("DÍA", StringComparison.OrdinalIgnoreCase);
                if (diaIdx < 0) diaIdx = line.IndexOf("DIA", StringComparison.OrdinalIgnoreCase);
                if (diaIdx < 0) continue;

                var muscleText = (match.Groups[1].Value + " " + match.Groups[3].Value).Trim();
                var muscleGroups = ExtractMuscleGroups(muscleText);

                if (muscleGroups.Count == 0)
                    muscleGroups = ExtractMuscleGroups(line);

                dayBoundaries.Add((i, dayNum, muscleGroups));
            }
        }

        if (dayBoundaries.Count == 0)
            return extraction;

        // Process each day section
        for (int d = 0; d < dayBoundaries.Count; d++)
        {
            var (startLine, dayNum, muscleGroups) = dayBoundaries[d];
            var endLine = d + 1 < dayBoundaries.Count ? dayBoundaries[d + 1].lineIndex : lines.Count;

            var dayRoutine = new PdfDayRoutine
            {
                DayOfWeek = dayNum,
                MuscleGroups = muscleGroups,
            };

            var sectionLines = lines.Skip(startLine + 1).Take(endLine - startLine - 1).ToList();
            dayRoutine.Exercises = ParseExercises(sectionLines, muscleGroups);

            if (dayRoutine.Exercises.Count > 0)
                extraction.Routines.Add(dayRoutine);
        }

        return extraction;
    }

    private static List<string> ExtractMuscleGroups(string text)
    {
        var groups = new List<string>();
        // Split by common separators
        var parts = Regex.Split(text, @"[\+\-–—,/\\&y]+", RegexOptions.IgnoreCase);

        foreach (var part in parts)
        {
            var clean = part.Trim().TrimEnd('S'); // handle plurals loosely
            var cleanFull = part.Trim();

            // Try exact match first, then trimmed
            foreach (var candidate in new[] { cleanFull, clean })
            {
                if (MuscleGroupMap.TryGetValue(candidate, out var mapped))
                {
                    if (!groups.Contains(mapped))
                        groups.Add(mapped);
                    break;
                }

                // Try partial match
                foreach (var kvp in MuscleGroupMap)
                {
                    if (candidate.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!groups.Contains(kvp.Value))
                            groups.Add(kvp.Value);
                        break;
                    }
                }
            }
        }

        return groups;
    }

    private static List<PdfExercise> ParseExercises(List<string> lines, List<string> dayMuscleGroups)
    {
        var exercises = new List<PdfExercise>();
        PdfExercise? current = null;
        var notesBuilder = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("---")) continue; // page separator

            // Check if this is a rest line (signals end of current exercise)
            if (RestLine.IsMatch(line))
            {
                if (current != null)
                {
                    current.Notes = notesBuilder.ToString().Trim();
                    notesBuilder.Clear();
                }
                continue;
            }

            // Try to extract reps
            var repsMatch = RepsLine.Match(line);
            if (repsMatch.Success && current != null)
            {
                var repsNums = Regex.Matches(repsMatch.Groups[1].Value, @"\d+")
                    .Select(m => int.Parse(m.Value)).ToList();
                current.Sets.Clear();
                foreach (var r in repsNums)
                    current.Sets.Add(new PdfSet { Reps = r });
                continue;
            }

            // Try shorthand reps like "4*15*12*10*8"
            var shortMatch = RepsShorthand.Match(line);
            if (shortMatch.Success && current != null)
            {
                var nums = Regex.Matches(line, @"\d+").Select(m => int.Parse(m.Value)).ToList();
                if (nums.Count >= 2)
                {
                    // First number could be series count, rest are reps
                    // Pattern: "4*15" means 4 sets of 15, "4*15*12*10*8" means reps per set
                    if (nums.Count == 2)
                    {
                        current.Sets.Clear();
                        for (int i = 0; i < nums[0]; i++)
                            current.Sets.Add(new PdfSet { Reps = nums[1] });
                    }
                    else
                    {
                        current.Sets.Clear();
                        foreach (var r in nums.Skip(0))
                            current.Sets.Add(new PdfSet { Reps = r });
                    }
                }
                continue;
            }

            // Try to extract tempo
            var tempoPosMatch = TempoPos.Match(line);
            if (tempoPosMatch.Success && current != null)
            {
                var val = int.Parse(tempoPosMatch.Groups[1].Value);
                foreach (var s in current.Sets) s.TempoPos = val;
                continue;
            }

            var tempoNegMatch = TempoNeg.Match(line);
            if (tempoNegMatch.Success && current != null)
            {
                var val = int.Parse(tempoNegMatch.Groups[1].Value);
                foreach (var s in current.Sets) s.TempoNeg = val;
                continue;
            }

            // Series count
            var seriesMatch = SeriesCount.Match(line);
            if (seriesMatch.Success && current != null && current.Sets.Count == 0)
            {
                var count = int.Parse(seriesMatch.Groups[1].Value);
                for (int i = 0; i < count; i++)
                    current.Sets.Add(new PdfSet { Reps = 12 });
                continue;
            }

            // Check if line looks like an exercise name (mostly uppercase, not a table keyword)
            if (IsExerciseName(line))
            {
                // Save previous exercise
                if (current != null)
                    current.Notes = notesBuilder.ToString().Trim();
                notesBuilder.Clear();

                current = new PdfExercise
                {
                    Name = ToTitleCase(line),
                    MuscleGroup = dayMuscleGroups.FirstOrDefault() ?? "Pecho",
                };
                exercises.Add(current);
                continue;
            }

            // Anything else is notes for the current exercise
            if (current != null && line.Length > 3)
            {
                notesBuilder.AppendLine(line);
            }
        }

        // Finalize last exercise
        if (current != null)
            current.Notes = notesBuilder.ToString().Trim();

        // Default sets for exercises with none
        foreach (var ex in exercises)
        {
            if (ex.Sets.Count == 0)
            {
                for (int i = 0; i < 3; i++)
                    ex.Sets.Add(new PdfSet { Reps = 12 });
            }
        }

        return exercises;
    }

    private static bool IsExerciseName(string line)
    {
        if (line.Length < 5) return false;
        if (line.Length > 80) return false;

        // Must have significant uppercase content
        var upperCount = line.Count(c => char.IsUpper(c));
        var letterCount = line.Count(c => char.IsLetter(c));
        if (letterCount == 0) return false;

        var upperRatio = (double)upperCount / letterCount;

        // Skip lines that are mostly numbers
        if (line.Count(char.IsDigit) > line.Count(char.IsLetter)) return false;

        // Skip lines containing DÍA (day headers)
        if (line.Contains("DÍA", StringComparison.OrdinalIgnoreCase) || line.Contains("DIA ", StringComparison.OrdinalIgnoreCase))
            return false;

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return false;

        // Skip lines that are entirely table keywords
        if (words.All(w => TableKeywords.Contains(w.TrimEnd(':', ',', '.', 'º'))))
            return false;

        // Skip lines starting with structural table keywords
        var firstWord = words[0].TrimEnd(':', ',', '.', 'º');
        if (TableKeywords.Contains(firstWord))
            return false;

        // Skip lines starting with note/instruction words
        if (NoteStartWords.Contains(firstWord))
            return false;

        // Skip lines starting with ordinals like "1º.", "2º."
        if (Regex.IsMatch(words[0], @"^\d+[º°]"))
            return false;

        // Exercise names are typically in UPPERCASE or mostly uppercase
        // and contain at least 2 words (e.g., "PRESS BANCA")
        return upperRatio > 0.5 && words.Length >= 2;
    }

    private static string ToTitleCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var ti = CultureInfo.GetCultureInfo("es-ES").TextInfo;
        return ti.ToTitleCase(text.ToLower(CultureInfo.GetCultureInfo("es-ES")));
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
