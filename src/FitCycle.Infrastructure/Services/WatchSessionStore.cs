using System.Collections.Concurrent;
using System.Globalization;

namespace FitCycle.Infrastructure.Services;

/// <summary>One stop of the "Next" walk through a workout, as computed by the phone.</summary>
public record WatchStep(int Ex, int Set, string Name, int SetNo, int TotalSets, int Reps, decimal Weight, bool LastOfExercise);

/// <summary>
/// Snapshot the phone publishes while a workout is open. <see cref="Steps"/> is the exact
/// sequence the phone's Next button would visit (supersets already resolved), so the
/// server never has to re-implement the navigation rules — a watch "next" is cursor+1.
/// </summary>
public record WatchSessionPublish(string SessionKey, int DayNum, int Cursor, List<WatchStep> Steps, int RestSeconds, long BaseRevision,
    int RestRemaining = 0);

public record WatchWeightEdit(int Ex, int Set, decimal Weight);

/// <summary>What the phone needs to catch up with commands sent from the watch.</summary>
public record WatchSessionView(string SessionKey, long Revision, bool Pending, int Cursor, int Ex, int Set,
    List<WatchWeightEdit> WeightEdits, int RestRemaining, bool Finished);

public record WatchCommandResult(bool HasSession, bool Ok, string Text, int Rest,
    string? Exercise, int SetNo, int TotalSets, int Reps, decimal Weight, bool Finished);

public record WatchPublishResult(bool Accepted, WatchSessionView View);

/// <summary>
/// In-memory live-workout state shared between the phone (JWT, publishes snapshots and
/// polls) and the Apple Watch (Shortcuts, sends commands). In-memory on purpose: it is
/// ephemeral, and after a restart the phone simply republishes on its next poll.
///
/// Conflict rule: while the watch has changes the phone hasn't applied yet (Pending), a
/// phone publish built on an older revision is rejected and answered with the current
/// view, so the phone applies the watch's moves first and then republishes.
/// </summary>
public class WatchSessionStore
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromHours(4);
    public const int MaxSteps = 400;

    private readonly ConcurrentDictionary<int, State> _sessions = new();
    private readonly Func<DateTime> _now;

    public WatchSessionStore() : this(() => DateTime.UtcNow) { }
    public WatchSessionStore(Func<DateTime> now) { _now = now; }

    private sealed class State
    {
        public string SessionKey = string.Empty;
        public int DayNum;
        public List<WatchStep> Steps = new();
        public int Cursor;
        public int RestSeconds;
        public long Revision;
        public bool Pending;
        public Dictionary<(int Ex, int Set), decimal> WeightEdits = new();
        public DateTime? RestUntil;
        public bool Finished;
        public DateTime TouchedAt;
    }

    public WatchPublishResult Publish(int userId, WatchSessionPublish p)
    {
        var steps = (p.Steps ?? new List<WatchStep>())
            .Take(MaxSteps)
            .Select(s => s with { Name = Trunc((s.Name ?? string.Empty).Trim(), 120) })
            .ToList();
        var now = _now();
        var state = _sessions.GetOrAdd(userId, _ => new State());
        lock (state)
        {
            var sameSession = state.SessionKey == (p.SessionKey ?? string.Empty) && !IsStale(state, now);
            if (sameSession && state.Pending && p.BaseRevision < state.Revision)
                return new WatchPublishResult(false, View(state, now));

            var cursor = steps.Count == 0 ? 0 : Math.Clamp(p.Cursor, 0, steps.Count - 1);
            state.Finished = sameSession && state.Finished && cursor == steps.Count - 1;
            // The phone owns the rest countdown once it has applied everything: what it
            // reports is what the watch timer should pick up ("status" → remaining rest).
            state.RestUntil = p.RestRemaining > 0 ? now.AddSeconds(Math.Min(p.RestRemaining, 3600)) : null;
            state.SessionKey = p.SessionKey ?? string.Empty;
            state.DayNum = p.DayNum;
            state.Steps = steps;
            state.Cursor = cursor;
            state.RestSeconds = Math.Clamp(p.RestSeconds, 0, 3600);
            state.Pending = false;
            state.WeightEdits.Clear();
            state.TouchedAt = now;
            // Revision is never reset (even across sessions) so a stale phone tab can't
            // mistake an old revision number for a fresh one.
            return new WatchPublishResult(true, View(state, now));
        }
    }

    public WatchSessionView? Get(int userId)
    {
        if (!_sessions.TryGetValue(userId, out var state)) return null;
        var now = _now();
        lock (state)
        {
            if (state.Steps.Count == 0 || IsStale(state, now)) return null;
            return View(state, now);
        }
    }

    public void End(int userId) => _sessions.TryRemove(userId, out _);

    public WatchCommandResult Command(int userId, string action, decimal? kg, string? lang)
    {
        var L = Phrases.For(lang);
        var now = _now();
        if (!_sessions.TryGetValue(userId, out var state))
            return NoSession(L);
        lock (state)
        {
            if (state.Steps.Count == 0 || IsStale(state, now)) return NoSession(L);

            var rest = 0;
            string? prefix = null;
            var changed = false;
            var ok = true;
            var act = (action ?? string.Empty).Trim().ToLowerInvariant();
            switch (act)
            {
                case "status":
                    if (state.RestUntil is { } until && until > now)
                    {
                        rest = (int)Math.Ceiling((until - now).TotalSeconds);
                        prefix = string.Format(L.Rest, FormatSeconds(rest));
                    }
                    break;

                case "next":
                case "skip":
                    if (state.Cursor >= state.Steps.Count - 1)
                    {
                        state.Finished = true;
                        state.RestUntil = null;
                        changed = true;
                        break;
                    }
                    var done = state.Steps[state.Cursor];
                    state.Cursor++;
                    // Same rule as the phone: rest only BETWEEN sets of one exercise.
                    rest = act == "next" && !done.LastOfExercise ? state.RestSeconds : 0;
                    state.RestUntil = rest > 0 ? now.AddSeconds(rest) : null;
                    prefix = rest > 0 ? string.Format(L.Rest, FormatSeconds(rest)) : L.Done;
                    changed = true;
                    break;

                case "rest":
                    // Manual rest without advancing (like Start on the phone).
                    rest = state.RestSeconds;
                    state.RestUntil = rest > 0 ? now.AddSeconds(rest) : null;
                    if (rest > 0) prefix = string.Format(L.Rest, FormatSeconds(rest));
                    changed = true;
                    break;

                case "prev":
                    if (state.Finished) { state.Finished = false; changed = true; break; }
                    if (state.Cursor == 0) { prefix = L.First; break; }
                    state.Cursor--;
                    state.RestUntil = null;
                    changed = true;
                    break;

                case "plus":
                case "minus":
                    var delta = Math.Clamp(Math.Abs(kg ?? 2.5m), 0.25m, 50m);
                    if (act == "minus") delta = -delta;
                    var cur = state.Steps[state.Cursor];
                    var w = Math.Clamp(Math.Round(cur.Weight + delta, 2), 0m, 1000m);
                    state.Steps[state.Cursor] = cur with { Weight = w };
                    state.WeightEdits[(cur.Ex, cur.Set)] = w;
                    changed = true;
                    break;

                default:
                    ok = false;
                    prefix = L.Unknown;
                    break;
            }

            if (changed)
            {
                state.Revision++;
                state.Pending = true;
                state.TouchedAt = now;
            }

            var step = state.Steps[state.Cursor];
            string text;
            if (state.Finished)
                text = L.Finished;
            else
            {
                var line2 = step.Weight > 0
                    ? $"{string.Format(L.Set, step.SetNo, step.TotalSets)} · {step.Reps} × {step.Weight.ToString("0.##", L.Culture)} kg"
                    : $"{string.Format(L.Set, step.SetNo, step.TotalSets)} · {step.Reps} reps";
                text = (prefix is null ? string.Empty : prefix + "\n") + step.Name + "\n" + line2;
            }
            return new WatchCommandResult(true, ok, text, rest, step.Name, step.SetNo, step.TotalSets, step.Reps, step.Weight, state.Finished);
        }
    }

    private static WatchCommandResult NoSession(Phrases L) =>
        new(false, false, L.NoSession, 0, null, 0, 0, 0, 0m, false);

    private static bool IsStale(State s, DateTime now) => now - s.TouchedAt > StaleAfter;

    private static WatchSessionView View(State s, DateTime now)
    {
        var step = s.Steps.Count > 0 ? s.Steps[s.Cursor] : new WatchStep(0, 0, string.Empty, 0, 0, 0, 0m, false);
        var restRemaining = s.RestUntil is { } until && until > now ? (int)Math.Ceiling((until - now).TotalSeconds) : 0;
        return new WatchSessionView(s.SessionKey, s.Revision, s.Pending, s.Cursor, step.Ex, step.Set,
            s.WeightEdits.Select(kv => new WatchWeightEdit(kv.Key.Ex, kv.Key.Set, kv.Value)).ToList(),
            restRemaining, s.Finished);
    }

    private static string FormatSeconds(int secs) => $"{secs / 60}:{secs % 60:00}";

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];

    private sealed record Phrases(CultureInfo Culture, string NoSession, string Rest, string Done, string Finished,
        string First, string Set, string Unknown)
    {
        private static readonly Phrases Es = new(CultureInfo.GetCultureInfo("es-ES"),
            "No hay ningún entreno abierto en FitCycle. Ábrelo en el iPhone.",
            "✅ Descansa {0}", "✅ ¡Ejercicio terminado!",
            "🏁 ¡Última serie hecha! Pulsa Finalizar en el iPhone.",
            "Ya estás en la primera serie.", "Serie {0}/{1}", "Acción no válida.");
        private static readonly Phrases En = new(CultureInfo.GetCultureInfo("en-US"),
            "No workout open in FitCycle. Open it on your iPhone.",
            "✅ Rest {0}", "✅ Exercise done!",
            "🏁 Last set done! Tap Finish on your iPhone.",
            "You're already on the first set.", "Set {0}/{1}", "Unknown action.");
        private static readonly Phrases Fr = new(CultureInfo.GetCultureInfo("fr-FR"),
            "Aucune séance ouverte dans FitCycle. Ouvre-la sur l'iPhone.",
            "✅ Repos {0}", "✅ Exercice terminé !",
            "🏁 Dernière série faite ! Appuie sur Terminer sur l'iPhone.",
            "Tu es déjà à la première série.", "Série {0}/{1}", "Action inconnue.");

        public static Phrases For(string? lang) => (lang ?? "es").ToLowerInvariant() switch
        {
            var l when l.StartsWith("en", StringComparison.Ordinal) => En,
            var l when l.StartsWith("fr", StringComparison.Ordinal) => Fr,
            _ => Es,
        };
    }
}
