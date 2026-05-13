using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

public class PdfParserDayDetectionTests
{
    [Fact]
    public void Detects_Spanish_Day_Name_In_Parenthesis()
    {
        var text = """
            PECTORAL+TRÍCEPS+HOMBRO (MARTES)
            Press banca plano
            Serie 1 2 3
            Reps 12 10 8
            """;
        var result = LocalPdfParser.Parse(text);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 2);
    }

    [Fact]
    public void Detects_All_Spanish_Days()
    {
        var text = """
            PECHO (LUNES)
            Press
            ESPALDA (MARTES)
            Remo
            HOMBROS (MIÉRCOLES)
            Press militar
            PIERNAS (JUEVES)
            Sentadilla
            BRAZOS (VIERNES)
            Curl
            """;
        var result = LocalPdfParser.Parse(text);
        var days = result.Routines.Select(r => r.DayOfWeek).OrderBy(d => d).ToList();
        Assert.Contains(1, days);
        Assert.Contains(2, days);
        Assert.Contains(3, days);
        Assert.Contains(4, days);
        Assert.Contains(5, days);
    }

    [Fact]
    public void Detects_Day_Range_LUNES_VIERNES_Creates_Two_Entries()
    {
        var text = """
            CUADRICEPS+ ABDUCTOR+ADUCTOR+GEMELO (LUNES-VIERNES)
            Extensión de cuadriceps
            Serie 1 2 3
            Reps 15 12 10
            Sentadilla
            """;
        var result = LocalPdfParser.Parse(text);
        // Should create 2 entries: one for Monday (1) and one for Friday (5)
        Assert.Contains(result.Routines, r => r.DayOfWeek == 1);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 5);
    }

    [Fact]
    public void Still_Detects_DIA_N_Format()
    {
        var text = """
            DÍA 1 - PECHO
            Press banca
            Serie 1 2
            Reps 12 10
            DÍA 2 - ESPALDA
            Remo
            """;
        var result = LocalPdfParser.Parse(text);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 1);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 2);
    }

    [Fact]
    public void Handles_Miercoles_Without_Accent()
    {
        var text = """
            ESPALDA (MIERCOLES)
            Dominadas
            """;
        var result = LocalPdfParser.Parse(text);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 3);
    }
}
