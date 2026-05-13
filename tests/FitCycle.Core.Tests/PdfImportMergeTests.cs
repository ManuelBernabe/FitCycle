using System.Reflection;
using FitCycle.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitCycle.Core.Tests;

public class PdfImportMergeTests
{
    // Reflection helper to call the private static MergeExtractions method
    private static PdfExtraction Merge(PdfExtraction? local, PdfExtraction? ai)
    {
        var method = typeof(PdfImportService).GetMethod("MergeExtractions",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (PdfExtraction)method!.Invoke(null, new object?[] { local, ai, NullLogger.Instance })!;
    }

    private static PdfExtraction MakeExtraction(params (int day, int exerciseCount)[] days)
    {
        var ext = new PdfExtraction();
        foreach (var (day, count) in days)
        {
            var routine = new PdfDayRoutine { DayOfWeek = day };
            for (int i = 0; i < count; i++)
                routine.Exercises.Add(new PdfExercise { Name = $"Ex-D{day}-{i}" });
            ext.Routines.Add(routine);
        }
        return ext;
    }

    [Fact]
    public void Merge_Empty_Local_Returns_AI()
    {
        var ai = MakeExtraction((1, 5), (2, 3));
        var result = Merge(new PdfExtraction(), ai);
        Assert.Equal(2, result.Routines.Count);
        Assert.Equal(5, result.Routines[0].Exercises.Count);
    }

    [Fact]
    public void Merge_Empty_AI_Returns_Local()
    {
        var local = MakeExtraction((1, 5), (2, 3));
        var result = Merge(local, new PdfExtraction());
        Assert.Equal(2, result.Routines.Count);
        Assert.Equal(5, result.Routines[0].Exercises.Count);
    }

    [Fact]
    public void Merge_AI_Has_More_Exercises_For_Day_AI_Wins()
    {
        var local = MakeExtraction((1, 3));
        var ai = MakeExtraction((1, 7));
        var result = Merge(local, ai);
        Assert.Single(result.Routines);
        Assert.Equal(7, result.Routines[0].Exercises.Count);
    }

    [Fact]
    public void Merge_Local_Has_More_Exercises_Local_Wins()
    {
        var local = MakeExtraction((1, 8));
        var ai = MakeExtraction((1, 4));
        var result = Merge(local, ai);
        Assert.Single(result.Routines);
        Assert.Equal(8, result.Routines[0].Exercises.Count);
    }

    [Fact]
    public void Merge_Different_Days_From_Each_Source_Both_Kept()
    {
        var local = MakeExtraction((1, 5));
        var ai = MakeExtraction((2, 4));
        var result = Merge(local, ai);
        Assert.Equal(2, result.Routines.Count);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 1 && r.Exercises.Count == 5);
        Assert.Contains(result.Routines, r => r.DayOfWeek == 2 && r.Exercises.Count == 4);
    }

    [Fact]
    public void Merge_Per_Day_Selection_Is_Independent()
    {
        // Local wins day 1 (more exercises), AI wins day 2
        var local = MakeExtraction((1, 8), (2, 2));
        var ai = MakeExtraction((1, 3), (2, 6));
        var result = Merge(local, ai);
        Assert.Equal(2, result.Routines.Count);
        Assert.Equal(8, result.Routines.First(r => r.DayOfWeek == 1).Exercises.Count);
        Assert.Equal(6, result.Routines.First(r => r.DayOfWeek == 2).Exercises.Count);
    }
}
