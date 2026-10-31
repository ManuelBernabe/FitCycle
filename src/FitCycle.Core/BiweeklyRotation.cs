namespace FitCycle.Core;

/// <summary>
/// Week parity for biweekly exercise variants ("2 semanas en barra y 2 semanas en
/// máquina"). Weeks are counted from a fixed Monday epoch so the parity is stable
/// across year boundaries and timezones (date-only arithmetic).
/// </summary>
public static class BiweeklyRotation
{
    private static readonly DateTime Epoch = new(2024, 1, 1); // a Monday

    /// <summary>True on "B" weeks, when the alternate variant should be served.</summary>
    public static bool IsAlternateWeek(DateTime date)
    {
        var weeks = (int)Math.Floor((date.Date - Epoch).TotalDays / 7.0);
        return (weeks % 2 + 2) % 2 == 1;
    }
}
