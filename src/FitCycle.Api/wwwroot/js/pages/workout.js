// FitCycle Workout Page — exercise-by-exercise, set-by-set workout with rest timer

import { t, dayName, muscleGroup as mgTranslate, exerciseName as exTranslate } from '../l10n.js';
import { api } from '../api.js';
import { offline } from '../offline.js';
import { escapeHtml } from '../utils.js';

let dayNum = 0;
let exercises = [];
let currentIndex = 0;
let currentSet = 0;
let startedAt = null;
let timerSeconds = 60;
let timerRunning = false;
let timerInterval = null;
let showExerciseList = false;

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
    try {
      const lastWorkout = await api.get(`/workouts/last-weights/${dayNum}`);
      console.log('[prefill] last-weights response for day', dayNum, lastWorkout);
      if (lastWorkout?.exercises?.length) {
        let filled = 0;
        for (const lastEx of lastWorkout.exercises) {
          const match = exercises.find(ex => {
            const routineId = ex.exerciseId ?? ex.ExerciseId ?? ex.id ?? ex.Id;
            return routineId === lastEx.exerciseId;
          });
          if (!match) {
            console.log('[prefill] no match for exerciseId', lastEx.exerciseId);
            continue;
          }
          // Try per-set details first (most accurate)
          let lastSetDetails = null;
          try { lastSetDetails = lastEx.setDetails ? JSON.parse(lastEx.setDetails) : null; } catch { }
          if (Array.isArray(lastSetDetails) && lastSetDetails.length > 0) {
            for (let i = 0; i < match.setDetails.length; i++) {
              const src = i < lastSetDetails.length ? lastSetDetails[i] : lastSetDetails[lastSetDetails.length - 1];
              if (src.weight > 0) { match.setDetails[i].weight = src.weight; filled++; }
              if (src.reps > 0) match.setDetails[i].reps = src.reps;
            }
          } else if (lastEx.weight > 0) {
            for (const sd of match.setDetails) { sd.weight = lastEx.weight; filled++; }
          }
        }
        console.log('[prefill] filled', filled, 'sets with weights');
      }
    } catch (err) { console.warn('[prefill] failed:', err); }

    // Restore saved progress if same day AND recent (within 4 hours)
    const saved = loadProgress();
    const savedAge = saved?.startedAt ? (Date.now() - new Date(saved.startedAt).getTime()) : Infinity;
    const isFreshProgress = saved && saved.dayNum === dayNum && saved.exercises && savedAge < 4 * 60 * 60 * 1000;
    if (isFreshProgress) {
      startedAt = saved.startedAt ? new Date(saved.startedAt) : new Date();
      currentIndex = Math.min(saved.currentIndex || 0, exercises.length - 1);
      currentSet = saved.currentSet || 0;
      // Restore saved weights/reps — but only overwrite if saved value is > 0
      // (don't replace pre-filled weights with zeros from incomplete progress)
      for (const savedEx of saved.exercises) {
        const match = exercises.find(ex =>
          (ex.exerciseId || ex.ExerciseId || ex.id || ex.Id) === savedEx.exerciseId
        );
        if (match && savedEx.setDetails) {
          for (let i = 0; i < match.setDetails.length && i < savedEx.setDetails.length; i++) {
            if (savedEx.setDetails[i].weight > 0) match.setDetails[i].weight = savedEx.setDetails[i].weight;
            if (savedEx.setDetails[i].reps > 0) match.setDetails[i].reps = savedEx.setDetails[i].reps;
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
}

// ── Exercise List ──

function buildExerciseList() {
  return exercises.map((ex, idx) => {
    const name = exTranslate(ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '');
    const muscle = ex.muscleGroupName || ex.MuscleGroupName || '';
    const totalSets = ex.setDetails.length;
    const maxWeight = Math.max(...ex.setDetails.map(s => s.weight), 0);
    const isCurrent = idx === currentIndex;
    const isDone = idx < currentIndex;

    return `
      <div class="exercise-list-item ${isCurrent ? 'current' : ''} ${isDone ? 'done' : ''}" data-go-exercise="${idx}">
        <div class="exercise-list-num">${idx + 1}</div>
        <div class="exercise-list-info">
          <div class="exercise-list-name">${escapeHtml(name)}</div>
          <div class="exercise-list-meta">${mgTranslate(muscle)} · ${totalSets}s${maxWeight > 0 ? ` · ${maxWeight}kg` : ''}</div>
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

  // Default timer: 1 minute (1:00). Pickers reflect the current `timerSeconds` state.
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

      <div class="card workout-exercise">
        <div style="display:flex;align-items:center;gap:12px;text-align:left;margin-bottom:6px;">
          <div id="workout-exercise-image" class="workout-exercise-image" style="margin:0;flex-shrink:0;cursor:pointer;position:relative;" title="${t('ChangeImage')}">
            ${exImage
              ? `<img src="${exImage}" alt="${escapeHtml(exName)}" onerror="this.onerror=null;this.parentElement.querySelector('.workout-img-placeholder').style.display='flex';this.style.display='none';">`
              : ''
            }
            <div class="workout-img-placeholder" style="display:${exImage ? 'none' : 'flex'};align-items:center;justify-content:center;width:100%;height:100%;font-size:40px;opacity:0.3;">&#128247;</div>
          </div>
          <div style="min-width:0;">
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
            <div>
              <div style="font-size:10px;color:gray;">kg</div>
              <div style="display:flex;gap:4px;align-items:center;">
                <select id="workout-weight"
                  style="width:72px;font-size:18px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:5px;background:#fff;">
                  ${buildWorkoutWeightOptions(currentSetData.weight)}
                </select>
                <input type="number" id="workout-weight-manual" step="0.25" min="0" max="500"
                  value="${currentSetData.weight}"
                  placeholder="kg"
                  title="${t('ManualWeight')}"
                  style="width:64px;font-size:16px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:5px;">
              </div>
            </div>
          </div>
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

  document.getElementById('workout-finish')?.addEventListener('click', () => { saveCurrentSetValues(); finishWorkout(); });

  // Auto-save on weight/reps input change. Keep the manual input and the dropdown in sync.
  document.getElementById('workout-weight')?.addEventListener('change', (e) => {
    const manual = document.getElementById('workout-weight-manual');
    if (manual) manual.value = e.target.value;
    saveCurrentSetValues(); saveProgress();
  });
  document.getElementById('workout-weight-manual')?.addEventListener('change', (e) => {
    const val = parseFloat(e.target.value);
    if (!isNaN(val) && val >= 0) {
      const select = document.getElementById('workout-weight');
      if (select) {
        // Add option dynamically if not present so the select shows the manual value
        if (![...select.options].some(o => parseFloat(o.value) === val)) {
          const opt = document.createElement('option');
          opt.value = val; opt.textContent = val;
          select.appendChild(opt);
        }
        select.value = val;
      }
    }
    saveCurrentSetValues(); saveProgress();
  });
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

  document.getElementById('timer-start')?.addEventListener('click', onTimerStartClicked);
  document.getElementById('timer-reset')?.addEventListener('click', onTimerResetClicked);
  document.getElementById('timer-min')?.addEventListener('change', onTimePickerChanged);
  document.getElementById('timer-sec')?.addEventListener('change', onTimePickerChanged);

  stopTimer();
  resetTimerDisplay();
}

function buildWorkoutWeightOptions(selected) {
  const vals = [0];
  for (let i = 0.25; i <= 150; i += 0.25) vals.push(i);
  if (selected > 0 && !vals.includes(selected)) { vals.push(selected); vals.sort((a, b) => a - b); }
  return vals.map(v => `<option value="${v}" ${v === selected ? 'selected' : ''}>${v}</option>`).join('');
}

function buildWorkoutRepsOptions(selected) {
  const vals = [];
  for (let i = 1; i <= 50; i++) vals.push(i);
  if (selected > 0 && !vals.includes(selected)) { vals.push(selected); vals.sort((a, b) => a - b); }
  return vals.map(v => `<option value="${v}" ${v === selected ? 'selected' : ''}>${v}</option>`).join('');
}

function saveCurrentSetValues() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  const repsEl = document.getElementById('workout-reps');
  const weightEl = document.getElementById('workout-weight');
  const weightManualEl = document.getElementById('workout-weight-manual');
  if (repsEl) ex.setDetails[currentSet].reps = parseInt(repsEl.value) || 12;
  // Prefer the manual input if non-empty AND different from the dropdown — that means the user typed a custom value
  let weightVal = weightEl ? (parseFloat(weightEl.value) || 0) : 0;
  if (weightManualEl && weightManualEl.value !== '') {
    const manualVal = parseFloat(weightManualEl.value);
    if (!isNaN(manualVal) && manualVal !== weightVal) weightVal = manualVal;
  }
  ex.setDetails[currentSet].weight = weightVal;
}

/**
 * Advances to the next set/exercise. Used by the Next button AND by the
 * rest-timer auto-advance. Handles superset alternation transparently.
 */
function advanceToNext() {
  saveCurrentSetValues();
  stopTimer();
  const ex2 = exercises[currentIndex];
  if (!ex2) return;
  const ssGrp = ex2.supersetGroup || 0;

  if (ssGrp > 0) {
    const partnerIdx = exercises.findIndex((e, i) => i !== currentIndex && (e.supersetGroup || 0) === ssGrp);
    if (partnerIdx >= 0) {
      const isFirstInPair = currentIndex < partnerIdx;
      if (isFirstInPair) {
        currentIndex = partnerIdx;
        if (currentSet >= exercises[partnerIdx].setDetails.length) currentSet = exercises[partnerIdx].setDetails.length - 1;
      } else {
        const origIdx = currentIndex;
        currentIndex = partnerIdx;
        currentSet++;
        if (currentSet >= exercises[partnerIdx].setDetails.length) {
          const maxIdx = Math.max(origIdx, partnerIdx);
          currentIndex = maxIdx + 1;
          currentSet = 0;
          if (currentIndex >= exercises.length) {
            currentIndex = exercises.length - 1;
            currentSet = exercises[currentIndex].setDetails.length - 1;
          }
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

  timerSeconds = getPickerTotalSeconds();
  if (timerSeconds <= 0) return;
  timerRunning = true;
  if (startBtn) { startBtn.textContent = t('Pause'); startBtn.style.background = '#e67e22'; }
  if (pickerRow) pickerRow.style.display = 'none';

  timerInterval = setInterval(() => {
    timerSeconds--;
    updateTimerDisplay();
    if (timerSeconds <= 0) {
      stopTimer();
      if (startBtn) { startBtn.textContent = t('Start'); startBtn.style.background = '#512BD4'; }
      if (pickerRow) pickerRow.style.display = '';
      const display = document.getElementById('timer-display');
      if (display) display.style.color = '#28a745';
      playRestEndSound();
      showRestEndAlert();
      // Auto-advance to the next set/exercise so the user doesn't have to tap
      advanceToNext();
    }
  }, 1000);
}

function onTimerResetClicked() { stopTimer(); resetTimerDisplay(); }

function stopTimer() {
  timerRunning = false;
  if (timerInterval) { clearInterval(timerInterval); timerInterval = null; }
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
  overlay.style.cssText = `
    position:fixed; top:0; left:0; right:0; z-index:10000;
    background:#28a745; color:#fff; padding:18px 16px;
    font-size:20px; font-weight:700; text-align:center;
    box-shadow:0 4px 12px rgba(0,0,0,0.2);
    animation:slideDown 0.25s ease-out;
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

  const workoutPayload = {
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
  };

  let saved = false;
  try {
    await api.post('/workouts', workoutPayload);
    saved = true;
  } catch (e) {
    // Queue for sync when back online
    offline.enqueue('POST', '/workouts', workoutPayload);
    saved = true; // Consider it saved locally
    offline.showSyncToast(t('WorkoutSavedOffline'));
  }

  if (saved) clearProgress();

  sessionStorage.setItem('workout_summary', JSON.stringify({
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
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
      saveProgress();
      renderExercise();
    } catch (err) {
      alert(t('ErrorFmt', err.message || err));
    }
  });

  input.click();
}
