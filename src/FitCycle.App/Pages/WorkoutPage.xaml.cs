using FitCycle.App.Services;
using FitCycle.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FitCycle.App.Pages;

[QueryProperty(nameof(DayValue), "day")]
public partial class WorkoutPage : ContentPage
{
    private int _dayValue;
    private List<RoutineExercise> _exercises = [];
    private int _currentIndex;
    private IDispatcherTimer? _timer;
    private int _timerSeconds;
    private bool _timerRunning;
    private DateTime _startedAt;

    public string DayValue
    {
        get => _dayValue.ToString();
        set
        {
            if (int.TryParse(value?.ToString(), out var parsed))
                _dayValue = parsed;
        }
    }

    public WorkoutPage()
    {
        InitializeComponent();
        InitTimerPickers();
    }

    private void InitTimerPickers()
    {
        // Minutes: 0-59
        MinutesPicker.ItemsSource = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();
        // Seconds: 0-59 in steps of 5
        SecondsPicker.ItemsSource = Enumerable.Range(0, 12).Select(i => (i * 5).ToString("D2")).ToList();

        // Default: 1 min 30 sec
        MinutesPicker.SelectedIndex = 1;  // "01"
        SecondsPicker.SelectedIndex = 6;  // "30"
        _timerSeconds = 90;
        UpdateTimerLabel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = L10n.T("Workout");
        RestLabel.Text = L10n.T("Rest");
        MinLabel.Text = L10n.T("Min");
        SecLabel.Text = L10n.T("Sec");
        TimerStartBtn.Text = L10n.T("Start");
        TimerResetBtn.Text = L10n.T("Reset");
        PrevBtn.Text = L10n.T("Previous");
        FinishBtn.Text = L10n.T("Finish");
        NextBtn.Text = L10n.T("Next");
        Dispatcher.Dispatch(async () => await LoadAsync());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTimer();
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
            CardFrame.IsVisible = false;

            var svc = GetService();
            if (svc is null)
            {
                StatusLbl.Text = L10n.T("ServiceUnavailable");
                return;
            }

            var day = (DayOfWeek)_dayValue;
            DayTitle.Text = L10n.DayName(day);

            var dayRoutine = await svc.GetDayRoutineAsync(day);
            _exercises = dayRoutine.Exercises;

            if (_exercises.Count == 0)
            {
                StatusLbl.Text = L10n.T("NoExercisesDay");
                return;
            }

            _currentIndex = 0;
            _startedAt = DateTime.UtcNow;
            StatusLbl.Text = string.Empty;
            CardFrame.IsVisible = true;
            ShowCurrentExercise();
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private void ShowCurrentExercise()
    {
        if (_currentIndex < 0 || _currentIndex >= _exercises.Count) return;

        var exercise = _exercises[_currentIndex];

        ProgressLabel.Text = L10n.T("ExerciseProgress", _currentIndex + 1, _exercises.Count);
        ProgressBar.Progress = (double)(_currentIndex + 1) / _exercises.Count;

        ExerciseName.Text = exercise.ExerciseName;
        MuscleGroupLabel.Text = exercise.MuscleGroupName;
        SetsRepsLabel.Text = exercise.Weight > 0
            ? L10n.T("SetsRepsFormat", exercise.Sets, exercise.Reps) + $" @ {exercise.Weight:0.#} {L10n.T("WeightKg")}"
            : L10n.T("SetsRepsFormat", exercise.Sets, exercise.Reps);

        // Load image
        LoadImage(exercise.ImageUrl);

        PrevBtn.IsEnabled = _currentIndex > 0;
        var isLast = _currentIndex == _exercises.Count - 1;
        NextBtn.IsVisible = !isLast;
        FinishBtn.IsVisible = isLast;

        // Reset timer for new exercise
        StopTimer();
        ResetTimerDisplay();
    }

    private void LoadImage(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            ExerciseImage.IsVisible = false;
            ImageDebugLabel.Text = L10n.T("NoImage");
            return;
        }

        try
        {
            ExerciseImage.IsVisible = true;
            ExerciseImage.Source = new UriImageSource
            {
                Uri = new Uri(url),
                CacheValidity = TimeSpan.FromHours(1)
            };
            ImageDebugLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            ExerciseImage.IsVisible = false;
            ImageDebugLabel.Text = $"Error imagen: {ex.Message}";
        }
    }

    #region Timer

    private int GetPickerTotalSeconds()
    {
        var mins = MinutesPicker.SelectedIndex >= 0 ? MinutesPicker.SelectedIndex : 1;
        var secsIdx = SecondsPicker.SelectedIndex >= 0 ? SecondsPicker.SelectedIndex : 6;
        return mins * 60 + secsIdx * 5;
    }

    private void OnTimePickerChanged(object? sender, EventArgs e)
    {
        if (!_timerRunning)
        {
            _timerSeconds = GetPickerTotalSeconds();
            UpdateTimerLabel();
        }
    }

    private void OnTimerStartClicked(object? sender, EventArgs e)
    {
        if (_timerRunning)
        {
            StopTimer();
            TimerStartBtn.Text = L10n.T("Start");
            TimerStartBtn.BackgroundColor = Color.FromArgb("#512BD4");
            return;
        }

        _timerSeconds = GetPickerTotalSeconds();
        if (_timerSeconds <= 0) return;

        _timerRunning = true;
        TimerStartBtn.Text = L10n.T("Pause");
        TimerStartBtn.BackgroundColor = Color.FromArgb("#e67e22");
        TimerPickerRow.IsVisible = false;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _timerSeconds--;
        UpdateTimerLabel();

        if (_timerSeconds <= 0)
        {
            StopTimer();
            TimerStartBtn.Text = L10n.T("Start");
            TimerStartBtn.BackgroundColor = Color.FromArgb("#512BD4");
            TimerLabel.TextColor = Color.FromArgb("#28a745");
        }
    }

    private void OnTimerResetClicked(object? sender, EventArgs e)
    {
        StopTimer();
        ResetTimerDisplay();
    }

    private void StopTimer()
    {
        _timerRunning = false;
        _timer?.Stop();
        _timer = null;
        TimerPickerRow.IsVisible = true;
    }

    private void ResetTimerDisplay()
    {
        _timerSeconds = GetPickerTotalSeconds();
        TimerStartBtn.Text = L10n.T("Start");
        TimerStartBtn.BackgroundColor = Color.FromArgb("#512BD4");
        TimerLabel.TextColor = Color.FromArgb("#333");
        UpdateTimerLabel();
    }

    private void UpdateTimerLabel()
    {
        var mins = _timerSeconds / 60;
        var secs = _timerSeconds % 60;
        TimerLabel.Text = $"{mins:D2}:{secs:D2}";
    }

    #endregion

    private void OnPreviousClicked(object? sender, EventArgs e)
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            ShowCurrentExercise();
        }
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        if (_currentIndex < _exercises.Count - 1)
        {
            _currentIndex++;
            ShowCurrentExercise();
        }
    }

    private async void OnFinishClicked(object? sender, EventArgs e)
    {
        StopTimer();

        // Save workout session
        try
        {
            var svc = GetService();
            if (svc != null)
            {
                var session = new WorkoutSession
                {
                    Day = (DayOfWeek)_dayValue,
                    StartedAt = _startedAt,
                    CompletedAt = DateTime.UtcNow,
                    ExerciseLogs = _exercises.Select(ex => new WorkoutExerciseLog
                    {
                        ExerciseId = ex.ExerciseId,
                        ExerciseName = ex.ExerciseName,
                        Sets = ex.Sets,
                        Reps = ex.Reps,
                        Weight = ex.Weight,
                        MuscleGroupName = ex.MuscleGroupName
                    }).ToList()
                };
                await svc.SaveWorkoutAsync(session);
            }
        }
        catch { /* Don't block finish if save fails */ }

        await Shell.Current.GoToAsync($"workoutsummary?day={_dayValue}&started={_startedAt:O}&completed={DateTime.UtcNow:O}&count={_exercises.Count}");
    }

}
