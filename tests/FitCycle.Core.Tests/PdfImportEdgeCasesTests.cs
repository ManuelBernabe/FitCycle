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
}
