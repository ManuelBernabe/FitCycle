using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// End-to-end check of Martes (Pectoral + Tríceps + Hombro). Day 2 has two warm-ups in
/// free-text form ("3 Series *10 reps" and "3 series*10 reps") and a "Descanso 1 minuto x
/// serie" line that must not swallow neighbouring rep data.
/// </summary>
public class PdfImportManuMartesTests
{
    private const string MartesText = """
        PECTORAL+TRÍCEPS+HOMBRO (MARTES)
        Calentamiento previo a ejercicio:
        [EX] Elevaciones laterales con mínimo peso (DE MANERA UNILATERAL)
        3 Series *10 reps
        [EX] Tríceps con anilla de manera unilateral (agarre supino)
        PESO LIGERO-LO USAMOS PARA CALENTAR TRÍCEPS PARA INTRODUCIRNOS A LOS EMPUJES
        3 series*10 reps
        Descanso 1 minuto x serie
        [EX] Press banca plano (unilateral)
        Serie 1 2 3 4
        Reps 15 12 10 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4 4
        Descanso 1m 1m 1m 1m
        [EX] Aperturas unilateral (En maquina ángulo plano)
        Serie 1 2 3
        Reps 15 15 15
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 1m 1m 1m
        [EX] Press inclinado nautilius unilateral
        Serie 1 2 3
        Reps 15 12 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 1m 1m 1m
        [EX] Tríceps agarre cuerda unilateral
        Serie 1 2 3 4
        Reps 15 12 10 8
        Fase positiva
        Seg. Ejecución
        2 2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4 3
        Descanso 1m 1m 1m 1m
        [EX] Tríceps unilateral agarre supino
        Serie 1 2 3
        Reps 15 12 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 1m 1m 1m
        [EX] Hombro press militar en hammer (modo unilateral)
        Serie 1 2 3
        Reps 15 12 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        2 4 4
        Descanso 1m 1m 1m
        [EX] Frontal desde polea-altura baja
        Serie 1 2 3
        Reps 12 10 10
        Fase positiva
        Seg. Ejecución
        2 2 2
        Fase negativa
        Seg. Ejecución
        3 3 3
        Descanso 1m 1m 1m
        """;

    [Fact]
    public void Martes_Elevaciones_Laterales_Three_Sets_Of_Ten()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Elevaciones Laterales", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Martes_Triceps_Anilla_Three_Sets_Of_Ten()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Anilla", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Martes_Press_Banca_Plano_Four_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Press Banca Plano", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Martes_Aperturas_Three_Sets_Of_Fifteen()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Aperturas", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(15, s.Reps));
    }

    [Fact]
    public void Martes_Press_Inclinado_Three_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Press Inclinado", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Martes_Triceps_Cuerda_Four_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Cuerda", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Martes_Triceps_Supino_Three_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Agarre Supino", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Martes_Hombro_Press_Militar_Three_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Press Militar", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Martes_Frontal_Polea_Three_Sets()
    {
        var dia = Parse().Routines.First(r => r.DayOfWeek == 2);
        var ex = dia.Exercises.First(e => (e.Name ?? "").Contains("Frontal", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 12, 10, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    private static PdfExtraction Parse() => LocalPdfParser.Parse(MartesText);
}
