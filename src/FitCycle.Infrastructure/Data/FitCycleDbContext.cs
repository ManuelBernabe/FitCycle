using FitCycle.Core.Models;
using FitCycle.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitCycle.Infrastructure.Data;

public class FitCycleDbContext : DbContext
{
    public FitCycleDbContext(DbContextOptions<FitCycleDbContext> options) : base(options) { }

    public DbSet<MuscleGroup> MuscleGroups => Set<MuscleGroup>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<DayMuscleGroupEntity> DayMuscleGroups => Set<DayMuscleGroupEntity>();
    public DbSet<DayExerciseEntity> DayExercises => Set<DayExerciseEntity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutExerciseLog> WorkoutExerciseLogs => Set<WorkoutExerciseLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MuscleGroup
        modelBuilder.Entity<MuscleGroup>(e =>
        {
            e.HasKey(mg => mg.Id);
            e.Property(mg => mg.Name).IsRequired().HasMaxLength(100);
            e.Ignore(mg => mg.Exercises); // navigation managed separately
        });

        // Exercise
        modelBuilder.Entity<Exercise>(e =>
        {
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Name).IsRequired().HasMaxLength(200);
            e.Property(ex => ex.ImageUrl).HasMaxLength(500);
            e.HasOne(ex => ex.MuscleGroup)
             .WithMany()
             .HasForeignKey(ex => ex.MuscleGroupId);
        });

        // DayMuscleGroup join
        modelBuilder.Entity<DayMuscleGroupEntity>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.UserId, d.Day, d.MuscleGroupId }).IsUnique();
            e.HasOne(d => d.MuscleGroup).WithMany().HasForeignKey(d => d.MuscleGroupId);
        });

        // DayExercise join
        modelBuilder.Entity<DayExerciseEntity>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => new { d.UserId, d.Day, d.ExerciseId });
            e.HasOne(d => d.Exercise).WithMany().HasForeignKey(d => d.ExerciseId);
        });

        // WorkoutSession
        modelBuilder.Entity<WorkoutSession>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasMany(w => w.ExerciseLogs).WithOne(l => l.WorkoutSession).HasForeignKey(l => l.WorkoutSessionId);
        });

        // WorkoutExerciseLog
        modelBuilder.Entity<WorkoutExerciseLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.ExerciseName).HasMaxLength(200);
            e.Property(l => l.MuscleGroupName).HasMaxLength(100);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).IsRequired().HasMaxLength(100);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<int>();
            e.Property(u => u.RefreshToken).HasMaxLength(256);
        });

        // Seed muscle groups
        modelBuilder.Entity<MuscleGroup>().HasData(
            new { Id = 1, Name = "Pecho" },
            new { Id = 2, Name = "Espalda" },
            new { Id = 3, Name = "Hombros" },
            new { Id = 4, Name = "Bíceps" },
            new { Id = 5, Name = "Tríceps" },
            new { Id = 6, Name = "Piernas" },
            new { Id = 7, Name = "Abdominales" },
            new { Id = 8, Name = "Glúteos" }
        );

        // Seed exercises
        modelBuilder.Entity<Exercise>().HasData(
            // Pecho
            new { Id = 1, Name = "Press banca", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
            new { Id = 2, Name = "Press inclinado", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png" },
            new { Id = 3, Name = "Aperturas", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
            new { Id = 4, Name = "Fondos", MuscleGroupId = 1, ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
            // Espalda
            new { Id = 5, Name = "Dominadas", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
            new { Id = 6, Name = "Remo con barra", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
            new { Id = 7, Name = "Jalón al pecho", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
            new { Id = 8, Name = "Remo con mancuerna", MuscleGroupId = 2, ImageUrl = "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png" },
            // Hombros
            new { Id = 9, Name = "Press militar", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
            new { Id = 10, Name = "Elevaciones laterales", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
            new { Id = 11, Name = "Elevaciones frontales", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png" },
            new { Id = 12, Name = "Pájaros", MuscleGroupId = 3, ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
            // Bíceps
            new { Id = 13, Name = "Curl con barra", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
            new { Id = 14, Name = "Curl con mancuernas", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
            new { Id = 15, Name = "Curl martillo", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png" },
            new { Id = 16, Name = "Curl concentrado", MuscleGroupId = 4, ImageUrl = "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg" },
            // Tríceps
            new { Id = 17, Name = "Fondos en paralelas", MuscleGroupId = 5, ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
            new { Id = 18, Name = "Extensión con polea", MuscleGroupId = 5, ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
            new { Id = 19, Name = "Press francés", MuscleGroupId = 5, ImageUrl = "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png" },
            new { Id = 20, Name = "Patada de tríceps", MuscleGroupId = 5, ImageUrl = "https://placehold.co/400x300/EEE/31343C.png?font=montserrat&text=Patada%20triceps" },
            // Piernas
            new { Id = 21, Name = "Sentadilla", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
            new { Id = 22, Name = "Prensa", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
            new { Id = 23, Name = "Extensión de cuádriceps", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp" },
            new { Id = 24, Name = "Curl femoral", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
            new { Id = 25, Name = "Zancadas", MuscleGroupId = 6, ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
            // Abdominales
            new { Id = 26, Name = "Crunch", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
            new { Id = 27, Name = "Plancha", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
            new { Id = 28, Name = "Elevación de piernas", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
            new { Id = 29, Name = "Russian twist", MuscleGroupId = 7, ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
            // Glúteos
            new { Id = 30, Name = "Hip thrust", MuscleGroupId = 8, ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
            new { Id = 31, Name = "Peso muerto rumano", MuscleGroupId = 8, ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
            new { Id = 32, Name = "Patada de glúteo", MuscleGroupId = 8, ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
            new { Id = 33, Name = "Puente de glúteos", MuscleGroupId = 8, ImageUrl = "https://placehold.co/400x300/EEE/31343C.png?font=montserrat&text=Puente%20gluteos" }
        );

        // Seed superuser (password: Admin123!)
        modelBuilder.Entity<User>().HasData(
            new
            {
                Id = 1,
                Username = "admin",
                Email = "admin@fitcycle.local",
                PasswordHash = "$2a$11$r1zN2HmMy2FnebH4onffcOzWj8IsqmrB0Yxe5k1VgbPzXOh29WGDm",
                Role = UserRole.Superuser,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
