// FitCycle Routines Page — weekly routine overview with 7 day cards

import { t, dayName, muscleGroup } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';

let weekData = null;

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="flex items-center justify-between">
          <div class="section-title">${t('MyWeeklyRoutine')}</div>
          ${auth.isAdmin() ? `<div class="flex gap-4"><button id="import-pdf-btn" class="btn btn-sm btn-outline" style="font-size:12px;">&#128196; ${t('ImportPdf')}</button><button id="copy-routines-btn" class="btn btn-sm btn-outline" style="font-size:12px;">&#128203; ${t('CopyRoutines')}</button></div>` : ''}
        </div>
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
  document.getElementById('import-pdf-btn')?.addEventListener('click', showImportModal);
  document.getElementById('copy-routines-btn')?.addEventListener('click', showCopyModal);
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
      try { if (rawDetails) details = JSON.parse(rawDetails); } catch (e) { /* */ }

      if (Array.isArray(details) && details.length > 0) {
        const hasVaryingWeight = new Set(details.map(s => s.weight)).size > 1;
        if (hasVaryingWeight) {
          // Show per-set weights compactly
          setInfo = details.map(s => `${s.weight > 0 ? '<span class="weight-val">' + s.weight + 'kg</span>' : '-'}`).join('/');
          setInfo = `<span class="exercise-meta">${details.length}S ${details[0].reps}r [${setInfo}]</span>`;
        } else {
          const w = details[0].weight || weight;
          setInfo = `<span class="exercise-meta">${details.length}x${details[0].reps}${w > 0 ? ' @<span class="weight-val">' + w + 'kg</span>' : ''}</span>`;
        }
      } else {
        const weightStr = weight > 0 ? ` @${weight}kg` : '';
        setInfo = `<span class="exercise-meta">${sets}x${reps}${weight > 0 ? ' @<span class="weight-val">' + weight + 'kg</span>' : ''}</span>`;
      }

      // Tempo info from setDetails
      let tempoInfo = '';
      if (Array.isArray(details) && details.length > 0) {
        const tp = details[0].tempoPos || 0;
        const tn = details[0].tempoNeg || 0;
        if (tp > 0 || tn > 0) tempoInfo = `<span class="tempo-info" title="${t('TempoAscFull')} / ${t('TempoDescFull')}">&#9201; ${tp}s&#8593;${t('TempoAsc')} ${tn}s&#8595;${t('TempoDesc')}</span>`;
        const grip = details[0].grip || '';
        if (grip) tempoInfo += ` <span class="grip-info" title="${t('Grip')}">&#9994; ${t('Grip' + grip.charAt(0).toUpperCase() + grip.slice(1).toLowerCase()) || grip}</span>`;
      }

      const exNotes = e.notes || e.Notes || '';
      const notesIcon = exNotes ? '<span class="notes-icon" title="' + exNotes.replace(/"/g, '&quot;') + '">&#128221;</span>' : '';

      const ssGroup = e.supersetGroup || e.SupersetGroup || 0;
      const ssIcon = ssGroup > 0 ? `<span class="superset-icon" title="Superset #${ssGroup}">&#8644;</span> ` : '';
      return `<div class="exercise-line-compact">
        <div class="ex-name-line">${ssIcon}${name} ${notesIcon}</div>
        <div class="ex-detail-line">${setInfo} ${tempoInfo} ${mgTag}</div>
      </div>`;
    }).join('');

    // Cardio & Abs badges
    const dayCardio = dayData?.cardioType || dayData?.CardioType || '';
    const dayCardioMin = dayData?.cardioMinutes || dayData?.CardioMinutes || 0;
    const dayAbs = dayData?.absExercise || dayData?.AbsExercise || '';
    const dayAbsSets = dayData?.absSets || dayData?.AbsSets || 0;
    const dayAbsReps = dayData?.absReps || dayData?.AbsReps || 0;
    let extrasBadges = '';
    if (dayCardio && dayCardioMin > 0) {
      extrasBadges += `<span style="display:inline-flex;align-items:center;gap:3px;font-size:11px;color:#e67e22;background:#fff3e0;padding:2px 8px;border-radius:10px;">&#127939; ${dayCardio} ${dayCardioMin}${t('MinUnit')}</span>`;
    }
    if (dayAbs && (dayAbsSets > 0 || dayAbsReps > 0)) {
      extrasBadges += `<span style="display:inline-flex;align-items:center;gap:3px;font-size:11px;color:#512BD4;background:#f3f0fc;padding:2px 8px;border-radius:10px;">&#128170; ${dayAbs} ${dayAbsSets}×${dayAbsReps}</span>`;
    }

    html += `
      <div class="day-card day-card-vertical" data-day="${dayNum}" data-has-exercises="${hasExercises}">
        <div class="day-card-header">
          <div class="day-card-name">${dayName(dayNum)}</div>
          <div class="day-card-groups">${groupNames}</div>
        </div>
        ${hasExercises ? `<div class="day-card-exercises-compact">${exerciseLines}</div>` : ''}
        ${extrasBadges ? `<div style="display:flex;gap:6px;flex-wrap:wrap;padding:4px 0;">${extrasBadges}</div>` : ''}
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

// ── PDF Import (Superuser only) ──

async function showImportModal() {
  // Load users
  let users = [];
  try {
    users = await api.get('/users');
  } catch (e) {
    alert(t('ErrorFmt', e.message));
    return;
  }

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';

  const userOptions = (users || []).map(u => {
    const uid = u.id || u.Id;
    const uname = u.username || u.Username;
    const uemail = u.email || u.Email;
    return `<option value="${uid}">${uname} (${uemail})</option>`;
  }).join('');

  overlay.innerHTML = `
    <div class="modal-content" style="max-width:400px;">
      <div class="modal-header">
        <div class="modal-title">&#128196; ${t('ImportPdf')}</div>
        <button class="modal-close" id="import-modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('SelectUser')}</label>
        <select id="import-user" class="form-input">${userOptions}</select>
      </div>
      <div class="form-group">
        <label class="form-label">${t('SelectPdfFile')}</label>
        <input type="file" id="import-file" accept=".pdf" class="form-input" style="padding:6px;">
      </div>
      <button id="import-submit" class="btn btn-primary btn-block">${t('ImportPdf')}</button>
      <div id="import-status" style="margin-top:8px;font-size:13px;text-align:center;"></div>
    </div>
  `;

  document.body.appendChild(overlay);

  overlay.querySelector('#import-modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });

  overlay.querySelector('#import-submit')?.addEventListener('click', async () => {
    const fileInput = overlay.querySelector('#import-file');
    const userSelect = overlay.querySelector('#import-user');
    const statusEl = overlay.querySelector('#import-status');
    const submitBtn = overlay.querySelector('#import-submit');

    if (!fileInput?.files?.length) {
      if (statusEl) statusEl.textContent = t('SelectPdfFile');
      return;
    }

    const file = fileInput.files[0];
    if (file.size > 10 * 1024 * 1024) {
      if (statusEl) statusEl.textContent = 'Max 10 MB';
      return;
    }

    const userId = userSelect?.value;
    if (!userId) return;

    // Disable and show progress
    if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = t('Importing'); }
    if (statusEl) { statusEl.style.color = '#512BD4'; statusEl.textContent = t('Importing'); }

    try {
      const formData = new FormData();
      formData.append('pdf', file);
      formData.append('userId', userId);

      const result = await api.postForm('/routines/import-pdf', formData);

      console.log('PDF import result:', JSON.stringify(result, null, 2));

      if (result?.success) {
        const daysSummary = (result.days || []).map(d =>
          `${d.dayName}: ${d.exerciseCount} ej.${d.newExercisesCreated > 0 ? ' (+' + d.newExercisesCreated + ' nuevos)' : ''}`
        ).join('\n');

        // Show debug info (DÍA lines found in extracted text)
        const diaInfo = (result.debugDiaLines || []).map(l => `  ${l}`).join('\n');
        const debugInfo = diaInfo ? `\n\nLíneas DÍA encontradas:\n${diaInfo}` : '';

        if (statusEl) {
          statusEl.style.color = '#28a745';
          statusEl.innerHTML = `<strong>${t('ImportSuccess')}</strong><br><pre style="text-align:left;font-size:10px;margin-top:4px;white-space:pre-wrap;max-height:300px;overflow-y:auto;">${daysSummary}${debugInfo}\n\nMsg: ${result.message || ''}</pre>`;
        }

        // Reload routines after 5s (more time to read debug)
        setTimeout(() => {
          overlay.remove();
          loadRoutines();
        }, 8000);
      } else {
        const errDebug = (result?.debugDiaLines || []).join(' | ');
        const errMsg = result?.message || t('ImportError');
        if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = `${errMsg}${errDebug ? ' [DÍA: ' + errDebug + ']' : ''}`; }
        if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = t('ImportPdf'); }
      }
    } catch (err) {
      if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = t('ErrorFmt', err.message); }
      if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = t('ImportPdf'); }
    }
  });
}

async function showCopyModal() {
  let users;
  try {
    users = await api.get('/users');
  } catch (e) {
    alert(t('ErrorFmt', e.message));
    return;
  }

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';

  const userOptions = (users || []).map(u => {
    const uid = u.id || u.Id;
    const uname = u.username || u.Username;
    const uemail = u.email || u.Email;
    return `<option value="${uid}">${uname} (${uemail})</option>`;
  }).join('');

  overlay.innerHTML = `
    <div class="modal-content" style="max-width:400px;">
      <div class="modal-header">
        <div class="modal-title">&#128203; ${t('CopyRoutines')}</div>
        <button class="modal-close" id="copy-modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('SourceUser')}</label>
        <select id="copy-source-user" class="form-input">${userOptions}</select>
      </div>
      <div class="form-group">
        <label class="form-label">${t('TargetUser')}</label>
        <select id="copy-target-user" class="form-input">${userOptions}</select>
      </div>
      <button id="copy-submit" class="btn btn-primary btn-block">${t('CopyRoutines')}</button>
      <div id="copy-status" style="margin-top:8px;font-size:13px;text-align:center;"></div>
    </div>
  `;

  document.body.appendChild(overlay);

  overlay.querySelector('#copy-modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });

  overlay.querySelector('#copy-submit')?.addEventListener('click', async () => {
    const sourceSelect = overlay.querySelector('#copy-source-user');
    const targetSelect = overlay.querySelector('#copy-target-user');
    const statusEl = overlay.querySelector('#copy-status');
    const submitBtn = overlay.querySelector('#copy-submit');

    const sourceUserId = parseInt(sourceSelect?.value);
    const targetUserId = parseInt(targetSelect?.value);

    if (sourceUserId === targetUserId) {
      if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = t('SameUserError'); }
      return;
    }

    if (!confirm(t('ConfirmCopyRoutines'))) return;

    if (submitBtn) { submitBtn.disabled = true; submitBtn.textContent = t('CopyingRoutines'); }
    if (statusEl) { statusEl.style.color = '#512BD4'; statusEl.textContent = t('CopyingRoutines'); }

    try {
      const result = await api.post('/routines/copy', { sourceUserId, targetUserId });

      if (result?.success) {
        if (statusEl) { statusEl.style.color = '#28a745'; statusEl.textContent = t('CopySuccess') + ' — ' + (result.message || ''); }
        setTimeout(() => {
          overlay.remove();
          loadRoutines();
        }, 3000);
      } else {
        const errMsg = result?.error || result?.message || t('ImportError');
        if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = errMsg; }
        if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = t('CopyRoutines'); }
      }
    } catch (err) {
      if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = t('ErrorFmt', err.message); }
      if (submitBtn) { submitBtn.disabled = false; submitBtn.textContent = t('CopyRoutines'); }
    }
  });
}
