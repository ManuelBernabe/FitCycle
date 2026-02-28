using FitCycle.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitCycle.App.Pages;

public partial class RoutinesPage : ContentPage
{
    public RoutinesPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.Dispatch(async () => await LoadAsync());
    }

    private IRoutineService? GetService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<IRoutineService>()
            ?? this.Handler?.MauiContext?.Services.GetService<IRoutineService>();
    }

    private async Task LoadAsync()
    {
        try
        {
            StatusLbl.Text = "Cargando...";
            var svc = GetService();
            if (svc is null)
            {
                StatusLbl.Text = "Servicio no disponible";
                return;
            }
            var week = await svc.GetWeekRoutineAsync();
            var items = week.Days.Select(d =>
            {
                var mgNames = d.MuscleGroups.Count == 0
                    ? "Sin grupos asignados"
                    : string.Join(", ", d.MuscleGroups.Select(mg => mg.Name));

                var exerciseLines = d.Exercises.Count == 0
                    ? string.Empty
                    : string.Join("\n", d.Exercises.Select(e => $"  • {e.ExerciseName} ({e.Sets}x{e.Reps})"));

                return new DayViewModel
                {
                    DayValue = (int)d.Day,
                    DayName = DayNameInSpanish(d.Day),
                    MuscleGroupNames = mgNames,
                    ExerciseDetails = exerciseLines,
                    HasExercises = d.Exercises.Count > 0
                };
            }).ToList();

            DaysList.ItemsSource = items;
            StatusLbl.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusLbl.Text = $"Error: {ex.Message}";
        }
    }

    private static int? GetDayFromButton(object? sender)
    {
        if (sender is not Button btn) return null;
        var param = btn.CommandParameter;
        if (param is int i) return i;
        if (param is string s && int.TryParse(s, out var parsed)) return parsed;
        return null;
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        var day = GetDayFromButton(sender);
        if (day.HasValue)
            await Shell.Current.GoToAsync($"editday?day={day.Value}");
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        var day = GetDayFromButton(sender);
        if (day.HasValue)
            await Shell.Current.GoToAsync($"workout?day={day.Value}");
    }

    private static string DayNameInSpanish(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Lunes",
        DayOfWeek.Tuesday => "Martes",
        DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves",
        DayOfWeek.Friday => "Viernes",
        _ => day.ToString()
    };
}

public class DayViewModel
{
    public int DayValue { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string MuscleGroupNames { get; set; } = string.Empty;
    public string ExerciseDetails { get; set; } = string.Empty;
    public bool HasExercises { get; set; }
}
