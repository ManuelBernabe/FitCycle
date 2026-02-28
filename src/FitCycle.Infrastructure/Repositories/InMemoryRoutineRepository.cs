using FitCycle.Core.Models;

namespace FitCycle.Infrastructure.Repositories;

public class InMemoryRoutineRepository : IRoutineRepository
{
    private readonly List<MuscleGroup> _muscleGroups =
    [
        new() { Id = 1, Name = "Pecho" },
        new() { Id = 2, Name = "Espalda" },
        new() { Id = 3, Name = "Hombros" },
        new() { Id = 4, Name = "Bíceps" },
        new() { Id = 5, Name = "Tríceps" },
        new() { Id = 6, Name = "Piernas" },
        new() { Id = 7, Name = "Abdominales" },
        new() { Id = 8, Name = "Glúteos" }
    ];

    private static string PlaceholderUrl(string name) =>
        $"https://placehold.co/400x300/EEE/31343C.png?font=montserrat&text={Uri.EscapeDataString(name)}";

    private readonly List<Exercise> _exercises =
    [
        // Pecho
        new() { Id = 1, Name = "Press banca", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
        new() { Id = 2, Name = "Press inclinado", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png" },
        new() { Id = 3, Name = "Aperturas", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
        new() { Id = 4, Name = "Fondos", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
        // Espalda
        new() { Id = 5, Name = "Dominadas", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
        new() { Id = 6, Name = "Remo con barra", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
        new() { Id = 7, Name = "Jalón al pecho", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
        new() { Id = 8, Name = "Remo con mancuerna", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png" },
        // Hombros
        new() { Id = 9, Name = "Press militar", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
        new() { Id = 10, Name = "Elevaciones laterales", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
        new() { Id = 11, Name = "Elevaciones frontales", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png" },
        new() { Id = 12, Name = "Pájaros", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
        // Bíceps
        new() { Id = 13, Name = "Curl con barra", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
        new() { Id = 14, Name = "Curl con mancuernas", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
        new() { Id = 15, Name = "Curl martillo", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png" },
        new() { Id = 16, Name = "Curl concentrado", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg" },
        // Tríceps
        new() { Id = 17, Name = "Fondos en paralelas", MuscleGroupId = 5, ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
        new() { Id = 18, Name = "Extensión con polea", MuscleGroupId = 5, ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
        new() { Id = 19, Name = "Press francés", MuscleGroupId = 5, ImageUrl = "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png" },
        new() { Id = 20, Name = "Patada de tríceps", MuscleGroupId = 5, ImageUrl = PlaceholderUrl("Patada triceps") },
        // Piernas
        new() { Id = 21, Name = "Sentadilla", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
        new() { Id = 22, Name = "Prensa", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
        new() { Id = 23, Name = "Extensión de cuádriceps", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp" },
        new() { Id = 24, Name = "Curl femoral", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
        new() { Id = 25, Name = "Zancadas", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
        // Abdominales
        new() { Id = 26, Name = "Crunch", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
        new() { Id = 27, Name = "Plancha", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
        new() { Id = 28, Name = "Elevación de piernas", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
        new() { Id = 29, Name = "Russian twist", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
        // Glúteos
        new() { Id = 30, Name = "Hip thrust", MuscleGroupId = 8, ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
        new() { Id = 31, Name = "Peso muerto rumano", MuscleGroupId = 8, ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
        new() { Id = 32, Name = "Patada de glúteo", MuscleGroupId = 8, ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
        new() { Id = 33, Name = "Puente de glúteos", MuscleGroupId = 8, ImageUrl = PlaceholderUrl("Puente gluteos") }
    ];

    private int _nextExerciseId = 34;

    private readonly Dictionary<DayOfWeek, List<int>> _muscleGroupRoutines = new()
    {
        [DayOfWeek.Monday] = [],
        [DayOfWeek.Tuesday] = [],
        [DayOfWeek.Wednesday] = [],
        [DayOfWeek.Thursday] = [],
        [DayOfWeek.Friday] = []
    };

    private readonly Dictionary<DayOfWeek, List<RoutineExercise>> _exerciseRoutines = new()
    {
        [DayOfWeek.Monday] = [],
        [DayOfWeek.Tuesday] = [],
        [DayOfWeek.Wednesday] = [],
        [DayOfWeek.Thursday] = [],
        [DayOfWeek.Friday] = []
    };

    public IReadOnlyList<MuscleGroup> GetAllMuscleGroups() => _muscleGroups;

    public IReadOnlyList<Exercise> GetExercises(int? muscleGroupId = null)
    {
        if (muscleGroupId.HasValue)
            return _exercises.Where(e => e.MuscleGroupId == muscleGroupId.Value).ToList();
        return _exercises;
    }

    public Exercise AddExercise(string name, int muscleGroupId)
    {
        if (!_muscleGroups.Any(mg => mg.Id == muscleGroupId))
            throw new ArgumentException("Grupo muscular no válido.");

        var exercise = new Exercise
        {
            Id = _nextExerciseId++,
            Name = name,
            MuscleGroupId = muscleGroupId,
            ImageUrl = PlaceholderUrl(name)
        };
        _exercises.Add(exercise);
        return exercise;
    }

    public WeekRoutine GetWeekRoutine()
    {
        var days = _muscleGroupRoutines
            .Select(kvp => BuildDayRoutine(kvp.Key))
            .OrderBy(d => d.Day)
            .ToList();

        return new WeekRoutine { Days = days };
    }

    public DayRoutine GetDayRoutine(DayOfWeek day) => BuildDayRoutine(day);

    public DayRoutine SetDayRoutine(DayOfWeek day, List<int> muscleGroupIds, List<RoutineExerciseInput> exercises)
    {
        if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new ArgumentException("Solo se permiten días de lunes a viernes.");

        var validMgIds = muscleGroupIds
            .Where(id => _muscleGroups.Any(mg => mg.Id == id))
            .Distinct()
            .ToList();

        _muscleGroupRoutines[day] = validMgIds;

        var routineExercises = exercises
            .Where(e => _exercises.Any(ex => ex.Id == e.ExerciseId))
            .Select(e =>
            {
                var ex = _exercises.First(x => x.Id == e.ExerciseId);
                var mg = _muscleGroups.FirstOrDefault(m => m.Id == ex.MuscleGroupId);
                return new RoutineExercise
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = ex.Name,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    ImageUrl = ex.ImageUrl,
                    MuscleGroupName = mg?.Name ?? string.Empty
                };
            })
            .ToList();

        _exerciseRoutines[day] = routineExercises;

        return BuildDayRoutine(day);
    }

    private DayRoutine BuildDayRoutine(DayOfWeek day)
    {
        var mgIds = _muscleGroupRoutines.GetValueOrDefault(day, []);
        var exercises = _exerciseRoutines.GetValueOrDefault(day, []);

        return new DayRoutine
        {
            Day = day,
            MuscleGroups = _muscleGroups.Where(mg => mgIds.Contains(mg.Id)).ToList(),
            Exercises = exercises.ToList()
        };
    }
}
