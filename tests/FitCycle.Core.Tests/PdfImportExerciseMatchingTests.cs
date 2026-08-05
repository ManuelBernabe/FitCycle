using FitCycle.Core.Models;
using FitCycle.Infrastructure.Services;

namespace FitCycle.Core.Tests;

/// <summary>
/// Regression tests for the fuzzy matching that resolves PDF exercise names against the
/// existing Exercises table on import. The single-token subset rule used to collapse
/// "APERTURAS INCLINADAS CON MANCUERNAS" and "APERTURAS EN BANCO PLANO" onto the seeded
/// generic "Aperturas" — the pecho day imported ONE aperturas instead of two.
/// </summary>
public class PdfImportExerciseMatchingTests
{
    private static Exercise Ex(int id, string name) => new() { Id = id, Name = name, MuscleGroupId = 1 };

    private static readonly List<Exercise> Seeded = new()
    {
        Ex(1, "Press banca"),
        Ex(2, "Press inclinado"),
        Ex(3, "Aperturas"),
        Ex(9, "Press militar"),
        Ex(21, "Sentadilla"),
    };

    [Theory]
    [InlineData("APERTURAS INCLINADAS CON MANCUERNAS")]
    [InlineData("APERTURAS EN BANCO PLANO")]
    [InlineData("APERTURAS INCLINADAS EN MÁQUINA")]
    public void GenericSingleTokenSeed_DoesNotSwallowDistinctVariants(string pdfName)
    {
        // "Aperturas" (one significant token) must NOT claim multi-word variants — each
        // variant is a distinct movement and needs its own Exercise row.
        var match = PdfImportService.FindBestFuzzyExerciseMatch(pdfName, Seeded);
        Assert.True(match == null || match.Name != "Aperturas",
            $"'{pdfName}' se tragó el ejercicio genérico 'Aperturas'");
    }

    [Fact]
    public void TwoAperturasVariants_ResolveToDifferentExercises()
    {
        var inclinadas = PdfImportService.FindBestFuzzyExerciseMatch("APERTURAS INCLINADAS CON MANCUERNAS", Seeded);
        var bancoPlano = PdfImportService.FindBestFuzzyExerciseMatch("APERTURAS EN BANCO PLANO", Seeded);
        // Neither matches an existing seed → the import creates two NEW distinct exercises.
        Assert.Null(inclinadas);
        Assert.Null(bancoPlano);
    }

    [Fact]
    public void MultiTokenSubset_StillMatches_PreservingImages()
    {
        // ≥2-token subsets remain strong matches: this is what keeps user-uploaded photos
        // attached across re-imports when the trainer adds/drops descriptor words.
        var match = PdfImportService.FindBestFuzzyExerciseMatch("PRESS MILITAR EN BARRA MULTIPOWER", Seeded);
        Assert.NotNull(match);
        Assert.Equal("Press militar", match!.Name);
    }

    [Fact]
    public void ParentheticalDescriptors_DoNotBreakTheMatch()
    {
        var existing = new List<Exercise> { Ex(50, "Press banca plano (unilateral)") };
        var match = PdfImportService.FindBestFuzzyExerciseMatch("Press banca plano", existing);
        Assert.NotNull(match);
        Assert.Equal(50, match!.Id);
    }

    [Fact]
    public void ExcludedIds_AreNeverReturned()
    {
        // The import loop excludes Exercise.Ids already claimed by a DIFFERENT pdf name in
        // the same day, so a second exercise can't silently merge into the first one's row.
        var existing = new List<Exercise> { Ex(9, "Press militar") };
        var match = PdfImportService.FindBestFuzzyExerciseMatch(
            "PRESS MILITAR EN BARRA MULTIPOWER", existing, new HashSet<int> { 9 });
        Assert.Null(match);
    }
}
