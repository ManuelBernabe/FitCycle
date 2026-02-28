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
        Title = L10n.T("EditDay");
        SubtitleLbl.Text = L10n.T("SelectGroupsExercises");
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
            _svc = GetService();
            if (_svc is null)
            {
                StatusLbl.Text = L10n.T("ServiceUnavailable");
                return;
            }

            var day = (DayOfWeek)_dayValue;
            DayTitle.Text = L10n.DayName(day);

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
                            ImageUrl = e.ImageUrl,
                            IsSelected = existing != null,
                            Sets = existing?.Sets ?? 3,
                            Reps = existing?.Reps ?? 12,
                            Weight = existing?.Weight ?? 0
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
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
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
                Text = L10n.MuscleGroup(group.Name),
                FontAttributes = FontAttributes.Bold,
                FontSize = 17,
                VerticalOptions = LayoutOptions.Center
            };

            // Exercise container
            var exerciseStack = new VerticalStackLayout
            {
                Spacing = 4,
                Margin = new Thickness(20, 4, 0, 0),
                IsVisible = group.IsSelected
            };

            foreach (var exercise in group.Exercises)
            {
                BuildExerciseRow(exercise, group, exerciseStack);
            }

            // Add exercise button
            var addBtn = new Button
            {
                Text = L10n.T("AddExercise"),
                FontSize = 12,
                Padding = new Thickness(8, 2),
                HeightRequest = 32,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#512BD4")
            };
            var capturedGroup = group;
            addBtn.Clicked += async (s, e) => await OnAddExercise(capturedGroup);
            exerciseStack.Children.Add(addBtn);

            // Toggle exercise visibility
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

    private void BuildExerciseRow(ExerciseViewModel exercise, MuscleGroupViewModel group, VerticalStackLayout exerciseStack)
    {
        var exContainer = new VerticalStackLayout { Spacing = 2 };
        var capturedEx = exercise;
        var capturedGroup = group;

        // Row 1: Checkbox + Image + Name + Save + Delete
        var topRow = new HorizontalStackLayout { Spacing = 6 };
        var exCheck = new CheckBox { IsChecked = exercise.IsSelected };
        exCheck.CheckedChanged += (s, e) => capturedEx.IsSelected = e.Value;

        var exImage = new Image
        {
            Source = exercise.ImageUrl,
            HeightRequest = 32,
            WidthRequest = 32,
            Aspect = Aspect.AspectFit,
            VerticalOptions = LayoutOptions.Center
        };

        var exLabel = new Label
        {
            Text = exercise.Name,
            VerticalOptions = LayoutOptions.Center,
            FontSize = 13,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalOptions = LayoutOptions.Fill
        };

        // Save button per exercise
        var saveBtn = new Button
        {
            Text = "\u2713",
            FontSize = 14,
            Padding = new Thickness(6, 2),
            HeightRequest = 30,
            MinimumWidthRequest = 34,
            BackgroundColor = Color.FromArgb("#28a745"),
            TextColor = Colors.White,
            CornerRadius = 6
        };
        saveBtn.Clicked += async (s, e) => await DoSave();

        // Delete button per exercise
        var deleteBtn = new Button
        {
            Text = "\u2715",
            FontSize = 14,
            Padding = new Thickness(6, 2),
            HeightRequest = 30,
            MinimumWidthRequest = 34,
            BackgroundColor = Color.FromArgb("#dc3545"),
            TextColor = Colors.White,
            CornerRadius = 6
        };
        deleteBtn.Clicked += async (s, e) => await OnDeleteExercise(capturedEx, capturedGroup);

        topRow.Children.Add(exCheck);
        topRow.Children.Add(exImage);
        topRow.Children.Add(exLabel);
        topRow.Children.Add(saveBtn);
        topRow.Children.Add(deleteBtn);

        // Row 2: Sets picker + "x" + Reps picker
        var bottomRow = new HorizontalStackLayout
        {
            Spacing = 6,
            Margin = new Thickness(36, 0, 0, 0)
        };

        var setsLabel = new Label
        {
            Text = L10n.T("Sets"),
            FontSize = 12,
            TextColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center
        };

        var setsItems = Enumerable.Range(1, 25).Select(i => i.ToString()).ToList();
        var setsPicker = new Picker
        {
            ItemsSource = setsItems,
            FontSize = 13,
            WidthRequest = 70
        };
        setsPicker.SelectedIndex = Math.Clamp(exercise.Sets - 1, 0, 24);
        setsPicker.SelectedIndexChanged += (s, e) =>
        {
            if (setsPicker.SelectedIndex >= 0)
                capturedEx.Sets = setsPicker.SelectedIndex + 1;
        };

        var xLabel = new Label
        {
            Text = "x",
            VerticalOptions = LayoutOptions.Center,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        };

        var repsLabel = new Label
        {
            Text = L10n.T("Reps"),
            FontSize = 12,
            TextColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center
        };

        var repsItems = Enumerable.Range(1, 25).Select(i => i.ToString()).ToList();
        var repsPicker = new Picker
        {
            ItemsSource = repsItems,
            FontSize = 13,
            WidthRequest = 70
        };
        repsPicker.SelectedIndex = Math.Clamp(exercise.Reps - 1, 0, 24);
        repsPicker.SelectedIndexChanged += (s, e) =>
        {
            if (repsPicker.SelectedIndex >= 0)
                capturedEx.Reps = repsPicker.SelectedIndex + 1;
        };

        var weightLabel = new Label { Text = L10n.T("WeightKg"), FontSize = 12, TextColor = Colors.Gray, VerticalOptions = LayoutOptions.Center };
        var weightEntry = new Entry
        {
            Text = exercise.Weight > 0 ? exercise.Weight.ToString("0.#") : "",
            Placeholder = "0",
            Keyboard = Keyboard.Numeric,
            WidthRequest = 70,
            FontSize = 13
        };
        weightEntry.TextChanged += (s, e) =>
        {
            if (decimal.TryParse(weightEntry.Text, out var w))
                capturedEx.Weight = w;
        };

        bottomRow.Children.Add(setsLabel);
        bottomRow.Children.Add(setsPicker);
        bottomRow.Children.Add(xLabel);
        bottomRow.Children.Add(repsLabel);
        bottomRow.Children.Add(repsPicker);
        bottomRow.Children.Add(weightLabel);
        bottomRow.Children.Add(weightEntry);

        exContainer.Children.Add(topRow);
        exContainer.Children.Add(bottomRow);
        exerciseStack.Children.Add(exContainer);
    }

    private async Task OnAddExercise(MuscleGroupViewModel group)
    {
        if (_svc is null) return;

        var existingNames = group.Exercises.Select(e => e.Name.ToLowerInvariant()).ToHashSet();
        var allSuggestions = ExerciseData.GetSuggestions(group.Name);
        var suggestions = allSuggestions
            .Where(s => !existingNames.Contains(s.Name.ToLowerInvariant()))
            .ToList();

        ExercisePickerResult? result;

        if (suggestions.Count > 0)
        {
            var tcs = new TaskCompletionSource<ExercisePickerResult?>();
            var displayName = L10n.MuscleGroup(group.Name);
            var pickerPage = new ExercisePickerPage(group.Name, displayName, suggestions, tcs);
            await Navigation.PushModalAsync(pickerPage, false);
            result = await tcs.Task;
        }
        else
        {
            var name = await DisplayPromptAsync(
                L10n.T("NewExercise"),
                L10n.T("ExerciseNameFor", L10n.MuscleGroup(group.Name)),
                L10n.T("Add"),
                L10n.T("Cancel"));
            if (string.IsNullOrWhiteSpace(name)) return;
            var imageUrl = await SearchExerciseImageAsync(name.Trim());
            result = new ExercisePickerResult(name.Trim(), imageUrl);
        }

        if (result is null) return;

        try
        {
            var newEx = await _svc.CreateExerciseAsync(result.Name, group.Id);
            var vm = new ExerciseViewModel
            {
                ExerciseId = newEx.Id,
                Name = newEx.Name,
                ImageUrl = result.ImageUrl ?? newEx.ImageUrl,
                IsSelected = true,
                Sets = 3,
                Reps = 12
            };
            group.Exercises.Add(vm);
            BuildUI();
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async Task OnDeleteExercise(ExerciseViewModel exercise, MuscleGroupViewModel group)
    {
        bool confirm = await DisplayAlert(
            L10n.T("Confirm"),
            L10n.T("ConfirmDeleteExercise", exercise.Name),
            L10n.T("Yes"),
            L10n.T("No"));
        if (!confirm) return;

        group.Exercises.Remove(exercise);
        BuildUI();
    }

    private async Task<string?> SearchExerciseImageAsync(string exerciseName)
    {
        try
        {
            using var http = new HttpClient();
            var encoded = Uri.EscapeDataString(exerciseName);
            var resp = await http.GetAsync($"https://wger.de/api/v2/exercise/search/?term={encoded}&language=2&format=json");
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var suggestions = doc.RootElement.GetProperty("suggestions");
            foreach (var s in suggestions.EnumerateArray())
            {
                if (s.TryGetProperty("data", out var data) && data.TryGetProperty("image_thumbnail", out var img))
                {
                    var url = img.GetString();
                    if (!string.IsNullOrEmpty(url))
                        return url.StartsWith("http") ? url : $"https://wger.de{url}";
                }
            }
            return null;
        }
        catch { return null; }
    }

    private async Task DoSave()
    {
        if (_svc is null) return;
        try
        {
            StatusLbl.Text = L10n.T("Saving");

            var selectedMgIds = _groups.Where(g => g.IsSelected).Select(g => g.Id).ToList();
            var selectedExercises = _groups
                .Where(g => g.IsSelected)
                .SelectMany(g => g.Exercises)
                .Where(ex => ex.IsSelected)
                .Select(ex => new ExerciseInputDto(ex.ExerciseId, ex.Sets, ex.Reps, ex.Weight))
                .ToList();

            await _svc.UpdateDayRoutineAsync((DayOfWeek)_dayValue, selectedMgIds, selectedExercises);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }
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
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public int Sets { get; set; } = 3;
    public int Reps { get; set; } = 12;
    public decimal Weight { get; set; }
}
