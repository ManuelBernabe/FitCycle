using FitCycle.Infrastructure.Data;
using FitCycle.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FitCycle.Infrastructure.Services;

/// <summary>
/// Uses the configured AI provider (Groq → OpenRouter → Gemini) to map user-created
/// exercise names to canonical entries in <see cref="SqliteRoutineRepository.KnownExerciseImages"/>
/// so they get a real image URL instead of a placehold.co placeholder.
/// </summary>
public class ExerciseImageAutoFiller : IExerciseImageAutoFiller
{
    private const int BatchSize = 50;

    private readonly FitCycleDbContext _db;
    private readonly IAiService _ai;
    private readonly ILogger<ExerciseImageAutoFiller> _logger;

    public ExerciseImageAutoFiller(
        FitCycleDbContext db,
        IAiService ai,
        ILogger<ExerciseImageAutoFiller> logger)
    {
        _db = db;
        _ai = ai;
        _logger = logger;
    }

    public async Task<int> AutoPopulateImagesAsync()
    {
        if (!_ai.IsConfigured)
        {
            _logger.LogWarning("Auto-populate images: no AI provider configured, aborting.");
            return 0;
        }

        // 1. Find candidates: empty image URL or a placehold.co placeholder.
        var candidates = await _db.Exercises
            .Where(e => string.IsNullOrEmpty(e.ImageUrl) || e.ImageUrl.Contains("placehold.co"))
            .ToListAsync();

        if (candidates.Count == 0)
        {
            _logger.LogInformation("Auto-populate images: no candidates found.");
            return 0;
        }

        _logger.LogInformation(
            "Auto-populate images: {Count} candidate exercise(s) found, processing in batches of {BatchSize}.",
            candidates.Count, BatchSize);

        // 2. Build canonical names list once — reused across every batch prompt.
        var canonicalNames = SqliteRoutineRepository.KnownExerciseImages.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var canonicalNamesBlock = string.Join("\n", canonicalNames.Select(n => $"- {n}"));

        int totalUpdated = 0;

        // 3. Process in batches to keep prompts small.
        for (int batchStart = 0; batchStart < candidates.Count; batchStart += BatchSize)
        {
            var batch = candidates
                .Skip(batchStart)
                .Take(BatchSize)
                .ToList();

            // Deduplicate by name within the batch (multiple exercises may share a name).
            var uniqueNames = batch
                .Select(e => e.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueNames.Count == 0)
                continue;

            var userExercisesBlock = string.Join("\n", uniqueNames.Select(n => $"- {n}"));

            var prompt = $@"Eres un experto en fitness que mapea nombres de ejercicios escritos por usuarios a un catálogo canónico en español.

CATÁLOGO CANÓNICO (estos son los únicos valores válidos para el mapeo):
{canonicalNamesBlock}

EJERCICIOS DE USUARIO A MAPEAR:
{userExercisesBlock}

INSTRUCCIONES:
- Para cada ejercicio de usuario, devuelve el nombre canónico EXACTO del catálogo que mejor lo represente.
- Sé CONSERVADOR: si tienes dudas o no hay un equivalente claro y de alta confianza, devuelve null.
- Solo asigna un nombre canónico si estás MUY SEGURO de que el ejercicio del usuario es el mismo movimiento (mismo grupo muscular y patrón) que el canónico.
- Ignora diferencias de mayúsculas, tildes, plurales, equipamiento menor (mancuerna vs barra cuando el catálogo lo distingue: respeta la variante correcta).
- NO inventes nombres canónicos que no estén en la lista anterior.
- NO traduzcas; las claves y los valores deben estar en español tal cual aparecen.

FORMATO DE RESPUESTA (JSON estricto, sin markdown, sin texto extra):
{{
  ""nombre del ejercicio de usuario"": ""Nombre Canónico Exacto"" o null,
  ...
}}

Devuelve UN ÚNICO objeto JSON con TODOS los ejercicios de usuario como claves. Nada más.";

            var (mapping, error) = await _ai.GenerateStructuredAsync<Dictionary<string, string>>(prompt);

            if (error != null)
            {
                _logger.LogWarning(
                    "Auto-populate images: AI call failed for batch {BatchStart}-{BatchEnd}: {Error}",
                    batchStart, batchStart + uniqueNames.Count, error);
                continue;
            }

            if (mapping == null || mapping.Count == 0)
            {
                _logger.LogInformation(
                    "Auto-populate images: AI returned no mappings for batch {BatchStart}-{BatchEnd}.",
                    batchStart, batchStart + uniqueNames.Count);
                continue;
            }

            // Build a case-insensitive lookup for this batch.
            var mappingCi = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in mapping)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    continue;
                // Treat "null", "Null", or whitespace as no-match.
                var val = string.IsNullOrWhiteSpace(kvp.Value) ||
                          string.Equals(kvp.Value, "null", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : kvp.Value.Trim();
                mappingCi[kvp.Key.Trim()] = val;
            }

            // Apply mappings to every exercise in this batch.
            foreach (var ex in batch)
            {
                if (!mappingCi.TryGetValue(ex.Name, out var canonical) || canonical == null)
                    continue;

                // Look up the URL — exact match in KnownExerciseImages first,
                // fall back to FindImageUrl (handles aliases & normalization) for safety.
                if (!SqliteRoutineRepository.KnownExerciseImages.TryGetValue(canonical, out var url))
                    url = SqliteRoutineRepository.FindImageUrl(canonical);

                if (string.IsNullOrWhiteSpace(url))
                {
                    _logger.LogDebug(
                        "Auto-populate images: AI suggested '{Canonical}' for '{UserName}' but it's not in the catalog.",
                        canonical, ex.Name);
                    continue;
                }

                ex.ImageUrl = url;
                totalUpdated++;
            }
        }

        if (totalUpdated > 0)
            await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Auto-populate images: updated {Updated}/{Total} exercise(s) with real images via {Provider}.",
            totalUpdated, candidates.Count, _ai.ProviderName);

        return totalUpdated;
    }
}
