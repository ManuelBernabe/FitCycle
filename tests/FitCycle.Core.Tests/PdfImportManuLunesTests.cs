using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// End-to-end check of the Lunes (Monday) day. Covers the trainer's long-form prescriptions
/// like "Extensión de cuadriceps: Calentamiento 3 series con peso ligero y controlado
/// (15 reps por serie)" where the rep info is buried inside a paragraph.
/// </summary>
public class PdfImportManuLunesTests
{
    private const string LunesText = """
        CUADRICEPS+ ABDUCTOR+ADUCTOR+GEMELO (LUNES-VIERNES)
        [EX] Extensión de cuadriceps:
        Calentamiento 3 series con peso ligero y controlado (15 reps por serie) -- 1 minuto de descanso por serie
        [EX] Zancada con mancuernas en el pasillo de las mancuernas o en el de la entrada
        4*10 pasos ida*10 vuelta
        [EX] Abductor:
        Serie 1 2 3 4
        Reps 20 12 10 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4 3-4
        Tiempo de descanso 1m 1m 1m 1m
        [EX] Sentadilla con máquina que bloquea hombros:
        Serie 1 2 3 4
        Reps 12 10 10 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 3 3 3
        Tiempo de descanso 90s 90s 90s 90s
        [EX] Aductor:
        Serie 1 2 3 4 5
        Reps 20 12 10 8 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4 3-4 3-4
        [EX] Prensa unilateral en la máquina cibex:
        Serie 1 2 3 4 5
        Reps 15 12 10 8 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 2 4 4 4
        Descanso 3 minutos 3 minutos 1 1 1
        [EX] Extensión de cuadríceps:
        Iniciamos desde la serie 1 con el máximo peso que podamos mover
        Serie 1 2 3
        Reps 15 15 15
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 2m 2m 2m
        [EX] Gemelo de pie:
        4 series x 20 reps
        """;

    [Fact]
    public void Lunes_Extension_Cuadriceps_Calentamiento_Three_Sets_Of_Fifteen()
    {
        // The line "Calentamiento 3 series con peso ligero y controlado (15 reps por serie)"
        // used to be promoted to a bogus exercise (PESO is a keyword) and the real Extensión
        // de cuadriceps fell back to a 3x12 default. Now the line attaches as 3x15 to the
        // current exercise (Extensión de cuadriceps).
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e =>
            (e.Name ?? "").Equals("Extensión De Cuadriceps", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(15, s.Reps));
    }

    [Fact]
    public void Lunes_Bogus_Calentamiento_Description_Is_Not_An_Exercise()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var names = lunes.Exercises.Select(e => e.Name ?? "").ToList();
        Assert.DoesNotContain(names, n =>
            n.StartsWith("Calentamiento", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Lunes_Abductor_Four_Sets()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Equals("Abductor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 20, 12, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Lunes_Sentadilla_Four_Sets()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Contains("Sentadilla", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 12, 10, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Lunes_Aductor_Five_Sets()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Equals("Aductor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(5, ex.Sets.Count);
        Assert.Equal(new[] { 20, 12, 10, 8, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Lunes_Prensa_Cibex_Five_Sets()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Contains("Prensa", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(5, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Lunes_Second_Extension_Cuadriceps_Three_Sets_Of_Fifteen()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        // There are two "Extensión de cuadr..." entries. Take the last one (the 3x15 table).
        var ex = lunes.Exercises.Last(e =>
            (e.Name ?? "").Contains("Extensión De Cuadr", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(15, s.Reps));
    }

    [Fact]
    public void Lunes_Zancada_Four_Sets_Of_Ten()
    {
        // The "4*10 pasos ida*10 vuelta" line — the second "*10" used to leak into the reps
        // list and produce 3 sets of [4,10,10] instead of 4 sets of 10. User screenshot
        // showed "Ejercicio 2 de 9 / Serie 3 de 3" for Zancada — exactly the bug.
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Contains("Zancada", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Lunes_Gemelo_Four_Sets_Of_Twenty()
    {
        var lunes = Parse().Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Contains("Gemelo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(20, s.Reps));
    }

    private static PdfExtraction Parse() => LocalPdfParser.Parse(LunesText);
}
