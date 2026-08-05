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

                    // The AI sometimes echoes back table-cell annotations ("Peso Alto") or
                    // coaching descriptions ("Peso ligero y que se pueda controlar...") as if
                    // they were exercises. Strip them out before merging so they don't get
                    // re-introduced after the local parser already rejected them.
                    if (aiExtraction != null) SanitizeAiExtraction(aiExtraction, _logger);

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
            // Exercise.Id → normalized PDF name that claimed it in THIS day. Two DIFFERENT
            // PDF exercises must never resolve to the same Exercise row (that's how the two
            // aperturas of día 1 collapsed into one) — but the same movement repeated in the
            // day (Extensión de cuadríceps opens AND closes Lunes) legitimately shares its Id.
            var claimedExerciseIds = new Dictionary<int, string>();

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
                var claimedByOthers = claimedExerciseIds
                    .Where(kv => !string.Equals(kv.Value, normalizedPdfName, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Key)
                    .ToHashSet();
                var exercise = allExercises.FirstOrDefault(e =>
                    !claimedByOthers.Contains(e.Id) &&
                    (string.Equals(e.Name, pdfExName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(NormalizeExerciseName(e.Name), normalizedPdfName, StringComparison.OrdinalIgnoreCase)));

                // Fuzzy fallback: reuse the existing exercise even if the new name dropped/added
                // parenthetical descriptors or articles. Critical for preserving images uploaded
                // by the user across re-imports — Exercise.ImageUrl is keyed on Exercise.Id, so
                // creating a new Exercise would orphan the photo.
                if (exercise == null)
                {
                    exercise = FindBestFuzzyExerciseMatch(pdfExName, allExercises, claimedByOthers);
                    if (exercise != null)
                    {
                        _logger.LogInformation("Fuzzy-matched '{Pdf}' to existing exercise '{Existing}' (preserves image)", pdfExName, exercise.Name);
                        // Keep the row (preserves the user's uploaded photo, keyed on the Id),
                        // but when the PDF name is MORE specific than the stored one, upgrade
                        // the stored name so the app shows the trainer's full wording
                        // ("Press militar" → "Press Militar En Barra Multipower").
                        if (SignificantTokens(pdfExName).Count > SignificantTokens(exercise.Name).Count)
                        {
                            var renamed = _repo.RenameExercise(exercise.Id, pdfExName);
                            if (renamed != null)
                            {
                                exercise = renamed;
                                allExercises = _repo.GetExercises();
                                _logger.LogInformation("Upgraded exercise {Id} name to '{Name}'", exercise.Id, pdfExName);
                            }
                        }
                    }
                }

                if (exercise == null)
                {
                    exercise = _repo.AddExercise(pdfExName, muscleGroupId);
                    allExercises = _repo.GetExercises();
                    summary.NewExercisesCreated++;
                    _logger.LogInformation("Created new exercise '{Name}' for muscle group {Mg}", pdfExName, exMg?.Name ?? "?");
                }

                claimedExerciseIds[exercise.Id] = normalizedPdfName;

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
                    // Require BIDIRECTIONAL agreement: A says its partner is B AND B says its
                    // partner is A. Prevents AI-injected one-sided "partners" (e.g. "Femoral
                    // unilateral de pie" claiming a pairing with Femoral tumbado when the real
                    // partner from the PDF is "Femoral unilateral tumbado").
                    var partner = orderedExercises.FirstOrDefault(other =>
                        !ReferenceEquals(other, pdfEx)
                        && string.Equals(NormalizeExerciseName(other.Name ?? ""),
                                         NormalizeExerciseName(pdfEx.SupersetWith ?? ""),
                                         StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(other.SupersetWith)
                        && string.Equals(NormalizeExerciseName(other.SupersetWith ?? ""),
                                         NormalizeExerciseName(pdfEx.Name ?? ""),
                                         StringComparison.OrdinalIgnoreCase));

                    if (partner != null)
                    {
                        var key = string.Compare(pdfEx.Name, partner.Name, StringComparison.OrdinalIgnoreCase) < 0
                            ? $"{pdfEx.Name}|{partner.Name}"
                            : $"{partner.Name}|{pdfEx.Name}";
                        if (!supersetMap.TryGetValue(key, out supersetGroup))
                        {
                            supersetGroup = supersetCounter++;
                            supersetMap[key] = supersetGroup;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Skipping orphan superset link: '{A}' → '{B}' (no bidirectional match found)",
                            pdfEx.Name, pdfEx.SupersetWith);
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

    // Spanish stop-words / connectors that we strip before comparing exercise names.
    private static readonly HashSet<string> ExerciseStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "del", "la", "el", "los", "las", "en", "con", "y", "o", "u", "a",
        "por", "para", "al", "un", "una", "uno", "unas", "unos",
    };

    /// <summary>
    /// Tokenises a normalised exercise name into significant words (stop-words and very short
    /// fragments removed). Used by the fuzzy matcher so "Press banca plano (unilateral)" and
    /// "Press Banca Plano Unilateral" reduce to the same token set.
    /// </summary>
    private static HashSet<string> SignificantTokens(string name)
    {
        var normalized = NormalizeExerciseName(name);
        // Strip parenthetical descriptors before tokenising — they're rarely identity-changing.
        normalized = Regex.Replace(normalized, @"\([^)]*\)", " ").Trim();
        return normalized
            .Split(new[] { ' ', '-', '/', ',', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !ExerciseStopWords.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Picks the existing exercise whose name overlaps the PDF name on at least 60% of the
    /// significant tokens (Jaccard similarity). Returns null if no candidate is good enough.
    /// Reusing the existing Exercise.Id is what keeps user-uploaded photos attached after a
    /// re-import.
    /// </summary>
    internal static Exercise? FindBestFuzzyExerciseMatch(string pdfName, IReadOnlyList<Exercise> existing,
        IReadOnlySet<int>? excludeIds = null)
    {
        var pdfTokens = SignificantTokens(pdfName);
        if (pdfTokens.Count == 0) return null;

        Exercise? best = null;
        double bestScore = 0;
        foreach (var ex in existing)
        {
            if (excludeIds != null && excludeIds.Contains(ex.Id)) continue;
            var exTokens = SignificantTokens(ex.Name);
            if (exTokens.Count == 0) continue;
            var common = pdfTokens.Intersect(exTokens, StringComparer.OrdinalIgnoreCase).Count();
            if (common == 0) continue;
            var union = pdfTokens.Union(exTokens, StringComparer.OrdinalIgnoreCase).Count();
            double score = (double)common / union;
            // Treat subset relationships as strong matches even when one side has extra words:
            // "Press banca plano" ⊂ "Press banca plano unilateral" should still pair.
            // ONLY when the smaller side has ≥2 tokens: a single generic token is not an
            // identity. Without this guard, "APERTURAS EN BANCO PLANO" and "APERTURAS
            // INCLINADAS CON MANCUERNAS" both collapsed onto the seeded exercise "Aperturas"
            // and the day ended up with one aperturas instead of two distinct movements.
            var smaller = Math.Min(pdfTokens.Count, exTokens.Count);
            if (smaller >= 2 && (pdfTokens.IsSubsetOf(exTokens) || exTokens.IsSubsetOf(pdfTokens)))
                score = Math.Max(score, 0.85);
            if (score > bestScore) { bestScore = score; best = ex; }
        }
        return bestScore >= 0.6 ? best : null;
    }

    /// <summary>
    /// Merges local-parser and AI extractions: for each day-of-week, keeps the
    /// version with more exercises. Days only present in one source are kept as-is.
    /// </summary>
    /// <summary>
    /// Strips bogus "exercises" the AI may have hallucinated from green-styled table cells or
    /// coaching descriptions before the merge step runs. Keeps real exercises untouched.
    /// </summary>
    internal static void SanitizeAiExtraction(PdfExtraction ai, ILogger logger)
    {
        foreach (var day in ai.Routines)
        {
            // First, normalize names: truncate at colon+description and comma+conjunction so
            // AI-generated names match the local-parser canonical form for the merge dedup.
            foreach (var ex in day.Exercises)
            {
                if (!string.IsNullOrWhiteSpace(ex.Name))
                    ex.Name = LocalPdfParser.CleanExerciseName(ex.Name).Trim();

                // CRITICAL: drop SupersetWith from AI suggestions. The "+ Super serie ..." line
                // in the PDF is the trainer's only authoritative source for pairings; the AI
                // tends to hallucinate alternative partners (e.g. pairing "Femoral tumbado"
                // with "Femoral unilateral de pie" instead of the actual "Femoral unilateral
                // tumbado"). Letting an AI-only pairing reach the routine produces wrong
                // SupersetGroup ids and visually-wrong partners in the workout UI.
                ex.SupersetWith = null;
            }

            int before = day.Exercises.Count;
            day.Exercises = day.Exercises
                .Where(e => !string.IsNullOrWhiteSpace(e.Name)
                            && e.Name!.Length >= 3
                            && !LocalPdfParser.IsBogusGreenExerciseLine(e.Name!))
                .ToList();
            int removed = before - day.Exercises.Count;
            if (removed > 0)
                logger.LogInformation("AI sanitize day {Day}: removed {N} bogus exercise(s)", day.DayOfWeek, removed);
        }
    }

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

            // True right after emitting an [EX] header whose parentheses are unbalanced —
            // that means the trainer's header wrapped to the next PDF line ("APERTURAS EN
            // BANCO PLANO (FIJATE EN LA DISTANCIA…" / "PARA QUE SEA SUFICIENTE RECORRIDO…)").
            // The immediately-following green line is the tail of the SAME header, not a new
            // exercise, so it must be demoted to a plain note line.
            bool prevHeaderLeftParenOpen = false;

            foreach (var line in lines.OrderByDescending(l => l.y))
            {
                var sortedWords = line.words.OrderBy(w => w.x).ToList();
                var fullLineText = string.Join(" ", sortedWords.Select(w => w.text));

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

                    // Filter out lines whose leading green is actually a note (Tiempo, Descanso, Movilidad...),
                    // a bare muscle-group section label ("Pectoral:", "Femoral:"), or the wrapped
                    // continuation of the previous exercise header.
                    if (prevHeaderLeftParenOpen || LooksLikeNoteHeader(firstGreenWord, greenWords))
                    {
                        prevHeaderLeftParenOpen = false;
                        sb.AppendLine(fullLineText);
                        continue;
                    }

                    sb.Append(ExerciseMarker).AppendLine(greenSegment);
                    prevHeaderLeftParenOpen = fullLineText.Count(c => c == '(') > fullLineText.Count(c => c == ')');

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
                    prevHeaderLeftParenOpen = false;
                    sb.AppendLine(fullLineText);
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
        // Positional adverbs / prepositions that lead wrapped header continuations in the
        // JULIO plan ("ABAJO ESTA COLOCADA", "ATRÁS", "EN ESTE CASO COJEREMOS…",
        // "PARA QUE SEA SUFICIENTE RECORRIDO…") — a real exercise never starts with these.
        "EN", "PARA", "ABAJO", "ARRIBA", "ATRAS", "ATRÁS", "ADENTRO", "AFUERA",
        "ESTA", "ESTE", "ESTO", "ESTOS", "ESTAS",
    };

    // Whole-line annotations that look like exercises but are really table-cell intensity tags
    // (e.g. "PESO ALTO", "PESO LIGERO" written next to a reps number). Reject when the whole green
    // segment matches one of these. "Peso muerto" remains a valid exercise.
    internal static readonly HashSet<string> WeightAnnotations = new(StringComparer.OrdinalIgnoreCase)
    {
        "PESO ALTO", "PESO LIGERO", "PESO MEDIO", "PESO MAXIMO", "PESO MÁXIMO",
        "PESO MINIMO", "PESO MÍNIMO", "PESO BAJO", "ALTO PESO", "BAJO PESO",
    };

    // Intensity modifiers that, when paired with the word PESO, signal a table-cell annotation
    // rather than an exercise. "Muerto" is NOT here — "Peso muerto" stays a valid exercise.
    private static readonly HashSet<string> PesoIntensityModifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALTO", "LIGERO", "MEDIO", "BAJO", "MAXIMO", "MÁXIMO", "MINIMO", "MÍNIMO",
    };

    // Words that, when they FOLLOW a comma inside an exercise name, indicate the rest of the
    // line is a coaching instruction. We cut the name at the comma.
    internal static readonly HashSet<string> PostCommaContinuations = new(StringComparer.OrdinalIgnoreCase)
    {
        "PERO", "AUNQUE", "MIENTRAS", "CUANDO", "DONDE", "PORQUE", "PUES",
        "TENIENDO", "DEJANDO", "MANTENIENDO", "GENERANDO", "HACIENDO",
        "PROCURANDO", "INTENTANDO", "EVITANDO", "CUIDANDO",
        "Y", "O", "U",
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

        // "Peso ALTO …" / "Peso LIGERO …" — first word PESO followed by an intensity modifier.
        // "Peso muerto" is preserved because MUERTO is not in PesoIntensityModifiers.
        var tokens = stripped.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2
            && tokens[0].Equals("PESO", StringComparison.OrdinalIgnoreCase)
            && PesoIntensityModifiers.Contains(tokens[1].TrimEnd(',', '.', ':')))
        {
            return true;
        }

        // Line consists ONLY of repeated "PESO <modifier>" pairs (e.g. "Peso Alto Peso Alto Peso Alto").
        if (tokens.Length >= 2 && tokens.Length % 2 == 0)
        {
            bool allWeightPairs = true;
            for (int i = 0; i < tokens.Length; i += 2)
            {
                var a = tokens[i].TrimEnd(',', '.', ':');
                var b = tokens[i + 1].TrimEnd(',', '.', ':');
                if (!a.Equals("PESO", StringComparison.OrdinalIgnoreCase)
                    || !PesoIntensityModifiers.Contains(b))
                {
                    allWeightPairs = false;
                    break;
                }
            }
            if (allWeightPairs) return true;
        }

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

    // Anchor to the start of the line so a stray "descanso" inside a longer instruction
    // ("Calentamiento 3 series ... 1 minuto de descanso por serie") doesn't get classified
    // as a rest row — which would swallow the line before its embedded rep count is parsed.
    private static readonly Regex RestLine = new(
        @"^\s*(?:TIEMPO\s+DE\s+)?DESCANSO\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Keywords that are NOT exercise names
    private static readonly HashSet<string> NonExerciseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SERIE", "SERIES", "REPS", "REPETICIONES", "FASE", "POSITIVA", "NEGATIVA",
        "TEMPO", "DESCANSO", "REST", "AGARRE", "GRIP", "WEIGHT",
        "PÁGINA", "PAGE", "PLAN", "ENTRENAMIENTO", "TRAINING", "NOTA", "NOTAS",
        "TIEMPO", "SEG", "SEGUNDOS", "MIN", "MINUTO", "MINUTOS", "KG", "LBS",
        "RGANUTRI", "ASESORÍA", "TOTAL", "EJECUCIÓN", "EJECUCION",
    };

    private static readonly HashSet<string> NoteStartWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOVILIDAD", "HAZ", "POR", "SIEMPRE", "AUMENTANDO", "VAMOS",
        "RECUERDA", "IMPORTANTE", "NOTA", "NOTAS", "REALIZA", "MANTÉN",
        "INTENTA", "ASEGÚRATE", "CUIDADO", "EVITA", "NO", "SI", "CUANDO",
        "TIEMPO", "DESCANSO",
        "BUSCAMOS", "PROCURA", "SUPER", "CALENTAREMOS", "ARRANCAMOS",
        "REALIZAREMOS", "FIN",
        // First words of coaching notes in the JULIO plan that were being promoted to
        // bogus exercises ("CALENTAMIENTO DE TRÍCEPS PREVIO…", "HACEMOS PAUSA…",
        // "FIJATE EN LA PISADA…", "TODAS LAS SERIES…", "POSICIÓN Y REALIZAMOS…").
        "CALENTAMIENTO", "CALENTAMOS", "HACEMOS", "HAREMOS",
        "POSICIÓN", "POSICION", "FIJATE", "FÍJATE",
        "TODAS", "TODOS", "TODA", "TODO",
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
        // Body-part positioning cues ("Codo completamente estirado, sin involucrar al
        // bíceps…") and positional adverbs from wrapped header continuations
        // ("ABAJO ESTA COLOCADA", "ATRÁS", "FATIGADO DEL ANTERIOR").
        "CODO", "CODOS", "RODILLA", "RODILLAS",
        "ABAJO", "ARRIBA", "ATRÁS", "ATRAS", "ADENTRO", "AFUERA",
        "FATIGADO", "FATIGADA",
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

        SuppressTrailingDuplicateRun(extraction);

        return extraction;
    }

    /// <summary>
    /// The trainer's PDFs sometimes end with leftover pages that repeat a block from an
    /// earlier day (copy-paste artifact — e.g. the JULIO plan repeats día 2's Abductor /
    /// Femoral sentado / Aductor / Gemelos after día 5). Those trailing exercises would
    /// otherwise import into the LAST day. Drop the trailing run of the last day when it is
    /// ≥2 consecutive exercises that each exactly duplicate (name + rep scheme) an exercise
    /// of a previous day. If the ENTIRE last day duplicates an earlier one it's a sibling
    /// day (LUNES-VIERNES share content) and is left untouched.
    /// </summary>
    private static void SuppressTrailingDuplicateRun(PdfExtraction extraction)
    {
        if (extraction.Routines.Count < 2) return;
        var last = extraction.Routines[^1];

        static string SignatureOf(PdfExercise ex) =>
            (ex.Name ?? "").Trim().ToUpperInvariant() + "|" + string.Join(",", ex.Sets.Select(s => s.Reps));

        var earlier = extraction.Routines.Take(extraction.Routines.Count - 1)
            .SelectMany(r => r.Exercises)
            .Select(SignatureOf)
            .ToHashSet();

        int runStart = last.Exercises.Count;
        while (runStart > 0 && earlier.Contains(SignatureOf(last.Exercises[runStart - 1])))
            runStart--;

        if (runStart == 0) return; // whole-day duplicate = shared sibling day, keep it
        if (last.Exercises.Count - runStart >= 2)
            last.Exercises.RemoveRange(runStart, last.Exercises.Count - runStart);
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
        // True while `current` came from a green line of the form "Section name: inline
        // description" ("Glúteo en máquina de elevación de lumbar: Nos posicionamos…").
        // If ANOTHER green exercise line arrives before any sets are captured, the pair is
        // ONE exercise (section-style header + concrete movement name), not two.
        bool currentIsColonSectionHeader = false;
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
                var partnerWords = partnerLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var partnerFirst = partnerWords.Length > 0 ? partnerWords[0].TrimEnd(':', ',', '.') : "";
                bool isBogusPartner =
                    PdfImportService.WeightAnnotations.Contains(partnerUpper) ||
                    Regex.IsMatch(partnerUpper, @"^\d+(\s+(REPS?|SERIES?|PESO\s+(ALTO|LIGERO|MEDIO|BAJO|MAX[IÍ]MO|M[IÍ]NIMO)))?$") ||
                    // Table-cell fragments like "+super serie con / mitad de peso / 8reps" wrap
                    // across PDF lines and produce partners like "con" or "con +2 series". A real
                    // partner names a movement — reject connector-led or keyword-less short tails.
                    InstructionStartWords.Contains(partnerFirst) ||
                    NoteStartWords.Contains(partnerFirst) ||
                    NonExerciseWords.Contains(partnerFirst) ||
                    (partnerWords.Length <= 2 && !ContainsExerciseKeyword(partnerLine));
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
                    currentIsColonSectionHeader = false;
                    continue;
                }
            }

            // "Seg. Ejecución" lines — may contain numbers for a pending Fase row
            if (Regex.IsMatch(line, @"Seg\.?\s*(?:de\s+)?(?:Ejecuci[oó]n|ejecuci[oó]n)", RegexOptions.IgnoreCase))
            {
                if (current != null && pendingFaseType != null)
                {
                    var nums = ParseTempoNumbers(line);
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

            // PRIORITY rep-prescription. Any UNMARKED line that mentions "N series ..."
            // and "... M reps" with text between describes the immediately preceding
            // exercise. This covers the trainer's narrative form:
            //   [EX] Extensión de cuadriceps:                                  <-- creates current
            //   Calentamiento 3 series con peso ligero y controlado (15 reps por serie)
            // The Calentamiento line is NOT marked [EX] (the green segment was only
            // "Extensión de cuadriceps:"), so we apply 3×15 to current and skip the
            // exercise-name heuristics entirely. Only fires when:
            //   - the line is not [EX]-marked (so we don't accidentally consume a marked
            //     exercise that legitimately starts with a number),
            //   - current exists and has no sets yet (don't clobber a proper table),
            //   - both anchors point to plausible counts (1..30 sets, reps 1..999).
            if (!isMarkedExercise && current != null && current.Sets.Count == 0)
            {
                var priSeries = Regex.Match(line, @"(\d+)\s+series?\b", RegexOptions.IgnoreCase);
                var priReps = Regex.Match(line, @"\breps?\b", RegexOptions.IgnoreCase);
                if (priSeries.Success && priReps.Success
                    && priReps.Index > priSeries.Index + priSeries.Length)
                {
                    var count = int.Parse(priSeries.Groups[1].Value);
                    var between = line.Substring(
                        priSeries.Index + priSeries.Length,
                        priReps.Index - (priSeries.Index + priSeries.Length));
                    var repsNums = Regex.Matches(between, @"\d+")
                        .Select(m => int.Parse(m.Value))
                        .Where(n => n > 0 && n < 1000)
                        .ToList();
                    if (repsNums.Count > 0 && count > 0 && count <= 30)
                    {
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
                }
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
                    currentIsColonSectionHeader = false;
                    continue;
                }
            }

            // Table-cell annotations like "PESO ALTO PESO ALTO PESO ALTO" (typed in black inside
            // the reps cell) or "PESO LIGERO Y QUE SE PUEDA CONTROLAR..." (an instruction above
            // the next table) look exercise-like to the uppercase/title-case heuristic. Reject
            // them regardless of whether they were marked green by the extractor — otherwise
            // they become `current` and the next "+ Super serie ..." line attaches to them.
            if (IsBogusGreenExerciseLine(line))
            {
                continue;
            }

            // Check for exercise name FIRST (before table parsing).
            // [EX]-marked lines bypass the heuristic — they come from green-coloured text in the PDF.
            if (isMarkedExercise || IsExerciseName(line, lines, idx))
            {
                // Merge: "[EX] Glúteo en máquina de elevación de lumbar: Nos posicionamos…"
                // immediately followed by "[EX] Hiperextensiones enfoque glúteo" (no table in
                // between) is ONE exercise. Fold the section header into the notes and let
                // this line rename the SAME exercise instead of creating a second one.
                if (isMarkedExercise && current != null && currentIsColonSectionHeader && current.Sets.Count == 0)
                {
                    notesBuilder.AppendLine(current.Name);
                    current.Name = ToTitleCase(CleanExerciseName(line));
                    ExtractGripFromName(current);
                    currentIsColonSectionHeader = false;
                    continue;
                }

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
                        currentIsColonSectionHeader = false;
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
                // "Name: inline description" green headers may be renamed by the NEXT green
                // line (see the merge above). A trailing bare colon ("Femoral tumbado:") is
                // NOT a section header — only a colon followed by more text qualifies.
                currentIsColonSectionHeader = isMarkedExercise && Regex.IsMatch(line, @":\s*\S");
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
                var nums = ParseTempoNumbers(tpMatch.Groups[1].Value);
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
                var nums = ParseTempoNumbers(tnMatch.Groups[1].Value);
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

            // "N series de N" / "N series x N reps" / "N series* N reps" / "N series N reps"
            // The optional [*x×] between "series" and the reps number covers the trainer's
            // free-text patterns: "4 series* 15 reps" (Patada de glúteo) and "4 series x 20 reps"
            // (Gemelo de pie). Also matches the legacy "4 series de 12" form.
            var seriesDeMatch = Regex.Match(line,
                @"(\d+)\s+series?\s*[*x×]?\s*(?:de\s+)?(\d[\d\s,]*)",
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

            // Free-text fallback: "N series [arbitrary words] M reps". Catches the trainer's
            // long-form prescriptions like "Calentamiento 3 series con peso ligero y controlado
            // (15 reps por serie)" where seriesDeMatch fails because non-numeric words sit
            // between "series" and the reps count. We anchor on both keywords and extract every
            // numeric value that appears between them — so "4 series x 20, 15, 12, 10 reps"
            // still resolves to four per-set values.
            var seriesAnchor = Regex.Match(line, @"(\d+)\s+series?\b", RegexOptions.IgnoreCase);
            var repsAnchor = Regex.Match(line, @"\breps?\b", RegexOptions.IgnoreCase);
            if (seriesAnchor.Success && repsAnchor.Success && current != null
                && repsAnchor.Index > seriesAnchor.Index + seriesAnchor.Length)
            {
                var count = int.Parse(seriesAnchor.Groups[1].Value);
                var between = line.Substring(
                    seriesAnchor.Index + seriesAnchor.Length,
                    repsAnchor.Index - (seriesAnchor.Index + seriesAnchor.Length));
                var repsNums = Regex.Matches(between, @"\d+")
                    .Select(m => int.Parse(m.Value))
                    .Where(n => n > 0 && n < 1000)
                    .ToList();
                if (repsNums.Count > 0 && count > 0 && count <= 30)
                {
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
            }

            // Reps shorthand: "4*15*12*10*8" / "4x15" / "4*10".
            // Capture ONLY the contiguous N*N*N... chain — stop at the first non-digit /
            // non-operator character. Otherwise lines like "4*10 pasos ida*10 vuelta"
            // (Zancada del Lunes) would have picked up the second "10" as a third rep
            // value and produced 3 sets of [4, 10, 10] instead of 4 sets of 10.
            var shorthand = Regex.Match(line, @"\b(\d+(?:\s*[*x×]\s*\d+)+)\b", RegexOptions.IgnoreCase);
            if (shorthand.Success && current != null)
            {
                var chain = shorthand.Groups[1].Value;
                var nums = Regex.Matches(chain, @"\d+").Select(m => int.Parse(m.Value)).Where(n => n > 0).ToList();
                if (nums.Count >= 2)
                {
                    current.Sets.Clear();
                    // "N * R1 * R2 * R3 * ... * Rn" with n == N → per-set reps.
                    // Anything else (including "N * R" / "N * R * trailing junk") collapses
                    // to N sets of R using nums[0] as the set count and nums[1] as reps.
                    if (nums.Count == nums[0] + 1)
                    {
                        for (int i = 1; i < nums.Count; i++)
                            current.Sets.Add(new PdfSet { Reps = nums[i] });
                    }
                    else
                    {
                        var count = Math.Min(nums[0], 20); // sanity clamp
                        var reps = nums[1];
                        for (int i = 0; i < count; i++)
                            current.Sets.Add(new PdfSet { Reps = reps });
                    }
                }
                continue;
            }

            // REPS line: "REPS 15 12 10 8" or "Reps 8 rep por pierna 8 rep por pierna 8 rep por pierna".
            // Strip the REPS prefix and grab every number in the rest of the line — text like
            // "rep por pierna" between counts must not stop the capture at the first digit.
            var repsHeaderMatch = Regex.Match(line, @"^(?:REPS?|REPETICION(?:ES)?)\b\s*[:\s]?",
                RegexOptions.IgnoreCase);
            if (repsHeaderMatch.Success)
            {
                var rest = line[repsHeaderMatch.Length..];
                // A range like "8-10" is ONE set (take the top of the range), not two.
                // Without this, "Reps 8-10 8-10 8-10 8-10" produced 8 sets instead of 4.
                var repsNums = Regex.Matches(rest, @"\d+(?:\s*-\s*\d+)?")
                    .Select(m => int.Parse(m.Value.Split('-')[^1].Trim()))
                    .Where(n => n > 0 && n < 1000)
                    .ToList();
                if (repsNums.Count > 0)
                {
                    current.Sets.Clear();
                    foreach (var r in repsNums)
                        current.Sets.Add(new PdfSet { Reps = r });
                    continue;
                }
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

            // "4 series con peso añadido (disco)" — a set count with no rep spec. Capture the
            // count (default 12 reps) but DON'T consume the line: the text stays as a note.
            var bareSeriesMatch = Regex.Match(line, @"^(\d+)\s+series\b", RegexOptions.IgnoreCase);
            if (bareSeriesMatch.Success && current.Sets.Count == 0
                && !Regex.IsMatch(line, @"\breps?\b", RegexOptions.IgnoreCase))
            {
                var count = int.Parse(bareSeriesMatch.Groups[1].Value);
                for (int i = 0; i < Math.Min(count, 10); i++)
                    current.Sets.Add(new PdfSet { Reps = 12 });
            }

            // Standalone numbers line after pending Fase: "2 2 3 4" — also accepts "2 4 4 3-4"
            if (pendingFaseType != null && current != null && Regex.IsMatch(line, @"^\d[\d\s\-]+$"))
            {
                var nums = ParseTempoNumbers(line);
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
    /// Parses a row like "2 4 4 3-4 3-4" into one number per cell. Ranges "N-M" collapse to
    /// the higher value so a 4-cell row never explodes into 5 numbers (the bug that smeared
    /// tempos across the wrong sets).
    /// </summary>
    internal static List<int> ParseTempoNumbers(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<int>();
        // "3-4" → "4", "2-3" → "3". Pick the upper bound: it's the conservative tempo target.
        var collapsed = Regex.Replace(text, @"(\d+)\s*-\s*(\d+)", m =>
        {
            var a = int.Parse(m.Groups[1].Value);
            var b = int.Parse(m.Groups[2].Value);
            return Math.Max(a, b).ToString();
        });
        return Regex.Matches(collapsed, @"\d+")
            .Select(m => int.Parse(m.Value))
            .Where(n => n >= 0 && n < 100) // tempos are seconds — sanity filter
            .ToList();
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
        // Reject inline rep schemes: "3 series x 20 reps", "4 series de 15",
        // "4 series* 15 reps", "4 series 15 reps". These are data lines that happen to
        // contain the words "series" and "reps" — they belong to seriesDeMatch downstream,
        // not to the table-row parser.
        if (Regex.IsMatch(lower, @"\d+\s+series?\s*[*x×]?\s*(?:de\s+)?\d+")) return false;
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
    /// <summary>
    /// True when a green-marked line is clearly NOT an exercise (table-cell weight annotation,
    /// "Peso ligero/alto" prefixed coaching text, or a wrapping description that opens with a
    /// continuation/instruction word). "Peso muerto" and other legitimate exercises still pass.
    /// </summary>
    internal static bool IsBogusGreenExerciseLine(string line)
    {
        var stripped = line.Trim().TrimEnd(':', '.', ',').Trim();
        if (stripped.Length == 0) return true;

        var upper = stripped.ToUpperInvariant();
        if (PdfImportService.WeightAnnotations.Contains(upper)) return true;

        var tokens = stripped.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return true;

        // Pure digit row: "12" by itself, "12 reps", "12 series" — annotations the trainer
        // sometimes stamps into a Serie cell. We deliberately do NOT include `*` / `x` / `×`
        // here so that "4*15" / "4x10" (legitimate set shorthand) is allowed downstream.
        if (Regex.IsMatch(stripped, @"^\d+(\s*(reps?|series?)\s*\d*)?$", RegexOptions.IgnoreCase))
            return true;

        // "Peso ALTO …" / "Peso LIGERO …" — keep "Peso muerto". Match even when the modifier
        // has a glued suffix like "LIGERO-LO" (from "PESO LIGERO-LO USAMOS PARA CALENTAR ..."):
        // we accept any token that STARTS WITH one of the intensity modifier words.
        if (tokens.Length >= 2
            && tokens[0].Equals("PESO", StringComparison.OrdinalIgnoreCase))
        {
            var second = tokens[1].TrimEnd(',', '.', ':').ToUpperInvariant();
            if (PesoIntensityModifiersInternal.Contains(second))
                return true;
            foreach (var modifier in PesoIntensityModifiersInternal)
            {
                if (second.StartsWith(modifier, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        // Line consists ONLY of repeated "PESO <modifier>" pairs.
        if (tokens.Length >= 2 && tokens.Length % 2 == 0)
        {
            bool allWeightPairs = true;
            for (int i = 0; i < tokens.Length; i += 2)
            {
                var a = tokens[i].TrimEnd(',', '.', ':');
                var b = tokens[i + 1].TrimEnd(',', '.', ':');
                if (!a.Equals("PESO", StringComparison.OrdinalIgnoreCase)
                    || !PesoIntensityModifiersInternal.Contains(b))
                {
                    allWeightPairs = false;
                    break;
                }
            }
            if (allWeightPairs) return true;
        }

        // Table-cell labels that the trainer types into the SERIE row to flag a warm-up or
        // a max-effort set (e.g. "Calentamiento", "Calentamiento Max peso", "Max peso"). The
        // word "PESO" is in ExerciseKeywords, so without this guard IsExerciseName accepts
        // "Calentamiento Max peso" as an exercise — stealing the reps row from the real
        // exercise above it (e.g. Femoral sentado loses its 4-set table).
        var firstToken = tokens[0].TrimEnd(',', '.', ':').ToUpperInvariant();
        if (TableCellLabelStarters.Contains(firstToken))
        {
            // Reject only when the WHOLE line reads like a cell label: short, no exercise verb.
            if (tokens.Length <= 4 && !ContainsExerciseMovement(tokens))
                return true;
        }

        // Wrapped-header continuations and instruction sentences that arrive green-marked
        // ("PARA QUE SEA SUFICIENTE…", "ABAJO ESTA COLOCADA", "ATRÁS", "FATIGADO DEL
        // ANTERIOR", "EN ESTE CASO COJEREMOS…") — no real exercise starts with these words.
        if (InstructionStartWords.Contains(firstToken) || NoteStartWords.Contains(firstToken))
            return true;

        // Standalone "Max peso" / "Máximo peso" / "Mínimo peso" labels (no other words).
        if (tokens.Length == 2)
        {
            var a = tokens[0].TrimEnd(',', '.', ':').ToUpperInvariant();
            var b = tokens[1].TrimEnd(',', '.', ':').ToUpperInvariant();
            if ((a is "MAX" or "MAXIMO" or "MÁXIMO" or "MIN" or "MINIMO" or "MÍNIMO") && b == "PESO")
                return true;
            if (a == "PESO" && (b is "MAX" or "MAXIMO" or "MÁXIMO" or "MIN" or "MINIMO" or "MÍNIMO"))
                return true;
        }

        return false;
    }

    // Words that, when they LEAD a short cell-like line, mean the whole thing is a table
    // annotation rather than an exercise. Deliberately EXCLUDES "Fase", "Seg", "Descanso",
    // "Tiempo" — those are handled by their own dedicated parsers later in the pipeline
    // and rejecting them here would lose their tempo/rest data.
    private static readonly HashSet<string> TableCellLabelStarters = new(StringComparer.OrdinalIgnoreCase)
    {
        "CALENTAMIENTO", "ENFRIAMIENTO", "ESTIRAMIENTO", "ESTIRAMIENTOS",
        "MAX", "MAXIMO", "MÁXIMO", "MIN", "MINIMO", "MÍNIMO",
    };

    // Returns true if any token looks like a movement noun (press, curl, sentadilla...) —
    // used to keep "Calentamiento 3 series Press banca" from being mis-rejected.
    private static bool ContainsExerciseMovement(string[] tokens)
    {
        // Hard-coded small set of unambiguous movement verbs/nouns. We deliberately do NOT
        // include body parts here (FEMORAL, ABDUCTOR, GEMELO) because those appear as
        // standalone exercise names that should NOT save a bogus "Calentamiento femoral" line.
        var movements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PRESS", "CURL", "EXTENSION", "EXTENSIÓN", "PRENSA", "REMO", "APERTURA", "APERTURAS",
            "ELEVACION", "ELEVACIÓN", "ELEVACIONES", "JALON", "JALÓN", "DOMINADA", "DOMINADAS",
            "SENTADILLA", "SENTADILLAS", "ZANCADA", "ZANCADAS", "PATADA", "PATADAS",
            "CRUCE", "CRUCES", "FONDOS", "PULL", "MUERTO",
        };
        return tokens.Any(t => movements.Contains(t.TrimEnd(',', '.', ':')));
    }

    private static readonly HashSet<string> PesoIntensityModifiersInternal = new(StringComparer.OrdinalIgnoreCase)
    {
        "ALTO", "LIGERO", "MEDIO", "BAJO", "MAXIMO", "MÁXIMO", "MINIMO", "MÍNIMO",
    };

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

        // Exercise names never START with a digit — lines like "1 MINUTO DE DESCANSO X SERIE"
        // or "2 SEMANAS Y 2 SEMANAS EN MÁQUINA…" are notes/continuations, and promoting them
        // steals the following table from the real exercise above.
        if (char.IsDigit(line[0])) return false;

        // "N series ... M reps" is a SET PRESCRIPTION, not an exercise name. The trainer's
        // PDF has long-form lines like "Calentamiento 3 series con peso ligero y controlado
        // (15 reps por serie)" that contain the keyword PESO and would otherwise be promoted
        // to a bogus exercise. The set-count parser downstream still picks them up.
        bool hasSeries = Regex.IsMatch(line, @"\d+\s+series?\b", RegexOptions.IgnoreCase);
        bool hasReps = Regex.IsMatch(line, @"\breps?\b", RegexOptions.IgnoreCase);
        if (hasSeries && hasReps) return false;

        // Same idea for the shorthand form "3X10 reps" ("Tríceps barra recta desde polea
        // alta 3X10 reps" is the warm-up prescription, not a new exercise).
        if (Regex.IsMatch(line, @"\d\s*[x×*]\s*\d+\s*reps?\b", RegexOptions.IgnoreCase)) return false;

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
    internal static string CleanExerciseName(string name)
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

        // Truncate at "Name, <continuation>" — when text after a comma starts with a conjunction
        // or gerund (pero, y, teniendo, dejando, ...). Handles "Elevación de lumbar, pero teniendo...".
        var commaIdx = name.IndexOf(',');
        while (commaIdx > 2 && commaIdx < name.Length - 1)
        {
            var afterComma = name[(commaIdx + 1)..].TrimStart();
            var firstAfter = afterComma.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "";
            if (firstAfter.Length > 0 && PdfImportService.PostCommaContinuations.Contains(firstAfter.TrimEnd(',', '.', ':')))
            {
                name = name[..commaIdx].Trim();
                break;
            }
            // No match here — look for the next comma further along.
            var next = name.IndexOf(',', commaIdx + 1);
            if (next < 0) break;
            commaIdx = next;
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

        // Parenthetical groups: KEEP the ones that describe the movement — muscle/equipment
        // words like "(trapecio)" or "(CURL BÍCEPS CON BANCO INCLINADO)" are part of the
        // name the trainer wrote. DROP coaching instructions ("(Fijate en la posición…",
        // "(EL CONTRARIO AL DE LA FOTO)", "(Adelanta los codos en la ejecución)"). Text
        // AFTER a dropped group survives: "Elevación al mentón (trapecio) con barra montada
        // o desde polea baja y barra" stays complete. Unclosed groups (wrapped headers)
        // are treated the same.
        name = Regex.Replace(name, @"\(\s*([^)]*)\)?", m =>
        {
            var inner = m.Groups[1].Value.Trim().TrimEnd(',', '.', ':');
            if (inner.Length == 0) return " ";
            return ContainsExerciseKeyword(inner) && !IsCoachingText(inner) ? $"({inner}) " : " ";
        });
        name = Regex.Replace(name, @"\s+", " ").Trim();

        // Dash-joined segments: descriptors stay ("- CON BARRA RECTA- BANCO INCLINADO",
        // "- agarre neutro"), coaching tails go ("-como en la foto").
        var dashSegments = Regex.Split(name, @"\s*-\s+|(?<=\S)-(?=[A-Za-zÁÉÍÓÚÑáéíóúñ])")
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        if (dashSegments.Count > 1)
        {
            var kept = dashSegments.Where((seg, i) => i == 0 || !IsCoachingText(seg)).ToList();
            name = string.Join("- ", kept);
        }

        // Final length cap with word boundary
        if (name.Length > 100)
        {
            var lastSpace = name.LastIndexOf(' ', 100);
            if (lastSpace > 20)
                name = name[..lastSpace];
        }

        return name.TrimEnd(':', '.', ',', '-', ' ');
    }

    // Words that flag a parenthetical or dash segment as a coaching instruction rather than
    // part of the movement's identity ("como en la foto", "el contrario al de la foto",
    // "con un peso controlado ya que…", "se situan frente al espejo").
    private static readonly string[] CoachingMarkers =
    {
        "FOTO", "IMAGEN", "ESPEJO", "CONTRARIO", "EJECUCIÓN", "EJECUCION",
        "CONTROLADO", "RECORRIDO", "POSICIÓN", "POSICION", "FIJATE", "FÍJATE",
        "ADELANTA", "YA QUE", "SITUAN", "SITÚAN", "ASEO",
    };

    internal static bool IsCoachingText(string text)
    {
        foreach (var marker in CoachingMarkers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase)) return true;
        }
        var first = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        first = first.TrimEnd(',', '.', ':').ToUpperInvariant();
        return first is "COMO" or "QUE" or "SE" or "YA" or "EL" or "LA" or "LOS" or "LAS" or "UN" or "UNA" or "AMBAS" or "AMBOS";
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
