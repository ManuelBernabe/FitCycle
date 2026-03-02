using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FitCycle.Core.Models;
using FitCycle.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace FitCycle.Infrastructure.Services;

public interface IPdfImportService
{
    Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId);
}

public class PdfImportService : IPdfImportService
{
    private readonly IRoutineRepository _repo;
    private readonly ILogger<PdfImportService> _logger;

    // -- Regex patterns --
    private static readonly Regex DayHeaderRegex = new(
        @"^((?:[A-ZÁÉÍÓÚÑÜ]+(?:\s+Y\s+[A-ZÁÉÍÓÚÑÜ]+)*))[\s\-–]+D[IÍ]A\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExerciseNameRegex = new(
        @"^[A-ZÁÉÍÓÚÑÜ][A-ZÁÉÍÓÚÑÜ\s\.\,\(\)]{4,}$",
        RegexOptions.Compiled);

    private static readonly Regex RepsShorthandRegex = new(
        @"^(\d+)\s*[*xX×]\s*(\d+(?:\s*[*xX×]\s*\d+)*)$",
        RegexOptions.Compiled);

    private static readonly Regex SerieRowRegex = new(
        @"^SERIE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RepsRowRegex = new(
        @"^REPS\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TempoPositivaRegex = new(
        @"FASE\s+POSITIVA", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TempoNegativaRegex = new(
        @"FASE\s+NEGATIVA", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RestTimeRegex = new(
        @"TIEMPO\s+DE\s+DESCANSO", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NumbersRegex = new(
        @"\b(\d+)\b", RegexOptions.Compiled);

    private static readonly Regex TempoSecondsRegex = new(
        @"(\d+)\s*seg", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // -- Muscle group mapping from PDF headers to DB names --
    private static readonly Dictionary<string, string> MuscleGroupMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PECTORAL"] = "Pecho",
        ["PECTORALES"] = "Pecho",
        ["PECHO"] = "Pecho",
        ["ESPALDA"] = "Espalda",
        ["HOMBRO"] = "Hombros",
        ["HOMBROS"] = "Hombros",
        ["BICEPS"] = "Bíceps",
        ["BÍCEPS"] = "Bíceps",
        ["TRICEPS"] = "Tríceps",
        ["TRÍCEPS"] = "Tríceps",
        ["PIERNA"] = "Piernas",
        ["PIERNAS"] = "Piernas",
        ["ABDOMINAL"] = "Abdominales",
        ["ABDOMINALES"] = "Abdominales",
        ["GLUTEO"] = "Glúteos",
        ["GLÚTEO"] = "Glúteos",
        ["GLUTEOS"] = "Glúteos",
        ["GLÚTEOS"] = "Glúteos",
    };

    private enum ParserState { LookingForDay, LookingForExercise, CollectingNotes, ParsingTable }

    public PdfImportService(IRoutineRepository repo, ILogger<PdfImportService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public Task<PdfImportResult> ImportFromPdfAsync(byte[] pdfBytes, int targetUserId)
    {
        return Task.FromResult(ImportFromPdfInternal(pdfBytes, targetUserId));
    }

    private PdfImportResult ImportFromPdfInternal(byte[] pdfBytes, int targetUserId)
    {
        // 1. Extract and parse PDF locally
        PdfExtraction extraction;
        List<string> debugLines;
        try
        {
            (extraction, debugLines) = ParsePdf(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PDF");
            return new PdfImportResult { Success = false, Message = $"Error al parsear el PDF: {ex.Message}" };
        }

        if (extraction.Routines.Count == 0)
        {
            var preview = string.Join("\n", debugLines.Take(50));
            return new PdfImportResult { Success = false, Message = $"No se encontraron rutinas en el PDF.\n\nTexto extraído ({debugLines.Count} líneas):\n{preview}" };
        }

        // 2. Get all muscle groups and exercises
        var allMuscleGroups = _repo.GetAllMuscleGroups();
        var allExercises = _repo.GetExercises();
        var result = new PdfImportResult { Success = true, Message = "Rutinas importadas correctamente." };

        // 3. Process each day
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

    // ── PDF text extraction with PdfPig ──

    private (PdfExtraction Extraction, List<string> Lines) ParsePdf(byte[] pdfBytes)
    {
        var allLines = new List<string>();
        using (var document = PdfDocument.Open(pdfBytes))
        {
            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                var lines = text.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .ToList();
                allLines.AddRange(lines);
            }
        }

        _logger.LogDebug("Extracted {LineCount} lines from PDF", allLines.Count);
        return (ParseLines(allLines), allLines);
    }

    private PdfExtraction ParseLines(List<string> lines)
    {
        var extraction = new PdfExtraction();
        var state = ParserState.LookingForDay;

        PdfDayRoutine? currentDay = null;
        PdfExercise? currentExercise = null;
        var notesBuilder = new StringBuilder();

        List<int>? tableReps = null;
        List<int>? tableTempoPos = null;
        List<int>? tableTempoNeg = null;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // ── Day header always takes priority ──
            var dayMatch = DayHeaderRegex.Match(line);
            if (dayMatch.Success)
            {
                FinalizeExercise(currentExercise, notesBuilder, tableReps, tableTempoPos, tableTempoNeg);

                var rawMuscles = dayMatch.Groups[1].Value;
                var dayNumber = int.Parse(dayMatch.Groups[2].Value);

                // Split on " Y " for multi-muscle days (e.g., "PECTORAL Y TRÍCEPS")
                var muscleNames = rawMuscles.Split(new[] { " Y " }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => MapMuscleGroup(m.Trim()))
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                currentDay = new PdfDayRoutine
                {
                    DayOfWeek = dayNumber,
                    MuscleGroups = muscleNames,
                };
                extraction.Routines.Add(currentDay);

                currentExercise = null;
                notesBuilder.Clear();
                tableReps = null;
                tableTempoPos = null;
                tableTempoNeg = null;
                state = ParserState.LookingForExercise;
                continue;
            }

            if (currentDay == null) continue;

            // ── Rest time = end of exercise ──
            if (RestTimeRegex.IsMatch(line))
            {
                FinalizeExercise(currentExercise, notesBuilder, tableReps, tableTempoPos, tableTempoNeg);
                currentExercise = null;
                tableReps = null;
                tableTempoPos = null;
                tableTempoNeg = null;
                notesBuilder.Clear();
                state = ParserState.LookingForExercise;
                continue;
            }

            // ── Table: Serie row ──
            if (SerieRowRegex.IsMatch(line))
            {
                state = ParserState.ParsingTable;
                continue;
            }

            // ── Table: Reps row ──
            if (RepsRowRegex.IsMatch(line))
            {
                tableReps = ExtractNumbers(line);
                state = ParserState.ParsingTable;
                continue;
            }

            // ── Table: Fase positiva ──
            if (TempoPositivaRegex.IsMatch(line))
            {
                tableTempoPos = ExtractTempoValues(line, lines, ref i);
                state = ParserState.ParsingTable;
                continue;
            }

            // ── Table: Fase negativa ──
            if (TempoNegativaRegex.IsMatch(line))
            {
                tableTempoNeg = ExtractTempoValues(line, lines, ref i);
                state = ParserState.ParsingTable;
                continue;
            }

            // ── Skip "SEG. EJECUCIÓN" lines ──
            if (line.Contains("EJECUCI", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("SEG", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // ── Reps shorthand: "4*15*12*10*8" ──
            if (RepsShorthandRegex.IsMatch(line))
            {
                var allNums = ExtractNumbers(line);
                if (allNums.Count >= 2)
                {
                    // If first number = count of remaining, it's total_sets*rep1*rep2...
                    if (allNums[0] == allNums.Count - 1)
                        tableReps = allNums.Skip(1).ToList();
                    else
                        tableReps = allNums;
                }
                continue;
            }

            // ── Exercise name (all caps, not a table keyword) ──
            if (ExerciseNameRegex.IsMatch(line) && !IsTableKeyword(line))
            {
                FinalizeExercise(currentExercise, notesBuilder, tableReps, tableTempoPos, tableTempoNeg);

                currentExercise = new PdfExercise
                {
                    Name = ToTitleCase(line),
                    MuscleGroup = currentDay.MuscleGroups.FirstOrDefault() ?? "",
                };
                currentDay.Exercises.Add(currentExercise);

                notesBuilder.Clear();
                tableReps = null;
                tableTempoPos = null;
                tableTempoNeg = null;
                state = ParserState.CollectingNotes;
                continue;
            }

            // ── Collect notes text ──
            if (state == ParserState.CollectingNotes && currentExercise != null)
            {
                if (notesBuilder.Length > 0) notesBuilder.Append(' ');
                notesBuilder.Append(line);
            }
        }

        // Finalize last exercise
        FinalizeExercise(currentExercise, notesBuilder, tableReps, tableTempoPos, tableTempoNeg);

        return extraction;
    }

    // ── Helper methods ──

    private static void FinalizeExercise(
        PdfExercise? exercise, StringBuilder notesBuilder,
        List<int>? reps, List<int>? tempoPos, List<int>? tempoNeg)
    {
        if (exercise == null) return;

        var notes = notesBuilder.ToString().Trim();
        if (!string.IsNullOrEmpty(notes))
            exercise.Notes = notes;

        if (reps != null && reps.Count > 0)
        {
            for (int i = 0; i < reps.Count; i++)
            {
                exercise.Sets.Add(new PdfSet
                {
                    Reps = reps[i],
                    TempoPos = tempoPos != null && i < tempoPos.Count ? tempoPos[i] : 0,
                    TempoNeg = tempoNeg != null && i < tempoNeg.Count ? tempoNeg[i] : 0,
                    Grip = "",
                });
            }
        }

        notesBuilder.Clear();
    }

    private static List<int> ExtractNumbers(string line)
    {
        return NumbersRegex.Matches(line)
            .Cast<Match>()
            .Select(m => int.Parse(m.Value))
            .ToList();
    }

    private static List<int> ExtractTempoValues(string line, List<string> lines, ref int i)
    {
        // Try "N seg" pattern first
        var tempos = TempoSecondsRegex.Matches(line)
            .Cast<Match>()
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToList();

        if (tempos.Count > 0) return tempos;

        // Try plain numbers on same line (after the label)
        var nums = ExtractNumbers(line);
        if (nums.Count > 0) return nums;

        // Peek next line for values (table might split across lines)
        if (i + 1 < lines.Count)
        {
            var nextLine = lines[i + 1];
            if (!ExerciseNameRegex.IsMatch(nextLine) && !DayHeaderRegex.IsMatch(nextLine))
            {
                tempos = TempoSecondsRegex.Matches(nextLine)
                    .Cast<Match>()
                    .Select(m => int.Parse(m.Groups[1].Value))
                    .ToList();
                if (tempos.Count > 0) { i++; return tempos; }

                nums = ExtractNumbers(nextLine);
                if (nums.Count > 0) { i++; return nums; }
            }
        }

        return new List<int>();
    }

    private static bool IsTableKeyword(string line)
    {
        return SerieRowRegex.IsMatch(line)
            || RepsRowRegex.IsMatch(line)
            || TempoPositivaRegex.IsMatch(line)
            || TempoNegativaRegex.IsMatch(line)
            || RestTimeRegex.IsMatch(line)
            || (line.Contains("SEG", StringComparison.OrdinalIgnoreCase)
                && line.Contains("EJECUCI", StringComparison.OrdinalIgnoreCase))
            || line.StartsWith("PLAN DE ENTRENAMIENTO", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("MOVILIDAD", StringComparison.OrdinalIgnoreCase);
    }

    private static string MapMuscleGroup(string raw)
    {
        var cleaned = Regex.Replace(raw.Trim(), @"\(([A-ZÁÉÍÓÚÑ]*)\)", "$1");
        return MuscleGroupMap.TryGetValue(cleaned, out var mapped) ? mapped : cleaned;
    }

    private static string ToTitleCase(string allCaps)
    {
        if (string.IsNullOrWhiteSpace(allCaps)) return allCaps;
        var words = allCaps.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w =>
        {
            if (w.Length <= 2) return w.ToLowerInvariant();
            return char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant();
        }));
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
