// FitCycle Summary Page — shown after completing a workout

import { t, dayName } from '../l10n.js';
import { api } from '../api.js';

export function render() {
  const raw = sessionStorage.getItem('workout_summary');
  if (!raw) {
    return `
      <div class="page no-tabs">
        <div class="page-content">
          <div class="empty-state">
            <div class="empty-state-text">${t('NoWorkoutsYet')}</div>
            <button class="btn btn-primary mt-16" id="summary-back">${t('BackToRoutines')}</button>
          </div>
        </div>
      </div>
    `;
  }

  const data = JSON.parse(raw);
  const startedAt = new Date(data.startedAt);
  const completedAt = new Date(data.completedAt);
  const durationMs = completedAt - startedAt;
  const durationMin = Math.floor(durationMs / 60000);
  const durationSec = Math.floor((durationMs % 60000) / 1000);
  const durationStr = durationMin > 0 ? `${durationMin}${t('MinSuffix')}` : `${durationSec}${t('SecSuffix')}`;
  const exerciseCount = data.exercises.length;
  const totalSets = data.exercises.reduce((acc, ex) => acc + (ex.sets || 0), 0);

  return `
    <div class="page no-tabs">
      <div class="summary-hero" style="margin-top:56px;">
        <div class="summary-check">&#10003;</div>
        <div class="summary-title">${t('WorkoutCompleted')}</div>
        <div style="margin-top:4px;opacity:0.8">${dayName(data.day)}</div>
      </div>
      <div class="summary-stats">
        <div class="summary-stat">
          <div class="summary-stat-value">${durationStr}</div>
          <div class="summary-stat-label">${t('Duration')}</div>
        </div>
        <div class="summary-stat">
          <div class="summary-stat-value">${exerciseCount}</div>
          <div class="summary-stat-label">${t('Exercises')}</div>
        </div>
        <div class="summary-stat">
          <div class="summary-stat-value">${totalSets}</div>
          <div class="summary-stat-label">${t('TotalSets')}</div>
        </div>
      </div>
      <div class="page-content">
        <!-- PR Banner -->
        <div id="pr-banner" class="pr-banner" style="display:none;"></div>

        <!-- Weekly chart (loaded async) -->
        <div class="card mb-8" id="summary-weekly-card" style="display:none;">
          <div class="card-title mb-8">${t('WeeklyWorkouts')}</div>
          <div id="summary-weekly-chart"></div>
        </div>

        <!-- Exercises done -->
        <div class="card">
          <div class="card-title mb-8">${t('ExercisesDone')}</div>
          ${data.exercises.map(ex => {
            let setInfo = '';
            let details = null;
            try { if (ex.setDetails) details = typeof ex.setDetails === 'string' ? JSON.parse(ex.setDetails) : ex.setDetails; } catch (e) { /* */ }
            if (Array.isArray(details) && details.length > 0) {
              setInfo = details.map((s, i) => `S${i + 1}: ${s.reps}r x ${s.weight > 0 ? '<span class="weight-val">' + s.weight + 'kg</span>' : '-'}`).join(' | ');
            } else {
              const weightStr = (ex.weight || 0) > 0 ? ` @ ${ex.weight} ${t('WeightKg')}` : '';
              setInfo = `${t('SetsRepsFormat', ex.sets, ex.reps)}${(ex.weight || 0) > 0 ? ' @ <span class="weight-val">' + ex.weight + ' ' + t('WeightKg') + '</span>' : ''}`;
            }
            return `
            <div class="exercise-row">
              <div class="exercise-img">&#127947;</div>
              <div class="exercise-info">
                <div class="exercise-name">${ex.exerciseName}${ex.muscleGroupName ? ` <span class="text-primary" style="font-size:11px;">(${ex.muscleGroupName})</span>` : ''}</div>
                <div class="exercise-detail">${setInfo}</div>
              </div>
            </div>
          `;
          }).join('')}
        </div>
        <button id="summary-back" class="btn btn-primary btn-block btn-lg mt-16">${t('BackToRoutines')}</button>
      </div>
    </div>
  `;
}

export async function mount() {
  document.getElementById('summary-back')?.addEventListener('click', () => {
    sessionStorage.removeItem('workout_summary');
    location.hash = '#home';
  });

  // Check for PRs
  try {
    const raw = sessionStorage.getItem('workout_summary');
    if (raw) {
      const data = JSON.parse(raw);
      const workouts = await api.get('/workouts');
      const prs = detectPRs(data.exercises, workouts || []);
      if (prs.length > 0) {
        const prContainer = document.getElementById('pr-banner');
        if (prContainer) {
          prContainer.style.display = 'block';
          prContainer.innerHTML = prs.map(pr =>
            `<div style="font-size:14px;font-weight:600;">${t('PRExercise', pr.name, pr.weight)}</div>`
          ).join('');
        }
      }
    }
  } catch (e) { /* PR detection is optional */ }

  // Load stats async for weekly chart
  try {
    const stats = await api.get('/workouts/stats');
    if (stats && stats.weeklyData && stats.weeklyData.length > 0) {
      const weeklyCard = document.getElementById('summary-weekly-card');
      const chartContainer = document.getElementById('summary-weekly-chart');
      if (weeklyCard && chartContainer) {
        const maxCount = Math.max(...stats.weeklyData.map(w => w.count), 1);

        chartContainer.innerHTML = stats.weeklyData.map(w => {
          const barWidth = maxCount > 0 ? Math.max((w.count / maxCount * 100), w.count > 0 ? 5 : 0) : 0;
          return `
            <div class="bar-row">
              <div class="bar-label">${w.week}</div>
              <div class="bar-track">
                <div class="bar-fill" style="width:${barWidth.toFixed(0)}%"></div>
              </div>
              <div class="bar-value">${w.count}</div>
            </div>
          `;
        }).join('');

        weeklyCard.style.display = '';
      }
    }
  } catch (e) {
    /* Stats loading is optional */
  }
}

export function destroy() {}

function detectPRs(currentExercises, pastWorkouts) {
  const prs = [];
  if (!currentExercises || !pastWorkouts || pastWorkouts.length <= 1) return prs;

  // Build max weight per exercise from past workouts (excluding the latest which is the current one)
  const maxWeights = {};
  // Skip the first workout (the one we just completed)
  pastWorkouts.slice(1).forEach(w => {
    const logs = w.exerciseLogs || w.ExerciseLogs || [];
    logs.forEach(log => {
      const exId = log.exerciseId || log.ExerciseId || 0;
      let maxW = log.weight || log.Weight || 0;
      // Also check setDetails for max
      try {
        const details = typeof log.setDetails === 'string' ? JSON.parse(log.setDetails || '[]') : (log.setDetails || []);
        if (Array.isArray(details)) {
          details.forEach(s => { if (s.weight > maxW) maxW = s.weight; });
        }
      } catch (e) { /* */ }
      if (!maxWeights[exId] || maxW > maxWeights[exId]) maxWeights[exId] = maxW;
    });
  });

  // Check current exercises for new PRs
  currentExercises.forEach(ex => {
    const exId = ex.exerciseId || ex.ExerciseId || 0;
    let currentMax = ex.weight || 0;
    try {
      const details = typeof ex.setDetails === 'string' ? JSON.parse(ex.setDetails || '[]') : (ex.setDetails || []);
      if (Array.isArray(details)) {
        details.forEach(s => { if (s.weight > currentMax) currentMax = s.weight; });
      }
    } catch (e) { /* */ }

    const previousMax = maxWeights[exId] || 0;
    if (currentMax > 0 && previousMax > 0 && currentMax > previousMax) {
      prs.push({ name: ex.exerciseName || ex.ExerciseName || '', weight: currentMax });
    }
  });

  return prs;
}
