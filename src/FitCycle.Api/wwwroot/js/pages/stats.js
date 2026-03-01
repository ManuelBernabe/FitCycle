// FitCycle Stats Page — workout statistics and history

import { t, dayName } from '../l10n.js';
import { api } from '../api.js';

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="section-title">${t('YourProgress')}</div>
        <div id="stats-content">
          <div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div>
        </div>
      </div>
    </div>
  `;
}

export async function mount() {
  const container = document.getElementById('stats-content');
  if (!container) return;

  try {
    const [stats, workouts] = await Promise.all([
      api.get('/workouts/stats'),
      api.get('/workouts'),
    ]);

    if (!stats || stats.totalWorkouts === 0) {
      container.innerHTML = `
        <div class="empty-state">
          <div class="empty-state-icon">&#128200;</div>
          <div class="empty-state-title">${t('NoWorkoutsYet')}</div>
        </div>
      `;
      return;
    }

    let html = '';

    // Summary stat cards
    html += `
      <div class="stat-grid mb-16">
        <div class="stat-card">
          <div class="stat-value" style="color:#512BD4;">${stats.totalWorkouts}</div>
          <div class="stat-label">${t('Workouts')}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value" style="color:#512BD4;">${stats.totalSets}</div>
          <div class="stat-label">${t('TotalSets')}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value" style="color:#512BD4;">${formatNumber(stats.totalReps)}</div>
          <div class="stat-label">${t('TotalReps')}</div>
        </div>
      </div>
    `;

    // Weekly workouts bar chart
    if (stats.weeklyData && stats.weeklyData.length > 0) {
      const maxCount = Math.max(...stats.weeklyData.map(w => w.count), 1);
      html += `
        <div class="card mb-8">
          <div class="card-title mb-8">${t('WeeklyWorkoutsChart')}</div>
          <div class="bar-chart">
            ${stats.weeklyData.map(w => {
              const barWidth = maxCount > 0 ? Math.max((w.count / maxCount * 100), w.count > 0 ? 5 : 0) : 0;
              return `
                <div class="bar-row">
                  <div class="bar-label">${w.week}</div>
                  <div class="bar-track">
                    <div class="bar-fill" style="width:${barWidth.toFixed(0)}%;background:#512BD4;"></div>
                  </div>
                  <div class="bar-value">${w.count}</div>
                </div>
              `;
            }).join('')}
          </div>
        </div>
      `;
    }

    // Top exercises bar chart (clickable for weight progression)
    if (stats.topExercises && stats.topExercises.length > 0) {
      const maxEx = Math.max(...stats.topExercises.map(e => e.count), 1);
      html += `
        <div class="card mb-8">
          <div class="card-title mb-8">${t('MostFrequent')}</div>
          <div class="bar-chart">
            ${stats.topExercises.map((e, i) => {
              const barWidth = maxEx > 0 ? (e.count / maxEx * 100) : 0;
              const exId = e.exerciseId || e.ExerciseId || 0;
              return `
                <div class="bar-row top-exercise-row" data-exercise-id="${exId}" data-exercise-name="${e.name}" style="cursor:pointer;" title="${t('WeightProgress')}">
                  <div class="bar-label" style="font-weight:bold;">${e.name}</div>
                  <div class="bar-track">
                    <div class="bar-fill" style="width:${barWidth.toFixed(0)}%;background:#28a745;"></div>
                  </div>
                  <div class="bar-value">${e.count}</div>
                </div>
              `;
            }).join('')}
          </div>
        </div>
        <div id="weight-progress-section"></div>
      `;
    }

    // Recent workout history (last 10)
    const history = workouts || [];
    if (history.length > 0) {
      html += `
        <div class="card">
          <div class="card-title mb-8">${t('RecentHistory')}</div>
          ${history.slice(0, 10).map(w => {
            const completedDate = new Date(w.completedAt || w.CompletedAt);
            const startedDate = new Date(w.startedAt || w.StartedAt);
            const dateStr = completedDate.toLocaleDateString();
            const exLogs = w.exerciseLogs || w.ExerciseLogs || [];
            const exCount = exLogs.length;
            const durationMs = completedDate - startedDate;
            const dMin = Math.floor(durationMs / 60000);
            const durationStr = dMin > 0 ? `${dMin}${t('MinSuffix')}` : `<1${t('MinSuffix')}`;
            const wDay = w.day ?? w.Day ?? 0;

            return `
              <div class="exercise-row history-item" data-workout-id="${w.id || ''}" style="cursor:pointer;">
                <div class="exercise-info">
                  <div class="exercise-name">${dayName(wDay)} -- ${dateStr}</div>
                  <div class="exercise-detail">${t('ExercisesCount', exCount, durationStr)}</div>
                </div>
              </div>
            `;
          }).join('')}
        </div>
      `;
    }

    container.innerHTML = html;

    // Make history items expandable to show exercise details
    container.querySelectorAll('.history-item').forEach((item, idx) => {
      item.addEventListener('click', () => {
        const w = history[idx];
        if (!w) return;
        const exLogs = w.exerciseLogs || w.ExerciseLogs || [];
        const detailId = `history-detail-${idx}`;
        let existing = document.getElementById(detailId);
        if (existing) {
          existing.remove();
          return;
        }
        const detailDiv = document.createElement('div');
        detailDiv.id = detailId;
        detailDiv.style.cssText = 'margin-left:16px;margin-bottom:8px;font-size:14px;';
        detailDiv.innerHTML = exLogs.map(log => {
          const logName = log.exerciseName || log.ExerciseName || '';
          const logMg = log.muscleGroupName || log.MuscleGroupName || '';
          let setInfo = '';
          const rawDetails = log.setDetails || log.SetDetails || '';
          let details = null;
          try { if (rawDetails) details = JSON.parse(rawDetails); } catch (e) { /* */ }
          if (Array.isArray(details) && details.length > 0) {
            setInfo = details.map((s, i) => `S${i + 1}: ${s.reps}r/${s.weight > 0 ? '<span style="color:#28a745;font-weight:600">' + s.weight + 'kg</span>' : '-'}`).join(' | ');
          } else {
            const logSets = log.sets || log.Sets || 0;
            const logReps = log.reps || log.Reps || 0;
            const logWeight = log.weight || log.Weight || 0;
            setInfo = `${logSets}x${logReps}${logWeight > 0 ? ' @ <span style="color:#28a745;font-weight:600">' + logWeight + ' ' + t('WeightKg') + '</span>' : ''}`;
          }
          return `<div style="margin-bottom:4px;">&bull; <b>${logName}</b> — ${setInfo}${logMg ? ` <span style="font-size:11px;color:#512BD4;">(${logMg})</span>` : ''}</div>`;
        }).join('');
        item.after(detailDiv);
      });
    });

    // Click top exercise to show weight progress
    container.querySelectorAll('.top-exercise-row').forEach(row => {
      row.addEventListener('click', () => {
        const exerciseId = parseInt(row.dataset.exerciseId);
        const exerciseName = row.dataset.exerciseName || '';
        if (exerciseId > 0) {
          showWeightProgress(exerciseId, exerciseName);
        }
      });
    });

  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
  }
}

async function showWeightProgress(exerciseId, exerciseName) {
  const section = document.getElementById('weight-progress-section');
  if (!section) return;

  section.innerHTML = `<div class="card mb-8"><div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div></div>`;

  try {
    const data = await api.get(`/workouts/exercise/${exerciseId}/progress`);
    const points = data || [];

    if (points.length === 0) {
      section.innerHTML = `
        <div class="card mb-8">
          <div class="card-title mb-8">${t('WeightProgress')} — ${exerciseName}</div>
          <div style="text-align:center;color:gray;padding:16px;">No data</div>
        </div>
      `;
      return;
    }

    // Build a simple SVG line chart
    const maxWeight = Math.max(...points.map(p => p.weight || p.Weight || 0), 1);
    const minWeight = Math.min(...points.map(p => p.weight || p.Weight || 0));
    const range = maxWeight - minWeight || 1;
    const chartWidth = 400;
    const chartHeight = 150;
    const padding = 30;
    const innerWidth = chartWidth - padding * 2;
    const innerHeight = chartHeight - padding * 2;

    const svgPoints = points.map((p, i) => {
      const x = padding + (points.length > 1 ? (i / (points.length - 1)) * innerWidth : innerWidth / 2);
      const w = p.weight || p.Weight || 0;
      const y = padding + innerHeight - ((w - minWeight) / range) * innerHeight;
      return { x, y, w, date: p.date || p.Date || '' };
    });

    const polyline = svgPoints.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    const circles = svgPoints.map(p =>
      `<circle cx="${p.x.toFixed(1)}" cy="${p.y.toFixed(1)}" r="4" fill="#512BD4" />`
    ).join('');
    const labels = svgPoints.filter((_, i) => i === 0 || i === svgPoints.length - 1 || points.length <= 5).map(p =>
      `<text x="${p.x.toFixed(1)}" y="${(p.y - 8).toFixed(1)}" text-anchor="middle" font-size="10" fill="#333">${p.w}</text>`
    ).join('');
    const dateLabels = svgPoints.filter((_, i) => i === 0 || i === svgPoints.length - 1).map(p => {
      const d = new Date(p.date);
      const dateStr = isNaN(d.getTime()) ? '' : `${d.getMonth() + 1}/${d.getDate()}`;
      return `<text x="${p.x.toFixed(1)}" y="${(chartHeight - 5).toFixed(1)}" text-anchor="middle" font-size="10" fill="gray">${dateStr}</text>`;
    }).join('');

    section.innerHTML = `
      <div class="card mb-8">
        <div class="card-title mb-8">${t('WeightProgress')} — ${exerciseName}</div>
        <div style="overflow-x:auto;">
          <svg width="100%" viewBox="0 0 ${chartWidth} ${chartHeight}" style="max-width:${chartWidth}px;">
            <polyline points="${polyline}" fill="none" stroke="#512BD4" stroke-width="2" />
            ${circles}
            ${labels}
            ${dateLabels}
          </svg>
        </div>
      </div>
    `;
  } catch (err) {
    section.innerHTML = `
      <div class="card mb-8">
        <div class="card-title mb-8">${t('WeightProgress')} — ${exerciseName}</div>
        <div style="text-align:center;color:gray;padding:16px;">${t('ErrorFmt', err.message)}</div>
      </div>
    `;
  }
}

export function destroy() {}

function formatNumber(n) {
  if (n >= 1000) return (n / 1000).toFixed(1) + 'k';
  return String(n);
}
