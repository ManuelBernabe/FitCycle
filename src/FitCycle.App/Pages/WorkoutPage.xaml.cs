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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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
            StatusLbl.Text = "Cargando...";
            CardFrame.IsVisible = false;

            var svc = GetService();
            if (svc is null)
            {
                StatusLbl.Text = "Servicio no disponible";
                return;
            }

            var day = (DayOfWeek)_dayValue;
            DayTitle.Text = DayNameInSpanish(day);

            var dayRoutine = await svc.GetDayRoutineAsync(day);
            _exercises = dayRoutine.Exercises;

            if (_exercises.Count == 0)
            {
                StatusLbl.Text = "No hay ejercicios configurados para este día.";
                return;
            }

            _currentIndex = 0;
            StatusLbl.Text = string.Empty;
            CardFrame.IsVisible = true;
            ShowCurrentExercise();
        }
        catch (Exception ex)
        {
            StatusLbl.Text = $"Error: {ex.Message}";
        }
    }

    private void ShowCurrentExercise()
    {
        if (_currentIndex < 0 || _currentIndex >= _exercises.Count) return;

        var exercise = _exercises[_currentIndex];

        ProgressLabel.Text = $"Ejercicio {_currentIndex + 1} de {_exercises.Count}";
        ProgressBar.Progress = (double)(_currentIndex + 1) / _exercises.Count;

        ExerciseName.Text = exercise.ExerciseName;
        MuscleGroupLabel.Text = exercise.MuscleGroupName;
        SetsRepsLabel.Text = $"{exercise.Sets} series x {exercise.Reps} repeticiones";

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
            ImageDebugLabel.Text = "Sin imagen";
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

    private void OnTimerSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        var seconds = (int)e.NewValue;
        TimerSliderLabel.Text = $"{seconds} seg";
        if (!_timerRunning)
        {
            _timerSeconds = seconds;
            UpdateTimerLabel();
        }
    }

    private void OnTimerStartClicked(object? sender, EventArgs e)
    {
        if (_timerRunning)
        {
            StopTimer();
            TimerStartBtn.Text = "Iniciar";
            return;
        }

        _timerSeconds = (int)TimerSlider.Value;
        if (_timerSeconds <= 0) return;

        _timerRunning = true;
        TimerStartBtn.Text = "Pausar";
        TimerSlider.IsEnabled = false;

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
            TimerStartBtn.Text = "Iniciar";
            TimerLabel.TextColor = Colors.Green;
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
        TimerSlider.IsEnabled = true;
    }

    private void ResetTimerDisplay()
    {
        _timerSeconds = (int)TimerSlider.Value;
        TimerStartBtn.Text = "Iniciar";
        TimerLabel.TextColor = Colors.Black;
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
        await DisplayAlert("¡Buen trabajo!", "Has completado todos los ejercicios del día.", "Volver a Rutinas");
        await Shell.Current.GoToAsync("..");
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
