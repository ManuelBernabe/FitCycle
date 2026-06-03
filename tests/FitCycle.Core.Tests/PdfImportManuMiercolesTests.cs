using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// End-to-end check of Miércoles (Espalda + Bíceps). Covers the long-form free-text
/// prescriptions like "Curl predicador de bíceps UNILATERAL 3 seriesx10 reps".
/// </summary>
public class PdfImportManuMiercolesTests
{
    private const string MiercolesText = """
        ESPALDA+ BÍCEPS (MIÉRCOLES)
        [EX] Elevación de lumbar en máquina:
        4 series x 20, 15, 12, 10 reps
        Tiempo de descanso: 1 minuto por serie
        [EX] Curl predicador de bíceps unilateral
        3 seriesx10 reps
        Tiempo de descanso: 1 minuto por serie
        [EX] Máquina agarre supino unilateral
        Serie 1 2 3
        Reps 15 12 8
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 3 4
        Tiempo de descanso: 1 minuto por serie
        [EX] Posterior desde polea baja unilateral
        Serie 1 2 3
        Reps 12 10 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 2 2
        [EX] Pull over unilateral
        3 series*10 reps
        Tiempo de descanso: 1 minuto por serie
        [EX] Remo unilateral con mancuernas
        Serie 1 2 3
        Reps 15 12 8
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 2 2
        Tiempo de descanso: 1 minuto por serie
        [EX] Remo unilateral con agarre prono hammer
        Serie 1 2 3
        Reps 15 12 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 3 4
        [EX] Curl predicador unilateral en máquina
        3 series *10 reps
        [EX] Curl martillo con mancuernas
        3 series *10 reps
        """;

    [Fact]
    public void Miercoles_Elevacion_Lumbar_Four_Distinct_Reps()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 3);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Elevación De Lumbar", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 20, 15, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Miercoles_Curl_Predicador_Warmup_Three_Sets_Of_Ten()
    {
        // "3 seriesx10 reps" — no space between 'series' and 'x'.
        var dia = Parse().Routines.First(r => r.DayOfWeek == 3);
        var ex = dia.Exercises.First(e =>
            (e.Name ?? "").Contains("Curl Predicador De Bíceps", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Miercoles_Maquina_Agarre_Supino_Three_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 3);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Máquina Agarre Supino", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Miercoles_Pull_Over_Three_Sets_Of_Ten()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 3);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Pull Over", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Miercoles_Curl_Martillo_Three_Sets_Of_Ten()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 3);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Curl Martillo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Miercoles_Curl_Predicador_Unilateral_En_Maquina_Three_Sets_Of_Ten()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 3);
        var ex = dia.Exercises.First(e =>
            (e.Name ?? "").Contains("Curl Predicador Unilateral En", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    private static PdfExtraction Parse() => LocalPdfParser.Parse(MiercolesText);
}
