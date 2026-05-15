using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace FitCycle.Infrastructure.Services;

public interface IPdfImportService
{
    Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId, string language = "es");
    string ExtractTextFromPdf(byte[] pdfBytes);
    Task<PdfDiagnosticResult> DiagnosePdfAsync(byte[] pdfBytes);
}

public class PdfImportService : IPdfImportService
{
    private readonly IAiService _ai;
    private readonly IRoutineRepository _repo;
    private readonly ILogger<PdfImportService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PdfImportService(IAiService ai, IRoutineRepository repo, ILogger<PdfImportService> logger)
    {
        _ai = ai;
        _repo = repo;
        _logger = logger;
    }

    public async Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId, string language = "es")
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

        // 2. ALWAYS run local parser first (it's free and reliable)
        PdfExtraction? extraction = null;
        try
        {
            extraction = LocalPdfParser.Parse(pdfText);
            _logger.LogInformation("Local parser found {Days} days with {Ex} total exercises",
                extraction.Routines.Count,
                extraction.Routines.Sum(r => r.Exercises.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local parser failed");
        }

        // 3. Always try AI when configured — local parser may miss exercises within days
        var localUsefulDays = extraction?.Routines?.Count(r => r.Exercises.Count > 0) ?? 0;
        var localExerciseCount = extraction?.Routines?.Sum(r => r.Exercises.Count) ?? 0;
        if (_ai.IsConfigured)
        {
            _logger.LogInformation("Calling AI ({Provider}) — local found {Days} days, {Ex} exercises", _ai.ProviderName, localUsefulDays, localExerciseCount);
            var (extractedJson, apiError) = await CallGeminiWithTextAsync(pdfText);
            if (extractedJson != null)
            {
                try
                {
                    var aiExtraction = JsonSerializer.Deserialize<PdfExtraction>(extractedJson, _jsonOpts);
                    var aiUsefulDays = aiExtraction?.Routines?.Count(r => r.Exercises.Count > 0) ?? 0;
                    var aiExerciseCount = aiExtraction?.Routines?.Sum(r => r.Exercises.Count) ?? 0;
                    _logger.LogInformation("AI found {Days} days, {Ex} exercises", aiUsefulDays, aiExerciseCount);

                    extraction = MergeExtractions(extraction, aiExtraction, _logger);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse AI JSON response");
                }
            }
            else
            {
                _logger.LogWarning("AI failed: {Error}", apiError);
            }
        }

        // 4. Check results
        var daysWithExercises = extraction?.Routines?.Where(r => r.Exercises.Count > 0).ToList() ?? new();
        if (daysWithExercises.Count == 0)
        {
            var dayCount = extraction?.Routines?.Count ?? 0;
            var info = dayCount > 0
                ? $"Se encontraron {dayCount} cabeceras de día pero 0 ejercicios."
                : "No se encontraron cabeceras de día.";
            var errLines = pdfText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var diaLines = errLines.Where(l => Regex.IsMatch(l, @"D[IÍ]A", RegexOptions.IgnoreCase))
                .Select(l => l.Trim().Length > 80 ? l.Trim()[..80] : l.Trim()).Take(10);
            var first = errLines.Take(20).Select(l => l.Trim().Length > 60 ? l.Trim()[..60] : l.Trim());
            return new PdfImportResult
            {
                Success = false,
                Message = $"No se encontraron rutinas. {info}",
                DebugDiaLines = diaLines.ToList(),
                DebugLines = first.ToList(),
            };
        }

        // 4b. Translate exercise names if language is not Spanish
        if (!string.Equals(language, "es", StringComparison.OrdinalIgnoreCase) && daysWithExercises.Count > 0)
        {
            var allNames = daysWithExercises
                .SelectMany(d => d.Exercises)
                .Select(e => e.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (allNames.Count > 0)
            {
                var translations = await TranslateExerciseNamesAsync(allNames, language);
                if (translations.Count > 0)
                {
                    foreach (var day in daysWithExercises)
                    {
                        foreach (var ex in day.Exercises)
                        {
                            if (ex.Name != null && translations.TryGetValue(ex.Name, out var translated))
                                ex.Name = translated;
                        }
                    }
                    _logger.LogInformation("Translated {Count}/{Total} exercise names to {Lang}",
                        translations.Count, allNames.Count, language);
                }
            }
        }

        // 5. Get all muscle groups and exercises
        var allMuscleGroups = _repo.GetAllMuscleGroups();
        var allExercises = _repo.GetExercises();

        // Build result message with debug info
        var allDayInfo = extraction!.Routines.Select(r =>
        {
            var exNames = r.Exercises.Select(e => e.Name).Take(5);
            return $"D{r.DayOfWeek}({r.Exercises.Count}ej): {string.Join(", ", exNames)}";
        }).ToList();

        // Include extracted text for debugging
        var allLines = pdfText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var diaLinesDebug = allLines
            .Where(l => Regex.IsMatch(l, @"D[IÍ]A", RegexOptions.IgnoreCase))
            .Select(l => l.Trim().Length > 100 ? l.Trim()[..100] : l.Trim()).ToList();
        var pageLines = allLines
            .Where(l => l.TrimStart().StartsWith("---") || Regex.IsMatch(l, @"D[IÍ]A", RegexOptions.IgnoreCase)
                || l.Trim().Length > 10)
            .Select(l => l.Trim().Length > 120 ? l.Trim()[..120] : l.Trim())
            .Take(150).ToList();

        var result = new PdfImportResult
        {
            Success = true,
            Message = $"Importadas {daysWithExercises.Count} rutinas. {string.Join(" | ", allDayInfo)}",
            DebugLines = pageLines,
            DebugDiaLines = diaLinesDebug,
        };

        // 6. Process each day with exercises
        foreach (var dayRoutine in daysWithExercises)
        {
            var dayOfWeek = dayRoutine.DayOfWeek switch
            {
                1 => DayOfWeek.Monday,
                2 => DayOfWeek.Tuesday,
                3 => DayOfWeek.Wednesday,
                4 => DayOfWeek.Thursday,
                5 => DayOfWeek.Friday,
                6 => DayOfWeek.Saturday,
                7 => DayOfWeek.Sunday,
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

            // Force PDF order on save: sort by OrderHint (set by LocalPdfParser, re-numbered by MergeExtractions).
            var orderedExercises = (dayRoutine.Exercises ?? new()).OrderBy(e => e.OrderHint).ToList();
            foreach (var pdfEx in orderedExercises)
            {
                var exMg = allMuscleGroups.FirstOrDefault(m =>
                    string.Equals(m.Name, pdfEx.MuscleGroup, StringComparison.OrdinalIgnoreCase));
                var muscleGroupId = exMg?.Id ?? mgIds.FirstOrDefault();
                if (muscleGroupId == 0 && allMuscleGroups.Count > 0)
                    muscleGroupId = allMuscleGroups[0].Id;

                var pdfExName = pdfEx.Name ?? "Ejercicio";
                var normalizedPdfName = NormalizeExerciseName(pdfExName);
                var exercise = allExercises.FirstOrDefault(e =>
                    string.Equals(e.Name, pdfExName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(NormalizeExerciseName(e.Name), normalizedPdfName, StringComparison.OrdinalIgnoreCase));

                if (exercise == null)
                {
                    exercise = _repo.AddExercise(pdfExName, muscleGroupId);
                    allExercises = _repo.GetExercises();
                    summary.NewExercisesCreated++;
                    _logger.LogInformation("Created new exercise '{Name}' for muscle group {Mg}", pdfExName, exMg?.Name ?? "?");
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
            _logger.LogInformation("Day {Day}: saving {Count} exercises", dayOfWeek, exerciseInputs.Count);

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

    /// <summary>
    /// Runs the parser + AI without saving anything. Returns each source's
    /// extraction so you can see exactly which exercises each one found.
    /// </summary>
    public async Task<PdfDiagnosticResult> DiagnosePdfAsync(byte[] pdfBytes)
    {
        var result = new PdfDiagnosticResult();

        try { result.PdfText = ExtractTextFromPdf(pdfBytes); }
        catch (Exception ex) { result.Error = $"PDF extract failed: {ex.Message}"; return result; }

        result.PdfTextLength = result.PdfText.Length;

        try
        {
            var local = LocalPdfParser.Parse(result.PdfText);
            result.LocalRoutines = local.Routines.Select(r => new DiagDayRoutine
            {
                DayOfWeek = r.DayOfWeek,
                MuscleGroups = r.MuscleGroups,
                Exercises = r.Exercises.Select(e => e.Name ?? "").ToList(),
            }).ToList();
        }
        catch (Exception ex) { result.LocalError = ex.Message; }

        if (_ai.IsConfigured)
        {
            result.AiProvider = _ai.ProviderName;
            var (json, err) = await CallGeminiWithTextAsync(result.PdfText);
            result.AiRawResponse = json;
            result.AiError = err;
            if (json != null)
            {
                try
                {
                    var ai = JsonSerializer.Deserialize<PdfExtraction>(json, _jsonOpts);
                    result.AiRoutines = ai?.Routines?.Select(r => new DiagDayRoutine
                    {
                        DayOfWeek = r.DayOfWeek,
                        MuscleGroups = r.MuscleGroups,
                        Exercises = r.Exercises.Select(e => e.Name ?? "").ToList(),
                    }).ToList() ?? new();
                }
                catch (JsonException ex) { result.AiError = $"JSON parse error: {ex.Message}"; }
            }
        }

        return result;
    }

    /// <summary>
    /// Normalizes an exercise name for fuzzy matching: lowercase, no accents,
    /// collapsed whitespace. Used to dedupe variants like "Press Banca" vs "press banca".
    /// </summary>
    private static string NormalizeExerciseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var lower = name.Trim().ToLowerInvariant();
        // Remove accents
        var normalized = lower.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        // Collapse multiple spaces
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    /// <summary>
    /// Merges local-parser and AI extractions: for each day-of-week, keeps the
    /// version with more exercises. Days only present in one source are kept as-is.
    /// </summary>
    private static PdfExtraction MergeExtractions(PdfExtraction? local, PdfExtraction? ai, ILogger logger)
    {
        if (ai == null || ai.Routines.Count == 0) return local ?? new PdfExtraction();
        if (local == null || local.Routines.Count == 0) return ai;

        var merged = new PdfExtraction();
        var allDays = local.Routines.Select(r => r.DayOfWeek)
            .Union(ai.Routines.Select(r => r.DayOfWeek))
            .Distinct();

        foreach (var day in allDays)
        {
            var localDay = local.Routines.FirstOrDefault(r => r.DayOfWeek == day);
            var aiDay = ai.Routines.FirstOrDefault(r => r.DayOfWeek == day);

            if (localDay == null) { merged.Routines.Add(aiDay!); continue; }
            if (aiDay == null) { merged.Routines.Add(localDay); continue; }

            // Strategy:
            //  1. Start from local order (it always reflects the PDF reading order).
            //  2. Build a map of canonical names for fast lookup.
            //  3. Walk the AI list IN ORDER. Each AI exercise that isn't already in
            //     local gets inserted right after its predecessor's position (if found),
            //     otherwise appended. This preserves PDF order while still adding
            //     exercises the local parser missed.
            var combined = new PdfDayRoutine
            {
                DayOfWeek = day,
                MuscleGroups = (localDay.MuscleGroups.Count > 0 ? localDay.MuscleGroups : aiDay.MuscleGroups).ToList(),
                Exercises = localDay.Exercises.Select(e => e).ToList(),
            };

            string Norm(string? n) => (n ?? "").Trim().ToLowerInvariant();
            var seen = new HashSet<string>(combined.Exercises.Select(e => Norm(e.Name)));
            string? lastSeenLocalName = null;

            foreach (var aiEx in aiDay.Exercises)
            {
                var key = Norm(aiEx.Name);
                if (string.IsNullOrEmpty(key)) continue;
                if (seen.Contains(key))
                {
                    lastSeenLocalName = key;
                    continue;
                }
                // Insert after the last seen local exercise if any, else at the end
                int insertAt = combined.Exercises.Count;
                if (lastSeenLocalName != null)
                {
                    var idx = combined.Exercises.FindIndex(e => Norm(e.Name) == lastSeenLocalName);
                    if (idx >= 0) insertAt = idx + 1;
                }
                combined.Exercises.Insert(insertAt, aiEx);
                seen.Add(key);
                lastSeenLocalName = key;
            }

            // Re-number OrderHint in the merged list so the save path can rely on it.
            for (int i = 0; i < combined.Exercises.Count; i++) combined.Exercises[i].OrderHint = i;

            logger.LogInformation("Day {Day} merged: local={LocalCount}, ai={AiCount}, combined={Total} → {Names}",
                day, localDay.Exercises.Count, aiDay.Exercises.Count, combined.Exercises.Count,
                string.Join(" | ", combined.Exercises.Select(e => e.Name)));
            merged.Routines.Add(combined);
        }

        return merged;
    }

    /// <summary>Marker prefix that flags lines whose words are predominantly in the trainer's "exercise green" colour.</summary>
    public const string ExerciseMarker = "[EX] ";

    public string ExtractTextFromPdf(byte[] pdfBytes)
    {
        var sb = new StringBuilder();
        using var document = PdfDocument.Open(pdfBytes);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            sb.AppendLine($"--- Pagina {page.Number} ---");

            // Group words into lines by Y-coordinate. Each word carries a green flag from its glyph colour.
            var lines = new List<(double y, List<(double x, string text, bool green)> words)>();

            foreach (var word in words)
            {
                var y = Math.Round(word.BoundingBox.Bottom, 1);
                var green = IsGreenWord(word);
                var existingLine = lines.FirstOrDefault(l => Math.Abs(l.y - y) < 3);

                if (existingLine.words != null)
                {
                    existingLine.words.Add((word.BoundingBox.Left, word.Text, green));
                }
                else
                {
                    lines.Add((y, new List<(double x, string text, bool green)> { (word.BoundingBox.Left, word.Text, green) }));
                }
            }

            foreach (var line in lines.OrderByDescending(l => l.y))
            {
                var sortedWords = line.words.OrderBy(w => w.x).ToList();

                // Find the LEADING green segment of the line — that's the exercise name.
                // Any black text after it becomes the description / note on the following line.
                // This handles the very common case "Extensión de cuadriceps: Calentamiento 3 series con peso..."
                // where the green is only ~20% of letters but is unambiguously an exercise title.
                int leadingGreenEnd = 0;
                int leadingGreenLetters = 0;
                while (leadingGreenEnd < sortedWords.Count && sortedWords[leadingGreenEnd].green)
                {
                    leadingGreenLetters += sortedWords[leadingGreenEnd].text.Count(char.IsLetter);
                    leadingGreenEnd++;
                }

                bool hasLeadingGreen = leadingGreenLetters >= 2;
                if (hasLeadingGreen)
                {
                    var greenWords = sortedWords.Take(leadingGreenEnd).ToList();
                    var greenSegment = string.Join(" ", greenWords.Select(w => w.text));
                    var firstGreenWord = greenWords[0].text;

                    // Filter out lines whose leading green is actually a note (Tiempo, Descanso, Movilidad...)
                    // or a bare muscle-group section label ("Pectoral:", "Femoral:").
                    if (LooksLikeNoteHeader(firstGreenWord, greenWords))
                    {
                        sb.AppendLine(string.Join(" ", sortedWords.Select(w => w.text)));
                        continue;
                    }

                    sb.Append(ExerciseMarker).AppendLine(greenSegment);

                    // Black portion (if any) goes on the next logical line as a note for the exercise.
                    var blackWords = sortedWords.Skip(leadingGreenEnd).ToList();
                    if (blackWords.Count > 0)
                    {
                        var blackSegment = string.Join(" ", blackWords.Select(w => w.text));
                        if (!string.IsNullOrWhiteSpace(blackSegment))
                            sb.AppendLine(blackSegment);
                    }
                }
                else
                {
                    sb.AppendLine(string.Join(" ", sortedWords.Select(w => w.text)));
                }
            }

            sb.AppendLine();
        }

        var text = sb.ToString().Normalize(NormalizationForm.FormC);
        text = text.Replace('–', '-').Replace('—', '-').Replace('―', '-').Replace('−', '-');
        return text;
    }

    // Words that, when they are the FIRST green word of a line, mean the line is a
    // note/instruction (never an exercise) — e.g. "Tiempo de descanso", "Movilidad articular".
    private static readonly HashSet<string> NoteOnlyStarters = new(StringComparer.OrdinalIgnoreCase)
    {
        "TIEMPO", "DESCANSO", "REST", "MOVILIDAD", "NOTA", "NOTAS",
        "CALENTAMIENTO", "ENFRIAMIENTO", "ESTIRAMIENTO", "ESTIRAMIENTOS",
        "RECUERDA", "IMPORTANTE", "CUIDADO", "EVITA",
        "INICIO", "FIN", "FINAL", "PAUSA", "PAUSAS",
        "PROCURA", "MANTÉN", "MANTEN", "INTENTA", "ASEGÚRATE", "ASEGURATE",
        // Sentence connectors / participles — when these lead a green segment it's a wrapping
        // description, not an exercise (e.g. "Que quede en el aire, generando tensión...").
        "QUE", "Y", "PERO", "O", "U", "NI", "AUNQUE", "PORQUE", "PUES",
        "MIENTRAS", "CUANDO", "DONDE", "COMO", "HASTA", "DESDE",
        "QUEDE", "QUEDAR", "QUEDATE", "QUÉDATE",
        "DEJANDO", "GENERANDO", "MANTENIENDO", "HACIENDO", "SIENDO", "DANDO",
        "BAJANDO", "SUBIENDO", "EMPUJANDO", "TIRANDO", "APRETANDO",
        "PONIENDO", "PONIENDONOS", "PONIÉNDONOS", "PONIENDOTE", "PONIÉNDOTE",
    };

    // Whole-line annotations that look like exercises but are really table-cell intensity tags
    // (e.g. "PESO ALTO", "PESO LIGERO" written next to a reps number). Reject when the whole green
    // segment matches one of these. "Peso muerto" remains a valid exercise.
    internal static readonly HashSet<string> WeightAnnotations = new(StringComparer.OrdinalIgnoreCase)
    {
        "PESO ALTO", "PESO LIGERO", "PESO MEDIO", "PESO MAXIMO", "PESO MÁXIMO",
        "PESO MINIMO", "PESO MÍNIMO", "PESO BAJO", "ALTO PESO", "BAJO PESO",
    };

    // Pure muscle-group section labels — only reject if the ENTIRE line (minus punctuation)
    // is exactly one of these. "Femoral sentado:" must still be accepted as an exercise.
    private static readonly HashSet<string> PureSectionHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "PECTORAL", "PECTORALES", "PECHO",
        "ESPALDA", "HOMBROS", "HOMBRO",
        "PIERNAS", "PIERNA",
        "CUADRICEPS", "CUÁDRICEPS",
        "GLUTEO", "GLÚTEO", "GLUTEOS", "GLÚTEOS",
        "BICEPS", "BÍCEPS", "TRICEPS", "TRÍCEPS",
        "ABDOMINALES", "TRAPECIO", "TRAPECIOS",
    };

    /// <summary>
    /// True when a "green" line is actually a note or a bare section header rather than an exercise.
    ///  • First green word is a note-starter (TIEMPO, DESCANSO, MOVILIDAD, …) → note.
    ///  • Whole line trimmed of punctuation is exactly a muscle-group word (PECTORAL, ESPALDA, …) → section header.
    /// Multi-word movement names like "Femoral sentado", "Press banca plano", "Curl predicador" are accepted.
    /// </summary>
    private static bool LooksLikeNoteHeader(string firstWord, List<(double x, string text, bool green)> words)
    {
        var cleanFirst = firstWord.TrimEnd(':', '.', ',').ToUpperInvariant();
        if (NoteOnlyStarters.Contains(cleanFirst)) return true;

        // Pure single-word green muscle group with colon (e.g. "Pectoral:")
        var joined = string.Join(" ", words.Select(w => w.text)).Trim();
        var stripped = joined.TrimEnd(':', '.', ',').Trim();
        if (PureSectionHeaders.Contains(stripped.ToUpperInvariant())) return true;

        // Weight-intensity table annotations: "PESO ALTO", "PESO LIGERO" — never an exercise.
        if (WeightAnnotations.Contains(stripped)) return true;

        // Pure-digit / digit-only-with-units rows (table cells like "12", "12 reps").
        if (Regex.IsMatch(stripped, @"^\d+(\s*(reps?|series?|x|×|\*)\s*\d*)?$", RegexOptions.IgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// True if the first letter of the word is drawn in a green-ish fill colour.
    /// We accept any shade where G clearly dominates R and B, ignoring dark text.
    /// </summary>
    private static bool IsGreenWord(UglyToad.PdfPig.Content.Word word)
    {
        var letter = word.Letters?.FirstOrDefault(l => l.Value?.Length > 0 && char.IsLetter(l.Value[0]));
        if (letter == null) return false;
        var color = letter.Color;
        if (color == null) return false;
        try
        {
            var rgb = color.ToRGBValues();
            var r = (double)rgb.r;
            var g = (double)rgb.g;
            var b = (double)rgb.b;
            if (r < 0.2 && g < 0.2 && b < 0.2) return false;
            return g > 0.35 && g > r + 0.05 && g > b + 0.05;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Dictionary<string, string>> TranslateExerciseNamesAsync(List<string> names, string targetLanguage)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!_ai.IsConfigured)
        {
            _logger.LogInformation("No AI provider configured, skipping exercise name translation");
            return result;
        }

        var langName = targetLanguage switch
        {
            "en" => "English",
            "fr" => "French",
            _ => targetLanguage
        };

        var namesList = string.Join("\n", names.Select((n, i) => $"{i + 1}. {n}"));
        var prompt = $@"Translate these gym/fitness exercise names from Spanish to {langName}.
Return ONLY a JSON object mapping each original Spanish name to its {langName} translation.
Use Title Case for translations. Keep proper nouns (brand names, etc.) as-is.

Exercise names:
{namesList}

Example response format:
{{""Press Banca Inclinado"": ""Incline Bench Press"", ""Curl Con Mancuernas"": ""Dumbbell Curl""}}

Return ONLY the JSON object, no markdown, no explanation.";

        var (translations, error) = await _ai.GenerateStructuredAsync<Dictionary<string, string>>(prompt);
        if (error != null)
        {
            _logger.LogWarning("Failed to translate exercise names: {Error}", error);
            return result;
        }

        if (translations != null)
        {
            foreach (var kvp in translations)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                    result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private async Task<(string? Json, string? Error)> CallGeminiWithTextAsync(string pdfText)
    {
        // Modern LLMs (Llama 3.3, Gemini 2.0) handle 100K+ tokens easily;
        // 80K chars (~20K tokens) covers ~30-page training plans comfortably.
        if (pdfText.Length > 80000)
            pdfText = pdfText[..80000];

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
- dayOfWeek: 1=Lunes, 2=Martes, 3=Miércoles, 4=Jueves, 5=Viernes, 6=Sábado, 7=Domingo
- Las cabeceras de día pueden venir en formatos como:
    * ""DÍA 1"", ""DÍA 2 ESPALDA"", ""DÍA3---CUADRICEPS""
    * ""PECTORAL+TRÍCEPS+HOMBRO (MARTES)"", ""ESPALDA+ BÍCEPS (MIÉRCOLES)""
    * ""CUADRICEPS (LUNES-VIERNES)"" — un rango/lista de días significa que la MISMA rutina se repite en cada día.
      DEBES crear UNA entrada ""routines"" por cada día listado con los mismos ejercicios.
- Grupos musculares válidos: Pecho, Espalda, Hombros, Bíceps, Tríceps, Piernas, Abdominales, Glúteos
- Mapea: PECTORAL→Pecho, ESPALDA→Espalda, HOMBRO(S)→Hombros, BÍCEPS→Bíceps, TRÍCEPS→Tríceps, CUADRICEPS/FEMORAL/ABDUCTOR/ADUCTOR/GEMELO→Piernas, GLÚTEO→Glúteos
- Tipos de agarre (grip): prono, supino, neutro (o vacío)
- Crea un objeto por cada serie en ""sets"" con sus reps
- tempoPos y tempoNeg: segundos de fase concéntrica/excéntrica (0 si no se especifica)
- Las tablas con filas ""Serie / Reps / Fase positiva / Fase negativa"" indican una serie por columna con sus reps y tempos correspondientes
- supersetWith: nombre exacto del ejercicio pareja (null si no hay). Indicadores:
  • ""Ejercicio A + Ejercicio B"" en la misma línea
  • Una línea que EMPIEZA con ""+ super serie ..."" o ""+ X"" significa que ese ejercicio es la pareja del ejercicio inmediatamente anterior. Crea AMBOS ejercicios con supersetWith cruzado.
- Nombres de ejercicios en Title Case
- Extrae notas/instrucciones del entrenador
- **MUY IMPORTANTE**: las líneas que empiezan con el marcador ""[EX] "" son nombres de ejercicios reales
  (extraídos del color verde del PDF, que el entrenador usa para distinguir nombres de ejercicios).
  DEBES incluir TODOS los ejercicios marcados con [EX], aunque la línea sea larga o tenga descripción al lado.
  Quita el marcador ""[EX] "" del nombre cuando lo incluyas en el JSON.
- **ORDEN**: dentro de cada día, los ejercicios DEBEN aparecer en el JSON EN EL MISMO ORDEN en el que aparecen en el texto del PDF (de arriba hacia abajo). No reordenes nunca.
- **NO inventes ejercicios**. Si una línea verde es claramente un encabezado de sección o una nota (ej. ""Tiempo de descanso"", ""Movilidad articular"", ""Calentamiento"", o simplemente el nombre de un grupo muscular como ""Pectoral:""), NO la incluyas como ejercicio.

TEXTO DEL PDF:
" + pdfText;

        // 32K output tokens leaves headroom for 50+ exercises across 7 days
        return await _ai.GenerateContentAsync(prompt, maxOutputTokens: 32768);
    }
}

// -- Local PDF Parser --

public static class LocalPdfParser
{
    private static readonly Dictionary<string, string> MuscleGroupMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PECTORAL"] = "Pecho", ["PECHO"] = "Pecho", ["CHEST"] = "Pecho",
        ["ESPALDA"] = "Espalda", ["DORSAL"] = "Espalda", ["BACK"] = "Espalda", ["LUMBAR"] = "Espalda",
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

    // Regex for day headers — flexible to match many formats
    // "PECTORAL-DÍA 1", "DÍA 2 ESPALDA+ FEMORAL", "DÍA3---CUADRICEPS", "DÍA 4---HOMBRO:"
    private static readonly Regex DayHeaderRegex = new(
        @"D[IÍ]A\s*[-–—:]*\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex for headers that use Spanish day names like "(LUNES)", "(MARTES)", "(LUNES-VIERNES)"
    // Captures one or two day names so we can handle ranges/duplicates.
    private static readonly Regex SpanishDayHeaderRegex = new(
        @"\b(LUNES|MARTES|MI[EÉ]RCOLES|JUEVES|VIERNES|S[AÁ]BADO|DOMINGO)\b(?:\s*[-–—/,y\s]+\s*(LUNES|MARTES|MI[EÉ]RCOLES|JUEVES|VIERNES|S[AÁ]BADO|DOMINGO))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, int> SpanishDayMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LUNES"] = 1, ["MARTES"] = 2,
        ["MIÉRCOLES"] = 3, ["MIERCOLES"] = 3,
        ["JUEVES"] = 4, ["VIERNES"] = 5,
        ["SÁBADO"] = 6, ["SABADO"] = 6,
        ["DOMINGO"] = 7,
    };

    private static readonly Regex RestLine = new(
        @"(?:TIEMPO\s+DE\s+)?DESCANSO", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keywords that are NOT exercise names
    private static readonly HashSet<string> NonExerciseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SERIE", "SERIES", "REPS", "REPETICIONES", "FASE", "POSITIVA", "NEGATIVA",
        "TEMPO", "DESCANSO", "REST", "AGARRE", "GRIP", "WEIGHT",
        "PÁGINA", "PAGE", "PLAN", "ENTRENAMIENTO", "TRAINING", "NOTA", "NOTAS",
        "TIEMPO", "SEG", "SEGUNDOS", "MIN", "MINUTOS", "KG", "LBS",
        "RGANUTRI", "ASESORÍA", "TOTAL",
    };

    private static readonly HashSet<string> NoteStartWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOVILIDAD", "HAZ", "POR", "SIEMPRE", "AUMENTANDO", "VAMOS",
        "RECUERDA", "IMPORTANTE", "NOTA", "NOTAS", "REALIZA", "MANTÉN",
        "INTENTA", "ASEGÚRATE", "CUIDADO", "EVITA", "NO", "SI", "CUANDO",
        "TIEMPO", "DESCANSO",
        "BUSCAMOS", "PROCURA", "SUPER", "CALENTAREMOS", "ARRANCAMOS",
        "REALIZAREMOS", "FIN",
    };

    // Exercise-related keywords (equipment, movements, body parts used as exercise names)
    private static readonly HashSet<string> ExerciseKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Equipment
        "MÁQUINA", "MAQUINA", "BARRA", "MANCUERNA", "MANCUERNAS", "POLEA", "CABLE", "CABLES",
        "BANCO", "CUERDA", "MULTIPOWER", "SMITH", "NAUTILUS", "NAUTILIUS", "TRX",
        // Movements
        "PRESS", "CURL", "EXTENSIÓN", "EXTENSION", "PRENSA", "REMO", "ELEVACIÓN", "ELEVACION",
        "ELEVACIONES", "FONDOS", "APERTURA", "APERTURAS", "PULL", "JALÓN", "JALON",
        "DOMINADA", "DOMINADAS", "SENTADILLA", "SENTADILLAS", "BÚLGARA", "BULGARA",
        "PATADA", "PATADAS", "CRUCE", "CRUCES", "MUERTO", // peso muerto
        "ZANCADA", "ZANCADAS", "PASO", "PASOS", "HIP", "THRUST", "PESO",
        "CRUNCH", "CRUNCHES", "PLANCHA", "ABDOMINAL", "ABDOMINALES",
        // Types/modifiers
        "UNILATERAL", "BILATERAL", "INCLINADO", "INCLINADA", "PREDICADOR", "GIRONDA",
        "MARTILLO", "FRONTAL", "MILITAR", "TUMBADO", "TUMBADA", "SENTADO", "SENTADA",
        "DE PIE",
        // Body parts used as exercise name starters
        "FEMORAL", "FEMORALES", "TRAPECIO", "TRAPECIOS", "POSTERIOR", "POSTERIORES",
        "LATERAL", "LATERALES", "GEMELO", "GEMELOS", "ABDUCTOR", "ADUCTOR",
        "LUMBAR", "LUMBARES", "BÍCEPS", "BICEPS", "TRÍCEPS", "TRICEPS",
        "GLÚTEO", "GLUTEO", "HOMBRO", "HOMBROS", "CUADRICEPS", "CUÁDRICEPS",
        "PECHO", "PECTORAL", "ESPALDA", "PIERNA", "PIERNAS",
    };

    // Words that start instruction/note sentences — NOT exercise names
    private static readonly HashSet<string> InstructionStartWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles
        "EL", "LA", "LOS", "LAS", "UN", "UNA", "UNOS", "UNAS",
        // Prepositions
        "CON", "SIN", "PARA", "DESDE", "HACIA", "ENTRE", "SOBRE", "BAJO",
        "EN", "DE", "AL", "DEL",
        // Imperative verbs
        "POSICIONA", "COGE", "AGARRA", "TIRA", "EMPUJA", "GIRA", "COLOCA", "AJUSTA",
        "APRIETA", "CONTRAE", "ESTIRA", "FLEXIONA", "LEVANTA", "MUEVE", "SUBE", "BAJA",
        "ABRIMOS", "FLEXIONAMOS", "INICIO",
        // Instructional
        "PRIMERO", "PRIMER", "DESPUÉS", "DESPUES", "LUEGO", "AHORA", "TAMBIÉN", "TAMBIEN",
        "ADEMÁS", "ADEMAS", "AQUÍ", "AQUI", "COMO",
        // Demonstratives / pronouns
        "ESTE", "ESTA", "ESTOS", "ESTAS", "ESE", "ESA",
        "SE", "TE", "NOS", "ME", "QUE",
    };

    public static PdfExtraction Parse(string pdfText)
    {
        var extraction = new PdfExtraction();
        var lines = pdfText.Split('\n').Select(l => l.Trim()).ToList();

        // Pre-process: merge lines where DÍA is at end and next line starts with digit
        for (int i = 0; i < lines.Count - 1; i++)
        {
            if (Regex.IsMatch(lines[i], @"D[IÍ]A\s*$", RegexOptions.IgnoreCase))
            {
                var next = lines[i + 1].Trim();
                if (next.Length > 0 && char.IsDigit(next[0]))
                {
                    lines[i] = lines[i] + " " + next;
                    lines.RemoveAt(i + 1);
                }
            }
        }

        // Find all day boundaries
        var dayBoundaries = new List<(int lineIndex, int dayNum, List<string> muscleGroups)>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.StartsWith("---") && line.Contains("Página")) continue; // page separator
            if (line.Length < 3) continue;

            // Strip the [EX] color marker so day-header detection isn't confused by it.
            if (line.StartsWith(PdfImportService.ExerciseMarker, StringComparison.Ordinal))
                line = line[PdfImportService.ExerciseMarker.Length..].TrimStart();

            // Strategy 1: "DÍA N" format
            var dayNumbers = new List<int>();

            if (Regex.IsMatch(line, @"D[IÍ]A", RegexOptions.IgnoreCase))
            {
                var match = DayHeaderRegex.Match(line);
                if (match.Success)
                {
                    var dayNum = int.Parse(match.Groups[1].Value);
                    if (dayNum >= 1 && dayNum <= 7)
                        dayNumbers.Add(dayNum);
                }
            }

            // Strategy 2: Spanish day names — "(LUNES)", "(LUNES-VIERNES)", etc.
            if (dayNumbers.Count == 0)
            {
                var sMatch = SpanishDayHeaderRegex.Match(line);
                if (sMatch.Success)
                {
                    if (SpanishDayMap.TryGetValue(sMatch.Groups[1].Value, out var d1)) dayNumbers.Add(d1);
                    if (sMatch.Groups[2].Success && SpanishDayMap.TryGetValue(sMatch.Groups[2].Value, out var d2) && !dayNumbers.Contains(d2))
                        dayNumbers.Add(d2);
                }
            }

            if (dayNumbers.Count == 0) continue;

            // Extract muscle groups from the entire line
            var muscleGroups = ExtractMuscleGroups(line);

            // If no muscle groups found on this line, look at nearby lines
            if (muscleGroups.Count == 0)
            {
                for (int j = i + 1; j < Math.Min(i + 4, lines.Count); j++)
                {
                    var nextLine = lines[j].Trim();
                    if (string.IsNullOrWhiteSpace(nextLine) || nextLine.StartsWith("---")) continue;
                    if (Regex.IsMatch(nextLine, @"D[IÍ]A", RegexOptions.IgnoreCase)) break;
                    if (SpanishDayHeaderRegex.IsMatch(nextLine)) break;
                    var mg = ExtractMuscleGroups(nextLine);
                    if (mg.Count > 0) { muscleGroups = mg; break; }
                }
            }

            // Add one entry per detected day (handles LUNES-VIERNES = 2 entries with same content)
            foreach (var dayNum in dayNumbers)
            {
                if (dayBoundaries.Any(d => d.dayNum == dayNum)) continue; // avoid duplicates
                dayBoundaries.Add((i, dayNum, muscleGroups));
            }
        }

        // Sort by line index so subsequent slicing works
        dayBoundaries.Sort((a, b) => a.lineIndex.CompareTo(b.lineIndex));

        if (dayBoundaries.Count == 0)
            return extraction;

        // Process each day section
        for (int d = 0; d < dayBoundaries.Count; d++)
        {
            var (startLine, dayNum, muscleGroups) = dayBoundaries[d];
            // Find next boundary with a DIFFERENT line index — siblings (LUNES-VIERNES) share the same content
            int endLine = lines.Count;
            for (int n = d + 1; n < dayBoundaries.Count; n++)
            {
                if (dayBoundaries[n].lineIndex != startLine)
                {
                    endLine = dayBoundaries[n].lineIndex;
                    break;
                }
            }

            var sectionLines = lines.Skip(startLine + 1).Take(endLine - startLine - 1).ToList();

            var dayRoutine = new PdfDayRoutine
            {
                DayOfWeek = dayNum,
                MuscleGroups = muscleGroups,
                Exercises = ParseExercises(sectionLines, muscleGroups),
            };

            extraction.Routines.Add(dayRoutine);
        }

        return extraction;
    }

    private static List<string> ExtractMuscleGroups(string text)
    {
        var groups = new List<string>();
        var parts = Regex.Split(text, @"[\+\-–—,/\\&:()]+");

        foreach (var part in parts)
        {
            var clean = part.Trim();
            if (clean.Length < 3) continue;

            // Try each word in the part
            var words = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var w = word.TrimEnd('S', 's', ':', ',', '.');
                if (MuscleGroupMap.TryGetValue(word, out var mapped) ||
                    MuscleGroupMap.TryGetValue(w, out mapped))
                {
                    if (!groups.Contains(mapped))
                        groups.Add(mapped);
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
        bool inTable = false;
        string? pendingFaseType = null; // "positiva" or "negativa" — awaiting numbers on next Seg. line

        for (int idx = 0; idx < lines.Count; idx++)
        {
            var rawLine = lines[idx].Trim();
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            if (rawLine.StartsWith("---")) continue; // page separator

            // Extract the "[EX] " marker if present — those lines are guaranteed exercises (extracted from green text in the PDF).
            bool isMarkedExercise = rawLine.StartsWith(PdfImportService.ExerciseMarker, StringComparison.Ordinal);
            var line = isMarkedExercise ? rawLine[PdfImportService.ExerciseMarker.Length..].Trim() : rawLine;

            // Superset continuation: a line that starts with "+" (optionally followed by "super serie")
            // means the NEXT exercise we create is a superset partner of the previous one.
            // The trainer's PDF uses this pattern: "Femoral tumbado" (exercise) on one line,
            // then "+ Super serie femoral unilateral tumbado" on the next line.
            if (Regex.IsMatch(line, @"^\+\s*(super\s+serie\s+|superserie\s+)?", RegexOptions.IgnoreCase))
            {
                var partnerLine = Regex.Replace(line, @"^\+\s*(super\s+serie\s+|superserie\s+)?", "", RegexOptions.IgnoreCase).Trim();
                var partnerUpper = partnerLine.TrimEnd(':', '.', ',').ToUpperInvariant();
                bool isBogusPartner =
                    PdfImportService.WeightAnnotations.Contains(partnerUpper) ||
                    Regex.IsMatch(partnerUpper, @"^\d+(\s+(REPS?|SERIES?|PESO\s+(ALTO|LIGERO|MEDIO|BAJO|MAX[IÍ]MO|M[IÍ]NIMO)))?$");
                if (isBogusPartner)
                {
                    // Drop the whole line — it's a table-cell annotation, not an exercise.
                    continue;
                }
                if (partnerLine.Length >= 3 && current != null)
                {
                    FinalizeNotes(current, notesBuilder);
                    inTable = false;
                    pendingFaseType = null;

                    var partner = new PdfExercise
                    {
                        Name = ToTitleCase(CleanExerciseName(partnerLine)),
                        MuscleGroup = current.MuscleGroup,
                        SupersetWith = current.Name,
                        OrderHint = exercises.Count,
                    };
                    current.SupersetWith = partner.Name;
                    exercises.Add(partner);
                    ExtractGripFromName(partner);
                    current = partner;
                    continue;
                }
            }

            // "Seg. Ejecución" lines — may contain numbers for a pending Fase row
            if (Regex.IsMatch(line, @"Seg\.?\s*(?:de\s+)?(?:Ejecuci[oó]n|ejecuci[oó]n)", RegexOptions.IgnoreCase))
            {
                if (current != null && pendingFaseType != null)
                {
                    var nums = Regex.Matches(line, @"\d+").Select(m => int.Parse(m.Value)).ToList();
                    if (nums.Count > 0)
                    {
                        ApplyTempoValues(current, pendingFaseType, nums);
                        pendingFaseType = null; // only clear if we actually applied values
                    }
                    // If no numbers, keep pendingFaseType — numbers may be on next line
                }
                continue;
            }

            // Horizontal table "Serie" row: "Serie 1 2 3 4" — just skip (Reps row determines sets)
            if (Regex.IsMatch(line, @"^Serie\s+\d", RegexOptions.IgnoreCase))
            {
                pendingFaseType = null;
                continue;
            }

            // Rest/descanso line — finalize current exercise notes
            if (RestLine.IsMatch(line))
            {
                FinalizeNotes(current, notesBuilder);
                inTable = false;
                pendingFaseType = null;
                continue;
            }

            // Handle "Exercise name: N series x N reps" inline pattern
            var inlineMatch = Regex.Match(line, @"^(.+?):\s*(\d+)\s+series?\s*[x×]\s*(\d+)\s*reps?",
                RegexOptions.IgnoreCase);
            if (inlineMatch.Success)
            {
                var exName = inlineMatch.Groups[1].Value.Trim();
                if (exName.Length >= 3 && char.IsUpper(exName[0]))
                {
                    FinalizeNotes(current, notesBuilder);
                    inTable = false;
                    pendingFaseType = null;
                    var seriesCount = int.Parse(inlineMatch.Groups[2].Value);
                    var reps = int.Parse(inlineMatch.Groups[3].Value);
                    current = new PdfExercise
                    {
                        Name = ToTitleCase(CleanExerciseName(exName)),
                        MuscleGroup = dayMuscleGroups.FirstOrDefault() ?? "Pecho",
                    };
                    for (int i = 0; i < Math.Min(seriesCount, 10); i++)
                        current.Sets.Add(new PdfSet { Reps = reps });
                    current.OrderHint = exercises.Count;
                    exercises.Add(current);
                    // Extract grip from exercise name
                    ExtractGripFromName(current);
                    continue;
                }
            }

            // Check for exercise name FIRST (before table parsing).
            // [EX]-marked lines bypass the heuristic — they come from green-coloured text in the PDF.
            if (isMarkedExercise || IsExerciseName(line, lines, idx))
            {
                FinalizeNotes(current, notesBuilder);
                inTable = false;
                pendingFaseType = null;

                // Check for superset notation: "Exercise A + Exercise B"
                var plusParts = line.Split('+');
                if (plusParts.Length == 2 && plusParts[0].Trim().Length >= 3 && plusParts[1].Trim().Length >= 3)
                {
                    var nameA = ToTitleCase(CleanExerciseName(plusParts[0].Trim()));
                    var rawB = Regex.Replace(plusParts[1].Trim(), @"^super\s+serie\s+", "", RegexOptions.IgnoreCase).Trim();
                    var nameB = ToTitleCase(CleanExerciseName(rawB));

                    if (nameB.Length >= 3)
                    {
                        var exA = new PdfExercise
                        {
                            Name = nameA,
                            MuscleGroup = dayMuscleGroups.FirstOrDefault() ?? "Pecho",
                            SupersetWith = nameB,
                        };
                        var exB = new PdfExercise
                        {
                            Name = nameB,
                            MuscleGroup = dayMuscleGroups.FirstOrDefault() ?? "Pecho",
                            SupersetWith = nameA,
                        };
                        exA.OrderHint = exercises.Count;
                        exercises.Add(exA);
                        exB.OrderHint = exercises.Count;
                        exercises.Add(exB);
                        current = exB;
                        ExtractGripFromName(exA);
                        ExtractGripFromName(exB);
                        continue;
                    }
                }

                current = new PdfExercise
                {
                    Name = ToTitleCase(CleanExerciseName(line)),
                    MuscleGroup = dayMuscleGroups.FirstOrDefault() ?? "Pecho",
                    OrderHint = exercises.Count,
                };
                exercises.Add(current);
                ExtractGripFromName(current);
                continue;
            }

            if (current == null) continue; // Skip lines before first exercise

            // === FASE POSITIVA / NEGATIVA / AGARRE — MUST come before table header check ===
            // (IsTableHeader was incorrectly matching "Fase positiva" as a table header)
            bool matchedTempoGrip = false;

            var tpMatch = Regex.Match(line,
                @"(?:FASE\s+POSITIVA|CONC[EÉ]NTRIC[AO])\s*[:\s]*(.*)",
                RegexOptions.IgnoreCase);
            if (tpMatch.Success)
            {
                var nums = Regex.Matches(tpMatch.Groups[1].Value, @"\d+")
                    .Select(m => int.Parse(m.Value)).ToList();
                if (nums.Count > 0)
                {
                    ApplyTempoValues(current, "positiva", nums);
                    pendingFaseType = null;
                }
                else
                {
                    pendingFaseType = "positiva"; // numbers may be on next "Seg. Ejecución" line
                }
                matchedTempoGrip = true;
            }

            var tnMatch = Regex.Match(line,
                @"(?:FASE\s+NEGATIVA|EXC[EÉ]NTRIC[AO])\s*[:\s]*(.*)",
                RegexOptions.IgnoreCase);
            if (!tpMatch.Success && tnMatch.Success)
            {
                var nums = Regex.Matches(tnMatch.Groups[1].Value, @"\d+")
                    .Select(m => int.Parse(m.Value)).ToList();
                if (nums.Count > 0)
                {
                    ApplyTempoValues(current, "negativa", nums);
                    pendingFaseType = null;
                }
                else
                {
                    pendingFaseType = "negativa"; // numbers may be on next "Seg. Ejecución" line
                }
                matchedTempoGrip = true;
            }

            // AGARRE row: "Agarre Prono Prono Neutro Supino" — extract ALL grip values
            var gripMatch = Regex.Match(line,
                @"(?:AGARRE|GRIP)\s*[:\s]+(.*)",
                RegexOptions.IgnoreCase);
            if (gripMatch.Success)
            {
                var gripValues = Regex.Matches(gripMatch.Groups[1].Value, @"(prono|supino|neutro)",
                    RegexOptions.IgnoreCase).Select(m => m.Value.ToLower()).ToList();
                if (gripValues.Count > 0)
                {
                    if (gripValues.Count == 1)
                    {
                        // Single grip value → apply to all sets
                        foreach (var s in current.Sets) s.Grip = gripValues[0];
                    }
                    else
                    {
                        // Per-column grip values → apply per set
                        for (int gi = 0; gi < current.Sets.Count && gi < gripValues.Count; gi++)
                            current.Sets[gi].Grip = gripValues[gi];
                    }
                }
                matchedTempoGrip = true;
            }

            if (matchedTempoGrip) continue;

            // Table header detection: "Serie Reps Fase positiva Fase negativa"
            if (IsTableHeader(line))
            {
                inTable = true;
                continue;
            }

            // Table data row: "1 15 2 seg 3 seg" or "1 15 2 3"
            if (inTable)
            {
                var row = TryParseTableRow(line);
                if (row != null)
                {
                    current.Sets.Add(row);
                    continue;
                }
                // Not a valid row — exit table mode
                inTable = false;
            }

            // Individual pattern matchers (for non-table formats)

            // "N series de N" or "N series de N,N,N"
            var seriesDeMatch = Regex.Match(line, @"(\d+)\s+series?\s+(?:de\s+)?(\d[\d\s,]*)",
                RegexOptions.IgnoreCase);
            if (seriesDeMatch.Success && current != null)
            {
                var count = int.Parse(seriesDeMatch.Groups[1].Value);
                var repsNums = Regex.Matches(seriesDeMatch.Groups[2].Value, @"\d+")
                    .Select(m => int.Parse(m.Value)).ToList();
                current.Sets.Clear();
                if (repsNums.Count == 1)
                {
                    for (int i = 0; i < Math.Min(count, 10); i++)
                        current.Sets.Add(new PdfSet { Reps = repsNums[0] });
                }
                else
                {
                    foreach (var r in repsNums)
                        current.Sets.Add(new PdfSet { Reps = r });
                }
                continue;
            }

            // Reps shorthand: "4*15*12*10*8" or "4x15"
            if (Regex.IsMatch(line, @"\d+\s*[*x×]\s*\d+", RegexOptions.IgnoreCase))
            {
                var nums = Regex.Matches(line, @"\d+").Select(m => int.Parse(m.Value)).Where(n => n > 0).ToList();
                if (nums.Count >= 2)
                {
                    current.Sets.Clear();
                    if (nums.Count == 2)
                    {
                        for (int i = 0; i < nums[0]; i++)
                            current.Sets.Add(new PdfSet { Reps = nums[1] });
                    }
                    else
                    {
                        foreach (var r in nums)
                            current.Sets.Add(new PdfSet { Reps = r });
                    }
                }
                continue;
            }

            // REPS line: "REPS 15 12 10 8"
            var repsMatch = Regex.Match(line, @"(?:REPS?|REPETICION(?:ES)?)\s*[:\s]+(\d[\d\s,*]+)",
                RegexOptions.IgnoreCase);
            if (repsMatch.Success)
            {
                var repsNums = Regex.Matches(repsMatch.Groups[1].Value, @"\d+")
                    .Select(m => int.Parse(m.Value)).ToList();
                current.Sets.Clear();
                foreach (var r in repsNums)
                    current.Sets.Add(new PdfSet { Reps = r });
                continue;
            }

            // SERIES: N
            var seriesMatch = Regex.Match(line, @"(?:SERIES?|SETS?)\s*[:\s]*(\d+)",
                RegexOptions.IgnoreCase);
            if (seriesMatch.Success && current.Sets.Count == 0)
            {
                var count = int.Parse(seriesMatch.Groups[1].Value);
                for (int i = 0; i < Math.Min(count, 10); i++)
                    current.Sets.Add(new PdfSet { Reps = 12 });
                continue;
            }

            // Standalone numbers line after pending Fase: "2 2 3 4"
            if (pendingFaseType != null && current != null && Regex.IsMatch(line, @"^\d[\d\s]+$"))
            {
                var nums = Regex.Matches(line, @"\d+").Select(m => int.Parse(m.Value)).ToList();
                if (nums.Count > 0)
                {
                    ApplyTempoValues(current, pendingFaseType, nums);
                    pendingFaseType = null;
                    continue;
                }
            }

            // Anything else → notes (if it has some text substance)
            if (line.Length > 3)
                notesBuilder.AppendLine(line);
        }

        FinalizeNotes(current, notesBuilder);

        // Default sets for exercises with none
        foreach (var ex in exercises.Where(e => e.Sets.Count == 0))
            for (int i = 0; i < 3; i++)
                ex.Sets.Add(new PdfSet { Reps = 12 });

        // Propagate grip from exercise name to sets that don't have one yet
        foreach (var ex in exercises)
        {
            if (string.IsNullOrWhiteSpace(ex.Name)) continue;
            var nameGrip = Regex.Match(ex.Name, @"agarre\s+(prono|supino|neutro)", RegexOptions.IgnoreCase);
            if (nameGrip.Success)
            {
                var grip = nameGrip.Groups[1].Value.ToLower();
                foreach (var s in ex.Sets.Where(s => string.IsNullOrEmpty(s.Grip)))
                    s.Grip = grip;
            }
        }

        return exercises;
    }

    private static void FinalizeNotes(PdfExercise? ex, StringBuilder sb)
    {
        if (ex != null)
            ex.Notes = sb.ToString().Trim();
        sb.Clear();
    }

    /// <summary>
    /// Apply tempo values per-set (if multiple) or to all sets (if single value).
    /// </summary>
    private static void ApplyTempoValues(PdfExercise ex, string faseType, List<int> values)
    {
        if (values.Count == 1)
        {
            // Single value → apply to all sets
            foreach (var s in ex.Sets)
            {
                if (faseType == "positiva") s.TempoPos = values[0];
                else s.TempoNeg = values[0];
            }
        }
        else
        {
            // Per-column values → apply per set
            for (int i = 0; i < ex.Sets.Count && i < values.Count; i++)
            {
                if (faseType == "positiva") ex.Sets[i].TempoPos = values[i];
                else ex.Sets[i].TempoNeg = values[i];
            }
        }
    }

    /// <summary>
    /// Extract grip type from exercise name: "Máquina agarre supino" → grip = "supino"
    /// Sets the default grip for all sets that don't have one yet.
    /// </summary>
    private static void ExtractGripFromName(PdfExercise ex)
    {
        if (string.IsNullOrWhiteSpace(ex.Name)) return;
        var match = Regex.Match(ex.Name, @"agarre\s+(prono|supino|neutro)", RegexOptions.IgnoreCase);
        if (!match.Success) return;
        var grip = match.Groups[1].Value.ToLower();
        foreach (var s in ex.Sets.Where(s => string.IsNullOrEmpty(s.Grip)))
            s.Grip = grip;
        // Also set as default for future sets added later
        ex.Notes = string.IsNullOrEmpty(ex.Notes) ? $"grip:{grip}" : ex.Notes;
    }

    /// <summary>
    /// Detects table header lines like "Serie Reps Fase positiva Fase negativa"
    /// </summary>
    private static bool IsTableHeader(string line)
    {
        var lower = line.ToLowerInvariant();
        // Reject inline rep schemes: "3 series x 20 reps", "4 series de 15"
        if (Regex.IsMatch(lower, @"\d+\s+series?\s+(?:x|de)\s+\d+")) return false;
        // Reject lines that are too long to be table headers (likely exercise+instruction)
        if (lower.Length > 80) return false;
        // A real table header MUST contain "serie" or "reps" — prevents false positives
        // on lines like "Fase positiva" or "Fase negativa" which are data rows
        bool hasSerie = lower.Contains("serie");
        bool hasReps = lower.Contains("reps") || lower.Contains("repeticion");
        if (!hasSerie && !hasReps) return false;
        int hits = 0;
        if (hasSerie) hits++;
        if (hasReps) hits++;
        if (lower.Contains("fase")) hits++;
        if (lower.Contains("positiva") || lower.Contains("negativa")) hits++;
        if (lower.Contains("agarre") || lower.Contains("grip")) hits++;
        return hits >= 2;
    }

    /// <summary>
    /// Parses a table data row: "1 15 2 seg 3 seg" → PdfSet with reps, tempo
    /// Numbers: [serieNum, reps, tempoPos?, tempoNeg?]
    /// </summary>
    private static PdfSet? TryParseTableRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || !char.IsDigit(trimmed[0])) return null;

        var numbers = Regex.Matches(trimmed, @"\d+")
            .Select(m => int.Parse(m.Value)).ToList();

        if (numbers.Count < 2) return null;

        // First number is serie index (1-10), filter out unreasonable values
        if (numbers[0] < 1 || numbers[0] > 10) return null;

        // Second number is reps — should be reasonable (1-100)
        if (numbers[1] < 1 || numbers[1] > 100) return null;

        // Extract grip text (prono/supino/neutro) from the row
        var gripMatch = Regex.Match(trimmed, @"(prono|supino|neutro)", RegexOptions.IgnoreCase);

        return new PdfSet
        {
            Reps = numbers[1],
            TempoPos = numbers.Count > 2 ? numbers[2] : 0,
            TempoNeg = numbers.Count > 3 ? numbers[3] : 0,
            Grip = gripMatch.Success ? gripMatch.Groups[1].Value.ToLower() : "",
        };
    }

    /// <summary>
    /// Multi-tier exercise name detection:
    /// Tier 1: Mostly UPPERCASE (Days 1-2 format: "PRESS BANCA INCLINADO")
    /// Tier 2: Mixed case ending with ":" (Days 3-5: "Extensión de cuadriceps:")
    /// Tier 3: Mixed case with exercise keyword or table lookahead ("Curl predicador en máquina")
    /// </summary>
    private static bool IsExerciseName(string line, List<string> allLines, int currentIndex)
    {
        if (line.Length < 4 || line.Length > 120) return false;

        var letterCount = line.Count(char.IsLetter);
        if (letterCount == 0) return false;

        // Skip lines that are mostly numbers
        if (line.Count(char.IsDigit) > letterCount) return false;

        // Skip lines containing DÍA (day headers)
        if (Regex.IsMatch(line, @"D[IÍ]A", RegexOptions.IgnoreCase)) return false;

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return false;

        var firstWord = words[0].TrimEnd(':', ',', '.', 'º', '°', '+');

        // Skip lines starting with table/note/instruction keywords
        if (NonExerciseWords.Contains(firstWord)) return false;
        if (NoteStartWords.Contains(firstWord)) return false;
        if (InstructionStartWords.Contains(firstWord)) return false;

        // Skip ordinals: "1º.", "2º."
        if (Regex.IsMatch(words[0], @"^\d+[º°]")) return false;

        // Skip lines that are entirely table keywords
        if (words.All(w => NonExerciseWords.Contains(w.TrimEnd(':', ',', '.', 'º', '°'))))
            return false;

        // Skip muscle group description lines (≥2 muscle group keywords, high ratio)
        var mgHits = CountMuscleGroupKeywords(line);
        var cleanWords = Regex.Split(line, @"[\+\-–—,/\\&\s:()]+")
            .Where(w => w.Length >= 3).ToList();
        if (mgHits >= 2 && cleanWords.Count > 0 && mgHits >= cleanWords.Count * 0.5)
            return false;

        // --- TIER 1: Mostly UPPERCASE (days 1-2 format) ---
        var upperCount = line.Count(char.IsUpper);
        var upperRatio = (double)upperCount / letterCount;
        if (upperRatio > 0.5)
        {
            if (words.Length == 1)
                return line.TrimEnd(':', '.', ',').Length >= 7
                    && !MuscleGroupMap.ContainsKey(line.TrimEnd(':', '.', ','));
            return words.Length >= 2;
        }

        // --- From here: mixed-case lines (days 3-5 format) ---
        // Must start with uppercase letter
        if (!char.IsUpper(line[0])) return false;

        // --- TIER 2: Ends with ":" (common format: "Extensión de cuadriceps:") ---
        // Also match "exercise: N series x N" where colon is mid-line
        var colonIdx = line.IndexOf(':');
        var endsWithColon = line.TrimEnd().EndsWith(':');
        var hasInlineReps = colonIdx > 0 && colonIdx < line.Length - 1
            && Regex.IsMatch(line[(colonIdx + 1)..], @"^\s*\d+\s+series?", RegexOptions.IgnoreCase);
        if ((endsWithColon || hasInlineReps) && words.Length <= 15)
        {
            return true;
        }

        // --- TIER 3: Mixed case without colon ---
        // Superset lines (with +) tend to be longer
        var isSupersetLine = line.Contains('+');
        if (words.Length > (isSupersetLine ? 16 : 10)) return false;
        if (line.Length > (isSupersetLine ? 120 : 80)) return false;

        // Accept if contains exercise keyword (equipment, movement, etc.)
        if (ContainsExerciseKeyword(line)) return true;

        // Accept if followed by a table header within 5 lines (lookahead)
        for (int i = currentIndex + 1; i < Math.Min(currentIndex + 6, allLines.Count); i++)
        {
            var ahead = allLines[i].Trim();
            if (string.IsNullOrWhiteSpace(ahead) || ahead.StartsWith("---")) continue;
            if (IsTableHeader(ahead)) return true;
            if (Regex.IsMatch(ahead, @"D[IÍ]A", RegexOptions.IgnoreCase)) break;
            if (RestLine.IsMatch(ahead)) break;
        }

        return false;
    }

    private static int CountMuscleGroupKeywords(string line)
    {
        int count = 0;
        var used = new HashSet<string>();
        foreach (var kvp in MuscleGroupMap)
        {
            if (!used.Contains(kvp.Value) &&
                line.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                count++;
                used.Add(kvp.Value); // Count each muscle group only once
            }
        }
        return count;
    }

    private static bool ContainsExerciseKeyword(string line)
    {
        var words = Regex.Split(line, @"[\s\+\-–—,/\\&:()]+");
        foreach (var word in words)
        {
            if (word.Length < 3) continue;
            if (ExerciseKeywords.Contains(word)) return true;
            // Try without trailing 's' for plural forms
            var trimmed = word.TrimEnd('s', 'S');
            if (trimmed.Length >= 3 && ExerciseKeywords.Contains(trimmed)) return true;
        }
        return false;
    }

    /// <summary>
    /// Cleans exercise name: remove numbering, trailing punctuation, truncate long names
    /// </summary>
    private static string CleanExerciseName(string name)
    {
        // Remove leading numbering like "1." or "1-"
        name = Regex.Replace(name, @"^\d+[\.\-\)]\s*", "").Trim();

        // Truncate at "Name: <description>" — keep only the part before the colon when there
        // is substantive content after it. Handles cases like "Abductor: PONIENDONOS DE PIE...".
        var colonIdx = name.IndexOf(':');
        if (colonIdx > 2 && colonIdx < name.Length - 1)
        {
            var after = name[(colonIdx + 1)..].Trim();
            if (after.Length >= 3)
                name = name[..colonIdx].Trim();
        }

        // Remove trailing colons, periods, dashes
        name = name.TrimEnd(':', '.', ',', '-', ' ');

        // Truncate at instruction connectors (Spanish)
        foreach (var connector in new[] {
            " en la que ", " en el que ", " con la que ", " con el que ",
            " para que ", " donde ", " ya que " })
        {
            var ci = name.IndexOf(connector, StringComparison.OrdinalIgnoreCase);
            if (ci > 5)
            {
                name = name[..ci].TrimEnd(' ', '-', ',');
                break;
            }
        }

        // Truncate at parenthesis if name is already substantial
        var parenIdx = name.IndexOf('(');
        if (parenIdx > 5)
            name = name[..parenIdx].TrimEnd(' ', '-', ',');

        // Truncate long names at "- " break points (instruction after dash)
        if (name.Length > 50)
        {
            var dashIdx = name.IndexOf("- ", 20);
            if (dashIdx > 0 && dashIdx < name.Length - 3)
                name = name[..dashIdx].TrimEnd(' ', '-');
        }

        // Final length cap with word boundary
        if (name.Length > 60)
        {
            var lastSpace = name.LastIndexOf(' ', 60);
            if (lastSpace > 20)
                name = name[..lastSpace];
        }

        return name.TrimEnd(':', '.', ',', '-', ' ');
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
    /// <summary>
    /// 0-based hint of the exercise's position in the source PDF, used to preserve
    /// PDF reading order across the local-parser → AI merge step.
    /// </summary>
    public int OrderHint { get; set; }
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
    public List<string>? DebugLines { get; set; }
    public List<string>? DebugDiaLines { get; set; }
}

public class DayImportSummary
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = "";
    public int ExerciseCount { get; set; }
    public int NewExercisesCreated { get; set; }
    public List<string> ExerciseNames { get; set; } = new();
}

// -- Diagnostic DTOs (for /routines/diagnose-pdf) --

public class PdfDiagnosticResult
{
    public string? Error { get; set; }
    public string PdfText { get; set; } = "";
    public int PdfTextLength { get; set; }
    public List<DiagDayRoutine> LocalRoutines { get; set; } = new();
    public string? LocalError { get; set; }
    public string? AiProvider { get; set; }
    public List<DiagDayRoutine> AiRoutines { get; set; } = new();
    public string? AiRawResponse { get; set; }
    public string? AiError { get; set; }
}

public class DiagDayRoutine
{
    public int DayOfWeek { get; set; }
    public List<string> MuscleGroups { get; set; } = new();
    public List<string> Exercises { get; set; } = new();
}
