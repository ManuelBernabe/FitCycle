using FitCycle.App.Services;
using FitCycle.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FitCycle.App.Pages;

[QueryProperty(nameof(DayValue), "day")]
public partial class EditDayPage : ContentPage
{
    private int _dayValue;
    private IRoutineService? _svc;
    private List<MuscleGroupViewModel> _groups = [];

    public int DayValue
    {
        get => _dayValue;
        set => _dayValue = value;
    }

    public EditDayPage()
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
            _svc = GetService();
            if (_svc is null)
            {
                StatusLbl.Text = "Servicio no disponible";
                return;
            }

            var day = (DayOfWeek)_dayValue;
            DayTitle.Text = DayNameInSpanish(day);

            var allGroups = await _svc.GetMuscleGroupsAsync();
            var allExercises = await _svc.GetExercisesAsync();
            var weekRoutine = await _svc.GetWeekRoutineAsync();
            var dayRoutine = weekRoutine.Days.FirstOrDefault(d => d.Day == day);

            var selectedMgIds = dayRoutine?.MuscleGroups.Select(mg => mg.Id).ToHashSet() ?? [];
            var selectedExercises = dayRoutine?.Exercises ?? [];

            _groups = allGroups.Select(mg =>
            {
                var isSelected = selectedMgIds.Contains(mg.Id);
                var exercises = allExercises
                    .Where(e => e.MuscleGroupId == mg.Id)
                    .Select(e =>
                    {
                        var existing = selectedExercises.FirstOrDefault(se => se.ExerciseId == e.Id);
                        return new ExerciseViewModel
                        {
                            ExerciseId = e.Id,
                            Name = e.Name,
                            IsSelected = existing != null,
                            Sets = existing?.Sets ?? 3,
                            Reps = existing?.Reps ?? 12
                        };
                    }).ToList();

                return new MuscleGroupViewModel
                {
                    Id = mg.Id,
                    Name = mg.Name,
                    IsSelected = isSelected,
                    Exercises = exercises
                };
            }).ToList();

            BuildUI();
            StatusLbl.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusLbl.Text = $"Error: {ex.Message}";
        }
    }

    private void BuildUI()
    {
        GroupsContainer.Children.Clear();

        foreach (var group in _groups)
        {
            var groupFrame = new Frame
            {
                Padding = 10,
                Margin = new Thickness(0, 2)
            };

            var groupStack = new VerticalStackLayout { Spacing = 6 };

            // Group header with checkbox
            var headerStack = new HorizontalStackLayout { Spacing = 8 };
            var groupCheck = new CheckBox { IsChecked = group.IsSelected };
            var groupLabel = new Label
            {
                Text = group.Name,
                FontAttributes = FontAttributes.Bold,
                FontSize = 17,
                VerticalOptions = LayoutOptions.Center
            };

            // Exercise container (shown/hidden based on group selection)
            var exerciseStack = new VerticalStackLayout
            {
                Spacing = 4,
                Margin = new Thickness(20, 4, 0, 0),
                IsVisible = group.IsSelected
            };

            // Build exercise rows
            foreach (var exercise in group.Exercises)
            {
                var exRow = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(new GridLength(1, GridUnitType.Auto)),
                        new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                        new ColumnDefinition(new GridLength(50)),
                        new ColumnDefinition(new GridLength(1, GridUnitType.Auto)),
                        new ColumnDefinition(new GridLength(50))
                    },
                    ColumnSpacing = 4
                };

                var exCheck = new CheckBox { IsChecked = exercise.IsSelected };
                var capturedEx = exercise;
                exCheck.CheckedChanged += (s, e) => capturedEx.IsSelected = e.Value;

                var exLabel = new Label
                {
                    Text = exercise.Name,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14
                };

                var setsEntry = new Entry
                {
                    Text = exercise.Sets.ToString(),
                    Keyboard = Keyboard.Numeric,
                    WidthRequest = 45,
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                setsEntry.TextChanged += (s, e) =>
                {
                    if (int.TryParse(e.NewTextValue, out var v)) capturedEx.Sets = v;
                };

                var xLabel = new Label
                {
                    Text = "x",
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontSize = 14
                };

                var repsEntry = new Entry
                {
                    Text = exercise.Reps.ToString(),
                    Keyboard = Keyboard.Numeric,
                    WidthRequest = 45,
                    FontSize = 13,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                repsEntry.TextChanged += (s, e) =>
                {
                    if (int.TryParse(e.NewTextValue, out var v)) capturedEx.Reps = v;
                };

                Grid.SetColumn(exCheck, 0);
                Grid.SetColumn(exLabel, 1);
                Grid.SetColumn(setsEntry, 2);
                Grid.SetColumn(xLabel, 3);
                Grid.SetColumn(repsEntry, 4);

                exRow.Children.Add(exCheck);
                exRow.Children.Add(exLabel);
                exRow.Children.Add(setsEntry);
                exRow.Children.Add(xLabel);
                exRow.Children.Add(repsEntry);

                exerciseStack.Children.Add(exRow);
            }

            // Add custom exercise button
            var addBtn = new Button
            {
                Text = "+ Agregar ejercicio",
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 32,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#512BD4")
            };
            var capturedGroup = group;
            addBtn.Clicked += async (s, e) => await OnAddExercise(capturedGroup, exerciseStack);
            exerciseStack.Children.Add(addBtn);

            // Toggle exercise visibility when group is checked/unchecked
            var capturedGroupForCheck = group;
            groupCheck.CheckedChanged += (s, e) =>
            {
                capturedGroupForCheck.IsSelected = e.Value;
                exerciseStack.IsVisible = e.Value;
            };

            headerStack.Children.Add(groupCheck);
            headerStack.Children.Add(groupLabel);
            groupStack.Children.Add(headerStack);
            groupStack.Children.Add(exerciseStack);
            groupFrame.Content = groupStack;
            GroupsContainer.Children.Add(groupFrame);
        }
    }

    private async Task OnAddExercise(MuscleGroupViewModel group, VerticalStackLayout exerciseStack)
    {
        var name = await DisplayPromptAsync("Nuevo ejercicio", $"Nombre del ejercicio para {group.Name}:", "Agregar", "Cancelar");
        if (string.IsNullOrWhiteSpace(name) || _svc is null) return;

        try
        {
            var newEx = await _svc.CreateExerciseAsync(name.Trim(), group.Id);
            var vm = new ExerciseViewModel
            {
                ExerciseId = newEx.Id,
                Name = newEx.Name,
                IsSelected = true,
                Sets = 3,
                Reps = 12
            };
            group.Exercises.Add(vm);

            // Rebuild UI to show new exercise
            BuildUI();
        }
        catch (Exception ex)
        {
            StatusLbl.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_svc is null) return;
        try
        {
            StatusLbl.Text = "Guardando...";

            var selectedMgIds = _groups.Where(g => g.IsSelected).Select(g => g.Id).ToList();

            var selectedExercises = _groups
                .Where(g => g.IsSelected)
                .SelectMany(g => g.Exercises)
                .Where(ex => ex.IsSelected)
                .Select(ex => new ExerciseInputDto(ex.ExerciseId, ex.Sets, ex.Reps))
                .ToList();

            await _svc.UpdateDayRoutineAsync((DayOfWeek)_dayValue, selectedMgIds, selectedExercises);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            StatusLbl.Text = $"Error: {ex.Message}";
        }
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

public class MuscleGroupViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public List<ExerciseViewModel> Exercises { get; set; } = [];
}

public class ExerciseViewModel
{
    public int ExerciseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public int Sets { get; set; } = 3;
    public int Reps { get; set; } = 12;
}
