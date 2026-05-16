using FitCycle.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void PlusPrefix_Followed_By_PesoAlto_Is_Not_Treated_As_Partner()
    {
        // Real-world case: a PDF table cell contains "PESO ALTO" as an intensity annotation,
        // sometimes extracted on its own line as "+ PESO ALTO". This must NOT become a partner.
        var text = """
            FEMORAL+ GLUTEO (JUEVES)
            [EX] Femoral tumbado
            Serie 1 2 3
            Reps 12 12 10
            + PESO ALTO
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);
        Assert.DoesNotContain(day.Exercises, e => (e.Name ?? "").Contains("Peso Alto", StringComparison.OrdinalIgnoreCase));
        var femoral = day.Exercises.FirstOrDefault();
        Assert.NotNull(femoral);
        Assert.Null(femoral.SupersetWith);
    }

    [Fact]
    public void Exercise_Name_With_Inline_Description_After_Colon_Is_Truncated()
    {
        // The trainer's PDF emits green "Abductor:" followed by black "PONIENDONOS DE PIE...".
        // When extraction merges them, the exercise name must be truncated to just "Abductor".
        var text = """
            GLUTEO (LUNES)
            [EX] Abductor: PONIENDONOS DE PIE Y APRETANDO EL GLUTEO
            Serie 1 2 3
            Reps 20 12 10
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 1);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.Equal("Abductor", ex.Name);
    }

    [Fact]
    public void Repeated_Peso_Alto_Cells_Concatenated_Are_Not_Exercise()
    {
        // PDF renders three "Peso Alto" cells in green across a row; extractor concatenates
        // them into a single [EX] line. Must be discarded.
        var text = """
            FEMORAL (JUEVES)
            [EX] Femoral tumbado
            Serie 1 2 3
            Reps 12 12 10
            [EX] Peso Alto Peso Alto Peso Alto
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);
        Assert.DoesNotContain(day.Exercises, e => (e.Name ?? "").Contains("Peso Alto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Peso_Ligero_Description_Is_Not_Exercise()
    {
        // Green descriptive text starting with "Peso ligero ..." must be a note, not an exercise.
        // "Peso muerto" must still pass.
        var text = """
            CUADRICEPS (LUNES)
            [EX] Peso ligero y que se pueda controlar para llegar al fallo
            [EX] Peso muerto rumano
            Serie 1 2 3
            Reps 10 10 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 1);
        Assert.NotNull(day);
        Assert.DoesNotContain(day.Exercises, e => (e.Name ?? "").StartsWith("Peso Ligero", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(day.Exercises, e => (e.Name ?? "").Contains("Peso Muerto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Name_Is_Truncated_At_Comma_When_Followed_By_Conjunction()
    {
        var text = """
            LUMBAR (MARTES)
            [EX] Elevación de lumbar, pero teniendo la espalda recta y sin arquear
            Serie 1 2 3
            Reps 20 15 12
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 2);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.Equal("Elevación De Lumbar", ex.Name);
    }

    [Fact]
    public void Sanitize_Ai_Extraction_Drops_Bogus_Peso_Lines()
    {
        // AI returns these on top of legit exercises; SanitizeAiExtraction must strip them.
        var ai = new PdfExtraction
        {
            Routines =
            {
                new PdfDayRoutine
                {
                    DayOfWeek = 4,
                    Exercises =
                    {
                        new PdfExercise { Name = "Femoral Tumbado" },
                        new PdfExercise { Name = "Peso Alto Peso Alto Peso Alto" },
                        new PdfExercise { Name = "Peso ligero y que se pueda controlar" },
                        new PdfExercise { Name = "Peso muerto rumano" },
                        new PdfExercise { Name = "Elevación de lumbar, pero teniendo la espalda recta" },
                    }
                }
            }
        };

        PdfImportService.SanitizeAiExtraction(ai, NullLogger.Instance);

        var day = ai.Routines[0];
        var names = day.Exercises.Select(e => e.Name).ToList();
        Assert.Contains("Femoral Tumbado", names);
        Assert.Contains("Peso muerto rumano", names);
        Assert.Contains("Elevación de lumbar", names); // truncated at comma+pero
        Assert.DoesNotContain(names, n => n != null && n.StartsWith("Peso Alto", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n != null && n.StartsWith("Peso ligero", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Black_PESO_ALTO_Inside_Table_Does_Not_Steal_Superset_Partner()
    {
        // Reproduces the trainer's PDF: "Femoral tumbado" green, then a table whose Reps cell
        // contains "PESO ALTO" (in BLACK — no [EX] marker), then the partner line.
        // Bug we are fixing: "PESO ALTO PESO ALTO PESO ALTO" was being detected by the uppercase
        // heuristic, becoming the current exercise, and the "+ Super serie ..." partner was
        // attaching to it instead of "Femoral tumbado".
        var text = """
            FEMORAL (JUEVES)
            [EX] Femoral tumbado
            Serie 1 2 3
            Reps 12 12 10
            PESO ALTO PESO ALTO PESO ALTO
            Fase positiva 2 2 2
            Tiempo de descanso 1m 1m 1m
            + Super serie femoral unilateral tumbado
            Serie 1 2 3
            Reps 8 8 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);

        // "Peso Alto" must not have been imported as an exercise.
        Assert.DoesNotContain(day.Exercises, e => (e.Name ?? "").Contains("Peso Alto", StringComparison.OrdinalIgnoreCase));

        // The superset must pair Femoral Tumbado <-> Femoral Unilateral Tumbado.
        var femoralTumbado = day.Exercises.FirstOrDefault(e => (e.Name ?? "").Contains("Tumbado", StringComparison.OrdinalIgnoreCase) && !(e.Name ?? "").Contains("Unilateral", StringComparison.OrdinalIgnoreCase));
        var femoralUnilateral = day.Exercises.FirstOrDefault(e => (e.Name ?? "").Contains("Unilateral", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(femoralTumbado);
        Assert.NotNull(femoralUnilateral);
        Assert.Equal(femoralUnilateral.Name, femoralTumbado.SupersetWith);
        Assert.Equal(femoralTumbado.Name, femoralUnilateral.SupersetWith);
    }

    [Fact]
    public void Black_All_Caps_PESO_LIGERO_Instruction_Is_Not_Exercise()
    {
        // "PESO LIGERO Y QUE SE PUEDA CONTROLAR PARA LLEVAR LOS TIEMPOS PAUTADOS" appears in the
        // trainer's PDF as a black, all-caps instruction above the next table. The uppercase
        // heuristic was importing it as an exercise; it must be discarded.
        var text = """
            FEMORAL (JUEVES)
            [EX] Femoral unilateral tumbado
            PESO LIGERO Y QUE SE PUEDA CONTROLAR PARA LLEVAR LOS TIEMPOS PAUTADOS
            Serie 1 2 3
            Reps 8 8 8
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);
        Assert.DoesNotContain(day.Exercises, e => (e.Name ?? "").StartsWith("Peso Ligero", StringComparison.OrdinalIgnoreCase));
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
