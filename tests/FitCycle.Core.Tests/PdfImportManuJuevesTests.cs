using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// End-to-end check of the Jueves (Thursday) day of the trainer's Manu 6 SEMANAS PDF.
/// Reproduces the text the extractor emits for each table on pages 18-23, then asserts
/// every exercise was captured with the correct rep count and per-set reps.
/// </summary>
public class PdfImportManuJuevesTests
{
    private const string JuevesText = """
        FEMORAL+ GLUTEO + ABDUC+ ADUC+ GEMELO (JUEVES)
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
        Descanso 90s 90s 90s 90s
        [EX] Patada de glúteo:
        Flexionamos la rodilla a unos 90 grados
        4 series* 15 reps
        [EX] Abductor:
        Serie 1 2 3 4 5
        Reps 20 12 10 8 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4 2-3 2-3
        Tiempo de descanso 1m 1m 1m 1m 1m
        [EX] Femoral tumbado
        Serie 1 2 3
        Reps 12 12 10
        PESO ALTO PESO ALTO PESO ALTO
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Tiempo de descanso 1m 1m 1m
        + Super serie femoral unilateral tumbado
        PESO LIGERO Y QUE SE PUEDA CONTROLAR PARA LLEVAR LOS TIEMPOS PAUTADOS
        Serie 1 2 3
        Reps 8 rep por pierna 8 rep por pierna 8 rep por pierna
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        3 3 3
        Tiempo de descanso 1m 1m 1m
        [EX] Aductor:
        Serie 1 2 3
        Reps 20 12 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 1m 1m 1m
        [EX] Femoral unilateral:
        Serie 1 2 3 4
        Reps 15 12 10 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2
        Fase negativa
        Seg. Ejecución
        3 3 3 2
        Tiempo de descanso 1m 1m 1m 1m
        [EX] Elevación de lumbar, pero teniendo la espalda un poco en curva:
        Serie 1 2 3
        Reps 20 12 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 1m 1m 1m
        [EX] Gemelo de pie o sentado:
        4 series x 20 reps
        """;

    [Fact]
    public void Jueves_All_Nine_Exercises_Captured()
    {
        var result = LocalPdfParser.Parse(JuevesText);
        var jueves = result.Routines.FirstOrDefault(r => r.DayOfWeek == 4);
        Assert.NotNull(jueves);
        var names = jueves.Exercises.Select(e => e.Name ?? "").ToList();

        // 9 real exercises (no "Peso Alto", no "Calentamiento Max Peso", no PESO LIGERO instructions).
        Assert.Contains(names, n => n.Contains("Femoral Sentado", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Contains("Patada", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Equals("Abductor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Equals("Femoral Tumbado", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Contains("Femoral Unilateral Tumbado", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Equals("Aductor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Equals("Femoral Unilateral", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Contains("Elevación De Lumbar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Contains("Gemelo De Pie", StringComparison.OrdinalIgnoreCase));

        // No bogus exercises.
        Assert.DoesNotContain(names, n => n.Contains("Peso Alto", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.StartsWith("Peso Ligero", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Calentamiento Max", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Jueves_Femoral_Sentado_Has_Four_Sets_With_Correct_Reps()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Contains("Femoral Sentado", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 20, 15, 12, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
        Assert.Equal(new[] { 2, 2, 2, 2 }, ex.Sets.Select(s => s.TempoPos).ToArray());
        Assert.Equal(new[] { 2, 3, 3, 2 }, ex.Sets.Select(s => s.TempoNeg).ToArray());
    }

    [Fact]
    public void Jueves_Patada_Has_Four_Sets_Of_15()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Contains("Patada", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(15, s.Reps));
    }

    [Fact]
    public void Jueves_Abductor_Has_Five_Sets_With_Range_Tempo()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Equals("Abductor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(5, ex.Sets.Count);
        Assert.Equal(new[] { 20, 12, 10, 8, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
        // "2 4 4 2-3 2-3" collapses to [2,4,4,3,3]
        Assert.Equal(new[] { 2, 4, 4, 3, 3 }, ex.Sets.Select(s => s.TempoNeg).ToArray());
    }

    [Fact]
    public void Jueves_Femoral_Tumbado_Superset_Has_Three_Sets_Each()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var tumbado = jueves.Exercises.First(e => (e.Name ?? "").Equals("Femoral Tumbado", StringComparison.OrdinalIgnoreCase));
        var partner = jueves.Exercises.First(e => (e.Name ?? "").Contains("Femoral Unilateral Tumbado", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(3, tumbado.Sets.Count);
        Assert.Equal(new[] { 12, 12, 10 }, tumbado.Sets.Select(s => s.Reps).ToArray());

        Assert.Equal(3, partner.Sets.Count);
        Assert.Equal(new[] { 8, 8, 8 }, partner.Sets.Select(s => s.Reps).ToArray());

        Assert.Equal(partner.Name, tumbado.SupersetWith);
        Assert.Equal(tumbado.Name, partner.SupersetWith);
    }

    [Fact]
    public void Jueves_Aductor_Has_Three_Sets()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Equals("Aductor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 20, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Jueves_Femoral_Unilateral_Has_Four_Sets()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Equals("Femoral Unilateral", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Jueves_Elevacion_Lumbar_Has_Three_Sets()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Contains("Elevación De Lumbar", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 20, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Jueves_Gemelo_Has_Four_Sets_Of_20()
    {
        var jueves = Parse().Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Contains("Gemelo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(20, s.Reps));
    }

    private static PdfExtraction Parse() => LocalPdfParser.Parse(JuevesText);
}
