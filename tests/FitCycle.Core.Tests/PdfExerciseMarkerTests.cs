using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

public class PdfExerciseMarkerTests
{
    [Fact]
    public void Marked_Line_Becomes_Exercise_Even_If_Long_And_Descriptive()
    {
        // This line would normally be rejected by IsExerciseName (>80 chars, descriptive)
        // but the [EX] marker bypasses the heuristic.
        var text = """
            PECTORAL+TRÍCEPS+HOMBRO (MARTES)
            [EX] Zancada con mancuernas en el pasillo de las mancuernas o en el de la entrada
            4*10 pasos ida*10 vuelta
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 2);
        Assert.NotNull(day);
        Assert.Contains(day.Exercises, e => e.Name != null && e.Name.Contains("Zancada"));
    }

    [Fact]
    public void Marker_Prefix_Is_Stripped_From_Exercise_Name()
    {
        var text = """
            CHEST (LUNES)
            [EX] Extensión de cuadriceps:
            Serie 1 2 3
            Reps 12 10 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 1);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.False(ex.Name?.StartsWith("[EX]") ?? false, "Marker should be stripped from name");
        Assert.Contains("Extension", ex.Name!.Replace("ó", "o"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Day_Header_With_Marker_Still_Detected()
    {
        // Hypothetical: day headers could also be green in some PDFs
        var text = """
            [EX] DÍA 1 - PECHO
            Press banca
            Serie 1 2
            Reps 12 10
            """;
        var result = LocalPdfParser.Parse(text);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 1);
    }

    [Fact]
    public void Exercise_With_Inline_Description_Is_Detected_When_Marked()
    {
        // This is the real-world case the trainer's PDF produces:
        //   "Extensión de cuadriceps:" is green, the rest of the line is black description.
        // Our text extractor splits these into two logical lines: the [EX]-marked name and the description below.
        var text = """
            CUADRICEPS+ ABDUCTOR+ADUCTOR+GEMELO (LUNES-VIERNES)
            [EX] Extensión de cuadriceps:
            Calentamiento 3 series con peso ligero y controlado (15 reps por serie) -- 1 minuto de descanso por serie
            [EX] Zancada con mancuernas en el pasillo de las mancuernas o en el de la entrada
            4*10 pasos ida*10 vuelta
            [EX] Abductor:
            Serie 1 2 3 4
            Reps 20 12 10 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 1);
        Assert.NotNull(day);
        // Expect the three exercises in PDF order
        var names = day.Exercises.Select(e => e.Name).ToList();
        Assert.True(names.Count >= 3, $"Expected at least 3 exercises, got {names.Count}: {string.Join(" | ", names)}");
        Assert.Contains("Extensi", names[0]!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Zancada", names[1]!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Abductor", names[2]!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlusPrefix_Line_Creates_Superset_Partner_With_Previous_Exercise()
    {
        // From the user's PDF, page 21:
        //   "Femoral tumbado"  (exercise, green)
        //   [table]
        //   "+ Super serie femoral unilateral tumbado"  (superset partner — separate line, starts with "+")
        var text = """
            FEMORAL+ GLUTEO (JUEVES)
            [EX] Femoral tumbado
            Serie 1 2 3
            Reps 12 12 10
            + Super serie femoral unilateral tumbado
            Serie 1 2 3
            Reps 8 8 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);

        var femoralTumbado = day.Exercises.FirstOrDefault(e => (e.Name ?? "").Contains("Tumbado", StringComparison.OrdinalIgnoreCase) && !(e.Name ?? "").Contains("Unilateral", StringComparison.OrdinalIgnoreCase));
        var femoralUnilateral = day.Exercises.FirstOrDefault(e => (e.Name ?? "").Contains("Unilateral", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(femoralTumbado);
        Assert.NotNull(femoralUnilateral);
        Assert.Equal(femoralUnilateral.Name, femoralTumbado.SupersetWith);
        Assert.Equal(femoralTumbado.Name, femoralUnilateral.SupersetWith);
    }

    [Fact]
    public void Non_Marked_Lines_Still_Pass_Through_Heuristic()
    {
        // Regression: ensure the existing heuristic-based detection keeps working
        var text = """
            PECTORAL (MARTES)
            Press banca plano
            Serie 1 2 3
            Reps 12 10 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 2);
        Assert.NotNull(day);
        Assert.NotEmpty(day.Exercises);
    }
}
