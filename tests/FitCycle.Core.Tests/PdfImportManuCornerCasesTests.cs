using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// Edge cases scraped from the entire Manu 6 SEMANAS PDF that don't fit one specific day.
/// These guard against regressions in the trickier formatting patterns the trainer uses.
/// </summary>
public class PdfImportManuCornerCasesTests
{
    [Fact]
    public void Lunes_Cibex_With_Max_Peso_Cell_Labels_Keeps_Five_Sets()
    {
        // Page 4: "Serie 1 (max peso) 2 (max peso) 3 4 5 / Reps 15 12 10 8 8".
        // The "(max peso)" annotations must not be promoted to fake exercises and must
        // not steal the Reps row from Prensa cibex.
        var text = """
            PIERNAS (LUNES)
            [EX] Prensa unilateral en la máquina cibex:
            Serie 1 (max peso) 2 (max peso) 3 4 5
            Reps 15 12 10 8 8
            Fase positiva
            Seg. Ejecución
            2 2 2 2 2
            Fase negativa
            Seg. Ejecución
            2 2 4 4 4
            Descanso 3 minutos 3 minutos 1 1 1
            """;
        var result = LocalPdfParser.Parse(text);
        var lunes = result.Routines.First(r => r.DayOfWeek == 1);
        Assert.Single(lunes.Exercises);
        var ex = lunes.Exercises[0];
        Assert.Equal(5, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Lunes_Aductor_With_Pisada_Sumo_Cell_Labels_Keeps_Four_Sets()
    {
        // Page 3: Aductor table has "Pisada tipo sumo" annotations under every Serie
        // column. Must not split into multiple exercises.
        var text = """
            PIERNAS (LUNES)
            [EX] Aductor:
            Serie 1 2 3 4
            Pisada tipo sumo Pisada tipo sumo Pisada tipo sumo Pisada tipo sumo
            Reps 12 10 10 8
            Fase positiva
            Seg. Ejecución
            2 2 2 2
            Fase negativa
            Seg. Ejecución
            2 3 3 3
            Tiempo de descanso 90s 90s 90s 90s
            """;
        var result = LocalPdfParser.Parse(text);
        var lunes = result.Routines.First(r => r.DayOfWeek == 1);
        var ex = lunes.Exercises.First(e => (e.Name ?? "").Equals("Aductor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 12, 10, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Miercoles_Curl_Predicador_Inline_With_Rest_Parenthetical()
    {
        // Page 13: "Curl predicador de bíceps UNILATERAL 3 seriesx10 reps (1 minuto de
        // descanso por serie)". The trailing parenthetical contains "descanso" and "1" —
        // before the RestLine anchor fix, the parser swallowed the entire line.
        var text = """
            ESPALDA (MIÉRCOLES)
            [EX] Curl predicador de bíceps unilateral
            3 seriesx10 reps (1 minuto de descanso por serie)
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 3).Exercises.First();
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Jueves_Femoral_Unilateral_Header_Cells_Are_Stripped()
    {
        // Page 22: Femoral unilateral Serie row has long inline annotations
        // "1+ super serie mitad de peso a 8 reps* cada pierna" in every cell.
        // The exercise must still parse with 4 sets and the right Reps.
        var text = """
            FEMORAL (JUEVES)
            [EX] Femoral unilateral:
            Serie 1+ super serie mitad de peso a 8 reps cada pierna
            Reps 15 12 10 8
            Fase positiva
            Seg. Ejecución
            2 2 2 2
            Fase negativa
            Seg. Ejecución
            3 3 3 2
            Tiempo de descanso 1 minuto por pierna 1 minuto por pierna 1 minuto por pierna 1 minuto por pierna
            """;
        var result = LocalPdfParser.Parse(text);
        var jueves = result.Routines.First(r => r.DayOfWeek == 4);
        var ex = jueves.Exercises.First(e => (e.Name ?? "").Equals("Femoral Unilateral", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Jueves_Femoral_Sentado_Pausa_Dashes_Do_Not_Crash_Tempo_Parser()
    {
        // Page 18: Femoral sentado has a "Pausa --- --- --- 2s" row. The dashes mustn't
        // become tempo values or split into bogus sets.
        var text = """
            FEMORAL (JUEVES)
            [EX] Femoral sentado:
            Serie 1 2 3 4
            Reps 20 15 12 8
            Fase positiva
            Seg. Ejecución
            2 2 2 2
            Fase negativa
            Seg. Ejecución
            2 3 3 2
            Pausa --- --- --- 2s
            Descanso 90s 90s 90s 90s
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 4).Exercises.First();
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 20, 15, 12, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
        Assert.Equal(new[] { 2, 3, 3, 2 }, ex.Sets.Select(s => s.TempoNeg).ToArray());
    }

    [Fact]
    public void Generic_Tiempo_De_Descanso_Inline_Does_Not_Eat_Reps_Row()
    {
        // Defensive: a "Tiempo de descanso: 1 minuto por serie" line that follows a
        // Reps row must not retroactively clear the sets we just parsed.
        var text = """
            PIERNAS (LUNES)
            [EX] Test exercise
            Serie 1 2 3
            Reps 15 12 10
            Tiempo de descanso: 1 minuto por serie
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 1).Exercises.First();
        Assert.Equal(3, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10 }, ex.Sets.Select(s => s.Reps).ToArray());
    }
}
