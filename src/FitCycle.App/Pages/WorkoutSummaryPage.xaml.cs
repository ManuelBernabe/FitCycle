using FitCycle.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitCycle.App.Pages;

[QueryProperty(nameof(DayParam), "day")]
[QueryProperty(nameof(StartedParam), "started")]
[QueryProperty(nameof(CompletedParam), "completed")]
[QueryProperty(nameof(CountParam), "count")]
public partial class WorkoutSummaryPage : ContentPage
{
    public string DayParam { get; set; } = "1";
    public string StartedParam { get; set; } = "";
    public string CompletedParam { get; set; } = "";
    public string CountParam { get; set; } = "0";

    public WorkoutSummaryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = L10n.T("Summary");
        CompletedLabel.Text = L10n.T("WorkoutCompleted");
        DurationTitle.Text = L10n.T("Duration");
        ExercisesTitle.Text = L10n.T("Exercises");
        TotalSetsTitle.Text = L10n.T("TotalSets");
        WeeklyTitle.Text = L10n.T("WeeklyWorkouts");
        ExercisesDoneTitle.Text = L10n.T("ExercisesDone");
        BackBtn.Text = L10n.T("BackToRoutines");
        Dispatcher.Dispatch(async () => await LoadAsync());
    }

    private async Task LoadAsync()
    {
        // Parse params
        int.TryParse(DayParam, out var dayVal);
        var day = (DayOfWeek)dayVal;
        int.TryParse(CountParam, out var exerciseCount);

        DateTime.TryParse(StartedParam, null, System.Globalization.DateTimeStyles.RoundtripKind, out var started);
        DateTime.TryParse(CompletedParam, null, System.Globalization.DateTimeStyles.RoundtripKind, out var completed);

        var duration = completed - started;

        DayLabel.Text = L10n.DayName(day);
        DurationLabel.Text = duration.TotalMinutes < 1 ? $"{duration.Seconds}s" : $"{(int)duration.TotalMinutes}m";
        ExerciseCountLabel.Text = exerciseCount.ToString();

        // Load stats from API
        try
        {
            var svc = Application.Current?.Handler?.MauiContext?.Services.GetService<IRoutineService>();
            if (svc == null) return;

            var stats = await svc.GetWorkoutStatsAsync();
            TotalSetsLabel.Text = stats.TotalSets.ToString();

            // Build weekly chart
            WeeklyChart.Children.Clear();
            var maxCount = stats.WeeklyData.Count > 0 ? stats.WeeklyData.Max(w => w.Count) : 1;
            if (maxCount == 0) maxCount = 1;

            foreach (var week in stats.WeeklyData)
            {
                var barWidth = (double)week.Count / maxCount;
                if (barWidth < 0.05 && week.Count > 0) barWidth = 0.05;

                var row = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(new GridLength(90)), new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(30)) }
                };
                row.Add(new Label { Text = week.Week, FontSize = 12, TextColor = Colors.Gray, VerticalTextAlignment = TextAlignment.Center }, 0);

                var bar = new BoxView
                {
                    Color = Color.FromArgb("#512BD4"),
                    HeightRequest = 20,
                    CornerRadius = 4,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = barWidth * 200
                };
                row.Add(bar, 1);
                row.Add(new Label { Text = week.Count.ToString(), FontSize = 12, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center }, 2);

                WeeklyChart.Children.Add(row);
            }

            // Load recent workout exercises
            var history = await svc.GetWorkoutHistoryAsync();
            ExerciseList.Children.Clear();
            var latest = history.FirstOrDefault();
            if (latest != null)
            {
                foreach (var log in latest.ExerciseLogs)
                {
                    ExerciseList.Children.Add(new Label
                    {
                        Text = $"  {log.ExerciseName} — {log.Sets}x{log.Reps} ({log.MuscleGroupName})",
                        FontSize = 14
                    });
                }
            }
        }
        catch { /* Stats loading is optional */ }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("../..");
    }

}
