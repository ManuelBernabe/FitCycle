// FitCycle Workout Page — exercise-by-exercise, set-by-set workout with rest timer

import { t, dayName, muscleGroup as mgTranslate, exerciseName as exTranslate } from '../l10n.js';
import { api } from '../api.js';
import { offline } from '../offline.js';
import { escapeHtml, haptic, estimate1RM, celebrate, confetti, showVideoModal } from '../utils.js';

let dayNum = 0;
let exercises = [];
let currentIndex = 0;
let currentSet = 0;
let startedAt = null;
let timerSeconds = 60;
let timerRunning = false;
let timerInterval = null;
// Absolute wall-clock end time of the current rest. We compute `timerSeconds` from
// (timerEndsAt - Date.now()) every tick so the countdown stays accurate even when
// the browser throttles or suspends our setInterval — e.g. when the user switches
// to another app on iOS and comes back.
let timerEndsAt = 0;
let showExerciseList = false;
let prefillSource = null; // { date, count } when pre-fill applied weights from a previous workout
// All-time max weight per exercise (key: lowercase name), loaded in background at
// mount. Lets us celebrate a PR the moment the set is completed instead of only in
// the end-of-workout summary. Empty when offline — live PRs just stay silent then.
let bestWeights = {};
// Personal per-exercise notes (key: lowercase name), loaded at mount. Kept server-side
// (keyed by NAME so they survive PDF re-imports) — localStorage would be purged by iOS.
let myNotes = {};
// Periodic autosave so a forgotten or crashed session can still recover the user's last
// typed values from sessionStorage. Cleared on destroy() / finish.
let autosaveInterval = null;

const STORAGE_KEY = 'workout_progress';

// ── Persistence ──

function saveProgress() {
  const data = {
    dayNum,
    currentIndex,
    currentSet,
    startedAt: startedAt?.toISOString(),
    exercises: exercises.map(ex => ({
      exerciseId: ex.exerciseId || ex.ExerciseId || ex.id || ex.Id,
      setDetails: ex.setDetails,
    })),
  };
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}

function loadProgress() {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    return JSON.parse(raw);
  } catch { return null; }
}

function clearProgress() {
  sessionStorage.removeItem(STORAGE_KEY);
}

// ── Render / Mount ──

export function render(params) {
  dayNum = parseInt(params);
  return `
    <div class="page no-tabs">
      <div id="workout-content">
        <div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div>
      </div>
    </div>
  `;
}

export async function mount(params) {
  dayNum = parseInt(params);

  // The workout page pins the Prev/Next bar to the bottom of the viewport — lift the
  // global font-size FAB above it so neither covers the other. Restored in destroy().
  const zoomFab = document.getElementById('zoom-fab');
  if (zoomFab) zoomFab.style.bottom = 'calc(env(safe-area-inset-bottom, 0px) + 108px)';

  try {
    const dayData = await api.get(`/routines/${dayNum}`);
    const rawExercises = dayData?.exercises || dayData?.Exercises || [];

    exercises = rawExercises.map(ex => {
      const sets = ex.sets || ex.Sets || 3;
      const reps = ex.reps || ex.Reps || 12;
      const weight = ex.weight || ex.Weight || 0;
      let setDetails;
      try {
        const raw = ex.setDetails || ex.SetDetails || '';
        setDetails = raw ? JSON.parse(raw) : null;
      } catch (e) { setDetails = null; }
      if (!Array.isArray(setDetails) || setDetails.length === 0) {
        setDetails = Array.from({ length: sets }, () => ({ reps, weight, tempoPos: 0, tempoNeg: 0, grip: '' }));
      } else {
        setDetails = setDetails.map(s => ({ reps: s.reps || 12, weight: s.weight || 0, tempoPos: s.tempoPos || 0, tempoNeg: s.tempoNeg || 0, grip: s.grip || '' }));
      }
      const supersetGroup = ex.supersetGroup || ex.SupersetGroup || 0;
      const notes = ex.notes || ex.Notes || '';
      return { ...ex, setDetails, supersetGroup, notes };
    });

    if (exercises.length === 0) {
      document.getElementById('workout-content').innerHTML = `
        <div class="page-content">
          <div class="empty-state">
            <div class="empty-state-icon">&#128170;</div>
            <div class="empty-state-text">${t('NoExercisesDay')}</div>
            <button class="btn btn-primary mt-16" id="workout-empty-back">${t('BackToRoutines')}</button>
          </div>
        </div>
      `;
      document.getElementById('workout-empty-back')?.addEventListener('click', () => {
        location.hash = '#routines';
      });
      return;
    }

    // Pre-fill weights from last completed workout for this day
    prefillSource = null;
    try {
      const lastWorkout = await api.get(`/workouts/last-weights/${dayNum}`);
      console.log('[prefill] last-weights response for day', dayNum, lastWorkout);
      if (lastWorkout?.exercises?.length) {
        let filled = 0;
        let matchedExercises = 0;
        for (const lastEx of lastWorkout.exercises) {
          let match = exercises.find(ex => {
            const routineId = ex.exerciseId ?? ex.ExerciseId ?? ex.id ?? ex.Id;
            return routineId === lastEx.exerciseId;
          });
          // Re-imports can move a movement to a NEW Exercise.Id (full trainer wording →
          // new catalog row). Fall back to matching by name so the weights history
          // survives a re-import instead of prefilling zeros.
          if (!match && lastEx.name) {
            const lastName = String(lastEx.name).trim().toLowerCase();
            match = exercises.find(ex => {
              const exName = (ex.exerciseName ?? ex.ExerciseName ?? ex.name ?? ex.Name ?? '').trim().toLowerCase();
              return exName && exName === lastName;
            });
          }
          if (!match) {
            console.log('[prefill] no match for exerciseId', lastEx.exerciseId, lastEx.name || '');
            continue;
          }
          matchedExercises++;
          let lastSetDetails = null;
          try { lastSetDetails = lastEx.setDetails ? JSON.parse(lastEx.setDetails) : null; } catch { }
          // Keep the raw per-set history on the exercise: the per-set progression hint
          // compares what was actually LIFTED last session against today's plan.
          if (Array.isArray(lastSetDetails) && lastSetDetails.length > 0) {
            match._lastSets = lastSetDetails;
          }
          if (Array.isArray(lastSetDetails) && lastSetDetails.length > 0) {
            for (let i = 0; i < match.setDetails.length; i++) {
              const src = i < lastSetDetails.length ? lastSetDetails[i] : lastSetDetails[lastSetDetails.length - 1];
              // Only WEIGHT is prefilled from history — reps belong to the plan and stay
              // as defined in the routine. Otherwise reducing reps in one session (e.g. 12
              // instead of the planned 20) would silently rewrite the plan for next time.
              if (src.weight > 0) { match.setDetails[i].weight = src.weight; filled++; }
            }
          } else if (lastEx.weight > 0) {
            for (const sd of match.setDetails) { sd.weight = lastEx.weight; filled++; }
          }
        }
        console.log('[prefill] filled', filled, 'sets across', matchedExercises, 'exercises');
        if (filled > 0) {
          prefillSource = {
            date: lastWorkout.date,
            exerciseCount: matchedExercises,
            setCount: filled
          };
        }
      } else {
        console.log('[prefill] no previous workout for this day');
      }
    } catch (err) { console.warn('[prefill] failed:', err); }

    // Background: all-time bests for live PR detection (never blocks the workout UI).
    loadBestWeights();

    // Personal notes must be there on the FIRST render of each exercise, so this one
    // is awaited (a single tiny GET). Offline → empty map, the add-note button still shows.
    await loadMyNotes();

    // Restore saved progress if same day AND recent (within 4 hours).
    // Reps come from the PLAN (the routine's setDetails) and must NEVER be overwritten
    // by stale session storage — that's what made re-imported routines show old values
    // at workout time. Only the user's weights (their actual lift) get restored, since
    // those are progress data the user typed during the workout.
    const saved = loadProgress();
    const savedAge = saved?.startedAt ? (Date.now() - new Date(saved.startedAt).getTime()) : Infinity;
    const isFreshProgress = saved && saved.dayNum === dayNum && saved.exercises && savedAge < 4 * 60 * 60 * 1000;
    if (isFreshProgress) {
      startedAt = saved.startedAt ? new Date(saved.startedAt) : new Date();
      currentIndex = Math.min(saved.currentIndex || 0, exercises.length - 1);
      currentSet = saved.currentSet || 0;
      for (const savedEx of saved.exercises) {
        const match = exercises.find(ex =>
          (ex.exerciseId || ex.ExerciseId || ex.id || ex.Id) === savedEx.exerciseId
        );
        if (!match || !savedEx.setDetails) continue;
        // Only restore weights (and only when set count and reps still match — otherwise
        // we'd be applying weights from a different plan version).
        const sameShape = match.setDetails.length === savedEx.setDetails.length;
        for (let i = 0; i < match.setDetails.length && i < savedEx.setDetails.length; i++) {
          const savedReps = savedEx.setDetails[i].reps || 0;
          const planReps = match.setDetails[i].reps || 0;
          if (sameShape && savedEx.setDetails[i].weight > 0 && savedReps === planReps) {
            match.setDetails[i].weight = savedEx.setDetails[i].weight;
          }
        }
      }
      if (currentSet >= exercises[currentIndex].setDetails.length) currentSet = 0;
    } else {
      if (saved) clearProgress(); // Discard stale progress
      startedAt = new Date();
      currentIndex = 0;
      currentSet = 0;
    }

    renderExercise();

    // When the user comes back from another app, immediately reconcile the timer.
    // Mobile browsers throttle our setInterval to ~1/min while backgrounded; without
    // this hook the countdown would appear frozen for several seconds after resume.
    if (typeof document.__fitcycleVisHandler !== 'function') {
      document.__fitcycleVisHandler = onVisibilityChange;
      document.addEventListener('visibilitychange', document.__fitcycleVisHandler);
    }

    // Defensive autosave every 15s: even if the user navigates away, force-quits the app,
    // or the browser crashes, the last typed values stay in sessionStorage and the next
    // visit (within the 4-hour window in loadProgress) restores the weights.
    if (autosaveInterval) clearInterval(autosaveInterval);
    autosaveInterval = setInterval(autosaveTick, 15000);

    // Any touch on the page re-arms the wake lock and its idle timestamp.
    document.getElementById('workout-content')?.addEventListener('pointerdown', noteInteraction, { passive: true });

    // Keep the screen awake for the whole session (re-acquired on foreground resume).
    noteInteraction();
  } catch (err) {
    document.getElementById('workout-content').innerHTML = `
      <div class="page-content">
        <div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>
      </div>
    `;
  }
}

export function destroy() {
  stopTimer();
  stopTempoMetronome();
  closeRestOverlay();
  stopRestKeepalive();
  releaseWakeLock();
  const zoomFab = document.getElementById('zoom-fab');
  if (zoomFab) zoomFab.style.bottom = '';
  if (autosaveInterval) { clearInterval(autosaveInterval); autosaveInterval = null; }
  if (typeof document.__fitcycleVisHandler === 'function') {
    document.removeEventListener('visibilitychange', document.__fitcycleVisHandler);
    document.__fitcycleVisHandler = null;
  }
  // Final flush — in case the user navigates away mid-set, persist what's currently in
  // the inputs so the next workout visit (within the 4h freshness window) can recover it.
  try { saveCurrentSetValues(); saveProgress(); } catch { /* exercises may already be cleared */ }
}

// ── Exercise List ──

function buildExerciseList() {
  return exercises.map((ex, idx) => {
    const name = exTranslate(ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '');
    const muscle = ex.muscleGroupName || ex.MuscleGroupName || '';
    const totalSets = ex.setDetails.length;
    const setsWithWeight = ex.setDetails.filter(s => s.weight > 0).length;
    const maxWeight = Math.max(...ex.setDetails.map(s => s.weight), 0);
    const isCurrent = idx === currentIndex;
    const isDone = idx < currentIndex;

    // One dot per set: green if a weight is recorded, grey if not. Lets the user scan
    // the list at a glance and see exactly which sets are missing a weight value.
    const weightDots = ex.setDetails.map(s =>
      `<span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:${s.weight > 0 ? '#28a745' : '#ddd'};margin-right:3px;" title="${s.weight > 0 ? s.weight + 'kg' : 'sin peso'}"></span>`
    ).join('');

    return `
      <div class="exercise-list-item ${isCurrent ? 'current' : ''} ${isDone ? 'done' : ''}" data-go-exercise="${idx}">
        <div class="exercise-list-num">${idx + 1}</div>
        <div class="exercise-list-info">
          <div class="exercise-list-name">${escapeHtml(name)}</div>
          <div class="exercise-list-meta">${mgTranslate(muscle)} · ${setsWithWeight}/${totalSets} ${weightDots}${maxWeight > 0 ? ` · max ${maxWeight}kg` : ''}</div>
        </div>
        ${isDone ? '<div class="done-check">&#10003;</div>' : ''}
        ${isCurrent ? '<div class="current-arrow">&#9654;</div>' : ''}
      </div>
    `;
  }).join('');
}

// ── Render Exercise ──

function renderExercise() {
  const container = document.getElementById('workout-content');
  if (!container || currentIndex >= exercises.length) return;

  const ex = exercises[currentIndex];
  const exName = exTranslate(ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '');
  const exMuscle = ex.muscleGroupName || ex.MuscleGroupName || '';
  const exImage = ex.imageUrl || ex.ImageUrl || '';
  const totalSets = ex.setDetails.length;
  const currentSetData = ex.setDetails[currentSet] || { reps: 12, weight: 0, tempoPos: 0, tempoNeg: 0, grip: '' };
  // Pre-fill weight from previous set if current set has no weight yet
  if (currentSetData.weight === 0 && currentSet > 0) {
    const prevSet = ex.setDetails[currentSet - 1];
    if (prevSet && prevSet.weight > 0) currentSetData.weight = prevSet.weight;
  }
  const exNotes = ex.notes || '';
  const progressPct = ((currentIndex + 1) / exercises.length * 100).toFixed(0);
  const isLastExercise = currentIndex === exercises.length - 1;
  const isLastSet = currentSet >= totalSets - 1;

  // Superset partner info
  const ssGroup = ex.supersetGroup || 0;
  const ssPartner = ssGroup > 0 ? exercises.find((e, i) => i !== currentIndex && (e.supersetGroup || 0) === ssGroup) : null;
  const ssPartnerName = ssPartner ? exTranslate(ssPartner.exerciseName || ssPartner.ExerciseName || ssPartner.name || '') : '';

  const setDots = ex.setDetails.map((s, i) => {
    const cls = i < currentSet ? 'done' : (i === currentSet ? 'current' : '');
    return `<div class="set-dot ${cls}" title="S${i + 1}: ${s.reps}r / ${s.weight}kg"></div>`;
  }).join('');

  // Default rest stays at the user's setting (1 min by default).
  // suggestRestSeconds() is still available as a helper but we don't auto-apply it.

  // Per-set progression hint from the LAST recorded session (double progression:
  // completed planned reps → +increment; fell short → repeat the weight).
  const setHint = suggestSetWeight(ex, currentSet);

  // After the timer auto-advanced (or was reset), `timerSeconds` can be 0 — which would
  // render the pickers as 00:00 and the next Start press would no-op. Restore the user's
  // default rest (1:00) before building the picker HTML so the new set starts fresh.
  if (!timerRunning && (timerSeconds <= 0 || isNaN(timerSeconds))) timerSeconds = 60;
  const currentMin = Math.floor(timerSeconds / 60);
  const currentSec = timerSeconds % 60;
  const minOptions = Array.from({ length: 11 }, (_, i) =>
    `<option value="${i}" ${i === currentMin ? 'selected' : ''}>${String(i).padStart(2, '0')}</option>`
  ).join('');
  const secOptions = Array.from({ length: 12 }, (_, i) => {
    const val = i * 5;
    return `<option value="${val}" ${val === currentSec ? 'selected' : ''}>${String(val).padStart(2, '0')}</option>`;
  }).join('');

  container.innerHTML = `
    <button id="workout-back" class="floating-back-btn" style="bottom:calc(env(safe-area-inset-bottom, 0px) + 108px);">${t('Back')}</button>
    <div class="page-content" style="padding-top:8px;padding-bottom:calc(env(safe-area-inset-bottom, 0px) + 96px);">
      <div class="flex items-center justify-between" style="margin-bottom:4px;">
        <button id="toggle-exercise-list" class="btn btn-ghost text-primary" style="font-size:12px;padding:4px 8px;">
          ${showExerciseList ? '&#9650; ' + t('Exercises') : '&#9660; ' + t('Exercises')} (${exercises.length})
        </button>
        <div class="status-text" style="font-size:12px;">${dayName(dayNum)}</div>
      </div>

      <div id="exercise-list-panel" style="display:${showExerciseList ? 'block' : 'none'};margin-bottom:8px;">
        <div style="background:var(--card-bg);border-radius:var(--radius);box-shadow:var(--shadow);overflow:hidden;">
          ${buildExerciseList()}
        </div>
      </div>

      <div class="progress-bar" style="margin-bottom:6px;">
        <div class="fill" style="width:${progressPct}%"></div>
      </div>

      ${prefillSource ? `
        <div id="prefill-banner" style="background:#e8f5e9;border:1px solid #a5d6a7;color:#2e7d32;border-radius:8px;padding:6px 10px;margin-bottom:6px;font-size:12px;display:flex;align-items:center;justify-content:space-between;gap:6px;">
          <span>&#9989; ${t('PrefilledFrom', new Date(prefillSource.date).toLocaleDateString(), prefillSource.exerciseCount, prefillSource.setCount)}</span>
          <button id="prefill-dismiss" style="background:none;border:none;color:#2e7d32;font-size:16px;cursor:pointer;line-height:1;">&#10005;</button>
        </div>
      ` : ''}

      <div class="card workout-exercise">
        <div style="display:flex;align-items:center;gap:12px;text-align:left;margin-bottom:6px;">
          <div id="workout-exercise-image" class="workout-exercise-image" style="margin:0;flex-shrink:0;cursor:pointer;position:relative;" title="${t('ChangeImage')}">
            ${exImage
              ? `<img src="${exImage}" alt="${escapeHtml(exName)}" onerror="this.onerror=null;this.parentElement.querySelector('.workout-img-placeholder').style.display='flex';this.style.display='none';">`
              : ''
            }
            <div class="workout-img-placeholder" style="display:${exImage ? 'none' : 'flex'};align-items:center;justify-content:center;width:100%;height:100%;font-size:40px;opacity:0.3;">&#128247;</div>
          </div>
          <div style="min-width:0;flex:1;">
            <div style="font-size:12px;color:#512BD4;font-weight:600;">${t('ExerciseProgress', currentIndex + 1, exercises.length)}</div>
            <div class="workout-exercise-name">${escapeHtml(exName)}</div>
            <div style="font-size:13px;color:gray;">${mgTranslate(exMuscle)}</div>
            ${ssPartnerName ? `
              <div style="margin-top:3px;display:flex;align-items:center;gap:6px;flex-wrap:wrap;">
                <span style="font-size:11px;color:#e67e22;font-weight:600;">&#8644; ${t('Superset')}: ${escapeHtml(ssPartnerName)}</span>
                <button id="workout-switch-partner" style="background:#e67e22;color:#fff;border:none;border-radius:6px;padding:3px 8px;font-size:11px;font-weight:600;cursor:pointer;">
                  &#8644; ${t('SwitchPartner')}
                </button>
              </div>
            ` : ''}
            ${(ex.alternateExerciseName || ex.AlternateExerciseName) ? `
              <div style="margin-top:3px;font-size:11px;color:#512BD4;font-weight:600;">
                &#8646; ${t('BiweeklyWorkoutBadge', escapeHtml(exTranslate(ex.alternateExerciseName || ex.AlternateExerciseName)))}
              </div>
            ` : ''}
          </div>
        </div>

        <div style="display:flex;gap:8px;margin:8px 0 2px;">
          <button id="workout-video-btn" class="video-btn ${ex.videoUrl || ex.VideoUrl ? '' : 'empty'}" style="flex:1;justify-content:center;padding:7px 4px;font-size:12px;" title="${t('Demo') || 'Demo'}">▶ ${t('Demo') || 'Demo'}</button>
          <button id="workout-tips-btn" class="video-btn" style="flex:1;justify-content:center;padding:7px 4px;font-size:12px;background:#ffc107;color:#000;" title="${t('FormTips')}">💡 ${t('FormTips')}</button>
          <button id="workout-history-btn" class="video-btn" style="flex:1;justify-content:center;padding:7px 4px;font-size:12px;background:#512BD4;color:#fff;" title="${t('ExerciseHistory')}">📊 ${t('ExerciseHistory')}</button>
        </div>

        <div class="set-indicator" style="margin:6px 0;">${setDots}</div>

        <div style="background:#f5f5f5;border-radius:10px;padding:8px 12px;margin:0 auto;max-width:300px;">
          <div style="font-size:12px;color:#512BD4;font-weight:700;margin-bottom:4px;">
            ${t('SetN', currentSet + 1, totalSets)}
          </div>
          <div style="display:flex;align-items:center;justify-content:center;gap:10px;">
            <div>
              <div style="font-size:10px;color:gray;">${t('Reps')}</div>
              <select id="workout-reps"
                style="width:64px;font-size:18px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:5px;background:#fff;">
                ${buildWorkoutRepsOptions(currentSetData.reps)}
              </select>
            </div>
            <div style="font-size:20px;font-weight:bold;color:#ccc;">x</div>
            <div style="position:relative;">
              <div style="font-size:10px;color:gray;">kg</div>
              <input type="number" id="workout-weight" list="workout-weight-options"
                step="any" min="0" max="500" value="${currentSetData.weight}"
                inputmode="decimal"
                style="width:88px;font-size:18px;font-weight:bold;text-align:center;border-radius:8px;padding:5px;background:#fff;border:${currentSetData.weight > 0 ? '1px solid #ddd' : '2px solid #e67e22'};"
                title="${currentSetData.weight > 0 ? '' : t('EnterWeightHint')}">
              <span id="weight-saved-badge" style="position:absolute;right:-6px;top:-2px;background:#28a745;color:#fff;font-size:10px;font-weight:bold;padding:2px 6px;border-radius:10px;opacity:0;transition:opacity .25s;pointer-events:none;">&#10003;</span>
              <datalist id="workout-weight-options">${buildWorkoutWeightOptions(currentSetData.weight)}</datalist>
            </div>
            <button id="plate-calc-btn" title="${t('PlateCalc')}"
              style="align-self:flex-end;width:36px;height:36px;flex-shrink:0;border:1px solid #d4c4f5;border-radius:8px;background:#f3f0fc;font-size:17px;line-height:1;padding:0;cursor:pointer;">🏋️</button>
          </div>
          ${(() => {
            const oneRM = estimate1RM(currentSetData.weight, currentSetData.reps);
            return oneRM > 0
              ? `<div style="text-align:center;margin-top:6px;"><span class="one-rm-badge" title="${t('OneRMTooltip')}">${t('OneRM')}: ${oneRM} kg</span></div>`
              : '';
          })()}
          ${setHint ? `
            <button id="apply-set-hint"
              title="${t('SetHintTooltip')}"
              style="display:block;margin:8px auto 0;border:none;padding:6px 14px;border-radius:20px;font-size:13px;font-weight:600;cursor:pointer;color:#fff;
                background:${setHint.completed ? 'linear-gradient(135deg,#28a745,#20c997)' : 'linear-gradient(135deg,#546e7a,#78909c)'};
                box-shadow:0 2px 6px rgba(0,0,0,0.2);">
              ${t(setHint.completed ? 'SetHintProgress' : 'SetHintRepeat', setHint.lastW, setHint.lastR, setHint.target)}
            </button>
          ` : ''}
          ${(currentSetData.tempoPos > 0 || currentSetData.tempoNeg > 0 || currentSetData.grip) ? `
            <div style="margin-top:8px;display:flex;justify-content:center;gap:8px;flex-wrap:wrap;">
              ${currentSetData.tempoPos > 0 || currentSetData.tempoNeg > 0 ? `
                <div style="display:flex;gap:6px;align-items:center;background:#f3f0fc;padding:4px 10px;border-radius:8px;border:1px solid #d4c4f5;">
                  <span style="font-size:13px;font-weight:700;color:#512BD4;">⏱</span>
                  <span id="tempo-pos-span" style="font-size:13px;font-weight:600;color:#512BD4;">${currentSetData.tempoPos}s ${t('TempoAsc')}</span>
                  <span style="color:#999;font-size:11px;">·</span>
                  <span id="tempo-neg-span" style="font-size:13px;font-weight:600;color:#512BD4;">${currentSetData.tempoNeg}s ${t('TempoDesc')}</span>
                  <button id="tempo-metro-btn" title="${t('TempoMetronome')}"
                    style="background:#512BD4;color:#fff;border:none;border-radius:6px;padding:2px 8px;font-size:12px;font-weight:700;cursor:pointer;">&#9654;</button>
                </div>
              ` : ''}
              ${currentSetData.grip ? `<span style="font-size:12px;font-weight:600;color:#e67e22;background:#fff3e0;padding:4px 10px;border-radius:8px;border:1px solid #ffe0b2;">✊ ${t('Grip')}: ${t('Grip' + currentSetData.grip.charAt(0).toUpperCase() + currentSetData.grip.slice(1).toLowerCase()) || currentSetData.grip}</span>` : ''}
            </div>
          ` : ''}
        </div>
        ${exNotes ? `
          <div id="workout-notes-toggle" style="background:#fff3e0;border-radius:8px;padding:5px 10px;margin:6px auto 0;max-width:300px;text-align:left;cursor:pointer;user-select:none;">
            <div style="font-size:10px;color:#e67e22;font-weight:600;display:flex;align-items:center;justify-content:space-between;">
              <span>&#128221; ${t('ExerciseNotes')}</span>
              <span id="notes-chevron" style="font-size:12px;">&#9660;</span>
            </div>
            <div id="workout-notes-body" style="display:none;font-size:11px;color:#333;white-space:pre-wrap;margin-top:4px;">${escapeHtml(exNotes)}</div>
          </div>
        ` : ''}

        ${(() => {
          // Personal note (mine, not the trainer's): always visible when it exists,
          // otherwise a subtle add button. Tapping either opens the same editor.
          const myNote = myNotes[exName.trim().toLowerCase()] || '';
          return myNote ? `
            <div id="workout-mynote" style="background:#e8f0fe;border:1px solid #b6d0f7;border-radius:8px;padding:5px 10px;margin:6px auto 0;max-width:300px;text-align:left;cursor:pointer;user-select:none;">
              <div style="font-size:10px;color:#1a66c2;font-weight:600;display:flex;align-items:center;justify-content:space-between;gap:6px;">
                <span>&#128221; ${t('MyNote')}</span>
                <span style="font-size:11px;opacity:0.7;">&#9998;</span>
              </div>
              <div style="font-size:11px;color:#333;white-space:pre-wrap;margin-top:3px;">${escapeHtml(myNote)}</div>
            </div>
          ` : `
            <button id="workout-mynote" style="display:block;margin:6px auto 0;background:none;border:1px dashed #b6d0f7;color:#1a66c2;border-radius:8px;padding:4px 14px;font-size:11px;font-weight:600;cursor:pointer;">${t('AddMyNote')}</button>
          `;
        })()}

        <div style="border-top:1px solid #eee;margin-top:6px;padding-top:5px;">
          <div style="display:flex;align-items:center;justify-content:center;gap:6px;flex-wrap:wrap;">
            <span style="font-size:10px;color:#512BD4;font-weight:bold;letter-spacing:1px;">${t('Rest')}</span>
            <div id="timer-display" style="font-size:18px;font-weight:bold;color:#333;background:#f5f5f5;border-radius:8px;padding:2px 8px;">${String(currentMin).padStart(2, '0')}:${String(currentSec).padStart(2, '0')}</div>
            <div id="timer-picker-row" class="flex items-center gap-4" style="font-size:12px;">
              <select id="timer-min" class="picker-select" style="width:46px;font-size:12px;padding:3px 2px;">${minOptions}</select>
              <span style="color:gray;">:</span>
              <select id="timer-sec" class="picker-select" style="width:46px;font-size:12px;padding:3px 2px;">${secOptions}</select>
            </div>
            <button id="timer-start" class="btn btn-sm" style="background:#512BD4;color:#fff;padding:4px 10px;border-radius:8px;font-size:11px;">${t('Start')}</button>
            <button id="timer-reset" class="btn btn-sm" style="background:#6c757d;color:#fff;padding:4px 10px;border-radius:8px;font-size:11px;">${t('Reset')}</button>
          </div>
        </div>
      </div>

      <div style="position:fixed;left:0;right:0;bottom:0;z-index:100;display:grid;grid-template-columns:1fr 1fr;gap:8px;background:var(--bg);padding:8px 20px calc(env(safe-area-inset-bottom, 0px) + 8px);box-shadow:0 -4px 14px rgba(0,0,0,0.12);">
        <button id="workout-prev" class="btn btn-outline" style="padding:10px;background:var(--card-bg);" ${currentIndex === 0 && currentSet === 0 ? 'disabled' : ''}>
          ${currentSet > 0 ? t('PrevSet') : t('Previous')}
        </button>
        ${isLastExercise && isLastSet
          ? `<button id="workout-finish" class="btn btn-success" style="padding:10px;">${t('Finish')}</button>`
          : `
          <div style="display:flex;min-width:0;">
            <button id="workout-next" class="btn btn-primary" style="flex:1;min-width:0;padding:10px 4px;border-radius:10px 0 0 10px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">${isLastSet ? `${t('Next')} →` : `⏱ ${t('NextSet')}`}</button>
            <button id="workout-next-skip" class="btn btn-primary" title="${t('SkipRest')}" aria-label="${t('SkipRest')}" style="width:48px;flex-shrink:0;padding:10px 0;border-radius:0 10px 10px 0;border-left:1px solid rgba(255,255,255,0.4);font-size:16px;">⏭</button>
          </div>`
        }
      </div>
    </div>
  `;

  // ── Event Bindings ──

  document.getElementById('workout-back')?.addEventListener('click', () => {
    if (document.activeElement && typeof document.activeElement.blur === 'function') {
      document.activeElement.blur();
    }
    saveCurrentSetValues();
    saveProgress();
    stopTimer();
    location.hash = '#routines';
  });

  document.getElementById('toggle-exercise-list')?.addEventListener('click', () => {
    showExerciseList = !showExerciseList;
    const panel = document.getElementById('exercise-list-panel');
    const btn = document.getElementById('toggle-exercise-list');
    if (panel) panel.style.display = showExerciseList ? 'block' : 'none';
    if (btn) btn.innerHTML = `${showExerciseList ? '&#9650; ' : '&#9660; '}${t('Exercises')} (${exercises.length})`;
  });

  // Click exercise in list to jump to it
  document.querySelectorAll('[data-go-exercise]').forEach(el => {
    el.addEventListener('click', () => {
      if (document.activeElement && typeof document.activeElement.blur === 'function') {
        document.activeElement.blur();
      }
      saveCurrentSetValues();
      const idx = parseInt(el.dataset.goExercise);
      if (idx >= 0 && idx < exercises.length) {
        currentIndex = idx;
        currentSet = 0;
        saveProgress();
        stopTimer();
        renderExercise();
        window.scrollTo(0, 0);
      }
    });
  });

  document.getElementById('workout-prev')?.addEventListener('click', () => {
    if (document.activeElement && typeof document.activeElement.blur === 'function') {
      document.activeElement.blur();
    }
    saveCurrentSetValues();
    stopTimer();
    const ex2 = exercises[currentIndex];
    const ssGrp = ex2.supersetGroup || 0;

    if (ssGrp > 0) {
      const partnerIdx = exercises.findIndex((e, i) => i !== currentIndex && (e.supersetGroup || 0) === ssGrp);
      if (partnerIdx >= 0) {
        const isFirstInPair = currentIndex < partnerIdx;
        if (isFirstInPair) {
          // On first exercise: go to previous exercise before the pair, or decrement set via partner
          if (currentSet > 0) {
            // Go to partner at previous set (reverse of: partner→first + set++)
            currentIndex = partnerIdx;
            currentSet--;
          } else {
            // Set 0 of first exercise — go to exercise before superset pair
            const minIdx = Math.min(currentIndex, partnerIdx);
            if (minIdx > 0) { currentIndex = minIdx - 1; currentSet = exercises[currentIndex].setDetails.length - 1; }
          }
        } else {
          // On second exercise: go back to first exercise, same set
          currentIndex = partnerIdx;
        }
        saveProgress();
        renderExercise();
        window.scrollTo(0, 0);
        return;
      }
    }

    if (currentSet > 0) { currentSet--; }
    else if (currentIndex > 0) { currentIndex--; currentSet = exercises[currentIndex].setDetails.length - 1; }
    saveProgress();
    renderExercise();
    window.scrollTo(0, 0);
  });

  document.getElementById('workout-next')?.addEventListener('click', () => {
    // Unlock iOS audio INSIDE the tap gesture so the end-of-rest beep can actually play.
    unlockRestAudio();
    // User gestures always allow the wake lock — retry here in case mount's attempt
    // was denied (e.g. the page mounted while the app was still backgrounded).
    acquireWakeLock();
    // Read the configured rest BEFORE advancing — renderExercise() rebuilds the pickers.
    const restSecs = getPickerTotalSeconds();
    // Completing the exercise's LAST set moves on to a NEW exercise: no rest pop-up
    // there (user request) — show the next exercise directly. The countdown only runs
    // BETWEEN sets of the same exercise; a manual rest is always available via Start.
    const exerciseJustCompleted = isLastSet;
    advanceToNext();
    window.scrollTo(0, 0); // new set/exercise always starts at the top of the screen
    // Circular countdown over the freshly rendered next set.
    if (!exerciseJustCompleted) showRestOverlay(restSecs);
  });

  // Right half of the split button: advance WITHOUT the rest countdown (skip rest).
  document.getElementById('workout-next-skip')?.addEventListener('click', () => {
    acquireWakeLock();
    advanceToNext();
    window.scrollTo(0, 0);
  });

  document.getElementById('workout-switch-partner')?.addEventListener('click', () => switchSupersetPartner());

  document.getElementById('workout-exercise-image')?.addEventListener('click', () => pickAndUploadExerciseImage());

  document.getElementById('prefill-dismiss')?.addEventListener('click', () => {
    prefillSource = null;
    document.getElementById('prefill-banner')?.remove();
  });

  document.getElementById('workout-tips-btn')?.addEventListener('click', async () => {
    const ex = exercises[currentIndex];
    if (!ex) return;
    const exId = ex.exerciseId || ex.ExerciseId || ex.id || ex.Id;
    showFormTipsModal(exTranslate(ex.exerciseName || ex.name || ''), null, true); // loading
    try {
      const res = await api.get(`/ai/exercise-form/${exId}`);
      let parsed = null;
      try { parsed = res?.notes ? JSON.parse(res.notes) : null; } catch { /* not JSON */ }
      showFormTipsModal(exTranslate(ex.exerciseName || ex.name || ''), parsed || res?.notes || '', false);
    } catch (err) {
      showFormTipsModal(exTranslate(ex.exerciseName || ex.name || ''), { error: err.message || 'Error' }, false);
    }
  });

  document.getElementById('workout-video-btn')?.addEventListener('click', () => {
    const ex = exercises[currentIndex];
    if (!ex) return;
    const exId = ex.exerciseId || ex.ExerciseId || ex.id || ex.Id;
    const currentUrl = ex.videoUrl || ex.VideoUrl || '';
    showVideoModal({
      url: currentUrl,
      title: exTranslate(ex.exerciseName || ex.name || ''),
      editable: true,
      onSave: async (newUrl) => {
        try {
          const res = await api.put(`/exercises/${exId}/video`, { videoUrl: newUrl });
          const updated = res.videoUrl || res.VideoUrl || '';
          // Update all references in memory (could appear twice for supersets)
          for (const e of exercises) {
            if ((e.exerciseId || e.ExerciseId || e.id || e.Id) === exId) {
              e.videoUrl = updated;
              e.VideoUrl = updated;
            }
          }
          renderExercise();
        } catch (err) { console.warn('Failed to save video URL', err); }
      }
    });
  });

  document.getElementById('apply-set-hint')?.addEventListener('click', () => {
    const ex = exercises[currentIndex];
    if (!ex) return;
    const hint = suggestSetWeight(ex, currentSet);
    if (!hint) return;
    // Apply the suggested weight to THIS set only (each set progresses on its own).
    if (ex.setDetails[currentSet]) ex.setDetails[currentSet].weight = hint.target;
    haptic('success');
    saveProgress();
    renderExercise();
  });

  document.getElementById('workout-finish')?.addEventListener('click', () => { saveCurrentSetValues(); finishWorkout(); });

  // Auto-save on weight/reps input change. The weight input uses a datalist so users can
  // either tap from suggestions (0.25 kg steps up to 150 kg) or just type a custom value.
  document.getElementById('workout-weight')?.addEventListener('change', () => { saveCurrentSetValues(); saveProgress(); });
  document.getElementById('workout-weight')?.addEventListener('input', () => { saveCurrentSetValues(); saveProgress(); });
  // Extra safety net: when the user moves focus away from the weight input (tap on another
  // control, soft keyboard dismiss), force a final save. The change/input events already
  // cover most paths but mobile browsers sometimes skip `change` when the user just blurs.
  document.getElementById('workout-weight')?.addEventListener('blur', () => { saveCurrentSetValues(); saveProgress(); });
  document.getElementById('workout-reps')?.addEventListener('change', () => { saveCurrentSetValues(); saveProgress(); });

  document.getElementById('workout-notes-toggle')?.addEventListener('click', () => {
    const body = document.getElementById('workout-notes-body');
    const chevron = document.getElementById('notes-chevron');
    if (body) {
      const show = body.style.display === 'none';
      body.style.display = show ? 'block' : 'none';
      if (chevron) chevron.innerHTML = show ? '&#9650;' : '&#9660;';
    }
  });

  // Timer controls are recreated on every renderExercise() call. We attach listeners by
  // ID lookup which is fine on a fresh DOM, but if anything kept a reference to the OLD
  // element (e.g. listeners that were never garbage-collected), pressing Start would fire
  // multiple intervals. To be defensive, ensure any running timer is cleared FIRST so a
  // stale callback can't fire and increment a second interval.
  stopTimer();
  stopTempoMetronome(); // fresh DOM — the old metronome's highlighted spans are gone

  document.getElementById('workout-history-btn')?.addEventListener('click', showExerciseHistoryModal);
  document.getElementById('tempo-metro-btn')?.addEventListener('click', toggleTempoMetronome);
  document.getElementById('plate-calc-btn')?.addEventListener('click', () => {
    saveCurrentSetValues(); // commit whatever is typed in the weight input first
    showPlateCalcModal();
  });
  document.getElementById('workout-mynote')?.addEventListener('click', showMyNoteModal);

  document.getElementById('timer-start')?.addEventListener('click', onTimerStartClicked);
  document.getElementById('timer-reset')?.addEventListener('click', onTimerResetClicked);
  document.getElementById('timer-min')?.addEventListener('change', onTimePickerChanged);
  document.getElementById('timer-sec')?.addEventListener('change', onTimePickerChanged);

  resetTimerDisplay();
}

function buildWorkoutWeightOptions(selected) {
  // Suggestions for the weight datalist: 0, 0.25, 0.5 ... 150 plus the current value if outside this range.
  const vals = [0];
  for (let i = 0.25; i <= 150; i += 0.25) vals.push(i);
  if (selected > 0 && !vals.includes(selected)) { vals.push(selected); vals.sort((a, b) => a - b); }
  return vals.map(v => `<option value="${v}">`).join('');
}

function buildWorkoutRepsOptions(selected) {
  const vals = [];
  for (let i = 1; i <= 50; i++) vals.push(i);
  if (selected > 0 && !vals.includes(selected)) { vals.push(selected); vals.sort((a, b) => a - b); }
  return vals.map(v => `<option value="${v}" ${v === selected ? 'selected' : ''}>${v}</option>`).join('');
}

/**
 * Suggests a weight increment based on the user's recent performance on this exercise.
 * Heuristic:
 * - if last workout completed every set at the target reps (or above) → suggest +2.5kg compound / +1.25kg isolation.
 * - if reps high in every set (>= target+2) → suggest +5kg compound / +2.5kg isolation.
 * - otherwise → no suggestion.
 * Returns { delta: number, newWeight: number } or null.
 */
/**
 * Per-set double-progression hint based on the LAST recorded session of this exercise
 * (raw history captured by the prefill into `_lastSets`):
 *  - completed the planned reps at W → suggest W + increment ("prueba 42.5");
 *  - fell short of the planned reps  → suggest repeating W ("repite 40").
 * Increment: 1 kg below 15 kg (small dumbbells), else 2.5 kg.
 * Returns { lastW, lastR, completed, target } or null when there's no usable history.
 */
function suggestSetWeight(exercise, setIdx) {
  const last = exercise?._lastSets;
  if (!Array.isArray(last) || last.length === 0) return null;
  const lastSet = setIdx < last.length ? last[setIdx] : last[last.length - 1];
  const lastW = parseFloat(lastSet?.weight) || 0;
  const lastR = parseInt(lastSet?.reps) || 0;
  if (lastW <= 0 || lastR <= 0) return null;
  const planReps = exercise.setDetails?.[setIdx]?.reps || 0;
  if (planReps <= 0) return null;
  const completed = lastR >= planReps;
  const increment = lastW < 15 ? 1 : 2.5;
  const target = completed ? Math.round((lastW + increment) * 100) / 100 : lastW;
  return { lastW, lastR, completed, target };
}

/**
 * Suggests a rest time in seconds based on the exercise name.
 * Compound heavy (squat, bench, deadlift): 180s
 * Compound medium (press militar, remo, dominada): 120s
 * Isolation / arms / cardio: 60s
 * Defaults to 60s if nothing matches.
 */
function suggestRestSeconds(exercise) {
  const name = (exercise?.exerciseName || exercise?.name || '').toLowerCase();
  if (['sentadilla', 'peso muerto', 'press banca', 'prensa', 'hack'].some(k => name.includes(k))) return 180;
  if (['press militar', 'remo', 'dominada', 'jalón', 'jalon', 'hip thrust', 'sentadilla búlgara', 'sentadilla bulgara', 'zancada'].some(k => name.includes(k))) return 120;
  if (['superserie', 'super serie'].some(k => name.includes(k))) return 90;
  return 60;
}

function saveCurrentSetValues() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  const sd = ex.setDetails[currentSet];
  if (!sd) return;

  const repsEl = document.getElementById('workout-reps');
  if (repsEl) {
    const reps = parseInt(repsEl.value, 10);
    if (Number.isFinite(reps) && reps > 0) sd.reps = reps;
  }

  // Weight — read using THREE strategies, in order, and accept the first that yields a
  // valid non-empty number. Mobile browsers (especially iOS Safari with es-ES locale)
  // have three known failure modes:
  //   1) input.value comes back with a comma decimal ("7,5") → parseFloat truncates.
  //   2) A restrictive `step` attribute (was step="0.25") can leave `value=""` when the
  //      typed number doesn't fit — the user sees the digits on screen but JS sees "".
  //   3) `input` events sometimes lag behind rapid taps on iOS, so the last keystroke
  //      hasn't been committed when the user presses "Sig. serie".
  // valueAsNumber bypasses the string-parsing step entirely and gives back the parsed
  // number the browser is holding internally — locale-agnostic and unaffected by (1).
  const weightEl = document.getElementById('workout-weight');
  if (weightEl) {
    let weight = null;

    // Strategy 1: valueAsNumber (locale-independent, browser-parsed).
    const asNum = weightEl.valueAsNumber;
    if (Number.isFinite(asNum) && asNum >= 0) weight = asNum;

    // Strategy 2: the visible string with comma→dot normalisation.
    if (weight === null) {
      const raw = (weightEl.value ?? '').trim().replace(',', '.');
      if (raw !== '') {
        const parsed = parseFloat(raw);
        if (Number.isFinite(parsed) && parsed >= 0) weight = parsed;
      }
    }

    // Strategy 3: last-resort — pull digits and a decimal separator from the raw string.
    // Covers weird iOS states where valueAsNumber is NaN AND value has extra characters.
    if (weight === null) {
      const raw = (weightEl.value ?? '').replace(',', '.');
      const match = raw.match(/-?\d+(?:\.\d+)?/);
      if (match) {
        const parsed = parseFloat(match[0]);
        if (Number.isFinite(parsed) && parsed >= 0) weight = parsed;
      }
    }

    if (weight !== null) {
      const changed = sd.weight !== weight;
      sd.weight = weight;
      if (changed && weight > 0) flashWeightSaved();
    }
  }
}

let _weightSavedTimer = null;
function flashWeightSaved() {
  const badge = document.getElementById('weight-saved-badge');
  if (!badge) return;
  badge.style.opacity = '1';
  if (_weightSavedTimer) clearTimeout(_weightSavedTimer);
  _weightSavedTimer = setTimeout(() => { badge.style.opacity = '0'; }, 1200);
}

/**
 * Advances to the next set/exercise. Used by the Next button AND by the
 * rest-timer auto-advance. Handles superset alternation transparently.
 */
function advanceToNext() {
  // Force the focused input to commit its value before we read it. Without this, iOS
  // Safari can hold the last keystroke in an internal buffer and `weightEl.value` still
  // shows the previous value when we call saveCurrentSetValues immediately.
  if (document.activeElement && typeof document.activeElement.blur === 'function') {
    document.activeElement.blur();
  }
  saveCurrentSetValues();
  checkLivePR();
  stopTimer();
  haptic('success');
  const ex2 = exercises[currentIndex];
  if (!ex2) return;
  const ssGrp = ex2.supersetGroup || 0;

  if (ssGrp > 0) {
    const partnerIdx = exercises.findIndex((e, i) => i !== currentIndex && (e.supersetGroup || 0) === ssGrp);
    if (partnerIdx >= 0) {
      const isFirstInPair = currentIndex < partnerIdx;
      if (isFirstInPair) {
        // After the FIRST partner's set, hop to the SECOND partner at the same set index.
        // If the second partner has fewer sets, clamp to its last set. Don't increment yet.
        currentIndex = partnerIdx;
        if (currentSet >= exercises[partnerIdx].setDetails.length) currentSet = exercises[partnerIdx].setDetails.length - 1;
      } else {
        // After the SECOND partner's set, go back to the FIRST partner at the NEXT set.
        // If both partners have finished all their sets, exit the pair and move past it.
        const origIdx = currentIndex;
        const firstIdx = partnerIdx;
        const nextSet = currentSet + 1;
        const firstDone = nextSet >= exercises[firstIdx].setDetails.length;
        const secondDone = nextSet >= exercises[origIdx].setDetails.length;
        // The pair completes when neither partner has another set to do.
        if (firstDone && secondDone) {
          const pairLastIdx = Math.max(origIdx, firstIdx);
          if (pairLastIdx + 1 < exercises.length) {
            currentIndex = pairLastIdx + 1;
            currentSet = 0;
          } else {
            // No exercises after the pair — stay on the last partner's last set.
            // The user finalizes by pressing the "Finalizar" button; pressing Next again
            // must NOT loop back into the pair.
            currentIndex = origIdx;
            currentSet = exercises[origIdx].setDetails.length - 1;
          }
        } else if (firstDone) {
          // First partner has no sets left — keep doing sets of the second partner.
          currentIndex = origIdx;
          currentSet = nextSet;
        } else {
          // Normal alternation: jump back to the first partner at the incremented set.
          currentIndex = firstIdx;
          currentSet = nextSet;
        }
      }
      saveProgress();
      renderExercise();
      return;
    }
  }

  if (currentSet < ex2.setDetails.length - 1) { currentSet++; }
  else if (currentIndex < exercises.length - 1) { currentIndex++; currentSet = 0; }
  saveProgress();
  renderExercise();
}

/** Switches to the partner exercise of the current superset, keeping the same set index. */
function switchSupersetPartner() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  const ssGrp = ex.supersetGroup || 0;
  if (ssGrp <= 0) return;
  const partnerIdx = exercises.findIndex((e, i) => i !== currentIndex && (e.supersetGroup || 0) === ssGrp);
  if (partnerIdx < 0) return;
  // Blur before save so the input's last keystroke is committed. This was the missing
  // piece for the "los pesos del segundo ejercicio de la super serie no se registran"
  // bug — the user typed weight for the partner then tapped the swap button before iOS
  // fired the `input` event, so we saved nothing.
  if (document.activeElement && typeof document.activeElement.blur === 'function') {
    document.activeElement.blur();
  }
  saveCurrentSetValues();
  stopTimer();
  currentIndex = partnerIdx;
  // Clamp set to valid range for the partner
  if (currentSet >= exercises[partnerIdx].setDetails.length) currentSet = exercises[partnerIdx].setDetails.length - 1;
  saveProgress();
  renderExercise();
}

// ── Timer ──

function getPickerTotalSeconds() {
  const minEl = document.getElementById('timer-min');
  const secEl = document.getElementById('timer-sec');
  return (minEl ? parseInt(minEl.value) || 0 : 1) * 60 + (secEl ? parseInt(secEl.value) || 0 : 30);
}

function onTimePickerChanged() {
  if (!timerRunning) { timerSeconds = getPickerTotalSeconds(); updateTimerDisplay(); }
}

function onTimerStartClicked() {
  // The embedded inline countdown is gone: Start now launches the SAME blocking circular
  // overlay used after each set (manual extra rest), with audio unlocked by this tap.
  // It does NOT auto-advance — the user is resting on the set they're about to do.
  unlockRestAudio();
  const total = getPickerTotalSeconds();
  if (total <= 0) return;
  showRestOverlay(total);
}

function onTimerResetClicked() { stopTimer(); resetTimerDisplay(); }

function stopTimer() {
  timerRunning = false;
  timerEndsAt = 0;
  if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }
}

// ── Screen wake lock ──
// Keeps the screen on for the whole workout (iOS 16.4+ / Android). Without it the phone
// dims mid-rest and the end-of-rest beep never plays. The OS releases the lock whenever
// the app goes to background, so we re-acquire on every visibilitychange and on each tap.
let screenWakeLock = null;

async function acquireWakeLock() {
  try {
    if (!('wakeLock' in navigator) || document.hidden || screenWakeLock) return;
    screenWakeLock = await navigator.wakeLock.request('screen');
    screenWakeLock.addEventListener('release', () => { screenWakeLock = null; });
  } catch { screenWakeLock = null; /* e.g. low-power mode denies it — fail quietly */ }
}

function releaseWakeLock() {
  try { screenWakeLock?.release(); } catch { /* already released */ }
  screenWakeLock = null;
}

// ── Privacy guard ──
// The wake lock is a convenience, not a commitment: an unlocked always-on phone left
// on a gym bench is a gift to whoever walks past. If the user hasn't touched the page
// for WAKE_IDLE_LIMIT_MS and no rest countdown is running, release the lock so the
// iPhone's own auto-lock takes over and the phone ends up behind Face ID. Any touch
// re-arms both the timestamp and the lock, so during normal training (tap every
// couple of minutes) the screen never dims.
const WAKE_IDLE_LIMIT_MS = 3 * 60 * 1000;
let lastInteractionAt = Date.now();

function noteInteraction() {
  lastInteractionAt = Date.now();
  acquireWakeLock();
}

function enforceWakeIdleLimit() {
  if (document.getElementById('rest-overlay')) return; // resting — beeps need the screen on
  if (Date.now() - lastInteractionAt > WAKE_IDLE_LIMIT_MS) releaseWakeLock();
}

/** Shared 15 s tick: defensive autosave + the wake-lock privacy guard. */
function autosaveTick() {
  saveCurrentSetValues();
  saveProgress();
  enforceWakeIdleLimit();
}

/**
 * Called when the document becomes visible again after the user backgrounded the app.
 * Reconciles `timerSeconds` against the wall clock so the display catches up instantly
 * (the setInterval tick might have been throttled to once per minute on mobile).
 * If the timer already expired in the background, we fire the rest-end logic now.
 */
function onVisibilityChange() {
  if (document.hidden) return;
  // Coming back to the app is a user action: re-arm the privacy-guard timestamp too.
  lastInteractionAt = Date.now();
  acquireWakeLock(); // the OS dropped the lock while backgrounded — grab it again
  // iOS marks the AudioContext interrupted while backgrounded; wake it now so the
  // next tick/beep doesn't have to pay the async-resume cost at its scheduled time.
  try { if (restAudioCtx && restAudioCtx.state !== 'running') restAudioCtx.resume(); } catch { /* */ }
  if (!timerRunning || timerEndsAt <= 0) return;
  const remaining = Math.ceil((timerEndsAt - Date.now()) / 1000);
  if (remaining <= 0) {
    timerSeconds = 0;
    updateTimerDisplay();
    stopTimer();
    const startBtn = document.getElementById('timer-start');
    const pickerRow = document.getElementById('timer-picker-row');
    if (startBtn) { startBtn.textContent = t('Start'); startBtn.style.background = '#512BD4'; }
    if (pickerRow) pickerRow.style.display = '';
    const display = document.getElementById('timer-display');
    if (display) display.style.color = '#28a745';
    playRestEndSound();
    showRestEndAlert();
    advanceToNext();
  } else {
    timerSeconds = remaining;
    updateTimerDisplay();
  }
}

function resetTimerDisplay() {
  timerSeconds = getPickerTotalSeconds();
  const startBtn = document.getElementById('timer-start');
  const pickerRow = document.getElementById('timer-picker-row');
  const display = document.getElementById('timer-display');
  if (startBtn) { startBtn.textContent = t('Start'); startBtn.style.background = '#512BD4'; }
  if (pickerRow) pickerRow.style.display = '';
  if (display) display.style.color = '#333';
  updateTimerDisplay();
}

// Single shared AudioContext, created/resumed INSIDE a user gesture. iOS Safari mutes
// any context born outside a tap: the old code built a fresh context when the countdown
// hit 0 (no gesture in sight) and the beeps were silently dropped on iPhone.
let restAudioCtx = null;

function unlockRestAudio() {
  try {
    if (!restAudioCtx) restAudioCtx = new (window.AudioContext || window.webkitAudioContext)();
    if (restAudioCtx.state === 'suspended') restAudioCtx.resume();
    // Playing a silent 1-sample buffer inside the gesture fully unlocks audio output.
    const buf = restAudioCtx.createBuffer(1, 1, 22050);
    const src = restAudioCtx.createBufferSource();
    src.buffer = buf; src.connect(restAudioCtx.destination); src.start(0);
  } catch { /* audio not available */ }
}

function restBeep(freq, when = 0, duration = 250, volume = 0.5) {
  try {
    const ctx = restAudioCtx || new (window.AudioContext || window.webkitAudioContext)();
    if (!restAudioCtx) restAudioCtx = ctx;
    const schedule = () => {
      try {
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.frequency.value = freq;
        osc.type = 'sine';
        gain.gain.setValueAtTime(0.0001, ctx.currentTime + when);
        gain.gain.exponentialRampToValueAtTime(volume, ctx.currentTime + when + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + when + duration / 1000);
        osc.connect(gain); gain.connect(ctx.destination);
        osc.start(ctx.currentTime + when);
        osc.stop(ctx.currentTime + when + duration / 1000 + 0.05);
      } catch { /* audio not available */ }
    };
    // resume() is ASYNC: scheduling against currentTime while still suspended places the
    // beep at an instant that has already passed when the context finally wakes — that
    // beep is silently dropped (iOS suspends the ctx on screen lock / backgrounding).
    if (ctx.state === 'running') schedule();
    else ctx.resume().then(schedule).catch(() => { /* audio not available */ });
  } catch { /* audio not available */ }
}

// ── Bluetooth keepalive ──
// Wireless earbuds power their audio link down after a few seconds of silence. A lone
// 120–350 ms beep after a minute-long quiet rest falls entirely inside the ~1 s the
// link needs to wake back up — the user hears NOTHING (reported on iPhone + BT buds).
// Fix: play an inaudible low tone for the whole rest so the stream never closes and
// every tick/beep is delivered from its first millisecond.
let restKeepaliveNodes = null;
let restKeepaliveStopTimer = null;

function startRestKeepalive() {
  try {
    if (!restAudioCtx) return;
    if (restKeepaliveStopTimer) { clearTimeout(restKeepaliveStopTimer); restKeepaliveStopTimer = null; }
    if (restKeepaliveNodes) return;
    const ctx = restAudioCtx;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.frequency.value = 50;   // below earbud response at this level — inaudible
    gain.gain.value = 0.004;
    osc.connect(gain); gain.connect(ctx.destination);
    osc.start();
    restKeepaliveNodes = { osc, gain };
  } catch { /* audio not available */ }
}

function stopRestKeepalive(delayMs = 0) {
  if (restKeepaliveStopTimer) { clearTimeout(restKeepaliveStopTimer); restKeepaliveStopTimer = null; }
  const doStop = () => {
    restKeepaliveStopTimer = null;
    try { restKeepaliveNodes?.osc.stop(); } catch { /* already stopped */ }
    try { restKeepaliveNodes?.osc.disconnect(); restKeepaliveNodes?.gain.disconnect(); } catch { /* */ }
    restKeepaliveNodes = null;
  };
  if (delayMs > 0) restKeepaliveStopTimer = setTimeout(doStop, delayMs);
  else doStop();
}

function playRestEndSound() {
  // Three beeps, last one higher — clearly audible over gym noise / earbuds.
  restBeep(880, 0);
  restBeep(880, 0.35);
  restBeep(1320, 0.7, 350, 0.6);
}

// ── Blocking rest overlay ──
// Full-screen circular countdown shown after each completed set. It cannot be dismissed:
// the next set only unlocks when the ring reaches 0 (then it beeps + vibrates and closes).
let restOverlayInterval = null;

function closeRestOverlay() {
  if (restOverlayInterval) { clearInterval(restOverlayInterval); restOverlayInterval = null; }
  document.getElementById('rest-overlay')?.remove();
  // The set starts now — give the user the full idle window before the privacy guard
  // may release the wake lock (long sets shouldn't end with a dimmed screen).
  lastInteractionAt = Date.now();
}

// ── Tempo metronome ──
// Marks the prescribed rep rhythm (e.g. 2s positive / 3s negative) with a high beep at
// the start of the lifting phase and a low one at the start of the lowering phase,
// looping until stopped. Audio goes through the gesture-unlocked shared context.
let tempoMetroInterval = null;
let tempoMetroPhase = null;

function stopTempoMetronome() {
  if (tempoMetroInterval) { clearInterval(tempoMetroInterval); tempoMetroInterval = null; }
  tempoMetroPhase = null;
  const btn = document.getElementById('tempo-metro-btn');
  if (btn) btn.innerHTML = '&#9654;';
  const posEl = document.getElementById('tempo-pos-span');
  const negEl = document.getElementById('tempo-neg-span');
  if (posEl) posEl.style.cssText = 'font-size:13px;font-weight:600;color:#512BD4;';
  if (negEl) negEl.style.cssText = 'font-size:13px;font-weight:600;color:#512BD4;';
}

function toggleTempoMetronome() {
  if (tempoMetroInterval) { stopTempoMetronome(); return; }
  const ex = exercises[currentIndex];
  const sd = ex?.setDetails?.[currentSet];
  const pos = sd?.tempoPos || 0;
  const neg = sd?.tempoNeg || 0;
  if (pos + neg <= 0) return;
  unlockRestAudio(); // we're inside the tap — unlock so the phase beeps can play on iOS
  const btn = document.getElementById('tempo-metro-btn');
  if (btn) btn.innerHTML = '&#9632;';
  const cycleMs = (pos + neg) * 1000;
  const startedAt = Date.now();
  tempoMetroPhase = null;
  const tick = () => {
    const tMs = (Date.now() - startedAt) % cycleMs;
    const phase = tMs < pos * 1000 ? 'up' : 'down';
    if (phase === tempoMetroPhase) return;
    tempoMetroPhase = phase;
    restBeep(phase === 'up' ? 990 : 550, 0, phase === 'up' ? 120 : 200, 0.35);
    try { navigator.vibrate?.(phase === 'up' ? 40 : [40, 50, 40]); } catch { /* */ }
    const posEl = document.getElementById('tempo-pos-span');
    const negEl = document.getElementById('tempo-neg-span');
    if (posEl) posEl.style.cssText = phase === 'up'
      ? 'font-size:13px;font-weight:800;color:#28a745;' : 'font-size:13px;font-weight:600;color:#512BD4;';
    if (negEl) negEl.style.cssText = phase === 'down'
      ? 'font-size:13px;font-weight:800;color:#e67e22;' : 'font-size:13px;font-weight:600;color:#512BD4;';
  };
  tick();
  tempoMetroInterval = setInterval(tick, 60);
}

// ── Exercise history modal ──
// Last sessions of the current exercise, set by set — so the weight can be chosen with
// real context instead of only the single prefit value.
async function showExerciseHistoryModal() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  const exId = ex.exerciseId || ex.ExerciseId || ex.id || ex.Id;
  const exName = ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '';

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.innerHTML = `
    <div class="modal-content" style="max-width:420px;">
      <div class="modal-header">
        <div class="modal-title">&#128202; ${t('ExerciseHistory')}</div>
        <button class="modal-close" id="exhist-close">&times;</button>
      </div>
      <div style="font-size:13px;font-weight:600;margin-bottom:8px;">${escapeHtml(exTranslate(exName))}</div>
      <div id="exhist-body" style="font-size:13px;max-height:55vh;overflow-y:auto;">
        <div class="spinner" style="margin:12px auto;"></div>
      </div>
    </div>
  `;
  document.body.appendChild(overlay);
  overlay.querySelector('#exhist-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });

  try {
    const rows = await api.get(`/workouts/exercise/${exId}/history?name=${encodeURIComponent(exName)}`);
    const body = overlay.querySelector('#exhist-body');
    if (!body) return;
    if (!Array.isArray(rows) || rows.length === 0) {
      body.innerHTML = `<div style="color:gray;text-align:center;padding:8px 0;">${t('NoHistoryYet')}</div>`;
      return;
    }
    // Sparkline of the max weight per session (oldest → newest) so the trend is
    // visible at a glance before the per-session breakdown.
    const maxPerSession = rows.map(r => {
      let maxW = Number(r.weight) || 0;
      try {
        const sets = r.setDetails ? JSON.parse(r.setDetails) : [];
        if (Array.isArray(sets)) sets.forEach(s => { const sw = Number(s.weight) || 0; if (sw > maxW) maxW = sw; });
      } catch { /* legacy row */ }
      return maxW;
    }).reverse();
    let sparkline = '';
    if (rows.length >= 2 && maxPerSession.some(v => v > 0)) {
      const W = 300, H = 70, PAD = 14;
      const maxV = Math.max(...maxPerSession, 1);
      const minV = Math.min(...maxPerSession);
      const range = maxV - minV || 1;
      const pts = maxPerSession.map((v, i) => ({
        x: PAD + (i / (maxPerSession.length - 1)) * (W - PAD * 2),
        y: PAD + (H - PAD * 2) - ((v - minV) / range) * (H - PAD * 2),
        v,
      }));
      const poly = pts.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
      const circles = pts.map(p => `<circle cx="${p.x.toFixed(1)}" cy="${p.y.toFixed(1)}" r="3.5" fill="#512BD4"/>`).join('');
      const first = pts[0], last = pts[pts.length - 1];
      const trendUp = last.v > first.v;
      sparkline = `
        <div style="margin-bottom:8px;padding:6px 4px;background:rgba(81,43,212,0.06);border-radius:10px;">
          <svg width="100%" viewBox="0 0 ${W} ${H}" style="display:block;">
            <polyline points="${poly}" fill="none" stroke="${trendUp ? '#28a745' : '#512BD4'}" stroke-width="2.5" stroke-linejoin="round"/>
            ${circles}
            <text x="${first.x.toFixed(1)}" y="${Math.max(first.y - 7, 9).toFixed(1)}" text-anchor="start" font-size="10" font-weight="700" fill="currentColor" opacity="0.8">${first.v}</text>
            <text x="${last.x.toFixed(1)}" y="${Math.max(last.y - 7, 9).toFixed(1)}" text-anchor="end" font-size="10" font-weight="700" fill="currentColor" opacity="0.8">${last.v}</text>
          </svg>
        </div>
      `;
    }

    body.innerHTML = sparkline + rows.map(r => {
      let sets = [];
      try { sets = r.setDetails ? JSON.parse(r.setDetails) : []; } catch { /* legacy row */ }
      const chips = (sets.length > 0 ? sets : [{ weight: r.weight, reps: r.reps }]).map(s =>
        `<span style="white-space:nowrap;background:#f3f0fc;color:#512BD4;border-radius:6px;padding:2px 7px;font-weight:600;">${(parseFloat(s.weight) || 0) > 0 ? s.weight : '–'} kg × ${s.reps}</span>`
      ).join(' ');
      const d = new Date(r.date);
      return `
        <div style="padding:7px 0;border-bottom:1px solid #eee;">
          <div style="font-size:11px;color:gray;margin-bottom:3px;">${dayName(d.getDay())} ${d.toLocaleDateString()}</div>
          <div style="display:flex;gap:4px;flex-wrap:wrap;">${chips}</div>
        </div>
      `;
    }).join('');
  } catch (err) {
    const body = overlay.querySelector('#exhist-body');
    if (body) body.innerHTML = `<div style="color:#dc3545;">${t('ErrorFmt', err.message)}</div>`;
  }
}

function showRestOverlay(totalSeconds) {
  totalSeconds = Math.max(0, Math.floor(totalSeconds || 0));
  if (totalSeconds <= 0) return;
  closeRestOverlay();
  stopTempoMetronome(); // the set is over — the rep rhythm must not keep beeping

  const RADIUS = 95;
  const CIRC = 2 * Math.PI * RADIUS;

  // "Up next" card: by the time the overlay shows, advanceToNext() has already moved
  // currentIndex/currentSet to the UPCOMING set (and on a manual Start rest, the current
  // set IS the one about to be done) — so this always describes what comes after the rest.
  let nextCard = '';
  const nx = exercises[currentIndex];
  const nxSet = nx?.setDetails?.[currentSet];
  if (nxSet) {
    const nxName = exTranslate(nx.exerciseName || nx.ExerciseName || nx.name || nx.Name || '');
    const w = Number(nxSet.weight) || 0;
    const line = w > 0
      ? `${nxSet.reps} <span style="font-size:17px;font-weight:600;color:rgba(255,255,255,0.55);">reps ×</span> ${w} <span style="font-size:17px;font-weight:600;color:rgba(255,255,255,0.55);">kg</span>`
      : `${nxSet.reps} <span style="font-size:17px;font-weight:600;color:rgba(255,255,255,0.55);">reps</span>`;
    nextCard = `
      <div style="background:rgba(139,114,255,0.14);border:1px solid rgba(139,114,255,0.45);border-radius:14px;padding:10px 20px;text-align:center;max-width:300px;">
        <div style="font-size:11px;color:#b3a3ff;letter-spacing:1.5px;text-transform:uppercase;font-weight:700;">${t('NextUp')} · ${t('SetN', currentSet + 1, nx.setDetails.length)}</div>
        <div style="font-size:14px;font-weight:600;color:rgba(255,255,255,0.85);margin-top:4px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">${escapeHtml(nxName)}</div>
        <div style="font-size:26px;font-weight:800;color:#fff;margin-top:2px;line-height:1.1;font-variant-numeric:tabular-nums;">${line}</div>
      </div>`;
  }

  const overlay = document.createElement('div');
  overlay.id = 'rest-overlay';
  overlay.setAttribute('role', 'timer');
  overlay.setAttribute('aria-live', 'off');
  overlay.style.cssText = `
    position:fixed; inset:0; z-index:10000;
    background:rgba(10,10,16,0.93); -webkit-backdrop-filter:blur(6px); backdrop-filter:blur(6px);
    display:flex; align-items:center; justify-content:center;
    touch-action:none; overscroll-behavior:contain;
  `;
  overlay.innerHTML = `
    <div style="display:flex;flex-direction:column;align-items:center;gap:18px;padding:16px;">
      <div style="position:relative;width:240px;height:240px;">
        <svg width="240" height="240" viewBox="0 0 220 220" style="transform:rotate(-90deg);display:block;">
          <circle cx="110" cy="110" r="${RADIUS}" fill="none" stroke="rgba(255,255,255,0.14)" stroke-width="13"/>
          <circle id="rest-ring" cx="110" cy="110" r="${RADIUS}" fill="none" stroke="#8b72ff" stroke-width="13"
            stroke-linecap="round" stroke-dasharray="${CIRC}" stroke-dashoffset="0"/>
        </svg>
        <div style="position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;transform:none;">
          <div id="rest-count" style="font-size:64px;font-weight:800;color:#fff;line-height:1;font-variant-numeric:tabular-nums;">${totalSeconds}</div>
          <div style="font-size:13px;color:rgba(255,255,255,0.65);letter-spacing:2px;text-transform:uppercase;margin-top:6px;">${t('RestOverlayLabel')}</div>
        </div>
      </div>
      ${nextCard}
      <div style="font-size:13px;color:rgba(255,255,255,0.5);text-align:center;max-width:260px;">${t('RestOverlayHint')}</div>
      <button id="rest-skip-btn" style="background:rgba(255,255,255,0.1);border:1px solid rgba(255,255,255,0.35);color:rgba(255,255,255,0.9);border-radius:12px;padding:10px 22px;font-size:14px;font-weight:700;cursor:pointer;">${t('RestSkipBtn')}</button>
    </div>
  `;
  document.body.appendChild(overlay);

  // Early exit back to the set — no end beeps, no green banner, just close and lift.
  overlay.querySelector('#rest-skip-btn')?.addEventListener('click', () => {
    closeRestOverlay();
    stopRestKeepalive();
    haptic('tap');
  });

  // Both call sites run inside a tap, so the context is unlocked; the keepalive holds
  // the Bluetooth audio link open for the whole rest so the ticks and end beeps are
  // actually heard on wireless earbuds (they drop the first ~1 s after silence).
  unlockRestAudio();
  startRestKeepalive();

  const endsAt = Date.now() + totalSeconds * 1000;
  const numEl = overlay.querySelector('#rest-count');
  const ringEl = overlay.querySelector('#rest-ring');
  let lastShown = totalSeconds;

  // Wall-clock based tick: survives background throttling — on foreground resume the
  // very next tick recomputes from Date.now() and, if the rest already expired, closes.
  const tick = () => {
    const msLeft = endsAt - Date.now();
    const secsLeft = Math.max(0, Math.ceil(msLeft / 1000));
    if (numEl && secsLeft !== lastShown) {
      numEl.textContent = String(secsLeft);
      // Final 4 seconds: turn amber and tick audibly so the end doesn't surprise.
      if (secsLeft > 0 && secsLeft <= 4) {
        numEl.style.color = '#ffb020';
        restBeep(440, 0, 160, 0.35);
      }
      lastShown = secsLeft;
    }
    if (ringEl) {
      const frac = Math.min(1, Math.max(0, msLeft / (totalSeconds * 1000)));
      ringEl.style.strokeDashoffset = String(CIRC * (1 - frac));
      if (msLeft / 1000 <= 4) ringEl.style.stroke = '#ffb020';
    }
    if (msLeft <= 0) {
      closeRestOverlay();
      playRestEndSound();
      // Keep the BT stream open until the triple end beep (ends ~1.05 s in) has played.
      stopRestKeepalive(2500);
      haptic('finish');
      showRestEndAlert();
    }
  };
  tick();
  restOverlayInterval = setInterval(tick, 100);
}

function showRestEndAlert() {
  // Remove any previous one to avoid stacking
  document.getElementById('rest-end-alert')?.remove();

  const overlay = document.createElement('div');
  overlay.id = 'rest-end-alert';
  overlay.setAttribute('role', 'alert');
  overlay.setAttribute('aria-live', 'assertive');
  overlay.style.cssText = `
    position:fixed; top:0; left:0; right:0; z-index:10000;
    background:#28a745; color:#fff; padding:18px 16px;
    font-size:20px; font-weight:700; text-align:center;
    box-shadow:0 4px 12px rgba(0,0,0,0.2);
    animation:slideDown 0.25s ease-out;
    cursor:pointer;
  `;
  overlay.textContent = `⏰ ${t('RestEnded')}`;
  document.body.appendChild(overlay);

  // Try to vibrate too (Android only, iOS silently ignores)
  try { navigator.vibrate?.([200, 100, 200, 100, 200]); } catch { /* */ }

  // Auto-dismiss after 5 seconds, or on tap
  const dismiss = () => {
    overlay.style.transition = 'opacity 0.3s';
    overlay.style.opacity = '0';
    setTimeout(() => overlay.remove(), 300);
  };
  overlay.addEventListener('click', dismiss);
  setTimeout(dismiss, 5000);
}

function updateTimerDisplay() {
  const display = document.getElementById('timer-display');
  if (!display) return;
  const mins = Math.floor(timerSeconds / 60);
  const secs = timerSeconds % 60;
  display.textContent = `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
}

// ── Finish ──

async function loadBestWeights() {
  bestWeights = {};
  try {
    const workouts = await api.get('/workouts');
    for (const w of workouts || []) {
      for (const log of (w.exerciseLogs || w.ExerciseLogs || [])) {
        const key = String(log.exerciseName || log.ExerciseName || '').trim().toLowerCase();
        if (!key) continue;
        let maxW = Number(log.weight || log.Weight) || 0;
        try {
          const details = JSON.parse(log.setDetails || log.SetDetails || '[]');
          if (Array.isArray(details)) {
            details.forEach(s => { const sw = Number(s.weight) || 0; if (sw > maxW) maxW = sw; });
          }
        } catch { /* legacy row without setDetails */ }
        if (maxW > (bestWeights[key] || 0)) bestWeights[key] = maxW;
      }
    }
  } catch (e) { console.warn('[livePR] could not load bests:', e); }
}

/** Loads the user's personal per-exercise notes into `myNotes` (key: lowercase name). */
async function loadMyNotes() {
  try {
    const rows = await api.get('/exercise-notes');
    const map = {};
    if (Array.isArray(rows)) {
      for (const r of rows) {
        const key = (r.exerciseName || '').trim().toLowerCase();
        if (key && r.note) map[key] = r.note;
      }
    }
    myNotes = map;
  } catch (e) {
    console.warn('[myNotes] could not load:', e);
    myNotes = {};
  }
}

/** Editor for the personal note of the current exercise (create / edit / delete). */
function showMyNoteModal() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  saveCurrentSetValues(); // the save path re-renders — don't lose a half-typed weight
  const exName = ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '';
  const key = exName.trim().toLowerCase();
  const existing = myNotes[key] || '';

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.innerHTML = `
    <div class="modal-content" style="max-width:420px;">
      <div class="modal-header">
        <div class="modal-title">&#128221; ${t('MyNote')}</div>
        <button class="modal-close" id="mynote-close">&times;</button>
      </div>
      <div style="font-size:13px;font-weight:600;margin-bottom:6px;">${escapeHtml(exTranslate(exName))}</div>
      <textarea id="mynote-text" maxlength="1000" rows="4" placeholder="${t('MyNotePlaceholder')}"
        style="width:100%;box-sizing:border-box;border:1px solid #ccc;border-radius:8px;padding:8px;font-size:14px;font-family:inherit;resize:vertical;background:#fff;color:#333;">${escapeHtml(existing)}</textarea>
      <div style="font-size:11px;color:gray;margin:4px 0 10px;">${t('MyNoteHint')}</div>
      <div style="display:flex;gap:8px;">
        ${existing ? `<button id="mynote-delete" class="btn" style="flex:1;background:#dc3545;color:#fff;">${t('Delete')}</button>` : ''}
        <button id="mynote-save" class="btn btn-primary" style="flex:2;">${t('Save')}</button>
      </div>
      <div id="mynote-error" style="color:#dc3545;font-size:12px;margin-top:6px;display:none;"></div>
    </div>
  `;
  document.body.appendChild(overlay);
  const close = () => overlay.remove();
  overlay.querySelector('#mynote-close')?.addEventListener('click', close);
  overlay.addEventListener('click', (e) => { if (e.target === overlay) close(); });

  const save = async (text) => {
    const btn = overlay.querySelector('#mynote-save');
    if (btn) btn.disabled = true;
    try {
      await api.put('/exercise-notes', { exerciseName: exName, note: text });
      if (text) myNotes[key] = text; else delete myNotes[key];
      close();
      renderExercise();
    } catch (err) {
      const errEl = overlay.querySelector('#mynote-error');
      if (errEl) { errEl.style.display = 'block'; errEl.textContent = t('ErrorFmt', err.message); }
      if (btn) btn.disabled = false;
    }
  };
  overlay.querySelector('#mynote-save')?.addEventListener('click', () => {
    save((overlay.querySelector('#mynote-text')?.value || '').trim());
  });
  overlay.querySelector('#mynote-delete')?.addEventListener('click', () => save(''));
}

// ── Plate calculator ──
// Olympic color code per plate size; height/width give the side-view proportions.
const PLATE_STYLE = {
  25:     { color: '#d32f2f', h: 58, w: 15 },
  20:     { color: '#1565c0', h: 54, w: 15 },
  15:     { color: '#f9a825', h: 47, w: 14 },
  10:     { color: '#2e7d32', h: 40, w: 13 },
  5:      { color: '#eceff1', h: 32, w: 12 },
  2.5:    { color: '#e57373', h: 26, w: 11 },
  1.25:   { color: '#90a4ae', h: 22, w: 10 },
  0.5:    { color: '#b0bec5', h: 18, w: 9 },
};
const PLATE_SIZES = Object.keys(PLATE_STYLE).map(Number).sort((a, b) => b - a);
const BAR_OPTIONS = [20, 15, 10, 6];

function getPlateCfg() {
  let bar = parseFloat(localStorage.getItem('fc_plate_bar'));
  if (!Number.isFinite(bar) || bar <= 0) bar = 20;
  let plates = null;
  try { plates = JSON.parse(localStorage.getItem('fc_plate_set') || 'null'); } catch { /* reset below */ }
  if (!Array.isArray(plates) || plates.some(p => !PLATE_SIZES.includes(p))) plates = [...PLATE_SIZES];
  return { bar, plates };
}

/** Greedy per-side breakdown. Returns counts (descending), leftover per side, achieved total. */
function computePlates(target, bar, plates) {
  const perSide = (target - bar) / 2;
  let remaining = Math.round(perSide * 1000) / 1000;
  const counts = [];
  for (const p of [...plates].sort((a, b) => b - a)) {
    const n = Math.floor((remaining + 1e-9) / p);
    if (n > 0) {
      counts.push({ plate: p, n });
      remaining = Math.round((remaining - n * p) * 1000) / 1000;
    }
  }
  const achieved = Math.round((target - remaining * 2) * 100) / 100;
  return { counts, remaining, achieved, perSide };
}

function showPlateCalcModal() {
  const ex = exercises[currentIndex];
  const initialTarget = Number(ex?.setDetails?.[currentSet]?.weight) || 0;
  const cfg = getPlateCfg();

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.innerHTML = `
    <div class="modal-content" style="max-width:420px;">
      <div class="modal-header">
        <div class="modal-title">&#127947;&#65039; ${t('PlateCalc')}</div>
        <button class="modal-close" id="platecalc-close">&times;</button>
      </div>
      <div style="display:flex;gap:10px;align-items:flex-end;margin-bottom:8px;">
        <div style="flex:1;min-width:0;">
          <div style="font-size:10px;color:gray;margin-bottom:2px;">${t('PlateCalcTarget')}</div>
          <input id="plate-target" type="number" step="any" min="0" max="500" inputmode="decimal" value="${initialTarget > 0 ? initialTarget : ''}"
            style="width:100%;box-sizing:border-box;font-size:18px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:6px;background:#fff;color:#333;">
        </div>
        <div style="width:96px;flex-shrink:0;">
          <div style="font-size:10px;color:gray;margin-bottom:2px;">${t('PlateCalcBar')} (kg)</div>
          <select id="plate-bar" style="width:100%;font-size:16px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:7px 2px;background:#fff;color:#333;">
            ${(BAR_OPTIONS.includes(cfg.bar) ? BAR_OPTIONS : [...BAR_OPTIONS, cfg.bar].sort((a, b) => b - a))
              .map(b => `<option value="${b}" ${b === cfg.bar ? 'selected' : ''}>${b}</option>`).join('')}
          </select>
        </div>
      </div>
      <div id="plate-result" style="min-height:110px;"></div>
      <div style="font-size:11px;color:gray;margin:10px 0 6px;">${t('PlateCalcPlates')}</div>
      <div id="plate-toggles" style="display:flex;gap:6px;flex-wrap:wrap;"></div>
    </div>
  `;
  document.body.appendChild(overlay);
  overlay.querySelector('#platecalc-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });

  const renderToggles = () => {
    const { plates } = getPlateCfg();
    const el = overlay.querySelector('#plate-toggles');
    if (!el) return;
    el.innerHTML = PLATE_SIZES.map(p => {
      const on = plates.includes(p);
      return `<button data-plate="${p}" style="border-radius:16px;padding:5px 11px;font-size:13px;font-weight:700;cursor:pointer;
        border:1px solid ${on ? PLATE_STYLE[p].color : '#bbb'};
        background:${on ? PLATE_STYLE[p].color : 'transparent'};
        color:${on ? (p === 5 || p === 0.5 ? '#333' : '#fff') : '#999'};
        ${on ? '' : 'text-decoration:line-through;opacity:0.6;'}">${p}</button>`;
    }).join('');
    el.querySelectorAll('[data-plate]').forEach(btn => btn.addEventListener('click', () => {
      const p = parseFloat(btn.dataset.plate);
      const { plates: cur } = getPlateCfg();
      const next = cur.includes(p) ? cur.filter(x => x !== p) : [...cur, p];
      localStorage.setItem('fc_plate_set', JSON.stringify(next));
      renderToggles();
      renderResult();
    }));
  };

  const renderResult = () => {
    const el = overlay.querySelector('#plate-result');
    if (!el) return;
    const raw = (overlay.querySelector('#plate-target')?.value ?? '').trim().replace(',', '.');
    const target = parseFloat(raw);
    const { bar, plates } = getPlateCfg();

    if (!Number.isFinite(target) || target <= 0) {
      el.innerHTML = `<div style="color:gray;text-align:center;padding:24px 0;font-size:13px;">${t('EnterWeightHint')}</div>`;
      return;
    }
    if (target < bar) {
      el.innerHTML = `<div style="color:#e67e22;text-align:center;padding:24px 8px;font-size:13px;font-weight:600;">&#9888;&#65039; ${t('PlateCalcBelowBar', bar)}</div>`;
      return;
    }
    const { counts, remaining, achieved } = computePlates(target, bar, plates);
    const totalPlates = counts.reduce((s, c) => s + c.n, 0);

    if (totalPlates === 0 && remaining <= 0.001) {
      el.innerHTML = `<div style="color:#28a745;text-align:center;padding:24px 0;font-size:14px;font-weight:700;">&#10003; ${t('PlateCalcEmptyBar')} (${bar} kg)</div>`;
      return;
    }

    // Side view of one bar end: sleeve stub + plates standing on it (capped at 12 plates).
    let visual = '';
    if (totalPlates > 0 && totalPlates <= 12) {
      const plateDivs = counts.flatMap(c => Array.from({ length: c.n }, () => {
        const s = PLATE_STYLE[c.plate];
        return `<div title="${c.plate} kg" style="width:${s.w}px;height:${s.h}px;background:${s.color};border:1px solid rgba(0,0,0,0.25);border-radius:3px;flex-shrink:0;"></div>`;
      })).join('');
      visual = `
        <div style="display:flex;align-items:center;justify-content:center;gap:2px;padding:8px 0 4px;">
          <div style="width:34px;height:8px;background:#9e9e9e;border-radius:3px 0 0 3px;flex-shrink:0;"></div>
          <div style="width:7px;height:20px;background:#757575;border-radius:2px;flex-shrink:0;"></div>
          ${plateDivs}
          <div style="width:22px;height:8px;background:#9e9e9e;border-radius:0 3px 3px 0;flex-shrink:0;"></div>
        </div>`;
    }
    const summary = counts.map(c => `<b>${c.n} × ${c.plate}</b>`).join(' &nbsp;+&nbsp; ');
    const warn = remaining > 0.001
      ? `<div style="margin-top:8px;background:#fff8e1;border:1px solid #ffe082;color:#8a6d00;border-radius:8px;padding:6px 10px;font-size:12px;">&#9888;&#65039; ${t('PlateCalcShort', achieved, Math.round(remaining * 2 * 100) / 100)}</div>`
      : '';
    el.innerHTML = `
      <div style="text-align:center;">
        ${visual}
        <div style="font-size:11px;color:#512BD4;font-weight:700;letter-spacing:0.5px;margin-top:4px;">${t('PlateCalcPerSide').toUpperCase()}</div>
        <div style="font-size:16px;margin-top:3px;">${summary || '—'} kg</div>
        ${warn}
      </div>`;
  };

  overlay.querySelector('#plate-target')?.addEventListener('input', renderResult);
  overlay.querySelector('#plate-bar')?.addEventListener('change', () => {
    const v = parseFloat(overlay.querySelector('#plate-bar').value);
    if (Number.isFinite(v) && v > 0) localStorage.setItem('fc_plate_bar', String(v));
    renderResult();
  });

  renderToggles();
  renderResult();
}

/**
 * Celebrates in the moment when the set just saved beats the all-time max weight
 * for the exercise. Called right after saveCurrentSetValues() while currentIndex/
 * currentSet still point at the completed set. Raises the stored best so each new
 * record only fires once.
 */
function checkLivePR() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  const key = (ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '').trim().toLowerCase();
  const prev = bestWeights[key] || 0;
  if (prev <= 0) return; // first sessions of an exercise are baselines, not records
  const lifted = Number(ex.setDetails?.[currentSet]?.weight) || 0;
  if (lifted <= prev) return;
  bestWeights[key] = lifted;
  haptic('pr');
  confetti(2500);
  showLivePRToast(t('NewRecordLive', exTranslate(ex.exerciseName || ex.ExerciseName || ''), lifted, prev));
}

function showLivePRToast(message) {
  document.getElementById('live-pr-toast')?.remove();
  const el = document.createElement('div');
  el.id = 'live-pr-toast';
  // z-index above the rest overlay (10000) so the record shows over the countdown.
  el.style.cssText = `
    position:fixed; top:calc(env(safe-area-inset-top, 0px) + 70px); left:50%; transform:translateX(-50%);
    z-index:10001; max-width:92vw; text-align:center;
    background:linear-gradient(135deg,#ffd700,#ffb347); color:#5d4e00;
    font-size:14px; font-weight:800; border-radius:12px; padding:12px 18px;
    box-shadow:0 4px 16px rgba(255,183,0,0.5);
    animation: pr-bounce 0.6s cubic-bezier(0.16,1,0.3,1);
  `;
  el.textContent = message;
  document.body.appendChild(el);
  setTimeout(() => {
    el.style.transition = 'opacity 0.5s';
    el.style.opacity = '0';
    setTimeout(() => el.remove(), 550);
  }, 3500);
}

async function finishWorkout() {
  stopTimer();
  if (autosaveInterval) { clearInterval(autosaveInterval); autosaveInterval = null; }
  // Force the focused input to commit its value BEFORE saveCurrentSetValues reads it.
  // Without this, iOS Safari can hold the last keystroke uncommitted and we'd persist
  // the previous value instead of the number the user just typed.
  if (document.activeElement && typeof document.activeElement.blur === 'function') {
    document.activeElement.blur();
  }
  // Defensive: re-capture the current set's values right before serializing.
  saveCurrentSetValues();
  const completedAt = new Date();

  const exerciseLogs = exercises.map(ex => ({
    exerciseId: ex.exerciseId || ex.ExerciseId || ex.id || ex.Id,
    exerciseName: ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '',
    sets: ex.setDetails.length,
    reps: ex.setDetails.length > 0 ? ex.setDetails[0].reps : 12,
    weight: Math.max(...ex.setDetails.map(s => s.weight), 0),
    muscleGroupName: ex.muscleGroupName || ex.MuscleGroupName || '',
    setDetails: JSON.stringify(ex.setDetails),
  }));

  // Diagnostic: log the payload weights so the user can verify in DevTools if anything
  // went wrong. Only the user's own data is logged.
  console.log('[finishWorkout] saving', exerciseLogs.length, 'exercises:',
    exerciseLogs.map(e => `${e.exerciseName}: max=${e.weight}kg, sets=${e.sets}`).join(' | '));

  const workoutPayload = {
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
  };

  let prs = [];
  try {
    const result = await api.post('/workouts', workoutPayload);
    if (result && Array.isArray(result.prs)) prs = result.prs;
    // The service worker keeps cached copies of GET /workouts/* (including
    // /workouts/last-weights/{day}). The copy taken BEFORE this save predates the
    // weights we just persisted — if it survives, the next visit prefills zeros.
    // Drop the SW cache and the localStorage offline cache for everything /workouts.
    try { await api.invalidateCache('/workouts'); } catch { /* best-effort */ }
    offline.invalidateCache('/workouts');
  } catch (e) {
    const isOfflineError = !navigator.onLine || e?.status === 503 || (e instanceof TypeError);
    if (isOfflineError) {
      // Queue for sync when back online
      offline.enqueue('POST', '/workouts', workoutPayload);
      offline.showSyncToast(t('WorkoutSavedOffline'));
    } else {
      // The server actively rejected the save (4xx/5xx). Do NOT pretend it worked:
      // keep the session progress so the typed weights survive, restart the autosave,
      // and stay on the page so the user can fix connectivity and hit Finish again.
      saveProgress();
      if (!autosaveInterval) {
        autosaveInterval = setInterval(autosaveTick, 15000);
      }
      alert(t('ErrorFmt', e?.message || 'HTTP'));
      return;
    }
  }

  clearProgress();

  // Celebration: vibration + confetti, plus extra punch on PR
  haptic(prs.length > 0 ? 'pr' : 'finish');
  confetti(prs.length > 0 ? 3500 : 2000);

  sessionStorage.setItem('workout_summary', JSON.stringify({
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
    prs,
  }));

  location.hash = '#summary';
}

/** Opens the OS file picker and uploads the chosen image for the current exercise. */
function pickAndUploadExerciseImage() {
  const ex = exercises[currentIndex];
  const exId = ex?.exerciseId || ex?.ExerciseId || ex?.id || ex?.Id;
  if (!exId) return;

  const input = document.createElement('input');
  input.type = 'file';
  input.accept = 'image/jpeg,image/png,image/webp,image/gif';
  input.style.display = 'none';
  document.body.appendChild(input);

  input.addEventListener('change', async () => {
    const file = input.files?.[0];
    document.body.removeChild(input);
    if (!file) return;
    if (file.size > 5 * 1024 * 1024) {
      alert(t('ImageTooLarge'));
      return;
    }

    try {
      const formData = new FormData();
      formData.append('image', file);
      const updated = await api.postForm(`/exercises/${exId}/image`, formData);
      const newUrl = updated.imageUrl || updated.ImageUrl || '';
      // Update every reference to this exercise in the loaded list (it may appear twice as a superset partner)
      for (const e of exercises) {
        if ((e.exerciseId || e.ExerciseId || e.id || e.Id) === exId) {
          e.imageUrl = newUrl;
          e.ImageUrl = newUrl;
        }
      }
      // Drop SW-cached responses that still reference the old imageUrl so
      // editday and other devices see the new image on next load.
      await api.invalidateCache('/routines');
      await api.invalidateCache('/exercises');
      saveProgress();
      renderExercise();
    } catch (err) {
      alert(t('ErrorFmt', err.message || err));
    }
  });

  input.click();
}

/** Shows an AI-generated form tips modal with technique bullets, common error, breathing. */
function showFormTipsModal(exerciseName, content, loading = false) {
  document.getElementById('form-tips-modal')?.remove();

  const overlay = document.createElement('div');
  overlay.id = 'form-tips-modal';
  overlay.className = 'modal-overlay modal-centered';
  overlay.style.zIndex = '200';

  let body = '';
  if (loading) {
    body = `<div style="text-align:center;padding:24px;color:var(--text-light);">${t('AIThinking') || 'La IA está pensando...'}</div>`;
  } else if (typeof content === 'string') {
    body = `<div style="white-space:pre-wrap;font-size:13px;line-height:1.6;">${escapeHtml(content)}</div>`;
  } else if (content && typeof content === 'object') {
    if (content.error) {
      body = `<div style="color:#dc3545;padding:12px;">${escapeHtml(content.error)}</div>`;
    } else {
      body = `
        ${Array.isArray(content.technique) ? `
          <div style="margin-bottom:12px;">
            <div style="font-weight:700;font-size:13px;color:var(--primary);margin-bottom:4px;">${t('Technique') || 'Técnica'}</div>
            <ul style="margin:0;padding-left:18px;font-size:13px;line-height:1.6;">
              ${content.technique.map(t => `<li>${escapeHtml(t)}</li>`).join('')}
            </ul>
          </div>` : ''}
        ${content.commonError ? `
          <div style="background:#fff3e0;border-left:3px solid #e67e22;padding:8px 12px;margin-bottom:12px;border-radius:6px;">
            <div style="font-weight:700;font-size:12px;color:#e67e22;margin-bottom:2px;">⚠ ${t('CommonError') || 'Error común'}</div>
            <div style="font-size:13px;">${escapeHtml(content.commonError)}</div>
          </div>` : ''}
        ${content.breathing ? `
          <div style="background:#e3f2fd;border-left:3px solid #1976d2;padding:8px 12px;border-radius:6px;">
            <div style="font-weight:700;font-size:12px;color:#1976d2;margin-bottom:2px;">🫁 ${t('Breathing') || 'Respiración'}</div>
            <div style="font-size:13px;">${escapeHtml(content.breathing)}</div>
          </div>` : ''}
      `;
    }
  }

  overlay.innerHTML = `
    <div class="modal-content" style="max-width:420px;width:92%;">
      <div style="font-weight:700;font-size:15px;margin-bottom:10px;text-align:center;">💡 ${escapeHtml(exerciseName)}</div>
      ${body}
      <button class="btn btn-primary btn-block" id="form-tips-close" style="margin-top:12px;">${t('Close') || 'Cerrar'}</button>
    </div>
  `;
  document.body.appendChild(overlay);
  const close = () => overlay.remove();
  overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
  overlay.querySelector('#form-tips-close')?.addEventListener('click', close);
}
