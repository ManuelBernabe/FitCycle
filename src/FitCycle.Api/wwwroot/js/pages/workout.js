// FitCycle Workout Page — exercise-by-exercise, set-by-set workout with rest timer

import { t, dayName, muscleGroup as mgTranslate } from '../l10n.js';
import { api } from '../api.js';

let dayNum = 0;
let exercises = [];
let currentIndex = 0;
let currentSet = 0;
let startedAt = null;
let timerSeconds = 90;
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

    // Restore saved progress if same day
    const saved = loadProgress();
    if (saved && saved.dayNum === dayNum && saved.exercises) {
      startedAt = saved.startedAt ? new Date(saved.startedAt) : new Date();
      currentIndex = Math.min(saved.currentIndex || 0, exercises.length - 1);
      currentSet = saved.currentSet || 0;
      // Restore saved weights/reps into exercises
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
    const name = ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '';
    const muscle = ex.muscleGroupName || ex.MuscleGroupName || '';
    const totalSets = ex.setDetails.length;
    const maxWeight = Math.max(...ex.setDetails.map(s => s.weight), 0);
    const isCurrent = idx === currentIndex;
    const isDone = idx < currentIndex;

    return `
      <div class="exercise-list-item ${isCurrent ? 'current' : ''} ${isDone ? 'done' : ''}" data-go-exercise="${idx}">
        <div class="exercise-list-num">${idx + 1}</div>
        <div class="exercise-list-info">
          <div class="exercise-list-name">${name}</div>
          <div class="exercise-list-meta">${mgTranslate(muscle)} · ${totalSets}s${maxWeight > 0 ? ` · ${maxWeight}kg` : ''}</div>
        </div>
        ${isDone ? '<div style="color:#28a745;font-size:16px;">&#10003;</div>' : ''}
        ${isCurrent ? '<div style="color:#512BD4;font-size:12px;font-weight:600;">&#9654;</div>' : ''}
      </div>
    `;
  }).join('');
}

// ── Render Exercise ──

function renderExercise() {
  const container = document.getElementById('workout-content');
  if (!container || currentIndex >= exercises.length) return;

  const ex = exercises[currentIndex];
  const exName = ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '';
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
  const ssPartnerName = ssPartner ? (ssPartner.exerciseName || ssPartner.ExerciseName || ssPartner.name || '') : '';

  const setDots = ex.setDetails.map((s, i) => {
    const cls = i < currentSet ? 'done' : (i === currentSet ? 'current' : '');
    return `<div class="set-dot ${cls}" title="S${i + 1}: ${s.reps}r / ${s.weight}kg"></div>`;
  }).join('');

  const minOptions = Array.from({ length: 11 }, (_, i) =>
    `<option value="${i}" ${i === 1 ? 'selected' : ''}>${String(i).padStart(2, '0')}</option>`
  ).join('');
  const secOptions = Array.from({ length: 12 }, (_, i) => {
    const val = i * 5;
    return `<option value="${val}" ${val === 30 ? 'selected' : ''}>${String(val).padStart(2, '0')}</option>`;
  }).join('');

  container.innerHTML = `
    <div class="page-content" style="padding-top:8px;">
      <div class="flex items-center justify-between" style="margin-bottom:4px;">
        <button id="workout-back" class="btn btn-ghost" style="padding:4px 8px;font-size:13px;">${t('Back')}</button>
        <button id="toggle-exercise-list" class="btn btn-ghost" style="font-size:12px;color:#512BD4;padding:4px 8px;">
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
          <div class="workout-exercise-image" style="margin:0;flex-shrink:0;">
            ${exImage
              ? `<img src="${exImage}" alt="${exName}" onerror="this.onerror=null;this.parentElement.innerHTML='<div style=&quot;font-size:40px;opacity:0.3;&quot;>&#127947;</div>'">`
              : `<div style="font-size:40px;opacity:0.3;">&#127947;</div>`
            }
          </div>
          <div style="min-width:0;">
            <div style="font-size:12px;color:#512BD4;font-weight:600;">${t('ExerciseProgress', currentIndex + 1, exercises.length)}</div>
            <div class="workout-exercise-name">${exName}</div>
            <div style="font-size:13px;color:gray;">${mgTranslate(exMuscle)}</div>
            ${ssPartnerName ? `<div style="margin-top:3px;font-size:11px;color:#e67e22;font-weight:600;">&#8644; ${t('Superset')}: ${ssPartnerName}</div>` : ''}
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
              <input type="number" id="workout-reps" value="${currentSetData.reps}" min="1" max="100"
                style="width:56px;font-size:18px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:5px;">
            </div>
            <div style="font-size:20px;font-weight:bold;color:#ccc;">x</div>
            <div>
              <div style="font-size:10px;color:gray;">kg</div>
              <input type="number" id="workout-weight" value="${currentSetData.weight > 0 ? currentSetData.weight : ''}" placeholder="0" step="0.5" min="0"
                style="width:66px;font-size:18px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:5px;">
            </div>
          </div>
          ${(currentSetData.tempoPos > 0 || currentSetData.tempoNeg > 0 || currentSetData.grip) ? `
            <div style="margin-top:4px;display:flex;justify-content:center;gap:10px;flex-wrap:wrap;">
              ${currentSetData.tempoPos > 0 || currentSetData.tempoNeg > 0 ? `<span style="font-size:11px;color:#512BD4;">&#9201; ${currentSetData.tempoPos}s&#8593; / ${currentSetData.tempoNeg}s&#8595;</span>` : ''}
              ${currentSetData.grip ? `<span style="font-size:11px;color:#e67e22;">&#9994; ${currentSetData.grip}</span>` : ''}
            </div>
          ` : ''}
        </div>
        ${exNotes ? `
          <div style="background:#fff3e0;border-radius:8px;padding:6px 10px;margin:6px auto 0;max-width:300px;text-align:left;">
            <div style="font-size:10px;color:#e67e22;font-weight:600;">&#128221; ${t('ExerciseNotes')}</div>
            <div style="font-size:11px;color:#333;white-space:pre-wrap;">${exNotes}</div>
          </div>
        ` : ''}

        <div style="border-top:1px solid #eee;margin-top:8px;padding-top:6px;">
          <div style="display:flex;align-items:center;justify-content:center;gap:8px;flex-wrap:wrap;">
            <span style="font-size:11px;color:#512BD4;font-weight:bold;letter-spacing:1px;">${t('Rest')}</span>
            <div id="timer-display" style="font-size:24px;font-weight:bold;color:#333;background:#f5f5f5;border-radius:10px;padding:2px 12px;">01:30</div>
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
    if (currentSet > 0) { currentSet--; }
    else if (currentIndex > 0) { currentIndex--; currentSet = exercises[currentIndex].setDetails.length - 1; }
    saveProgress();
    renderExercise();
  });

  document.getElementById('workout-next')?.addEventListener('click', () => {
    saveCurrentSetValues();
    stopTimer();
    const ex2 = exercises[currentIndex];
    const ssGrp = ex2.supersetGroup || 0;

    if (ssGrp > 0) {
      // Superset logic: alternate between paired exercises
      const partnerIdx = exercises.findIndex((e, i) => i !== currentIndex && (e.supersetGroup || 0) === ssGrp);
      if (partnerIdx >= 0) {
        const isFirstInPair = currentIndex < partnerIdx;
        if (isFirstInPair) {
          currentIndex = partnerIdx;
          if (currentSet >= exercises[partnerIdx].setDetails.length) currentSet = exercises[partnerIdx].setDetails.length - 1;
        } else {
          currentIndex = partnerIdx;
          currentSet++;
          if (currentSet >= exercises[partnerIdx].setDetails.length) {
            const maxIdx = Math.max(currentIndex, partnerIdx);
            currentIndex = maxIdx + 1;
            currentSet = 0;
            if (currentIndex < exercises.length) { saveProgress(); renderExercise(); return; }
          }
        }
        saveProgress();
        renderExercise();
        return;
      }
    }

    // Normal flow
    if (currentSet < ex2.setDetails.length - 1) { currentSet++; }
    else if (currentIndex < exercises.length - 1) { currentIndex++; currentSet = 0; }
    saveProgress();
    renderExercise();
  });

  document.getElementById('workout-finish')?.addEventListener('click', () => { saveCurrentSetValues(); finishWorkout(); });

  // Auto-save on weight/reps input change
  document.getElementById('workout-weight')?.addEventListener('change', () => { saveCurrentSetValues(); saveProgress(); });
  document.getElementById('workout-reps')?.addEventListener('change', () => { saveCurrentSetValues(); saveProgress(); });

  document.getElementById('timer-start')?.addEventListener('click', onTimerStartClicked);
  document.getElementById('timer-reset')?.addEventListener('click', onTimerResetClicked);
  document.getElementById('timer-min')?.addEventListener('change', onTimePickerChanged);
  document.getElementById('timer-sec')?.addEventListener('change', onTimePickerChanged);

  stopTimer();
  resetTimerDisplay();
}

function saveCurrentSetValues() {
  const ex = exercises[currentIndex];
  if (!ex) return;
  const repsEl = document.getElementById('workout-reps');
  const weightEl = document.getElementById('workout-weight');
  if (repsEl) ex.setDetails[currentSet].reps = parseInt(repsEl.value) || 12;
  if (weightEl) ex.setDetails[currentSet].weight = parseFloat(weightEl.value) || 0;
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
      // Visual + audio alert instead of vibrate (vibrate triggers iOS "undo" dialog)
      try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        const osc = ctx.createOscillator();
        osc.frequency.value = 880;
        osc.connect(ctx.destination);
        osc.start();
        setTimeout(() => { osc.stop(); ctx.close(); }, 300);
      } catch (e) { /* */ }
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

  try {
    await api.post('/workouts', {
      day: dayNum,
      startedAt: startedAt.toISOString(),
      completedAt: completedAt.toISOString(),
      exercises: exerciseLogs,
    });
  } catch (e) { /* Don't block finish */ }

  // Clear saved progress after successful finish
  clearProgress();

  sessionStorage.setItem('workout_summary', JSON.stringify({
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
  }));

  location.hash = '#summary';
}
