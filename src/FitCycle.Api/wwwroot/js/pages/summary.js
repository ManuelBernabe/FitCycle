// FitCycle Summary Page — shown after completing a workout

import { t, dayName, muscleGroup as mgTranslate, exerciseName as exTranslate } from '../l10n.js';
import { api } from '../api.js';
import { escapeHtml } from '../utils.js';

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
  const totalVolume = exercisesVolume(data.exercises);

  return `
    <div class="page no-tabs">
      <div class="summary-hero" style="margin-top:56px;">
        <div class="summary-check">&#10003;</div>
        <div class="summary-title">${t('WorkoutCompleted')}</div>
        <div style="margin-top:4px;opacity:0.8">${dayName(completedAt.getDay())} — ${completedAt.toLocaleDateString()}</div>
      </div>
      <div class="summary-stats" style="grid-template-columns:repeat(2,1fr);">
        <div class="summary-stat">
          <div class="summary-stat-value">${durationStr}</div>
          <div class="summary-stat-label">${t('Duration')}</div>
        </div>
        <div class="summary-stat">
          <div class="summary-stat-value">${totalVolume > 0 ? totalVolume.toLocaleString() + ' ' + t('WeightKg') : '—'}</div>
          <div class="summary-stat-label">&#127947;&#65039; ${t('SessionVolume')}</div>
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

        <!-- Comparison vs previous session of the same routine day (loaded async) -->
        <div id="summary-compare"></div>

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
                <div class="exercise-name">${escapeHtml(exTranslate(ex.exerciseName))}${ex.muscleGroupName ? ` <span class="text-primary" style="font-size:11px;">(${escapeHtml(mgTranslate(ex.muscleGroupName))})</span>` : ''}</div>
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

  // PRs + comparison against the previous session of the same routine day.
  try {
    const raw = sessionStorage.getItem('workout_summary');
    if (raw) {
      const data = JSON.parse(raw);
      const workouts = await api.get('/workouts').catch(() => []);

      // Server-side PRs from the POST /workouts response are the source of truth.
      // Fall back to client-side detection for older clients / offline-queued workouts.
      let prs = Array.isArray(data.prs) && data.prs.length > 0
        ? data.prs.map(p => ({ name: p.exerciseName, weight: p.newMax, prev: p.previousMax }))
        : detectPRs(data.exercises, workouts || []);
      if (prs && prs.length > 0) {
        const prContainer = document.getElementById('pr-banner');
        if (prContainer) {
          prContainer.style.display = 'block';
          prContainer.innerHTML = `
            <div class="pr-banner-title">&#127942; ${t('NewPR')} ${prs.length > 1 ? '(' + prs.length + ')' : ''}</div>
            <div class="pr-banner-list">${prs.map(pr =>
              t('PRExercise', escapeHtml(exTranslate(pr.name)), pr.weight)
            ).join('<br>')}</div>
          `;
        }
      }

      renderComparison(data, workouts || []);
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
          const weekLabel = w.week === 'week_0' ? t('ThisWeek') : (() => { const m = w.week.match(/^week_(\d+)$/); return m ? t('WeeksAgo', parseInt(m[1]) + 1) : w.week; })();
          return `
            <div class="bar-row">
              <div class="bar-label">${weekLabel}</div>
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

/** Total kg moved: sum of reps × weight across every set (setDetails preferred). */
function exercisesVolume(exs) {
  let vol = 0;
  for (const ex of exs || []) {
    let details = null;
    try {
      details = typeof ex.setDetails === 'string' ? JSON.parse(ex.setDetails || '[]') : (ex.setDetails || []);
    } catch (e) { details = null; }
    if (Array.isArray(details) && details.length > 0) {
      details.forEach(s => { vol += (Number(s.reps) || 0) * (Number(s.weight) || 0); });
    } else {
      vol += (ex.sets || 0) * (ex.reps || 0) * (Number(ex.weight) || 0);
    }
  }
  return Math.round(vol);
}

/**
 * "Compared to last Monday": volume and duration deltas against the most recent
 * PREVIOUS workout of the same routine day. Silent when there is none.
 */
function renderComparison(data, workouts) {
  const container = document.getElementById('summary-compare');
  if (!container) return;
  const completedTime = new Date(data.completedAt).getTime();
  const prev = (workouts || []).find(w => {
    const wDay = w.day ?? w.Day;
    const wTime = new Date(w.completedAt || w.CompletedAt).getTime();
    // Exclude the workout we just saved (same timestamp ± a minute).
    return wDay === data.day && wTime < completedTime - 60000;
  });
  if (!prev) return;

  const prevDate = new Date(prev.completedAt || prev.CompletedAt);
  const prevVol = exercisesVolume(prev.exerciseLogs || prev.ExerciseLogs || []);
  const curVol = exercisesVolume(data.exercises);
  const prevDurMin = Math.floor((new Date(prev.completedAt || prev.CompletedAt) - new Date(prev.startedAt || prev.StartedAt)) / 60000);
  const curDurMin = Math.floor((new Date(data.completedAt) - new Date(data.startedAt)) / 60000);

  const rows = [];
  if (curVol > 0 && prevVol > 0) {
    const diff = curVol - prevVol;
    const pct = Math.round((diff / prevVol) * 100);
    const color = diff > 0 ? '#28a745' : diff < 0 ? '#dc3545' : 'var(--text-light,gray)';
    const sign = diff > 0 ? '+' : '';
    rows.push(`
      <div style="display:flex;justify-content:space-between;align-items:center;padding:4px 0;">
        <span>&#127947;&#65039; ${t('SessionVolume')}</span>
        <span><b>${curVol.toLocaleString()} ${t('WeightKg')}</b>
          <span style="color:${color};font-weight:700;margin-left:6px;">${sign}${diff.toLocaleString()} (${sign}${pct}%)</span>
        </span>
      </div>
    `);
  }
  if (curDurMin > 0 && prevDurMin > 0) {
    const dDiff = curDurMin - prevDurMin;
    const dSign = dDiff > 0 ? '+' : '';
    rows.push(`
      <div style="display:flex;justify-content:space-between;align-items:center;padding:4px 0;">
        <span>&#9201;&#65039; ${t('Duration')}</span>
        <span><b>${curDurMin}${t('MinSuffix')}</b>
          <span style="color:var(--text-light,gray);font-weight:700;margin-left:6px;">${dDiff !== 0 ? dSign + dDiff + t('MinSuffix') : '='}</span>
        </span>
      </div>
    `);
  }
  if (rows.length === 0) return;

  container.innerHTML = `
    <div class="card mb-8">
      <div class="card-title mb-8">&#128200; ${t('VsPrevSession', `${dayName(prevDate.getDay())} ${prevDate.toLocaleDateString()}`)}</div>
      <div style="font-size:14px;">${rows.join('')}</div>
    </div>
  `;
}

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
      prs.push({ name: exTranslate(ex.exerciseName || ex.ExerciseName || ''), weight: currentMax });
    }
  });

  return prs;
}
