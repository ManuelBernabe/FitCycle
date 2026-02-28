// FitCycle Routines Page — weekly routine overview with 7 day cards

import { t, dayName, muscleGroup } from '../l10n.js';
import { api } from '../api.js';

let weekData = null;

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="section-title">${t('MyWeeklyRoutine')}</div>
        <div class="section-subtitle">${t('ConfigureWeekly')}</div>
        <div id="routines-list">
          <div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div>
        </div>
      </div>
    </div>
  `;
}

export async function mount() {
  await loadRoutines();
}

export function destroy() {}

async function loadRoutines() {
  const container = document.getElementById('routines-list');
  if (!container) return;

  try {
    weekData = await api.get('/routines');
    renderDays(container);
  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
  }
}

function renderDays(container) {
  const days = weekData?.days || weekData?.Days || (Array.isArray(weekData) ? weekData : []);

  if (!days || days.length === 0) {
    // Show empty state but still render day cards with Create buttons
  }

  // Days order: Monday(1) through Sunday(0)
  const dayOrder = [1, 2, 3, 4, 5, 6, 0];

  let html = '';
  for (const dayNum of dayOrder) {
    const dayData = days.find(d => (d.day ?? d.Day) === dayNum);
    const groups = dayData?.muscleGroups || dayData?.MuscleGroups || [];
    const exercises = dayData?.exercises || dayData?.Exercises || [];
    const hasRoutine = groups.length > 0;
    const hasExercises = exercises.length > 0;

    const groupNames = groups.length > 0
      ? groups.map(g => muscleGroup(g.name || g.Name)).join(', ')
      : `<span class="text-muted">${t('NoGroupsAssigned')}</span>`;

    // Compact exercise lines with per-set weight details
    const exerciseLines = exercises.map(e => {
      const name = e.exerciseName || e.ExerciseName || e.name || '';
      const sets = e.sets || e.Sets || 0;
      const reps = e.reps || e.Reps || 0;
      const weight = e.weight || e.Weight || 0;
      const mgName = e.muscleGroupName || e.MuscleGroupName || '';
      const mgTag = mgName ? `<span class="exercise-mg-tag">${muscleGroup(mgName)}</span>` : '';

      // Try to parse setDetails for per-set display
      let setInfo = '';
      const rawDetails = e.setDetails || e.SetDetails || '';
      let details = null;
      try { if (rawDetails) details = JSON.parse(rawDetails); } catch { /* */ }

      if (Array.isArray(details) && details.length > 0) {
        const hasVaryingWeight = new Set(details.map(s => s.weight)).size > 1;
        if (hasVaryingWeight) {
          // Show per-set weights compactly
          setInfo = details.map(s => `${s.weight > 0 ? s.weight + 'kg' : '-'}`).join('/');
          setInfo = `<span class="exercise-meta">${details.length}S ${details[0].reps}r [${setInfo}]</span>`;
        } else {
          const w = details[0].weight || weight;
          setInfo = `<span class="exercise-meta">${details.length}x${details[0].reps}${w > 0 ? ' @' + w + 'kg' : ''}</span>`;
        }
      } else {
        const weightStr = weight > 0 ? ` @${weight}kg` : '';
        setInfo = `<span class="exercise-meta">${sets}x${reps}${weightStr}</span>`;
      }

      return `<div class="exercise-line-compact">${name} ${setInfo}${mgTag}</div>`;
    }).join('');

    html += `
      <div class="day-card day-card-vertical" data-day="${dayNum}">
        <div class="day-card-header">
          <div class="day-card-name">${dayName(dayNum)}</div>
          <div class="day-card-groups">${groupNames}</div>
        </div>
        ${hasExercises ? `<div class="day-card-exercises-compact">${exerciseLines}</div>` : ''}
        <div class="day-card-actions-bottom">
          ${!hasRoutine ? `
            <button class="btn btn-sm btn-outline" data-action="edit" data-day="${dayNum}">${t('Create')}</button>
          ` : `
            <button class="btn btn-sm btn-outline" data-action="edit" data-day="${dayNum}">${t('Edit')}</button>
            <button class="btn btn-sm btn-outline-danger" data-action="delete" data-day="${dayNum}">${t('Delete')}</button>
          `}
          ${hasExercises ? `
            <button class="btn btn-sm btn-primary" data-action="workout" data-day="${dayNum}">${t('StartWorkout')}</button>
          ` : ''}
        </div>
      </div>
    `;
  }

  container.innerHTML = html;

  // Event delegation
  container.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-action]');
    if (!btn) return;

    const action = btn.dataset.action;
    const day = btn.dataset.day;

    if (action === 'edit') {
      location.hash = `#editday/${day}`;
    } else if (action === 'workout') {
      location.hash = `#workout/${day}`;
    } else if (action === 'delete') {
      handleDelete(parseInt(day));
    }
  });
}

async function handleDelete(day) {
  if (!confirm(t('DeleteRoutineMsg'))) return;

  try {
    await api.put(`/routines/${day}`, { muscleGroupIds: [], exercises: [] });
    await loadRoutines();
  } catch (err) {
    alert(t('ErrorFmt', err.message));
  }
}
