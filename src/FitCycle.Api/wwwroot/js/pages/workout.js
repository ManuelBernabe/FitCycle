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
          const match = exercises.find(ex => {
            const routineId = ex.exerciseId ?? ex.ExerciseId ?? ex.id ?? ex.Id;
            return routineId === lastEx.exerciseId;
          });
          if (!match) {
            console.log('[prefill] no match for exerciseId', lastEx.exerciseId);
            continue;
          }
          matchedExercises++;
          let lastSetDetails = null;
          try { lastSetDetails = lastEx.setDetails ? JSON.parse(lastEx.setDetails) : null; } catch { }
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
    autosaveInterval = setInterval(() => {
      saveCurrentSetValues();
      saveProgress();
    }, 15000);
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

  // Progression suggestion (only on the FIRST set so it doesn't distract mid-exercise)
  const progression = currentSet === 0 ? suggestProgression(ex) : null;

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
    <button id="workout-back" class="floating-back-btn">${t('Back')}</button>
    <div class="page-content" style="padding-top:8px;">
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
            <div style="display:flex;align-items:center;gap:6px;">
              <div style="font-size:12px;color:#512BD4;font-weight:600;">${t('ExerciseProgress', currentIndex + 1, exercises.length)}</div>
              <button id="workout-video-btn" class="video-btn ${ex.videoUrl || ex.VideoUrl ? '' : 'empty'}" title="${t('Demo') || 'Demo'}">▶ ${t('Demo') || 'Demo'}</button>
              <button id="workout-tips-btn" class="video-btn" style="background:#ffc107;color:#000;" title="${t('FormTips')}">💡 ${t('FormTips')}</button>
            </div>
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
          </div>
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
                step="0.25" min="0" max="500" value="${currentSetData.weight}"
                inputmode="decimal"
                style="width:88px;font-size:18px;font-weight:bold;text-align:center;border-radius:8px;padding:5px;background:#fff;border:${currentSetData.weight > 0 ? '1px solid #ddd' : '2px solid #e67e22'};"
                title="${currentSetData.weight > 0 ? '' : t('EnterWeightHint')}">
              <span id="weight-saved-badge" style="position:absolute;right:-6px;top:-2px;background:#28a745;color:#fff;font-size:10px;font-weight:bold;padding:2px 6px;border-radius:10px;opacity:0;transition:opacity .25s;pointer-events:none;">&#10003;</span>
              <datalist id="workout-weight-options">${buildWorkoutWeightOptions(currentSetData.weight)}</datalist>
            </div>
          </div>
          ${(() => {
            const oneRM = estimate1RM(currentSetData.weight, currentSetData.reps);
            return oneRM > 0
              ? `<div style="text-align:center;margin-top:6px;"><span class="one-rm-badge" title="${t('OneRMTooltip')}">${t('OneRM')}: ${oneRM} kg</span></div>`
              : '';
          })()}
          ${progression ? `
            <button id="apply-progression" class="progression-chip"
              title="${t('ProgressionTooltip')}"
              style="display:block;margin:8px auto 0;background:linear-gradient(135deg,#28a745,#20c997);color:#fff;border:none;padding:6px 14px;border-radius:20px;font-size:13px;font-weight:600;cursor:pointer;box-shadow:0 2px 6px rgba(40,167,69,0.3);">
              ${t('ProgressionSuggest', '+' + progression.delta, progression.newWeight)}
            </button>
          ` : ''}
          ${(currentSetData.tempoPos > 0 || currentSetData.tempoNeg > 0 || currentSetData.grip) ? `
            <div style="margin-top:8px;display:flex;justify-content:center;gap:8px;flex-wrap:wrap;">
              ${currentSetData.tempoPos > 0 || currentSetData.tempoNeg > 0 ? `
                <div style="display:flex;gap:6px;align-items:center;background:#f3f0fc;padding:4px 10px;border-radius:8px;border:1px solid #d4c4f5;">
                  <span style="font-size:13px;font-weight:700;color:#512BD4;">⏱</span>
                  <span style="font-size:13px;font-weight:600;color:#512BD4;">${currentSetData.tempoPos}s ${t('TempoAsc')}</span>
                  <span style="color:#999;font-size:11px;">·</span>
                  <span style="font-size:13px;font-weight:600;color:#512BD4;">${currentSetData.tempoNeg}s ${t('TempoDesc')}</span>
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

        <div style="border-top:1px solid #eee;margin-top:8px;padding-top:6px;">
          <div style="display:flex;align-items:center;justify-content:center;gap:8px;flex-wrap:wrap;">
            <span style="font-size:11px;color:#512BD4;font-weight:bold;letter-spacing:1px;">${t('Rest')}</span>
            <div id="timer-display" style="font-size:24px;font-weight:bold;color:#333;background:#f5f5f5;border-radius:10px;padding:2px 12px;">${String(currentMin).padStart(2, '0')}:${String(currentSec).padStart(2, '0')}</div>
            <div id="timer-picker-row" class="flex items-center gap-4" style="font-size:12px;">
              <select id="timer-min" class="picker-select" style="width:50px;font-size:12px;padding:3px;">${minOptions}</select>
              <span style="color:gray;">:</span>
              <select id="timer-sec" class="picker-select" style="width:50px;font-size:12px;padding:3px;">${secOptions}</select>
            </div>
            <button id="timer-start" class="btn btn-sm" style="background:#512BD4;color:#fff;padding:4px 12px;border-radius:8px;font-size:12px;">${t('Start')}</button>
            <button id="timer-reset" class="btn btn-sm" style="background:#6c757d;color:#fff;padding:4px 12px;border-radius:8px;font-size:12px;">${t('Reset')}</button>
          </div>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-top:8px;">
        <button id="workout-prev" class="btn btn-outline" style="padding:10px;" ${currentIndex === 0 && currentSet === 0 ? 'disabled' : ''}>
          ${currentSet > 0 ? t('PrevSet') : t('Previous')}
        </button>
        ${isLastExercise && isLastSet
          ? `<button id="workout-finish" class="btn btn-success" style="padding:10px;">${t('Finish')}</button>`
          : `<button id="workout-next" class="btn btn-primary" style="padding:10px;">${isLastSet ? t('Next') : t('NextSet')}</button>`
        }
      </div>
    </div>
  `;

  // ── Event Bindings ──

  document.getElementById('workout-back')?.addEventListener('click', () => {
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
      saveCurrentSetValues();
      const idx = parseInt(el.dataset.goExercise);
      if (idx >= 0 && idx < exercises.length) {
        currentIndex = idx;
        currentSet = 0;
        saveProgress();
        stopTimer();
        renderExercise();
      }
    });
  });

  document.getElementById('workout-prev')?.addEventListener('click', () => {
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
        return;
      }
    }

    if (currentSet > 0) { currentSet--; }
    else if (currentIndex > 0) { currentIndex--; currentSet = exercises[currentIndex].setDetails.length - 1; }
    saveProgress();
    renderExercise();
  });

  document.getElementById('workout-next')?.addEventListener('click', () => advanceToNext());

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

  document.getElementById('apply-progression')?.addEventListener('click', () => {
    const ex = exercises[currentIndex];
    if (!ex) return;
    const progression = suggestProgression(ex);
    if (!progression) return;
    // Apply new weight to all sets of this exercise
    for (const sd of ex.setDetails) sd.weight = progression.newWeight;
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
function suggestProgression(exercise) {
  if (!exercise || !exercise.setDetails || exercise.setDetails.length === 0) return null;
  const sets = exercise.setDetails;
  const allWeightsEqual = sets.every(s => s.weight === sets[0].weight && s.weight > 0);
  if (!allWeightsEqual) return null;

  // Get the lowest reps across all sets — if even the hardest set was easy, suggest progression
  const minReps = Math.min(...sets.map(s => s.reps || 0));
  if (minReps === 0) return null;

  const name = (exercise.exerciseName || exercise.name || '').toLowerCase();
  const isCompound = ['press banca', 'sentadilla', 'peso muerto', 'press militar', 'dominada', 'remo', 'hip thrust', 'prensa'].some(k => name.includes(k));
  const isIsolation = ['curl', 'extension', 'extensión', 'elevación', 'patada', 'aductor', 'abductor', 'gemelo'].some(k => name.includes(k));

  // Target reps assumed from set's reps field (the routine value)
  const targetReps = sets[0].reps;
  if (minReps < targetReps) return null; // Didn't complete all reps

  let delta;
  if (minReps >= targetReps + 2) {
    delta = isCompound ? 5 : (isIsolation ? 2.5 : 2.5);
  } else if (minReps >= targetReps) {
    delta = isCompound ? 2.5 : (isIsolation ? 1.25 : 1.25);
  } else {
    return null;
  }

  return { delta, newWeight: sets[0].weight + delta };
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

  // Reps: only overwrite when the user picked a valid positive number. An empty/garbage
  // value MUST NOT overwrite the planned reps with a default (used to be 12 — that's why
  // re-imported routines showed wrong reps after a quick set advance).
  const repsEl = document.getElementById('workout-reps');
  if (repsEl) {
    const reps = parseInt(repsEl.value, 10);
    if (Number.isFinite(reps) && reps > 0) sd.reps = reps;
  }

  // Weight: same rule — an empty input (user cleared it then advanced, or the browser
  // returned "" momentarily) must not stamp 0 over a pre-filled weight from history.
  // The user can still explicitly type "0" to reset; that path keeps working because
  // parseFloat("0") === 0 passes the `Number.isFinite(val) && val >= 0` check.
  const weightEl = document.getElementById('workout-weight');
  if (weightEl) {
    const raw = (weightEl.value ?? '').trim();
    if (raw !== '') {
      const weight = parseFloat(raw);
      if (Number.isFinite(weight) && weight >= 0) {
        const changed = sd.weight !== weight;
        sd.weight = weight;
        // Visual confirmation: when the saved weight actually changed, flash a green
        // tick next to the input so the user has immediate proof their value made it
        // into memory. Without this, multiple users reported "no se guardan los pesos"
        // when the problem was actually a stale Service Worker.
        if (changed && weight > 0) flashWeightSaved();
      }
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
  saveCurrentSetValues();
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
  const startBtn = document.getElementById('timer-start');
  const pickerRow = document.getElementById('timer-picker-row');

  if (timerRunning) {
    stopTimer();
    if (startBtn) { startBtn.textContent = t('Start'); startBtn.style.background = '#512BD4'; }
    if (pickerRow) pickerRow.style.display = '';
    return;
  }

  const total = getPickerTotalSeconds();
  if (total <= 0) return;
  timerSeconds = total;
  timerEndsAt = Date.now() + total * 1000;
  timerRunning = true;
  if (startBtn) { startBtn.textContent = t('Pause'); startBtn.style.background = '#e67e22'; }
  if (pickerRow) pickerRow.style.display = 'none';

  timerInterval = setInterval(() => {
    if (!timerRunning) return; // Defensive: stopTimer was called from outside, drop this tick.
    // Recompute from wall clock — backgrounded tabs throttle setInterval to once per
    // minute (or pause it entirely on iOS), so we can't trust how often this fired.
    const remaining = Math.ceil((timerEndsAt - Date.now()) / 1000);
    timerSeconds = remaining > 0 ? remaining : 0;
    updateTimerDisplay();
    if (timerSeconds <= 0) {
      // Stop FIRST so the auto-advance can't re-enter and overlap with a new interval
      // started by the next renderExercise() call.
      stopTimer();
      const startBtnNow = document.getElementById('timer-start');
      const pickerRowNow = document.getElementById('timer-picker-row');
      if (startBtnNow) { startBtnNow.textContent = t('Start'); startBtnNow.style.background = '#512BD4'; }
      if (pickerRowNow) pickerRowNow.style.display = '';
      const display = document.getElementById('timer-display');
      if (display) display.style.color = '#28a745';
      playRestEndSound();
      showRestEndAlert();
      advanceToNext();
    }
  }, 250); // Tick 4x/sec so the UI catches up quickly after foreground resume.
}

function onTimerResetClicked() { stopTimer(); resetTimerDisplay(); }

function stopTimer() {
  timerRunning = false;
  timerEndsAt = 0;
  if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }
}

/**
 * Called when the document becomes visible again after the user backgrounded the app.
 * Reconciles `timerSeconds` against the wall clock so the display catches up instantly
 * (the setInterval tick might have been throttled to once per minute on mobile).
 * If the timer already expired in the background, we fire the rest-end logic now.
 */
function onVisibilityChange() {
  if (document.hidden) return;
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

function playRestEndSound() {
  // Two short beeps so it's clearly audible over background noise / earbuds
  try {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const beep = (when, duration = 250) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.frequency.value = 880;
      osc.type = 'sine';
      gain.gain.setValueAtTime(0.0001, ctx.currentTime + when);
      gain.gain.exponentialRampToValueAtTime(0.4, ctx.currentTime + when + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + when + duration / 1000);
      osc.connect(gain); gain.connect(ctx.destination);
      osc.start(ctx.currentTime + when);
      osc.stop(ctx.currentTime + when + duration / 1000 + 0.05);
    };
    beep(0); beep(0.35); beep(0.7);
    setTimeout(() => ctx.close(), 1500);
  } catch (e) { /* audio not available */ }
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

async function finishWorkout() {
  stopTimer();
  if (autosaveInterval) { clearInterval(autosaveInterval); autosaveInterval = null; }
  // Defensive: re-capture the current set's values right before serializing. The button
  // click handler already calls saveCurrentSetValues, but a second pass guarantees the
  // very last value the user typed lands in the payload — especially when the user was
  // still inside the input when they pressed Finish.
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

  let saved = false;
  let prs = [];
  try {
    const result = await api.post('/workouts', workoutPayload);
    saved = true;
    if (result && Array.isArray(result.prs)) prs = result.prs;
  } catch (e) {
    // Queue for sync when back online
    offline.enqueue('POST', '/workouts', workoutPayload);
    saved = true; // Consider it saved locally
    offline.showSyncToast(t('WorkoutSavedOffline'));
  }

  if (saved) clearProgress();

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
