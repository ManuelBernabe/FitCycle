// FitCycle Workout Page — exercise-by-exercise workout with rest timer

import { t, dayName, muscleGroup as mgTranslate } from '../l10n.js';
import { api } from '../api.js';

let dayNum = 0;
let exercises = [];
let currentIndex = 0;
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

  try {
    const dayData = await api.get(`/routines/${dayNum}`);
    exercises = dayData?.exercises || dayData?.Exercises || [];

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
}

function renderExercise() {
  const container = document.getElementById('workout-content');
  if (!container || currentIndex >= exercises.length) return;

  const ex = exercises[currentIndex];
  const exName = ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '';
  const exMuscle = ex.muscleGroupName || ex.MuscleGroupName || '';
  const exSets = ex.sets || ex.Sets || 3;
  const exReps = ex.reps || ex.Reps || 12;
  const exWeight = ex.weight || ex.Weight || 0;
  const exImage = ex.imageUrl || ex.ImageUrl || '';
  const progressPct = ((currentIndex + 1) / exercises.length * 100).toFixed(0);
  const isLast = currentIndex === exercises.length - 1;

  // Build minute options 0-10
  const minOptions = Array.from({ length: 11 }, (_, i) =>
    `<option value="${i}" ${i === 1 ? 'selected' : ''}>${String(i).padStart(2, '0')}</option>`
  ).join('');

  // Build second options 0-59 in steps of 5
  const secOptions = Array.from({ length: 12 }, (_, i) => {
    const val = i * 5;
    return `<option value="${val}" ${val === 30 ? 'selected' : ''}>${String(val).padStart(2, '0')}</option>`;
  }).join('');

  container.innerHTML = `
    <div class="page-content">
      <!-- Progress header -->
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

      <!-- Exercise card -->
      <div class="card workout-exercise" style="text-align:center;padding:16px;">
        <div class="workout-exercise-image" style="margin-bottom:10px;">
          ${exImage
            ? `<img src="${exImage}" alt="${exName}" style="max-height:220px;max-width:100%;object-fit:contain;border-radius:8px;" onerror="this.style.display='none'">`
            : `<div style="font-size:64px;opacity:0.3;">&#127947;</div>`
          }
        </div>
        <div class="workout-exercise-name" style="font-size:22px;font-weight:bold;">${exName}</div>
        <div style="font-size:15px;color:gray;margin-top:4px;">${mgTranslate(exMuscle)}</div>
        <div style="font-size:18px;margin-top:8px;">${t('SetsRepsFormat', exSets, exReps)}${exWeight > 0 ? ` @ ${exWeight} ${t('WeightKg')}` : ''}</div>

        <!-- Rest timer section -->
        <div style="border-top:1px solid #eee;margin-top:16px;padding-top:12px;">
          <div style="font-size:12px;color:#512BD4;font-weight:bold;letter-spacing:2px;text-align:center;">
            ${t('Rest')}
          </div>

          <!-- Timer display -->
          <div style="background:#f5f5f5;border-radius:16px;padding:12px 20px;display:inline-block;margin:8px 0;">
            <div id="timer-display" style="font-size:48px;font-weight:bold;color:#333;">01:30</div>
          </div>

          <!-- Minute/Second pickers -->
          <div id="timer-picker-row" class="flex items-center justify-center gap-8" style="margin:8px 0;">
            <span style="font-size:13px;color:gray;">${t('Min')}</span>
            <select id="timer-min" class="picker-select" style="width:70px;font-size:14px;">${minOptions}</select>
            <span style="font-size:13px;color:gray;">${t('Sec')}</span>
            <select id="timer-sec" class="picker-select" style="width:70px;font-size:14px;">${secOptions}</select>
          </div>

          <!-- Timer buttons -->
          <div class="flex items-center justify-center gap-10" style="margin-top:8px;">
            <button id="timer-start" class="btn btn-sm" style="background:#512BD4;color:#fff;padding:8px 20px;border-radius:8px;">
              ${t('Start')}
            </button>
            <button id="timer-reset" class="btn btn-sm" style="background:#6c757d;color:#fff;padding:8px 20px;border-radius:8px;">
              ${t('Reset')}
            </button>
          </div>
        </div>
      </div>

      <!-- Navigation buttons -->
      <div class="workout-nav" style="display:grid;grid-template-columns:1fr 1fr ${isLast ? '1fr' : '1fr'};gap:10px;margin-top:12px;">
        <button id="workout-prev" class="btn btn-outline" ${currentIndex === 0 ? 'disabled' : ''}>${t('Previous')}</button>
        ${isLast
          ? `<button id="workout-finish" class="btn btn-success" style="grid-column:span 2;">${t('Finish')}</button>`
          : `<button id="workout-next" class="btn btn-primary" style="grid-column:span 2;">${t('Next')}</button>`
        }
      </div>
    </div>
  `;

  // Bind navigation
  document.getElementById('workout-back')?.addEventListener('click', () => {
    stopTimer();
    location.hash = '#routines';
  });

  document.getElementById('workout-prev')?.addEventListener('click', () => {
    if (currentIndex > 0) {
      stopTimer();
      currentIndex--;
      renderExercise();
    }
  });

  document.getElementById('workout-next')?.addEventListener('click', () => {
    if (currentIndex < exercises.length - 1) {
      stopTimer();
      currentIndex++;
      renderExercise();
    }
  });

  document.getElementById('workout-finish')?.addEventListener('click', finishWorkout);

  // Bind timer controls
  document.getElementById('timer-start')?.addEventListener('click', onTimerStartClicked);
  document.getElementById('timer-reset')?.addEventListener('click', onTimerResetClicked);

  // Picker change updates display when not running
  document.getElementById('timer-min')?.addEventListener('change', onTimePickerChanged);
  document.getElementById('timer-sec')?.addEventListener('change', onTimePickerChanged);

  // Initialize timer display
  stopTimer();
  resetTimerDisplay();
}

// ── Timer ──

function getPickerTotalSeconds() {
  const minEl = document.getElementById('timer-min');
  const secEl = document.getElementById('timer-sec');
  const mins = minEl ? parseInt(minEl.value) || 0 : 1;
  const secs = secEl ? parseInt(secEl.value) || 0 : 30;
  return mins * 60 + secs;
}

function onTimePickerChanged() {
  if (!timerRunning) {
    timerSeconds = getPickerTotalSeconds();
    updateTimerDisplay();
  }
}

function onTimerStartClicked() {
  const startBtn = document.getElementById('timer-start');
  const pickerRow = document.getElementById('timer-picker-row');

  if (timerRunning) {
    // Pause
    stopTimer();
    if (startBtn) {
      startBtn.textContent = t('Start');
      startBtn.style.background = '#512BD4';
    }
    if (pickerRow) pickerRow.style.display = '';
    return;
  }

  timerSeconds = getPickerTotalSeconds();
  if (timerSeconds <= 0) return;

  timerRunning = true;
  if (startBtn) {
    startBtn.textContent = t('Pause');
    startBtn.style.background = '#e67e22';
  }
  if (pickerRow) pickerRow.style.display = 'none';

  timerInterval = setInterval(() => {
    timerSeconds--;
    updateTimerDisplay();

    if (timerSeconds <= 0) {
      stopTimer();
      if (startBtn) {
        startBtn.textContent = t('Start');
        startBtn.style.background = '#512BD4';
      }
      if (pickerRow) pickerRow.style.display = '';

      // Change color to green to indicate completion
      const display = document.getElementById('timer-display');
      if (display) display.style.color = '#28a745';

      // Try vibration/sound
      try {
        if (navigator.vibrate) navigator.vibrate([200, 100, 200]);
      } catch (e) { /* ignore */ }
    }
  }, 1000);
}

function onTimerResetClicked() {
  stopTimer();
  resetTimerDisplay();
}

function stopTimer() {
  timerRunning = false;
  if (timerInterval) {
    clearInterval(timerInterval);
    timerInterval = null;
  }
}

function resetTimerDisplay() {
  timerSeconds = getPickerTotalSeconds();
  const startBtn = document.getElementById('timer-start');
  const pickerRow = document.getElementById('timer-picker-row');
  const display = document.getElementById('timer-display');

  if (startBtn) {
    startBtn.textContent = t('Start');
    startBtn.style.background = '#512BD4';
  }
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

// ── Finish Workout ──

async function finishWorkout() {
  stopTimer();
  const completedAt = new Date();

  const exerciseLogs = exercises.map(ex => ({
    exerciseId: ex.exerciseId || ex.ExerciseId || ex.id || ex.Id,
    exerciseName: ex.exerciseName || ex.ExerciseName || ex.name || ex.Name || '',
    sets: ex.sets || ex.Sets || 3,
    reps: ex.reps || ex.Reps || 12,
    weight: ex.weight || ex.Weight || 0,
    muscleGroupName: ex.muscleGroupName || ex.MuscleGroupName || '',
  }));

  try {
    await api.post('/workouts', {
      day: dayNum,
      startedAt: startedAt.toISOString(),
      completedAt: completedAt.toISOString(),
      exercises: exerciseLogs,
    });
  } catch (e) {
    /* Don't block finish if save fails */
  }

  // Store summary data for the summary page
  sessionStorage.setItem('workout_summary', JSON.stringify({
    day: dayNum,
    startedAt: startedAt.toISOString(),
    completedAt: completedAt.toISOString(),
    exercises: exerciseLogs,
  }));

  location.hash = '#summary';
}
