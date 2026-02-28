using FitCycle.Core.Models;

namespace FitCycle.Infrastructure.Repositories;

public interface IRoutineRepository
{
    IReadOnlyList<MuscleGroup> GetAllMuscleGroups();
    IReadOnlyList<Exercise> GetExercises(int? muscleGroupId = null);
    Exercise AddExercise(string name, int muscleGroupId);
    WeekRoutine GetWeekRoutine();
    DayRoutine GetDayRoutine(DayOfWeek day);
    DayRoutine SetDayRoutine(DayOfWeek day, List<int> muscleGroupIds, List<RoutineExerciseInput> exercises);
}

public record RoutineExerciseInput(int ExerciseId, int Sets, int Reps);
