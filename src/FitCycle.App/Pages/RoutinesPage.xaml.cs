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
        Title = L10n.T("TabRoutines");
        TitleLabel.Text = L10n.T("MyWeeklyRoutine");
        SubtitleLabel.Text = L10n.T("ConfigureWeekly");
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
            StatusLbl.Text = L10n.T("Loading");
            var svc = GetService();
            if (svc is null)
            {
                StatusLbl.Text = L10n.T("ServiceUnavailable");
                return;
            }
            var week = await svc.GetWeekRoutineAsync();
            var items = week.Days.Select(d =>
            {
                var mgNames = d.MuscleGroups.Count == 0
                    ? L10n.T("NoGroupsAssigned")
                    : string.Join(", ", d.MuscleGroups.Select(mg => L10n.MuscleGroup(mg.Name)));

                var exerciseLines = d.Exercises.Count == 0
                    ? string.Empty
                    : string.Join("\n", d.Exercises.Select(e => $"  \u2022 {e.ExerciseName} ({e.Sets}x{e.Reps})"));

                return new DayViewModel
                {
                    DayValue = (int)d.Day,
                    DayName = L10n.DayName(d.Day),
                    MuscleGroupNames = mgNames,
                    ExerciseDetails = exerciseLines,
                    HasExercises = d.Exercises.Count > 0,
                    HasRoutine = d.MuscleGroups.Count > 0,
                    CreateText = L10n.T("Create"),
                    EditText = L10n.T("Edit"),
                    DeleteText = L10n.T("Delete"),
                    StartText = L10n.T("StartWorkout")
                };
            }).ToList();

            DaysList.ItemsSource = items;
            StatusLbl.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
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

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        var day = GetDayFromButton(sender);
        if (!day.HasValue) return;

        bool confirm = await DisplayAlert(L10n.T("Confirm"), L10n.T("DeleteRoutineMsg"), L10n.T("Yes"), L10n.T("No"));
        if (!confirm) return;

        var svc = GetService();
        if (svc is null) return;

        try
        {
            await svc.UpdateDayRoutineAsync((DayOfWeek)day.Value, new List<int>(), new List<Services.ExerciseInputDto>());
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }
}

public class DayViewModel
{
    public int DayValue { get; set; }
    public string DayName { get; set; } = string.Empty;
    public string MuscleGroupNames { get; set; } = string.Empty;
    public string ExerciseDetails { get; set; } = string.Empty;
    public bool HasExercises { get; set; }
    public bool HasRoutine { get; set; }
    public bool HasNoRoutine => !HasRoutine;
    public string CreateText { get; set; } = string.Empty;
    public string EditText { get; set; } = string.Empty;
    public string DeleteText { get; set; } = string.Empty;
    public string StartText { get; set; } = string.Empty;
}
