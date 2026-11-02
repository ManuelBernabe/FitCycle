// FitCycle Stats Page — workout statistics and history

import { t, dayName, exerciseName as exTranslate, muscleGroup as mgTranslate } from '../l10n.js';
import { api } from '../api.js';
import { escapeHtml, calculateStreak, skeleton } from '../utils.js';

// Accent color per muscle group (Spanish DB names) so each history day reads
// at a glance; chips/borders derive translucent shades from these.
const MG_COLORS = {
  'pecho': '#e74c3c', 'espalda': '#3498db', 'piernas': '#8e44ad',
  'cuádriceps': '#8e44ad', 'femoral': '#9b59b6', 'glúteos': '#e84393',
  'hombros': '#f39c12', 'bíceps': '#16a085', 'tríceps': '#e67e22',
  'abdominales': '#2ecc71', 'gemelos': '#1abc9c', 'cardio': '#ff6b35',
  'trapecio': '#2980b9', 'antebrazo': '#7f8c8d', 'lumbar': '#c0392b',
};

function mgColor(name) {
  return MG_COLORS[(name || '').trim().toLowerCase()] || '#512BD4';
}

function workoutMuscleGroups(exLogs) {
  const seen = [];
  for (const log of exLogs) {
    const mg = (log.muscleGroupName || log.MuscleGroupName || '').trim();
    if (mg && !seen.includes(mg)) seen.push(mg);
  }
  return seen;
}

function workoutVolume(exLogs) {
  let vol = 0;
  for (const log of exLogs) {
    let details = null;
    try {
      const raw = log.setDetails || log.SetDetails || '';
      if (raw) details = JSON.parse(raw);
    } catch (e) { /* fall through to aggregate fields */ }
    if (Array.isArray(details) && details.length > 0) {
      details.forEach(s => { vol += (Number(s.reps) || 0) * (Number(s.weight) || 0); });
    } else {
      vol += (log.sets || log.Sets || 0) * (log.reps || log.Reps || 0) * (Number(log.weight || log.Weight) || 0);
    }
  }
  return Math.round(vol);
}

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="section-title">${t('YourProgress')}</div>
        <div id="stats-content">
          ${skeleton('card')}${skeleton('list')}
        </div>
      </div>
    </div>
  `;
}

export async function mount() {
  const container = document.getElementById('stats-content');
  if (!container) return;

  try {
    const [stats, workouts, achievementsData] = await Promise.all([
      api.get('/workouts/stats'),
      api.get('/workouts'),
      api.get('/achievements').catch(() => null),
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

    // Calculate streak
    const streak = calculateStreak(workouts);

    // Summary stat cards
    html += `
      <div class="stat-grid mb-16">
        <div class="stat-card">
          <div class="stat-value" >${stats.totalWorkouts}</div>
          <div class="stat-label">${t('Workouts')}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value" >${stats.totalSets}</div>
          <div class="stat-label">${t('TotalSets')}</div>
        </div>
        <div class="stat-card">
          <div class="stat-value" >${formatNumber(stats.totalReps)}</div>
          <div class="stat-label">${t('TotalReps')}</div>
        </div>
      </div>
    `;

    // Recent workout history — FIRST, before any chart: each day shows the
    // muscle groups trained (colored chips), exercise count, duration, volume.
    const history = workouts || [];
    if (history.length > 0) {
      html += `
        <div class="card mb-8">
          <div class="card-title mb-8">${t('RecentHistory')}</div>
          ${history.slice(0, 10).map((w, idx) => {
            const completedDate = new Date(w.completedAt || w.CompletedAt);
            const startedDate = new Date(w.startedAt || w.StartedAt);
            const dateStr = completedDate.toLocaleDateString();
            const exLogs = w.exerciseLogs || w.ExerciseLogs || [];
            const durationMs = completedDate - startedDate;
            const dMin = Math.floor(durationMs / 60000);
            const durationStr = dMin > 0 ? `${dMin}${t('MinSuffix')}` : `<1${t('MinSuffix')}`;
            // Weekday derived from the workout's DATE — w.day is the ROUTINE trained
            // (día 1 = plan de Lunes) and doesn't have to match the calendar day the
            // user actually trained on.
            const wDay = completedDate.getDay();
            const mgs = workoutMuscleGroups(exLogs);
            const accent = mgColor(mgs[0]);
            const vol = workoutVolume(exLogs);
            const mgChips = mgs.map(mg => {
              const c = mgColor(mg);
              return `<span style="background:${c}22;color:${c};border:1px solid ${c}55;border-radius:10px;padding:2px 9px;font-size:12px;font-weight:700;">${escapeHtml(mgTranslate(mg))}</span>`;
            }).join('');

            return `
              <div class="history-item" data-idx="${idx}" style="cursor:pointer;padding:10px 12px;margin-bottom:8px;border-radius:12px;background:rgba(127,127,127,0.07);border-left:4px solid ${accent};">
                <div style="display:flex;justify-content:space-between;align-items:center;gap:8px;">
                  <div style="font-weight:700;font-size:15px;">${dayName(wDay)} · ${dateStr}</div>
                  <span class="hist-chevron" style="color:${accent};font-size:13px;transition:transform 0.2s;">&#9660;</span>
                </div>
                ${mgChips ? `<div style="display:flex;flex-wrap:wrap;gap:4px;margin-top:7px;">${mgChips}</div>` : ''}
                <div style="font-size:12px;color:var(--text-light,gray);margin-top:7px;">
                  &#128170; ${t('ExercisesCount', exLogs.length, durationStr)}${vol > 0 ? ` &nbsp;&middot;&nbsp; &#127947;&#65039; ${vol.toLocaleString()} ${t('WeightKg')}` : ''}
                </div>
              </div>
            `;
          }).join('')}
        </div>
      `;
    }

    // Streak card
    if (streak > 0) {
      html += `
        <div class="card mb-8" style="text-align:center;background:linear-gradient(135deg,#ff6b35,#ff8c00);color:#fff;padding:16px;">
          <div style="font-size:32px;">&#128293;</div>
          <div style="font-size:28px;font-weight:800;">${streak}</div>
          <div style="font-size:13px;opacity:0.9;">${t('Streak')}</div>
        </div>
      `;
    }

    // Weekly workouts bar chart
    if (stats.weeklyData && stats.weeklyData.length > 0) {
      const maxCount = Math.max(...stats.weeklyData.map(w => w.count), 1);
      html += `
        <div class="card mb-8">
          <div class="card-title mb-8">${t('WeeklyWorkoutsChart')}</div>
          <div class="bar-chart">
            ${stats.weeklyData.map(w => {
              const barWidth = maxCount > 0 ? Math.max((w.count / maxCount * 100), w.count > 0 ? 5 : 0) : 0;
              const weekLabel = formatWeekLabel(w.week);
              return `
                <div class="bar-row">
                  <div class="bar-label">${weekLabel}</div>
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
                <div class="bar-row top-exercise-row" data-exercise-id="${exId}" data-exercise-name="${exTranslate(e.name)}" style="cursor:pointer;" title="${t('WeightProgress')}">
                  <div class="bar-label" style="font-weight:bold;">${exTranslate(e.name)}</div>
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

    // Achievements card (last)
    if (achievementsData && achievementsData.achievements) {
      const unlocked = achievementsData.achievements.filter(a => a.unlocked).length;
      const total = achievementsData.achievements.length;
      html += `
        <div class="card mb-8">
          <div class="card-title mb-8">
            ${t('AchievementsTitle')}
            <span style="font-size:13px;font-weight:400;color:var(--text-light);float:right;">${unlocked}/${total}</span>
          </div>
          <div class="achievements-grid">
            ${achievementsData.achievements.map(a => `
              <div class="achievement-card ${a.unlocked ? 'unlocked' : 'locked'}" title="${a.labelEs} (${a.progress}%)">
                <div class="achievement-icon">${a.icon}</div>
                <div class="achievement-label">${a.labelEs}</div>
                <div class="achievement-progress"><div class="achievement-progress-bar" style="width:${a.progress}%"></div></div>
              </div>
            `).join('')}
          </div>
        </div>
      `;
    }

    container.innerHTML = html;

    // Make history items expandable to show exercise details
    container.querySelectorAll('.history-item').forEach((item) => {
      item.addEventListener('click', () => {
        const idx = parseInt(item.dataset.idx);
        const w = history[idx];
        if (!w) return;
        const exLogs = w.exerciseLogs || w.ExerciseLogs || [];
        const detailId = `history-detail-${idx}`;
        const chevron = item.querySelector('.hist-chevron');
        let existing = document.getElementById(detailId);
        if (existing) {
          existing.remove();
          if (chevron) chevron.style.transform = '';
          return;
        }
        if (chevron) chevron.style.transform = 'rotate(180deg)';
        const detailDiv = document.createElement('div');
        detailDiv.id = detailId;
        detailDiv.style.cssText = 'margin:-4px 0 8px;padding:10px 12px;border-radius:0 0 12px 12px;background:rgba(127,127,127,0.04);border-left:4px solid rgba(127,127,127,0.2);font-size:14px;';
        detailDiv.innerHTML = exLogs.map(log => {
          const logName = exTranslate(log.exerciseName || log.ExerciseName || '');
          const logMg = (log.muscleGroupName || log.MuscleGroupName || '').trim();
          const c = mgColor(logMg);
          const rawDetails = log.setDetails || log.SetDetails || '';
          let details = null;
          try { if (rawDetails) details = JSON.parse(rawDetails); } catch (e) { /* */ }
          let setChips = '';
          if (Array.isArray(details) && details.length > 0) {
            setChips = details.map(s =>
              `<span style="background:${c}18;color:${c};border-radius:6px;padding:2px 7px;font-size:12px;font-weight:700;white-space:nowrap;">${s.weight > 0 ? s.weight + ' ' + t('WeightKg') + ' × ' : ''}${s.reps}</span>`
            ).join('');
          } else {
            const logSets = log.sets || log.Sets || 0;
            const logReps = log.reps || log.Reps || 0;
            const logWeight = Number(log.weight || log.Weight) || 0;
            setChips = `<span style="background:${c}18;color:${c};border-radius:6px;padding:2px 7px;font-size:12px;font-weight:700;white-space:nowrap;">${logSets}×${logReps}${logWeight > 0 ? ' @ ' + logWeight + ' ' + t('WeightKg') : ''}</span>`;
          }
          return `
            <div style="margin-bottom:9px;">
              <div style="display:flex;align-items:center;gap:6px;margin-bottom:3px;">
                <span style="width:8px;height:8px;border-radius:50%;background:${c};flex-shrink:0;"></span>
                <b style="font-size:13px;">${escapeHtml(logName)}</b>
              </div>
              <div style="display:flex;flex-wrap:wrap;gap:4px;margin-left:14px;">${setChips}</div>
            </div>
          `;
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

function formatWeekLabel(key) {
  if (key === 'week_0') return t('ThisWeek');
  const match = key.match(/^week_(\d+)$/);
  if (match) return t('WeeksAgo', parseInt(match[1]) + 1);
  return key;
}
