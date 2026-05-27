using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

public class PdfImportEdgeCasesTests
{
    [Fact]
    public void Tempo_Row_With_Range_Collapses_To_Single_Number()
    {
        // The trainer's PDF has rows like "2 4 4 3-4 3-4" for 5 sets. The old `\d+` regex
        // captured 7 numbers and smeared the tempo across the wrong sets.
        var nums = LocalPdfParser.ParseTempoNumbers("2 4 4 3-4 3-4");

        Assert.Equal(5, nums.Count);
        Assert.Equal(new[] { 2, 4, 4, 4, 4 }, nums);
    }

    [Fact]
    public void Tempo_Row_Without_Ranges_Still_Parses()
    {
        var nums = LocalPdfParser.ParseTempoNumbers("2 2 2 2");
        Assert.Equal(new[] { 2, 2, 2, 2 }, nums);
    }

    [Fact]
    public void Tempo_Range_Picks_Upper_Bound()
    {
        var nums = LocalPdfParser.ParseTempoNumbers("3-4");
        Assert.Single(nums);
        Assert.Equal(4, nums[0]);
    }

    [Fact]
    public void Tempo_Row_With_Range_Applied_To_Five_Sets_Maps_One_To_One()
    {
        // Real Lunes 'Aductor' table: reps 20/12/10/8/8 → 5 sets,
        // Fase negativa "2 4 4 3-4 3-4". Each set must end up with the right negative tempo.
        var text = """
            PIERNAS (LUNES)
            [EX] Aductor
            Serie 1 2 3 4 5
            Reps 20 12 10 8 8
            Fase positiva
            Seg. Ejecución
            2 2 2 2 2
            Fase negativa
            Seg. Ejecución
            2 4 4 3-4 3-4
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 1);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.Equal(5, ex.Sets.Count);
        Assert.Equal(new[] { 2, 4, 4, 4, 4 }, ex.Sets.Select(s => s.TempoNeg).ToArray());
    }

    [Fact]
    public void Femoral_Sentado_Keeps_Its_Four_Sets_When_Calentamiento_Cell_Label_Is_Present()
    {
        // Page 18 of the trainer's PDF: 'Femoral sentado' Serie row has cell labels
        // 'Calentamiento' under serie 1 and 'Max peso' under serie 4. The extractor
        // emits "Calentamiento Max peso" on its own line. Because "PESO" is an
        // exercise keyword, this used to be promoted to an exercise and steal the
        // Reps row from Femoral sentado (which then fell back to 3x12 defaults).
        var text = """
            FEMORAL (JUEVES)
            [EX] Femoral sentado:
            Calentamiento Max peso
            Serie 1 2 3 4
            Reps 20 15 12 8
            Fase positiva
            Seg. Ejecución
            2 2 2 2
            Fase negativa
            Seg. Ejecución
            2 3 3 2
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);
        // 'Calentamiento Max peso' must NOT have become its own exercise.
        Assert.DoesNotContain(day.Exercises, e =>
            (e.Name ?? "").Contains("Calentamiento", StringComparison.OrdinalIgnoreCase));
        var femoral = day.Exercises.FirstOrDefault();
        Assert.NotNull(femoral);
        Assert.Equal(4, femoral.Sets.Count);
        Assert.Equal(new[] { 20, 15, 12, 8 }, femoral.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Patada_De_Gluteo_Parses_Four_Sets_From_Free_Text()
    {
        // Page 18: 'Patada de glúteo: 4 series* 15 reps' — written without a table.
        // The old `\d+\s+series\s+\d` regex required no separator; the new pattern
        // accepts the trainer's "series*" / "series x" forms.
        var text = """
            GLUTEO (JUEVES)
            [EX] Patada de glúteo:
            Flexionamos la rodilla a unos 90 grados
            4 series* 15 reps
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.Contains("Patada", ex.Name ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(15, s.Reps));
    }

    [Fact]
    public void Gemelo_De_Pie_Parses_Four_Sets_From_X_Notation()
    {
        // Page 6 / 23: 'Gemelo de pie: 4 series x 20 reps'.
        var text = """
            PIERNAS (LUNES)
            [EX] Gemelo de pie:
            4 series x 20 reps
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 1);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.Contains("Gemelo", ex.Name ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(20, s.Reps));
    }

    [Fact]
    public void Elevacion_Lumbar_Parses_Four_Distinct_Reps()
    {
        // Page 12: '4 series x 20, 15, 12, 10 reps' — same regex, multiple reps.
        var text = """
            ESPALDA (MIÉRCOLES)
            [EX] Elevación de lumbar en máquina
            4 series x 20, 15, 12, 10 reps
            """;
        var result = LocalPdfParser.Parse(text);
        var day = result.Routines.FirstOrDefault(r => r.DayOfWeek == 3);
        Assert.NotNull(day);
        var ex = day.Exercises.FirstOrDefault();
        Assert.NotNull(ex);
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 20, 15, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Ai_SupersetWith_Is_Discarded_During_Sanitize()
    {
        // The AI sometimes invents partners that contradict the trainer's PDF (e.g. pairing
        // Femoral tumbado with Femoral unilateral DE PIE instead of the real
        // Femoral unilateral TUMBADO). Letting that through corrupts the workout pairings.
        var ai = new PdfExtraction
        {
            Routines =
            {
                new PdfDayRoutine
                {
                    DayOfWeek = 4,
                    Exercises =
                    {
                        new PdfExercise { Name = "Femoral Tumbado", SupersetWith = "Femoral Unilateral De Pie" },
                        new PdfExercise { Name = "Femoral Unilateral De Pie", SupersetWith = "Femoral Tumbado" },
                    }
                }
            }
        };

        PdfImportService.SanitizeAiExtraction(ai, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        var day = ai.Routines[0];
        Assert.All(day.Exercises, e => Assert.Null(e.SupersetWith));
    }

    [Fact]
    public void Calentamiento_Max_Peso_Is_Never_Imported_As_Exercise()
    {
        // Direct check: even in isolation, this cell-label line must be rejected.
        Assert.True(LocalPdfParser.IsBogusGreenExerciseLine("Calentamiento Max peso"));
        Assert.True(LocalPdfParser.IsBogusGreenExerciseLine("Calentamiento"));
        Assert.True(LocalPdfParser.IsBogusGreenExerciseLine("Max peso"));
        // But a legit warm-up exercise mention must NOT be rejected.
        Assert.False(LocalPdfParser.IsBogusGreenExerciseLine("Press banca plano"));
        Assert.False(LocalPdfParser.IsBogusGreenExerciseLine("Femoral sentado"));
        Assert.False(LocalPdfParser.IsBogusGreenExerciseLine("Peso muerto rumano"));
    }
}
