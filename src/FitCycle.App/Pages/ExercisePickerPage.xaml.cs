using FitCycle.App.Services;

namespace FitCycle.App.Pages;

public partial class ExercisePickerPage : ContentPage
{
    private readonly TaskCompletionSource<ExercisePickerResult?> _tcs;
    private readonly string _muscleGroupSpanishName;

    public ExercisePickerPage(
        string muscleGroupSpanishName,
        string muscleGroupDisplayName,
        List<ExerciseSuggestion> suggestions,
        TaskCompletionSource<ExercisePickerResult?> tcs)
    {
        InitializeComponent();
        _tcs = tcs;
        _muscleGroupSpanishName = muscleGroupSpanishName;

        TitleLabel.Text = L10n.T("AddExerciseTo", muscleGroupDisplayName);
        SubtitleLabel.Text = L10n.T("SelectExercise");
        CustomBtn.Text = L10n.T("CustomName");
        CancelBtn.Text = L10n.T("Cancel");

        ExerciseList.ItemsSource = suggestions;
    }

    private async void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ExerciseSuggestion selected)
        {
            _tcs.TrySetResult(new ExercisePickerResult(selected.Name, selected.ImageUrl));
            await Navigation.PopModalAsync(false);
        }
    }

    private async void OnCustomClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync(
            L10n.T("NewExercise"),
            L10n.T("ExerciseNameFor", L10n.MuscleGroup(_muscleGroupSpanishName)),
            L10n.T("Add"),
            L10n.T("Cancel"));

        if (!string.IsNullOrWhiteSpace(name))
        {
            var imageUrl = await SearchExerciseImageAsync(name.Trim());
            _tcs.TrySetResult(new ExercisePickerResult(name.Trim(), imageUrl));
            await Navigation.PopModalAsync(false);
        }
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

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(false);
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }
}

public record ExercisePickerResult(string Name, string? ImageUrl);
