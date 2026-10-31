using FitCycle.Core;

namespace FitCycle.Core.Tests;

/// <summary>
/// Week parity for the biweekly exercise variants ("2 semanas en barra y 2 semanas en
/// máquina"). The parity must be stable across days of the same week, flip exactly at
/// Monday boundaries, and survive year transitions (fixed Monday epoch 2024-01-01).
/// </summary>
public class BiweeklyRotationTests
{
    [Fact]
    public void EpochWeek_IsWeekA()
    {
        Assert.False(BiweeklyRotation.IsAlternateWeek(new DateTime(2024, 1, 1)));
        Assert.False(BiweeklyRotation.IsAlternateWeek(new DateTime(2024, 1, 7))); // Sunday same week
    }

    [Fact]
    public void NextWeek_IsWeekB_AndAlternatesWeekly()
    {
        Assert.True(BiweeklyRotation.IsAlternateWeek(new DateTime(2024, 1, 8)));
        Assert.False(BiweeklyRotation.IsAlternateWeek(new DateTime(2024, 1, 15)));
        Assert.True(BiweeklyRotation.IsAlternateWeek(new DateTime(2024, 1, 22)));
    }

    [Fact]
    public void Parity_IsConstant_WithinAWeek()
    {
        // Monday..Sunday of one week all share the same parity.
        var monday = new DateTime(2026, 10, 26); // a Monday
        var parity = BiweeklyRotation.IsAlternateWeek(monday);
        for (var d = 0; d < 7; d++)
            Assert.Equal(parity, BiweeklyRotation.IsAlternateWeek(monday.AddDays(d)));
        // And the next Monday flips it.
        Assert.Equal(!parity, BiweeklyRotation.IsAlternateWeek(monday.AddDays(7)));
    }

    [Fact]
    public void Parity_SurvivesYearBoundary()
    {
        // 2025-12-29 (Mon) .. 2026-01-04 (Sun) is ONE week: same parity on both sides
        // of the new year, flipping only on 2026-01-05.
        var dec29 = new DateTime(2025, 12, 29);
        var parity = BiweeklyRotation.IsAlternateWeek(dec29);
        Assert.Equal(parity, BiweeklyRotation.IsAlternateWeek(new DateTime(2026, 1, 4)));
        Assert.Equal(!parity, BiweeklyRotation.IsAlternateWeek(new DateTime(2026, 1, 5)));
    }

    [Fact]
    public void TimeOfDay_DoesNotChangeParity()
    {
        var day = new DateTime(2026, 10, 31);
        Assert.Equal(
            BiweeklyRotation.IsAlternateWeek(day),
            BiweeklyRotation.IsAlternateWeek(day.AddHours(23).AddMinutes(59)));
    }
}
