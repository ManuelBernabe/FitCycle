using FitCycle.Core.Models;
using FitCycle.Infrastructure.Data;
using FitCycle.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitCycle.Infrastructure.Repositories;

public class SqliteRoutineRepository : IRoutineRepository
{
    private readonly FitCycleDbContext _db;

    public SqliteRoutineRepository(FitCycleDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<MuscleGroup> GetAllMuscleGroups()
    {
        return _db.MuscleGroups.OrderBy(mg => mg.Id).ToList();
    }

    public IReadOnlyList<Exercise> GetExercises(int? muscleGroupId = null)
    {
        IQueryable<Exercise> query = _db.Exercises;
        if (muscleGroupId.HasValue)
            query = query.Where(e => e.MuscleGroupId == muscleGroupId.Value);
        return query.OrderBy(e => e.Id).ToList();
    }

    // Known exercise images from wger.de
    private static readonly Dictionary<string, string> KnownExerciseImages = new(StringComparer.OrdinalIgnoreCase)
    {
        // Pecho
        ["Press banca"] = "https://wger.de/media/exercise-images/192/Bench-press-1.png",
        ["Press inclinado"] = "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png",
        ["Press declinado"] = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png",
        ["Aperturas con mancuernas"] = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png",
        ["Aperturas en polea"] = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png",
        ["Fondos"] = "https://wger.de/media/exercise-images/83/Bench-dips-1.png",
        ["Pullover"] = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png",
        ["Press con mancuernas"] = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png",
        ["Cruces en polea alta"] = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png",
        ["Cruces en polea baja"] = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png",
        ["Flexiones"] = "https://wger.de/media/exercise-images/120/Push-ups-1.png",
        ["Flexiones diamante"] = "https://wger.de/media/exercise-images/120/Push-ups-1.png",
        ["Press en máquina"] = "https://wger.de/media/exercise-images/192/Bench-press-1.png",
        ["Peck deck"] = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png",
        // Espalda
        ["Dominadas"] = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg",
        ["Remo con barra"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        ["Jalón al pecho"] = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp",
        ["Remo con mancuerna"] = "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png",
        ["Remo en polea baja"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        ["Jalón tras nuca"] = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp",
        ["Peso muerto"] = "https://wger.de/media/exercise-images/105/Deadlift-1.png",
        ["Pullover en polea"] = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png",
        ["Remo en máquina"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        ["Dominadas supinas"] = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg",
        ["Remo T"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        ["Face pull"] = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png",
        ["Hiperextensiones"] = "https://wger.de/media/exercise-images/125/Leg-raises-2.png",
        ["Encogimientos con barra"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        // Hombros
        ["Press militar"] = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png",
        ["Elevaciones laterales"] = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png",
        ["Elevaciones frontales"] = "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png",
        ["Pájaros"] = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png",
        ["Press Arnold"] = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png",
        ["Remo al mentón"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        ["Elevaciones laterales en polea"] = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png",
        ["Shrugs"] = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png",
        ["Rotación externa"] = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png",
        ["Plancha lateral"] = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png",
        // Bíceps
        ["Curl con barra"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl con mancuernas"] = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png",
        ["Curl martillo"] = "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png",
        ["Curl concentrado"] = "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg",
        ["Curl en predicador"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl en polea"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl 21s"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl araña"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl con barra Z"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl inclinado"] = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png",
        ["Curl en máquina"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        ["Curl inverso"] = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png",
        // Tríceps
        ["Fondos en paralelas"] = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png",
        ["Extensión con polea"] = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg",
        ["Press francés"] = "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png",
        ["Patada de tríceps"] = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg",
        ["Press cerrado"] = "https://wger.de/media/exercise-images/192/Bench-press-1.png",
        ["Extensión sobre cabeza"] = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg",
        ["Dips en banco"] = "https://wger.de/media/exercise-images/83/Bench-dips-1.png",
        ["Extensión con mancuerna"] = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg",
        ["Jalón con cuerda"] = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg",
        ["Press de tríceps en máquina"] = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png",
        ["Extensión en polea alta"] = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg",
        // Piernas
        ["Sentadilla"] = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg",
        ["Prensa"] = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp",
        ["Extensión de cuádriceps"] = "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp",
        ["Curl femoral"] = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png",
        ["Zancadas"] = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png",
        ["Sentadilla búlgara"] = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png",
        ["Sentadilla hack"] = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg",
        ["Peso muerto rumano"] = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp",
        ["Elevación de gemelos"] = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp",
        ["Sentadilla goblet"] = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg",
        ["Prensa de gemelos"] = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp",
        ["Step up"] = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png",
        ["Sentadilla sumo"] = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg",
        ["Abductores"] = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png",
        ["Aductores"] = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png",
        ["Leg curl sentado"] = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png",
        // Abdominales
        ["Crunch"] = "https://wger.de/media/exercise-images/91/Crunches-1.png",
        ["Plancha"] = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png",
        ["Elevación de piernas"] = "https://wger.de/media/exercise-images/125/Leg-raises-2.png",
        ["Russian twist"] = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png",
        ["Crunch en polea"] = "https://wger.de/media/exercise-images/91/Crunches-1.png",
        ["Ab wheel"] = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png",
        ["Mountain climbers"] = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png",
        ["Crunch bicicleta"] = "https://wger.de/media/exercise-images/91/Crunches-1.png",
        ["V-ups"] = "https://wger.de/media/exercise-images/125/Leg-raises-2.png",
        ["Dead bug"] = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png",
        ["Hollow hold"] = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png",
        ["Elevación de piernas colgado"] = "https://wger.de/media/exercise-images/125/Leg-raises-2.png",
        ["Woodchop"] = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png",
        // Glúteos
        ["Hip thrust"] = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg",
        ["Patada de glúteo"] = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg",
        ["Puente de glúteos"] = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg",
        ["Abducción de cadera"] = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg",
        ["Kickback en polea"] = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg",
        ["Clamshell"] = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg",
        ["Peso muerto sumo"] = "https://wger.de/media/exercise-images/105/Deadlift-1.png",
        ["Fire hydrant"] = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg",
    };

    public void UpdatePlaceholderImages()
    {
        var exercises = _db.Exercises
            .Where(e => e.ImageUrl.Contains("placehold"))
            .ToList();

        foreach (var ex in exercises)
        {
            if (KnownExerciseImages.TryGetValue(ex.Name, out var realUrl))
            {
                ex.ImageUrl = realUrl;
            }
        }

        if (exercises.Count > 0)
            _db.SaveChanges();
    }

    public Exercise AddExercise(string name, int muscleGroupId)
    {
        if (!_db.MuscleGroups.Any(mg => mg.Id == muscleGroupId))
            throw new ArgumentException("Grupo muscular no válido.");

        var imageUrl = KnownExerciseImages.TryGetValue(name, out var knownUrl)
            ? knownUrl
            : $"https://placehold.co/400x300/EEE/31343C.png?font=montserrat&text={Uri.EscapeDataString(name)}";

        var exercise = new Exercise
        {
            Name = name,
            MuscleGroupId = muscleGroupId,
            ImageUrl = imageUrl
        };
        _db.Exercises.Add(exercise);
        _db.SaveChanges();
        return exercise;
    }

    public WeekRoutine GetWeekRoutine(int userId)
    {
        var validDays = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };

        var days = validDays.Select(d => BuildDayRoutine(d, userId)).OrderBy(d => d.Day).ToList();
        return new WeekRoutine { Days = days };
    }

    public DayRoutine GetDayRoutine(DayOfWeek day, int userId) => BuildDayRoutine(day, userId);

    public DayRoutine SetDayRoutine(DayOfWeek day, List<int> muscleGroupIds, List<RoutineExerciseInput> exercises, int userId)
    {
        if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new ArgumentException("Solo se permiten días de lunes a viernes.");

        // Remove existing assignments
        var existingMg = _db.DayMuscleGroups.Where(d => d.Day == day && d.UserId == userId);
        _db.DayMuscleGroups.RemoveRange(existingMg);

        var existingEx = _db.DayExercises.Where(d => d.Day == day && d.UserId == userId);
        _db.DayExercises.RemoveRange(existingEx);

        // Add muscle groups
        var validMgIds = muscleGroupIds
            .Where(id => _db.MuscleGroups.Any(mg => mg.Id == id))
            .Distinct()
            .ToList();

        foreach (var mgId in validMgIds)
        {
            _db.DayMuscleGroups.Add(new DayMuscleGroupEntity { Day = day, MuscleGroupId = mgId, UserId = userId });
        }

        // Add exercises
        foreach (var input in exercises)
        {
            if (_db.Exercises.Any(e => e.Id == input.ExerciseId))
            {
                _db.DayExercises.Add(new DayExerciseEntity
                {
                    Day = day,
                    ExerciseId = input.ExerciseId,
                    Sets = input.Sets,
                    Reps = input.Reps,
                    Weight = input.Weight,
                    SetDetails = input.SetDetails ?? string.Empty,
                    UserId = userId
                });
            }
        }

        _db.SaveChanges();
        return BuildDayRoutine(day, userId);
    }

    private DayRoutine BuildDayRoutine(DayOfWeek day, int userId)
    {
        var muscleGroupIds = _db.DayMuscleGroups
            .Where(d => d.Day == day && d.UserId == userId)
            .Select(d => d.MuscleGroupId)
            .ToList();

        var muscleGroups = _db.MuscleGroups
            .Where(mg => muscleGroupIds.Contains(mg.Id))
            .ToList();

        var dayExercises = _db.DayExercises
            .Where(d => d.Day == day && d.UserId == userId)
            .Include(d => d.Exercise)
            .ToList();

        var routineExercises = dayExercises.Select(de =>
        {
            var mg = de.Exercise != null
                ? _db.MuscleGroups.FirstOrDefault(m => m.Id == de.Exercise.MuscleGroupId)
                : null;

            return new RoutineExercise
            {
                ExerciseId = de.ExerciseId,
                ExerciseName = de.Exercise?.Name ?? string.Empty,
                Sets = de.Sets,
                Reps = de.Reps,
                Weight = de.Weight,
                SetDetails = de.SetDetails ?? string.Empty,
                ImageUrl = de.Exercise?.ImageUrl ?? string.Empty,
                MuscleGroupName = mg?.Name ?? string.Empty
            };
        }).ToList();

        return new DayRoutine
        {
            Day = day,
            MuscleGroups = muscleGroups,
            Exercises = routineExercises
        };
    }
}
