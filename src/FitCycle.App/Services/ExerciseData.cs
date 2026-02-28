namespace FitCycle.App.Services;

public class ExerciseSuggestion
{
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public static class ExerciseData
{
    public static List<ExerciseSuggestion> GetSuggestions(string muscleGroupSpanishName)
    {
        var lang = L10n.CurrentLanguage;
        if (Data.TryGetValue(lang, out var langData) && langData.TryGetValue(muscleGroupSpanishName, out var list))
            return list;
        if (Data.TryGetValue("es", out var esData) && esData.TryGetValue(muscleGroupSpanishName, out var esList))
            return esList;
        return [];
    }

    private static readonly Dictionary<string, Dictionary<string, List<ExerciseSuggestion>>> Data = new()
    {
        // =====================================================================
        // SPANISH (es)
        // =====================================================================
        ["es"] = new()
        {
            ["Pecho"] =
            [
                new() { Name = "Press banca", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Press inclinado", ImageUrl = "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png" },
                new() { Name = "Press declinado", ImageUrl = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png" },
                new() { Name = "Aperturas con mancuernas", ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
                new() { Name = "Aperturas en polea", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Fondos", ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
                new() { Name = "Pullover", ImageUrl = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png" },
                new() { Name = "Press con mancuernas", ImageUrl = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png" },
                new() { Name = "Cruces en polea alta", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Cruces en polea baja", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Flexiones", ImageUrl = "https://wger.de/media/exercise-images/120/Push-ups-1.png" },
                new() { Name = "Flexiones diamante", ImageUrl = "https://wger.de/media/exercise-images/120/Push-ups-1.png" },
                new() { Name = "Press en máquina", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Peck deck", ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
            ],
            ["Espalda"] =
            [
                new() { Name = "Dominadas", ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
                new() { Name = "Remo con barra", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Jalón al pecho", ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
                new() { Name = "Remo con mancuerna", ImageUrl = "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png" },
                new() { Name = "Remo en polea baja", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Jalón tras nuca", ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
                new() { Name = "Peso muerto", ImageUrl = "https://wger.de/media/exercise-images/105/Deadlift-1.png" },
                new() { Name = "Pullover en polea", ImageUrl = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png" },
                new() { Name = "Remo en máquina", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Dominadas supinas", ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
                new() { Name = "Remo T", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Face pull", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Hiperextensiones", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Encogimientos con barra", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
            ],
            ["Hombros"] =
            [
                new() { Name = "Press militar", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Elevaciones laterales", ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
                new() { Name = "Elevaciones frontales", ImageUrl = "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png" },
                new() { Name = "Pájaros", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Press Arnold", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Remo al mentón", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Face pull", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Elevaciones laterales en polea", ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
                new() { Name = "Press con mancuernas", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Shrugs", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Rotación externa", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Plancha lateral", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
            ],
            ["Bíceps"] =
            [
                new() { Name = "Curl con barra", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl con mancuernas", ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
                new() { Name = "Curl martillo", ImageUrl = "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png" },
                new() { Name = "Curl concentrado", ImageUrl = "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg" },
                new() { Name = "Curl en predicador", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl en polea", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl 21s", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl araña", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl con barra Z", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl inclinado", ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
                new() { Name = "Curl en máquina", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl inverso", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
            ],
            ["Tríceps"] =
            [
                new() { Name = "Fondos en paralelas", ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
                new() { Name = "Extensión con polea", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Press francés", ImageUrl = "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png" },
                new() { Name = "Patada de tríceps", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Press cerrado", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Extensión sobre cabeza", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Dips en banco", ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
                new() { Name = "Extensión con mancuerna", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Jalón con cuerda", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Press de tríceps en máquina", ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
                new() { Name = "Extensión en polea alta", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
            ],
            ["Piernas"] =
            [
                new() { Name = "Sentadilla", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Prensa", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Extensión de cuádriceps", ImageUrl = "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp" },
                new() { Name = "Curl femoral", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Zancadas", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Sentadilla búlgara", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Sentadilla hack", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Peso muerto rumano", ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
                new() { Name = "Elevación de gemelos", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Sentadilla goblet", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Prensa de gemelos", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Step up", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Sentadilla sumo", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Abductores", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Aductores", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Leg curl sentado", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
            ],
            ["Abdominales"] =
            [
                new() { Name = "Crunch", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "Plancha", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Elevación de piernas", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Russian twist", ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
                new() { Name = "Crunch en polea", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "Ab wheel", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Mountain climbers", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Plancha lateral", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Crunch bicicleta", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "V-ups", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Dead bug", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Hollow hold", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Elevación de piernas colgado", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Woodchop", ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
            ],
            ["Glúteos"] =
            [
                new() { Name = "Hip thrust", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Peso muerto rumano", ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
                new() { Name = "Patada de glúteo", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Puente de glúteos", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Sentadilla sumo", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Step up", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Abducción de cadera", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Kickback en polea", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Clamshell", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Sentadilla búlgara", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Peso muerto sumo", ImageUrl = "https://wger.de/media/exercise-images/105/Deadlift-1.png" },
                new() { Name = "Fire hydrant", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
            ],
        },

        // =====================================================================
        // ENGLISH (en)
        // =====================================================================
        ["en"] = new()
        {
            ["Pecho"] =
            [
                new() { Name = "Bench press", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Incline bench press", ImageUrl = "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png" },
                new() { Name = "Decline bench press", ImageUrl = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png" },
                new() { Name = "Dumbbell flyes", ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
                new() { Name = "Cable flyes", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Dips", ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
                new() { Name = "Pullover", ImageUrl = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png" },
                new() { Name = "Dumbbell press", ImageUrl = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png" },
                new() { Name = "High cable crossover", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Low cable crossover", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Push-ups", ImageUrl = "https://wger.de/media/exercise-images/120/Push-ups-1.png" },
                new() { Name = "Diamond push-ups", ImageUrl = "https://wger.de/media/exercise-images/120/Push-ups-1.png" },
                new() { Name = "Machine press", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Pec deck", ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
            ],
            ["Espalda"] =
            [
                new() { Name = "Pull-ups", ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
                new() { Name = "Barbell row", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Lat pulldown", ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
                new() { Name = "Dumbbell row", ImageUrl = "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png" },
                new() { Name = "Seated cable row", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Behind neck pulldown", ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
                new() { Name = "Deadlift", ImageUrl = "https://wger.de/media/exercise-images/105/Deadlift-1.png" },
                new() { Name = "Cable pullover", ImageUrl = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png" },
                new() { Name = "Machine row", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Chin-ups", ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
                new() { Name = "T-bar row", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Face pull", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Hyperextensions", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Barbell shrugs", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
            ],
            ["Hombros"] =
            [
                new() { Name = "Military press", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Lateral raises", ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
                new() { Name = "Front raises", ImageUrl = "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png" },
                new() { Name = "Reverse flyes", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Arnold press", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Upright row", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Face pull", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Cable lateral raises", ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
                new() { Name = "Dumbbell press", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Shrugs", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "External rotation", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Side plank", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
            ],
            ["Bíceps"] =
            [
                new() { Name = "Barbell curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Dumbbell curl", ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
                new() { Name = "Hammer curl", ImageUrl = "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png" },
                new() { Name = "Concentration curl", ImageUrl = "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg" },
                new() { Name = "Preacher curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Cable curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "21s curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Spider curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "EZ bar curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Incline curl", ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
                new() { Name = "Machine curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Reverse curl", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
            ],
            ["Tríceps"] =
            [
                new() { Name = "Parallel bar dips", ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
                new() { Name = "Cable pushdown", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Skull crushers", ImageUrl = "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png" },
                new() { Name = "Tricep kickback", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Close grip bench press", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Overhead extension", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Bench dips", ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
                new() { Name = "Dumbbell extension", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Rope pushdown", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Machine tricep press", ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
                new() { Name = "High cable extension", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
            ],
            ["Piernas"] =
            [
                new() { Name = "Squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Leg press", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Leg extension", ImageUrl = "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp" },
                new() { Name = "Leg curl", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Lunges", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Bulgarian split squat", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Hack squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Romanian deadlift", ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
                new() { Name = "Calf raises", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Goblet squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Calf press", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Step up", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Sumo squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Hip abduction", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Hip adduction", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Seated leg curl", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
            ],
            ["Abdominales"] =
            [
                new() { Name = "Crunch", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "Plank", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Leg raises", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Russian twist", ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
                new() { Name = "Cable crunch", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "Ab wheel", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Mountain climbers", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Side plank", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Bicycle crunch", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "V-ups", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Dead bug", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Hollow hold", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Hanging leg raises", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Woodchop", ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
            ],
            ["Glúteos"] =
            [
                new() { Name = "Hip thrust", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Romanian deadlift", ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
                new() { Name = "Glute kickback", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Glute bridge", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Sumo squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Step up", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Hip abduction", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Cable kickback", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Clamshell", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Bulgarian split squat", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Sumo deadlift", ImageUrl = "https://wger.de/media/exercise-images/105/Deadlift-1.png" },
                new() { Name = "Fire hydrant", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
            ],
        },

        // =====================================================================
        // FRENCH (fr)
        // =====================================================================
        ["fr"] = new()
        {
            ["Pecho"] =
            [
                new() { Name = "Développé couché", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Développé incliné", ImageUrl = "https://wger.de/media/exercise-images/41/Incline-bench-press-1.png" },
                new() { Name = "Développé décliné", ImageUrl = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png" },
                new() { Name = "Écartés haltères", ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
                new() { Name = "Écartés poulie", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Dips", ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
                new() { Name = "Pullover", ImageUrl = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png" },
                new() { Name = "Développé haltères", ImageUrl = "https://wger.de/media/exercise-images/97/Decline-bench-press-1.png" },
                new() { Name = "Croisé poulie haute", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Croisé poulie basse", ImageUrl = "https://wger.de/media/exercise-images/122/Cable-crossover-1.png" },
                new() { Name = "Pompes", ImageUrl = "https://wger.de/media/exercise-images/120/Push-ups-1.png" },
                new() { Name = "Pompes diamant", ImageUrl = "https://wger.de/media/exercise-images/120/Push-ups-1.png" },
                new() { Name = "Presse pectorale", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Pec deck", ImageUrl = "https://wger.de/media/exercise-images/238/2fc242d3-5bdd-4f97-99bd-678adb8c96fc.png" },
            ],
            ["Espalda"] =
            [
                new() { Name = "Tractions", ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
                new() { Name = "Rowing barre", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Tirage poitrine", ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
                new() { Name = "Rowing haltère", ImageUrl = "https://wger.de/media/exercise-images/81/a751a438-ae2d-4751-8d61-cef0e9292174.png" },
                new() { Name = "Rowing poulie basse", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Tirage nuque", ImageUrl = "https://wger.de/media/exercise-images/158/02e8a7c3-dc67-434e-a4bc-77fdecf84b49.webp" },
                new() { Name = "Soulevé de terre", ImageUrl = "https://wger.de/media/exercise-images/105/Deadlift-1.png" },
                new() { Name = "Pullover poulie", ImageUrl = "https://wger.de/media/exercise-images/570/dumbbell-pullover-1.png" },
                new() { Name = "Rowing machine", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Tractions supination", ImageUrl = "https://wger.de/media/exercise-images/475/b0554016-16fd-4dbe-be47-a2a17d16ae0e.jpg" },
                new() { Name = "Rowing en T", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Face pull", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Hyperextensions", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Haussements barre", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
            ],
            ["Hombros"] =
            [
                new() { Name = "Développé militaire", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Élévations latérales", ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
                new() { Name = "Élévations frontales", ImageUrl = "https://wger.de/media/exercise-images/256/b7def5bc-2352-499b-b9e5-fff741003831.png" },
                new() { Name = "Oiseau", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Développé Arnold", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Rowing menton", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Face pull", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Élévations latérales poulie", ImageUrl = "https://wger.de/media/exercise-images/148/lateral-dumbbell-raises-large-2.png" },
                new() { Name = "Développé haltères", ImageUrl = "https://wger.de/media/exercise-images/418/fa2a2207-43cb-4dc0-bc2a-039e32544790.png" },
                new() { Name = "Shrugs", ImageUrl = "https://wger.de/media/exercise-images/109/Barbell-rear-delt-row-1.png" },
                new() { Name = "Rotation externe", ImageUrl = "https://wger.de/media/exercise-images/829/ad724e5c-b1ed-49e8-9279-a17545b0dd0b.png" },
                new() { Name = "Planche latérale", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
            ],
            ["Bíceps"] =
            [
                new() { Name = "Curl barre", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl haltères", ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
                new() { Name = "Curl marteau", ImageUrl = "https://wger.de/media/exercise-images/86/Bicep-hammer-curl-1.png" },
                new() { Name = "Curl concentré", ImageUrl = "https://wger.de/media/exercise-images/1649/441cc0e5-eca2-4828-8b0a-a0e554abb2ff.jpg" },
                new() { Name = "Curl pupitre", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl poulie", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl 21s", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl araignée", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl barre EZ", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl incliné", ImageUrl = "https://wger.de/media/exercise-images/81/Biceps-curl-1.png" },
                new() { Name = "Curl machine", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
                new() { Name = "Curl inversé", ImageUrl = "https://wger.de/media/exercise-images/74/Bicep-curls-1.png" },
            ],
            ["Tríceps"] =
            [
                new() { Name = "Dips parallèles", ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
                new() { Name = "Extension poulie", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Barre au front", ImageUrl = "https://wger.de/media/exercise-images/84/Lying-close-grip-triceps-press-to-chin-1.png" },
                new() { Name = "Extension triceps", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Développé serré", ImageUrl = "https://wger.de/media/exercise-images/192/Bench-press-1.png" },
                new() { Name = "Extension au-dessus tête", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Dips banc", ImageUrl = "https://wger.de/media/exercise-images/83/Bench-dips-1.png" },
                new() { Name = "Extension haltère", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Tirage corde", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
                new() { Name = "Presse triceps machine", ImageUrl = "https://wger.de/media/exercise-images/194/34600351-8b0b-4cb0-8daa-583537be15b0.png" },
                new() { Name = "Extension poulie haute", ImageUrl = "https://wger.de/media/exercise-images/1185/c5ca283d-8958-4fd8-9d59-a3f52a3ac66b.jpg" },
            ],
            ["Piernas"] =
            [
                new() { Name = "Squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Presse à cuisses", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Extension jambes", ImageUrl = "https://wger.de/media/exercise-images/851/4d621b17-f6cb-4107-97c0-9f44e9a2dbc6.webp" },
                new() { Name = "Curl ischio-jambiers", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Fentes", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Squat bulgare", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Hack squat", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Soulevé de terre roumain", ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
                new() { Name = "Mollets debout", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Squat goblet", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Mollets presse", ImageUrl = "https://wger.de/media/exercise-images/371/d2136f96-3a43-4d4c-9944-1919c4ca1ce1.webp" },
                new() { Name = "Step up", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Squat sumo", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Abducteurs", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Adducteurs", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
                new() { Name = "Leg curl assis", ImageUrl = "https://wger.de/media/exercise-images/364/b318dde9-f5f2-489f-940a-cd864affb9e3.png" },
            ],
            ["Abdominales"] =
            [
                new() { Name = "Crunch", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "Planche", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Relevé de jambes", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Russian twist", ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
                new() { Name = "Crunch poulie", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "Ab wheel", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Mountain climbers", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Planche latérale", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Crunch bicyclette", ImageUrl = "https://wger.de/media/exercise-images/91/Crunches-1.png" },
                new() { Name = "V-ups", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Dead bug", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Hollow hold", ImageUrl = "https://wger.de/media/exercise-images/458/b7bd9c28-9f1d-4647-bd17-ab6a3adf5770.png" },
                new() { Name = "Relevé jambes suspendues", ImageUrl = "https://wger.de/media/exercise-images/125/Leg-raises-2.png" },
                new() { Name = "Woodchop", ImageUrl = "https://wger.de/media/exercise-images/1193/70ca5d80-3847-4a8c-8882-c6e9e485e29e.png" },
            ],
            ["Glúteos"] =
            [
                new() { Name = "Hip thrust", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Soulevé terre roumain", ImageUrl = "https://wger.de/media/exercise-images/1750/c5ff74e1-b494-4df0-a13f-89c630b88ef9.webp" },
                new() { Name = "Kickback fessier", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Pont fessier", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Squat sumo", ImageUrl = "https://wger.de/media/exercise-images/1805/f166c599-4c03-42a0-9250-47f82a1f096d.jpg" },
                new() { Name = "Step up", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Abduction hanche", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Kickback poulie", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
                new() { Name = "Clamshell", ImageUrl = "https://wger.de/media/exercise-images/1614/7f3cfae2-e062-4211-9a6b-5a10851ce7f4.jpg" },
                new() { Name = "Squat bulgare", ImageUrl = "https://wger.de/media/exercise-images/984/5c7ffe68-e7b2-47f3-a22a-f9cc28640432.png" },
                new() { Name = "Soulevé terre sumo", ImageUrl = "https://wger.de/media/exercise-images/105/Deadlift-1.png" },
                new() { Name = "Fire hydrant", ImageUrl = "https://wger.de/media/exercise-images/1613/a851fe9d-771f-44da-82f0-799e02ae3fd1.jpg" },
            ],
        },
    };
}
