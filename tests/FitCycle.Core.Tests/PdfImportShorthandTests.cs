using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// Edge cases of the "N*M" / "N*R1*R2*..." shorthand syntax the trainer uses for sets×reps,
/// plus other compact prescription forms that appear scattered across the PDF.
/// </summary>
public class PdfImportShorthandTests
{
    [Fact]
    public void Zancada_4x10_With_Trailing_Pasos_Ida_Vuelta_Yields_Four_Sets_Of_Ten()
    {
        // Page 2 of the Manu 6 SEMANAS PDF: "Zancada con mancuernas ... / 4*10 pasos ida*10 vuelta".
        // BUG before fix: parser found three numbers (4, 10, 10) and emitted 3 sets of [4,10,10]
        // — so on the phone the user saw "Serie 3 de 3" with reps=10 instead of "Serie 1..4 de 4".
        var text = """
            PIERNAS (LUNES)
            [EX] Zancada con mancuernas en el pasillo
            4*10 pasos ida*10 vuelta
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 1).Exercises.First();
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(10, s.Reps));
    }

    [Fact]
    public void Shorthand_Full_Per_Set_Reps_Still_Works()
    {
        // Reps shorthand with the trainer-style per-set rep list: 4 sets with reps 15, 12, 10, 8.
        // Distinguishing rule: numbers.Count == numbers[0] + 1 → per-set reps.
        var text = """
            PIERNAS (LUNES)
            [EX] Test exercise
            4*15*12*10*8
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 1).Exercises.First();
        Assert.Equal(4, ex.Sets.Count);
        Assert.Equal(new[] { 15, 12, 10, 8 }, ex.Sets.Select(s => s.Reps).ToArray());
    }

    [Fact]
    public void Shorthand_Simple_NxM_Yields_N_Sets_Of_M()
    {
        // The basic "4x15" / "4*15" form must keep working — 4 sets of 15.
        var text = """
            PIERNAS (LUNES)
            [EX] Test exercise
            4*15
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 1).Exercises.First();
        Assert.Equal(4, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(15, s.Reps));
    }

    [Fact]
    public void Shorthand_With_Comma_Separated_Trailing_Number_Does_Not_Confuse()
    {
        // Defensive: "3*12 con descanso de 1 minuto" should give 3 sets of 12, not (3,12,1).
        var text = """
            PIERNAS (LUNES)
            [EX] Test exercise
            3*12 con descanso de 1 minuto
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 1).Exercises.First();
        Assert.Equal(3, ex.Sets.Count);
        Assert.All(ex.Sets, s => Assert.Equal(12, s.Reps));
    }

    [Fact]
    public void Shorthand_Set_Count_Is_Clamped_To_Twenty()
    {
        // Defensive against ridiculous numbers — never produce more than 20 sets.
        var text = """
            PIERNAS (LUNES)
            [EX] Test exercise
            999*10
            """;
        var result = LocalPdfParser.Parse(text);
        var ex = result.Routines.First(r => r.DayOfWeek == 1).Exercises.First();
        Assert.True(ex.Sets.Count <= 20, $"Expected at most 20 sets, got {ex.Sets.Count}");
    }
}
