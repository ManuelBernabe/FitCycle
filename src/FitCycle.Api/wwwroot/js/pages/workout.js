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
  startedAt = new Date();
  currentIndex = 0;
  currentSet = 0;

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
      } catch { setDetails = null; }
      if (!Array.isArray(setDetails) || setDetails.length === 0) {
        setDetails = Array.from({ length: sets }, () => ({ reps, weight }));
      }
      return { ...ex, setDetails };
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
  exercises = [];
  currentIndex = 0;
  currentSet = 0;
}

function renderExercise() {
  const container = document.getElementById('workout-content');
  if (!container || currentIndex >= exercises.length) return;

  const ex = exercises[currentIndex];
  const exName = ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '';
  const exMuscle = ex.muscleGroupName || ex.MuscleGroupName || '';
  const exImage = ex.imageUrl || ex.ImageUrl || '';
  const totalSets = ex.setDetails.length;
  const currentSetData = ex.setDetails[currentSet] || { reps: 12, weight: 0 };
  const progressPct = ((currentIndex + 1) / exercises.length * 100).toFixed(0);
  const isLastExercise = currentIndex === exercises.length - 1;
  const isLastSet = currentSet >= totalSets - 1;

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
    <div class="page-content">
      <div class="flex items-center justify-between mb-8">
        <button id="workout-back" class="btn btn-ghost">${t('Back')}</button>
        <div class="status-text">${dayName(dayNum)}</div>
      </div>
      <div class="text-center mb-4" style="font-size:15px;color:gray;">
        ${t('ExerciseProgress', currentIndex + 1, exercises.length)}
      </div>
      <div class="progress-bar mb-16">
        <div class="fill" style="width:${progressPct}%"></div>
      </div>

      <div class="card workout-exercise" style="text-align:center;padding:16px;">
        <div class="workout-exercise-image" style="margin-bottom:10px;">
          ${exImage
            ? `<img src="${exImage}" alt="${exName}" style="max-height:180px;max-width:100%;object-fit:contain;border-radius:8px;" onerror="this.style.display='none'">`
            : `<div style="font-size:64px;opacity:0.3;">&#127947;</div>`
          }
        </div>
        <div class="workout-exercise-name" style="font-size:22px;font-weight:bold;">${exName}</div>
        <div style="font-size:15px;color:gray;margin-top:4px;">${mgTranslate(exMuscle)}</div>

        <div class="set-indicator" style="margin:12px 0;">${setDots}</div>

        <div style="background:#f5f5f5;border-radius:12px;padding:12px 16px;margin:8px auto;max-width:300px;">
          <div style="font-size:13px;color:#512BD4;font-weight:700;margin-bottom:6px;">
            ${t('SetN', currentSet + 1, totalSets)}
          </div>
          <div style="display:flex;align-items:center;justify-content:center;gap:12px;">
            <div>
              <div style="font-size:11px;color:gray;">${t('Reps')}</div>
              <input type="number" id="workout-reps" value="${currentSetData.reps}" min="1" max="100"
                style="width:60px;font-size:20px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:6px;">
            </div>
            <div style="font-size:24px;font-weight:bold;color:#ccc;">x</div>
            <div>
              <div style="font-size:11px;color:gray;">kg</div>
              <input type="number" id="workout-weight" value="${currentSetData.weight > 0 ? currentSetData.weight : ''}" placeholder="0" step="0.5" min="0"
                style="width:70px;font-size:20px;font-weight:bold;text-align:center;border:1px solid #ddd;border-radius:8px;padding:6px;">
            </div>
          </div>
        </div>

        <div style="border-top:1px solid #eee;margin-top:16px;padding-top:12px;">
          <div style="font-size:12px;color:#512BD4;font-weight:bold;letter-spacing:2px;">${t('Rest')}</div>
          <div style="background:#f5f5f5;border-radius:16px;padding:8px 16px;display:inline-block;margin:6px 0;">
            <div id="timer-display" style="font-size:40px;font-weight:bold;color:#333;">01:30</div>
          </div>
          <div id="timer-picker-row" class="flex items-center justify-center gap-8" style="margin:6px 0;">
            <span style="font-size:13px;color:gray;">${t('Min')}</span>
            <select id="timer-min" class="picker-select" style="width:65px;font-size:14px;">${minOptions}</select>
            <span style="font-size:13px;color:gray;">${t('Sec')}</span>
            <select id="timer-sec" class="picker-select" style="width:65px;font-size:14px;">${secOptions}</select>
          </div>
          <div class="flex items-center justify-center gap-10" style="margin-top:6px;">
            <button id="timer-start" class="btn btn-sm" style="background:#512BD4;color:#fff;padding:6px 16px;border-radius:8px;">${t('Start')}</button>
            <button id="timer-reset" class="btn btn-sm" style="background:#6c757d;color:#fff;padding:6px 16px;border-radius:8px;">${t('Reset')}</button>
          </div>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-top:12px;">
        <button id="workout-prev" class="btn btn-outline" ${currentIndex === 0 && currentSet === 0 ? 'disabled' : ''}>
          ${currentSet > 0 ? t('PrevSet') : t('Previous')}
        </button>
        ${isLastExercise && isLastSet
          ? `<button id="workout-finish" class="btn btn-success">${t('Finish')}</button>`
          : `<button id="workout-next" class="btn btn-primary">${isLastSet ? t('Next') : t('NextSet')}</button>`
        }
      </div>
    </div>
  `;

  document.getElementById('workout-back')?.addEventListener('click', () => { stopTimer(); location.hash = '#routines'; });

  document.getElementById('workout-prev')?.addEventListener('click', () => {
    saveCurrentSetValues();
    stopTimer();
    if (currentSet > 0) { currentSet--; }
    else if (currentIndex > 0) { currentIndex--; currentSet = exercises[currentIndex].setDetails.length - 1; }
    renderExercise();
  });

  document.getElementById('workout-next')?.addEventListener('click', () => {
    saveCurrentSetValues();
    stopTimer();
    const ex2 = exercises[currentIndex];
    if (currentSet < ex2.setDetails.length - 1) { currentSet++; }
    else if (currentIndex < exercises.length - 1) { currentIndex++; currentSet = 0; }
    renderExercise();
  });

  document.getElementById('workout-finish')?.addEventListener('click', () => { saveCurrentSetValues(); finishWorkout(); });

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
      try { if (navigator.vibrate) navigator.vibrate([200, 100, 200]); } catch { /* */ }
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
  } catch { /* Don't block finish */ }

  sessionStorage.setItem('workout_summary', JSON.stringify({
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
  }));

  location.hash = '#summary';
}
