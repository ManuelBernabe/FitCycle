namespace FitCycle.Infrastructure.Services;

/// <summary>
/// AI-powered fallback that maps user-created exercise names (typically from PDF
/// imports with placehold.co images) to canonical exercises that have real images.
/// </summary>
public interface IExerciseImageAutoFiller
{
    /// <summary>
    /// Scans exercises whose ImageUrl is empty or a placehold.co placeholder,
    /// asks the AI to map each one to a canonical Spanish exercise name, and
    /// updates the ImageUrl when a high-confidence canonical match is found.
    /// </summary>
    /// <returns>The number of exercises that got a real image.</returns>
    Task<int> AutoPopulateImagesAsync();
}
