// FitCycle Calendar Page — monthly view of completed workouts.

import { t, muscleGroup as mgTranslate } from '../l10n.js';
import { api } from '../api.js';
import { skeleton } from '../utils.js';

let currentMonth = null; // { year, month0 } where month0 is 0-based

export function render() {
  if (!currentMonth) {
    const now = new Date();
    currentMonth = { year: now.getFullYear(), month0: now.getMonth() };
  }

  return `
    <div class="page">
      <div class="page-content">
        <div class="section-title">${t('CalendarTitle')}</div>
        <div class="section-subtitle">${t('CalendarSubtitle')}</div>
        <div id="calendar-content">
          ${skeleton('card')}
        </div>
      </div>
    </div>
  `;
}

export async function mount() {
  await loadCalendar();
}

export function destroy() {}

async function loadCalendar() {
  const container = document.getElementById('calendar-content');
  if (!container) return;

  const { year, month0 } = currentMonth;
  const monthStart = new Date(year, month0, 1);
  const monthEnd = new Date(year, month0 + 1, 0, 23, 59, 59);

  let sessions = [];
  try {
    const res = await api.get(`/workouts/calendar?from=${monthStart.toISOString()}&to=${monthEnd.toISOString()}`);
    sessions = res?.sessions || [];
  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
    return;
  }

  // Group sessions by date string yyyy-mm-dd for fast lookup
  const byDay = new Map();
  for (const s of sessions) {
    const d = new Date(s.date);
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    if (!byDay.has(key)) byDay.set(key, []);
    byDay.get(key).push(s);
  }

  const monthName = monthStart.toLocaleString(undefined, { month: 'long', year: 'numeric' });
  const todayKey = (() => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; })();

  // Build grid: first weekday of month (Monday=0 in our locale), then days
  const firstWeekday = (monthStart.getDay() + 6) % 7; // 0=Mon ... 6=Sun
  const daysInMonth = new Date(year, month0 + 1, 0).getDate();

  const dayHeaders = ['L', 'M', 'X', 'J', 'V', 'S', 'D']
    .map(d => `<div class="cal-day-header">${d}</div>`).join('');

  const cells = [];
  for (let i = 0; i < firstWeekday; i++) cells.push(`<div class="cal-cell cal-empty"></div>`);
  for (let day = 1; day <= daysInMonth; day++) {
    const dateStr = `${year}-${String(month0 + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    const daySessions = byDay.get(dateStr) || [];
    const isToday = dateStr === todayKey;
    const hasWorkout = daySessions.length > 0;
    const muscleGroups = [...new Set(daySessions.flatMap(s => s.muscleGroups || []))].slice(0, 3);
    const mgChips = muscleGroups.map(m => `<span class="cal-mg" title="${mgTranslate(m)}">${mgTranslate(m).slice(0, 3)}</span>`).join('');

    cells.push(`
      <div class="cal-cell ${hasWorkout ? 'cal-has-workout' : ''} ${isToday ? 'cal-today' : ''}">
        <div class="cal-day-num">${day}</div>
        ${hasWorkout ? `<div class="cal-dot"></div>` : ''}
        ${mgChips ? `<div class="cal-mg-row">${mgChips}</div>` : ''}
      </div>
    `);
  }

  // Summary
  const totalWorkouts = sessions.length;
  const daysActive = byDay.size;

  container.innerHTML = `
    <div class="cal-nav">
      <button id="cal-prev" class="btn btn-sm" aria-label="${t('PreviousMonth')}">&#9664;</button>
      <div class="cal-month-name">${monthName}</div>
      <button id="cal-next" class="btn btn-sm" aria-label="${t('NextMonth')}">&#9654;</button>
    </div>

    <div class="cal-stats">
      <div><strong>${totalWorkouts}</strong> ${t('Workouts')}</div>
      <div><strong>${daysActive}</strong> ${t('DaysActive')}</div>
    </div>

    <div class="cal-grid">
      ${dayHeaders}
      ${cells.join('')}
    </div>

    <div class="cal-legend">
      <span class="cal-legend-item"><span class="cal-dot"></span> ${t('WorkoutDone')}</span>
      <span class="cal-legend-item"><span class="cal-today-marker"></span> ${t('Today')}</span>
    </div>
  `;

  document.getElementById('cal-prev')?.addEventListener('click', () => {
    currentMonth.month0--;
    if (currentMonth.month0 < 0) { currentMonth.month0 = 11; currentMonth.year--; }
    loadCalendar();
  });
  document.getElementById('cal-next')?.addEventListener('click', () => {
    currentMonth.month0++;
    if (currentMonth.month0 > 11) { currentMonth.month0 = 0; currentMonth.year++; }
    loadCalendar();
  });
}
