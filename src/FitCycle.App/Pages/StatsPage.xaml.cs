using FitCycle.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitCycle.App.Pages;

public partial class StatsPage : ContentPage
{
    public StatsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = L10n.T("TabStats");
        ProgressTitle.Text = L10n.T("YourProgress");
        WorkoutsTitle.Text = L10n.T("Workouts");
        TotalSetsTitle.Text = L10n.T("TotalSets");
        TotalRepsTitle.Text = L10n.T("TotalReps");
        WeeklyChartTitle.Text = L10n.T("WeeklyWorkoutsChart");
        TopExTitle.Text = L10n.T("MostFrequent");
        HistoryTitle.Text = L10n.T("RecentHistory");
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

            var stats = await svc.GetWorkoutStatsAsync();
            var history = await svc.GetWorkoutHistoryAsync();

            // Summary
            TotalWorkoutsLabel.Text = stats.TotalWorkouts.ToString();
            TotalSetsLabel.Text = stats.TotalSets.ToString();
            TotalRepsLabel.Text = stats.TotalReps.ToString();

            // Weekly chart
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
                    HeightRequest = 24,
                    CornerRadius = 6,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = barWidth * 200
                };
                row.Add(bar, 1);
                row.Add(new Label { Text = week.Count.ToString(), FontSize = 13, FontAttributes = FontAttributes.Bold, VerticalTextAlignment = TextAlignment.Center }, 2);

                WeeklyChart.Children.Add(row);
            }

            // Top exercises
            TopExercisesList.Children.Clear();
            var topMax = stats.TopExercises.Count > 0 ? stats.TopExercises.Max(e => e.Count) : 1;
            if (topMax == 0) topMax = 1;

            foreach (var ex in stats.TopExercises)
            {
                var barWidth = (double)ex.Count / topMax;

                var row = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(30)) },
                    RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) }
                };

                row.Add(new Label { Text = ex.Name, FontSize = 13, FontAttributes = FontAttributes.Bold }, 0, 0);
                row.Add(new Label { Text = ex.Count.ToString(), FontSize = 13, TextColor = Colors.Gray, HorizontalTextAlignment = TextAlignment.End }, 1, 0);

                var bar = new BoxView
                {
                    Color = Color.FromArgb("#28a745"),
                    HeightRequest = 8,
                    CornerRadius = 4,
                    HorizontalOptions = LayoutOptions.Start,
                    WidthRequest = barWidth * 250
                };
                Grid.SetColumnSpan(bar, 2);
                row.Add(bar, 0, 1);

                TopExercisesList.Children.Add(row);
            }

            // History
            HistoryList.Children.Clear();
            foreach (var session in history.Take(10))
            {
                var duration = session.CompletedAt - session.StartedAt;
                var durationText = duration.TotalMinutes < 1 ? $"{duration.Seconds}s" : $"{(int)duration.TotalMinutes} min";
                var dateText = session.CompletedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");

                var frame = new Frame
                {
                    Padding = new Thickness(12, 8),
                    CornerRadius = 8,
                    BackgroundColor = Color.FromArgb("#f8f9fa")
                };

                var stack = new VerticalStackLayout { Spacing = 2 };
                stack.Add(new Label
                {
                    Text = $"{L10n.DayName(session.Day)} — {dateText}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold
                });
                stack.Add(new Label
                {
                    Text = L10n.T("ExercisesCount", session.ExerciseLogs.Count, durationText),
                    FontSize = 12,
                    TextColor = Colors.Gray
                });

                frame.Content = stack;
                HistoryList.Children.Add(frame);
            }

            StatusLbl.Text = stats.TotalWorkouts == 0 ? L10n.T("NoWorkoutsYet") : string.Empty;
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

}
