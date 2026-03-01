// FitCycle Edit Day Page — configure muscle groups and exercises for a day
// Supports multi-set (per-set reps/weight configuration)

import { t, dayName, muscleGroup as mgTranslate } from '../l10n.js';
import { api } from '../api.js';
import { getSuggestions, clearCache } from '../exercises.js';

let dayNum = 0;
let allMuscleGroups = [];
let allExercises = [];
let groups = [];
// Each exercise: { exerciseId, name, imageUrl, isSelected, sets, reps, weight, setDetails, supersetGroup, expanded }

export function render(params) {
  dayNum = parseInt(params);
  return `
    <div class="page no-tabs">
      <div class="page-content">
        <div class="flex items-center gap-8 mb-8">
          <button id="editday-back" class="btn btn-ghost">${t('Back')}</button>
          <div class="section-title" id="editday-title">${t('EditDay')}</div>
        </div>
        <div class="section-subtitle">${t('SelectGroupsExercises')}</div>
        <div id="editday-content">
          <div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div>
        </div>
        <div id="editday-status" class="status-text mt-8"></div>
      </div>
    </div>
  `;
}

export async function mount(params) {
  dayNum = parseInt(params);
  document.getElementById('editday-back')?.addEventListener('click', () => {
    location.hash = '#routines';
  });
  await loadData();
}

export function destroy() {
  groups = [];
  allMuscleGroups = [];
  allExercises = [];
}

// ── Helpers ──

function parseSetDetails(raw) {
  if (!raw) return null;
  try {
    const arr = typeof raw === 'string' ? JSON.parse(raw) : raw;
    if (Array.isArray(arr) && arr.length > 0) return arr;
  } catch (e) { /* ignore */ }
  return null;
}

function buildDefaultSets(count, reps, weight) {
  return Array.from({ length: count }, () => ({ reps, weight }));
}

// ── Data Loading ──

async function loadData() {
  const container = document.getElementById('editday-content');
  const titleEl = document.getElementById('editday-title');
  const statusEl = document.getElementById('editday-status');
  if (!container) return;

  try {
    if (titleEl) titleEl.textContent = `${t('EditDay')} — ${dayName(dayNum)}`;

    const [mgData, exData, weekRoutine] = await Promise.all([
      api.get('/musclegroups'),
      api.get('/exercises'),
      api.get('/routines'),
    ]);

    allMuscleGroups = mgData || [];
    allExercises = exData || [];

    const days = weekRoutine?.days || weekRoutine?.Days || (Array.isArray(weekRoutine) ? weekRoutine : []);
    const dayRoutine = days.find(d => (d.day ?? d.Day) === dayNum);

    const selectedMgIds = new Set(
      (dayRoutine?.muscleGroups || dayRoutine?.MuscleGroups || []).map(mg => mg.id || mg.Id)
    );
    const selectedExercises = dayRoutine?.exercises || dayRoutine?.Exercises || [];

    groups = allMuscleGroups.map(mg => {
      const mgId = mg.id || mg.Id;
      const mgName = mg.name || mg.Name;
      const isSelected = selectedMgIds.has(mgId);

      const exercises = allExercises
        .filter(e => (e.muscleGroupId || e.MuscleGroupId) === mgId)
        .map(e => {
          const eId = e.id || e.Id;
          const existing = selectedExercises.find(se =>
            (se.exerciseId || se.ExerciseId) === eId
          );
          const sets = existing ? (existing.sets || existing.Sets || 3) : 3;
          const reps = existing ? (existing.reps || existing.Reps || 12) : 12;
          const weight = existing ? (existing.weight || existing.Weight || 0) : 0;
          const rawDetails = existing ? (existing.setDetails || existing.SetDetails || '') : '';
          const setDetails = parseSetDetails(rawDetails) || buildDefaultSets(sets, reps, weight);

          const supersetGroup = existing ? (existing.supersetGroup || existing.SupersetGroup || 0) : 0;
          return {
            exerciseId: eId,
            name: e.name || e.Name,
            imageUrl: e.imageUrl || e.ImageUrl || '',
            isSelected: !!existing,
            sets,
            reps,
            weight,
            setDetails,
            supersetGroup,
            expanded: false,
          };
        });

      return { id: mgId, name: mgName, isSelected, exercises };
    });

    if (statusEl) statusEl.textContent = '';
    buildUI();
  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
  }
}

// ── UI Building ──

function buildUI() {
  const container = document.getElementById('editday-content');
  if (!container) return;

  let html = '';
  groups.forEach((group, gi) => {
    const displayName = mgTranslate(group.name);
    const checkedAttr = group.isSelected ? 'checked' : '';

    html += `
      <div class="card" style="margin-bottom:10px;">
        <label class="checkbox" style="cursor:pointer;">
          <input type="checkbox" class="mg-check" data-gi="${gi}" ${checkedAttr}>
          <span class="checkmark"></span>
          <span class="checkbox-label muscle-group-name" style="font-weight:bold;font-size:17px;">${displayName}</span>
        </label>
        <div class="exercise-container" id="ex-container-${gi}" style="margin-left:20px;margin-top:6px;${group.isSelected ? '' : 'display:none;'}">
          ${buildExerciseRows(group, gi)}
          <button class="btn btn-sm btn-ghost btn-add-exercise mt-8" data-gi="${gi}" style="color:#512BD4;">
            ${t('AddExercise')}
          </button>
        </div>
      </div>
    `;
  });

  html += `
    <button id="editday-save" class="btn btn-primary btn-block btn-lg mt-16">${t('Save')}</button>
  `;

  container.innerHTML = html;
  attachEvents(container);
}

function buildExerciseRows(group, gi) {
  return group.exercises.map((ex, ei) => {
    const checkedAttr = ex.isSelected ? 'checked' : '';
    const imgHtml = ex.imageUrl
      ? `<img src="${ex.imageUrl}" alt="" style="width:40px;height:40px;object-fit:cover;border-radius:4px;" onerror="this.onerror=null;this.parentElement.innerHTML='&#127947;&#65039;'">`
      : `<div style="width:40px;height:40px;background:#eee;border-radius:4px;display:flex;align-items:center;justify-content:center;font-size:18px;">&#127947;</div>`;

    const summaryText = ex.setDetails.map((s, i) => `S${i + 1}: ${s.reps}r/${s.weight > 0 ? s.weight + 'kg' : '-'}`).join(' | ');

    const setRows = ex.setDetails.map((s, si) => `
      <div class="set-detail-row" style="display:flex;align-items:center;gap:6px;margin:3px 0;padding:3px 0;border-bottom:1px solid #f0f0f0;">
        <span style="font-size:12px;color:#512BD4;font-weight:600;min-width:36px;">S${si + 1}</span>
        <span style="font-size:11px;color:gray;">${t('Reps')}</span>
        <select class="picker-select set-reps-picker" data-gi="${gi}" data-ei="${ei}" data-si="${si}" style="width:60px;font-size:13px;padding:4px;">
          ${buildPickerOptions(1, 30, s.reps)}
        </select>
        <span style="font-size:11px;color:gray;">kg</span>
        <input type="number" class="set-weight-input" data-gi="${gi}" data-ei="${ei}" data-si="${si}"
          value="${s.weight > 0 ? s.weight : ''}" placeholder="0" step="0.5" min="0"
          style="width:65px;font-size:13px;padding:4px 6px;border:1px solid #ccc;border-radius:4px;">
        ${ex.setDetails.length > 1 ? `<button class="btn-remove-set" data-gi="${gi}" data-ei="${ei}" data-si="${si}"
          style="background:none;border:none;color:#dc3545;font-size:16px;cursor:pointer;padding:2px 6px;" title="${t('Delete')}">&#10005;</button>` : ''}
      </div>
    `).join('');

    // Superset indicator
    const isInSuperset = ex.supersetGroup > 0;
    const supersetBorder = isInSuperset ? 'border-left:3px solid #e67e22;' : '';
    // Find partner name for superset label
    let supersetLabel = '';
    if (isInSuperset) {
      let partnerName = '';
      groups.forEach(g => g.exercises.forEach(e => {
        if (e.supersetGroup === ex.supersetGroup && e.exerciseId !== ex.exerciseId) partnerName = e.name;
      }));
      supersetLabel = `<div style="font-size:10px;color:#e67e22;font-weight:600;margin-left:52px;">&#8644; ${partnerName || t('Superset')}</div>`;
    }

    // Superset button
    let supersetBtn = '';
    if (ex.isSelected) {
      if (isInSuperset) {
        supersetBtn = `<button class="btn-unlink-superset" data-gi="${gi}" data-ei="${ei}" style="background:none;border:1px solid #e67e22;color:#e67e22;border-radius:6px;padding:2px 6px;font-size:10px;cursor:pointer;" title="${t('UnlinkSuperset')}">&#8644;</button>`;
      } else {
        supersetBtn = `<button class="btn-link-superset" data-gi="${gi}" data-ei="${ei}" style="background:none;border:1px solid #ccc;color:#999;border-radius:6px;padding:2px 6px;font-size:10px;cursor:pointer;" title="${t('LinkSuperset')}">&#8644;</button>`;
      }
    }

    return `
      <div class="exercise-row" style="margin-bottom:10px;border:1px solid #eee;border-radius:8px;padding:8px;${supersetBorder}">
        <div style="display:flex;align-items:center;gap:6px;">
          <input type="checkbox" class="ex-check" data-gi="${gi}" data-ei="${ei}" ${checkedAttr}>
          <div class="exercise-img">${imgHtml}</div>
          <span class="ex-name" style="font-size:13px;flex:1;min-width:60px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">${ex.name}</span>
          ${supersetBtn}
          <button class="btn-toggle-sets" data-gi="${gi}" data-ei="${ei}"
            style="background:${ex.expanded ? '#512BD4' : '#e0e0e0'};color:${ex.expanded ? '#fff' : '#333'};border:none;border-radius:6px;padding:3px 8px;font-size:11px;cursor:pointer;white-space:nowrap;">
            ${ex.setDetails.length}S
          </button>
          <button class="btn btn-xs btn-save-ex" data-gi="${gi}" data-ei="${ei}" style="background:#28a745;color:#fff;border:none;border-radius:6px;padding:2px 8px;font-size:14px;cursor:pointer;min-width:34px;" title="${t('Save')}">&#10003;</button>
          <button class="btn btn-xs btn-delete-ex" data-gi="${gi}" data-ei="${ei}" style="background:#dc3545;color:#fff;border:none;border-radius:6px;padding:2px 8px;font-size:14px;cursor:pointer;min-width:34px;" title="${t('Delete')}">&#10005;</button>
        </div>
        ${supersetLabel}
        ${!ex.expanded ? `
          <div style="margin-left:52px;margin-top:4px;font-size:12px;color:#666;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">
            ${summaryText}
          </div>
        ` : `
          <div style="margin-left:10px;margin-top:6px;">
            ${setRows}
            <button class="btn-add-set" data-gi="${gi}" data-ei="${ei}"
              style="background:none;border:1px dashed #512BD4;color:#512BD4;border-radius:6px;padding:4px 12px;font-size:12px;cursor:pointer;margin-top:4px;width:100%;">
              + ${t('AddSet')}
            </button>
          </div>
        `}
      </div>
    `;
  }).join('');
}

function showSupersetModal(sourceGi, sourceEi) {
  const sourceEx = groups[sourceGi].exercises[sourceEi];

  // Collect all selected exercises except the source and already in a superset
  const candidates = [];
  groups.forEach((g, gi) => {
    const mgName = mgTranslate(g.name);
    g.exercises.forEach((ex, ei) => {
      if (gi === sourceGi && ei === sourceEi) return;
      if (!ex.isSelected) return;
      if (ex.supersetGroup > 0) return;
      candidates.push({ gi, ei, name: ex.name, imageUrl: ex.imageUrl, mgName });
    });
  });

  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';

  const items = candidates.length > 0
    ? candidates.map((c, i) => `
        <div class="exercise-row" style="cursor:pointer;padding:10px;border-bottom:1px solid #f0f0f0;" data-idx="${i}">
          <div style="display:flex;align-items:center;gap:8px;">
            ${c.imageUrl ? `<img src="${c.imageUrl}" alt="" style="width:36px;height:36px;object-fit:cover;border-radius:4px;" onerror="this.onerror=null;this.parentElement.innerHTML='&#127947;&#65039;'">` : '<span style="font-size:18px;">&#127947;</span>'}
            <div style="flex:1;">
              <div style="font-size:14px;font-weight:600;">${c.name}</div>
              <div style="font-size:11px;color:#888;">${c.mgName}</div>
            </div>
          </div>
        </div>
      `).join('')
    : `<div style="padding:16px;text-align:center;color:#999;">${t('NoExercisesAvailable')}</div>`;

  overlay.innerHTML = `
    <div class="modal-content" style="max-height:70vh;overflow-y:auto;">
      <div class="modal-header">
        <div class="modal-title">&#8644; ${t('Superset')}: ${sourceEx.name}</div>
        <button class="modal-close" id="ss-modal-close">&times;</button>
      </div>
      <div style="padding:0 4px;font-size:12px;color:#666;margin-bottom:8px;">${t('SelectPair')}</div>
      <div id="ss-candidates">${items}</div>
    </div>
  `;

  document.body.appendChild(overlay);

  // Select candidate
  overlay.querySelectorAll('[data-idx]').forEach(item => {
    item.addEventListener('click', () => {
      const idx = parseInt(item.dataset.idx);
      const c = candidates[idx];
      const targetEx = groups[c.gi].exercises[c.ei];

      let maxGroup = 0;
      groups.forEach(g => g.exercises.forEach(e => { if (e.supersetGroup > maxGroup) maxGroup = e.supersetGroup; }));
      const newGroup = maxGroup + 1;
      sourceEx.supersetGroup = newGroup;
      targetEx.supersetGroup = newGroup;

      overlay.remove();
      buildUI();
    });
  });

  overlay.querySelector('#ss-modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });
}

function buildPickerOptions(min, max, selected) {
  let html = '';
  for (let i = min; i <= max; i++) {
    html += `<option value="${i}" ${i === selected ? 'selected' : ''}>${i}</option>`;
  }
  return html;
}

// ── Event Handling ──

function attachEvents(container) {
  // Muscle group checkboxes
  container.querySelectorAll('.mg-check').forEach(cb => {
    cb.addEventListener('change', () => {
      const gi = parseInt(cb.dataset.gi);
      groups[gi].isSelected = cb.checked;
      const exContainer = document.getElementById(`ex-container-${gi}`);
      if (exContainer) exContainer.style.display = cb.checked ? '' : 'none';
    });
  });

  // Exercise checkboxes
  container.querySelectorAll('.ex-check').forEach(cb => {
    cb.addEventListener('change', () => {
      const gi = parseInt(cb.dataset.gi);
      const ei = parseInt(cb.dataset.ei);
      groups[gi].exercises[ei].isSelected = cb.checked;
    });
  });

  // Toggle multi-set view
  container.querySelectorAll('.btn-toggle-sets').forEach(btn => {
    btn.addEventListener('click', () => {
      const gi = parseInt(btn.dataset.gi);
      const ei = parseInt(btn.dataset.ei);
      groups[gi].exercises[ei].expanded = !groups[gi].exercises[ei].expanded;
      buildUI();
    });
  });

  // Per-set reps pickers
  container.querySelectorAll('.set-reps-picker').forEach(sel => {
    sel.addEventListener('change', () => {
      const { gi, ei, si } = parseIndices(sel);
      groups[gi].exercises[ei].setDetails[si].reps = parseInt(sel.value);
      syncSummaryFromDetails(gi, ei);
    });
  });

  // Per-set weight inputs
  container.querySelectorAll('.set-weight-input').forEach(inp => {
    inp.addEventListener('input', () => {
      const { gi, ei, si } = parseIndices(inp);
      groups[gi].exercises[ei].setDetails[si].weight = parseFloat(inp.value) || 0;
      syncSummaryFromDetails(gi, ei);
    });
  });

  // Add set button
  container.querySelectorAll('.btn-add-set').forEach(btn => {
    btn.addEventListener('click', () => {
      const gi = parseInt(btn.dataset.gi);
      const ei = parseInt(btn.dataset.ei);
      const ex = groups[gi].exercises[ei];
      const lastSet = ex.setDetails[ex.setDetails.length - 1] || { reps: 12, weight: 0 };
      ex.setDetails.push({ reps: lastSet.reps, weight: lastSet.weight });
      syncSummaryFromDetails(gi, ei);
      buildUI();
    });
  });

  // Remove set button
  container.querySelectorAll('.btn-remove-set').forEach(btn => {
    btn.addEventListener('click', () => {
      const { gi, ei, si } = parseIndices(btn);
      const ex = groups[gi].exercises[ei];
      if (ex.setDetails.length > 1) {
        ex.setDetails.splice(si, 1);
        syncSummaryFromDetails(gi, ei);
        buildUI();
      }
    });
  });

  // Superset link: open modal with exercise list
  container.querySelectorAll('.btn-link-superset').forEach(btn => {
    btn.addEventListener('click', () => {
      const gi = parseInt(btn.dataset.gi);
      const ei = parseInt(btn.dataset.ei);
      showSupersetModal(gi, ei);
    });
  });

  // Superset unlink
  container.querySelectorAll('.btn-unlink-superset').forEach(btn => {
    btn.addEventListener('click', () => {
      const gi = parseInt(btn.dataset.gi);
      const ei = parseInt(btn.dataset.ei);
      const ex = groups[gi].exercises[ei];
      const oldGroup = ex.supersetGroup;
      ex.supersetGroup = 0;
      // If the pair is now alone, unlink it too
      groups.forEach(g => g.exercises.forEach(e => {
        if (e.supersetGroup === oldGroup) {
          const count = groups.reduce((acc, g2) => acc + g2.exercises.filter(e2 => e2.supersetGroup === oldGroup).length, 0);
          if (count <= 1) e.supersetGroup = 0;
        }
      }));
      buildUI();
    });
  });

  // Save buttons (per exercise)
  container.querySelectorAll('.btn-save-ex').forEach(btn => {
    btn.addEventListener('click', () => doSave());
  });

  // Delete exercise buttons
  container.querySelectorAll('.btn-delete-ex').forEach(btn => {
    btn.addEventListener('click', () => {
      const gi = parseInt(btn.dataset.gi);
      const ei = parseInt(btn.dataset.ei);
      const ex = groups[gi].exercises[ei];
      if (!confirm(t('ConfirmDeleteExercise', ex.name))) return;
      groups[gi].exercises.splice(ei, 1);
      buildUI();
    });
  });

  // Add exercise buttons
  container.querySelectorAll('.btn-add-exercise').forEach(btn => {
    btn.addEventListener('click', () => {
      const gi = parseInt(btn.dataset.gi);
      onAddExercise(groups[gi]);
    });
  });

  // Global save button
  document.getElementById('editday-save')?.addEventListener('click', () => doSave());
}

function parseIndices(el) {
  return {
    gi: parseInt(el.dataset.gi),
    ei: parseInt(el.dataset.ei),
    si: parseInt(el.dataset.si),
  };
}

function syncSummaryFromDetails(gi, ei) {
  const ex = groups[gi].exercises[ei];
  ex.sets = ex.setDetails.length;
  // Use first set's reps as the "main" reps, max weight as the "main" weight
  if (ex.setDetails.length > 0) {
    ex.reps = ex.setDetails[0].reps;
    ex.weight = Math.max(...ex.setDetails.map(s => s.weight));
  }
}

// ── Save Routine ──

async function doSave() {
  const statusEl = document.getElementById('editday-status');
  try {
    if (statusEl) statusEl.textContent = t('Saving');

    const selectedMgIds = groups.filter(g => g.isSelected).map(g => g.id);
    const selectedExercises = groups
      .filter(g => g.isSelected)
      .flatMap(g => g.exercises)
      .filter(ex => ex.isSelected)
      .map(ex => ({
        exerciseId: ex.exerciseId,
        sets: ex.setDetails.length,
        reps: ex.setDetails.length > 0 ? ex.setDetails[0].reps : ex.reps,
        weight: Math.max(...ex.setDetails.map(s => s.weight), 0),
        setDetails: JSON.stringify(ex.setDetails),
        supersetGroup: ex.supersetGroup || 0,
      }));

    await api.put(`/routines/${dayNum}`, {
      muscleGroupIds: selectedMgIds,
      exercises: selectedExercises,
    });

    location.hash = '#routines';
  } catch (err) {
    if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
  }
}

// ── Add Exercise ──

function onAddExercise(group) {
  const existingNames = new Set(group.exercises.map(e => e.name.toLowerCase()));
  const allSuggestions = getSuggestions(group.name);
  const suggestions = allSuggestions.filter(s => !existingNames.has(s.name.toLowerCase()));
  const displayName = mgTranslate(group.name);

  if (suggestions.length > 0) {
    showSuggestionModal(group, displayName, suggestions);
  } else {
    showCustomNamePrompt(group, displayName);
  }
}

function showSuggestionModal(group, displayName, suggestions) {
  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';

  const suggestionItems = suggestions.map((s, i) => `
    <div class="exercise-row" style="cursor:pointer;padding:8px;" data-idx="${i}">
      <div class="exercise-img">
        ${s.imageUrl ? `<img src="${s.imageUrl}" alt="" style="width:40px;height:40px;object-fit:cover;border-radius:4px;" onerror="this.onerror=null;this.parentElement.innerHTML='&#127947;&#65039;'">` : '&#127947;'}
      </div>
      <div class="exercise-info">
        <div class="exercise-name">${s.name}</div>
      </div>
    </div>
  `).join('');

  overlay.innerHTML = `
    <div class="modal-content" style="max-height:80vh;overflow-y:auto;">
      <div class="modal-header">
        <div class="modal-title">${t('AddExerciseTo', displayName)}</div>
        <button class="modal-close" id="modal-close">&times;</button>
      </div>
      <div id="modal-suggestions">${suggestionItems}</div>
      <div class="divider"></div>
      <div class="form-group">
        <input id="modal-custom-name" class="form-input" type="text" placeholder="${t('CustomName')}">
      </div>
      <button id="modal-custom-add" class="btn btn-outline btn-block">${t('Add')}</button>
    </div>
  `;

  document.body.appendChild(overlay);

  overlay.querySelectorAll('[data-idx]').forEach(item => {
    item.addEventListener('click', async () => {
      const idx = parseInt(item.dataset.idx);
      const suggestion = suggestions[idx];
      overlay.remove();
      await createAndAddExercise(group, suggestion.name, suggestion.imageUrl);
    });
  });

  overlay.querySelector('#modal-custom-add')?.addEventListener('click', async () => {
    const name = overlay.querySelector('#modal-custom-name')?.value?.trim();
    if (!name) return;
    overlay.remove();
    await createAndAddExercise(group, name, null);
  });

  overlay.querySelector('#modal-custom-name')?.addEventListener('keydown', async (e) => {
    if (e.key === 'Enter') {
      const name = e.target.value?.trim();
      if (!name) return;
      overlay.remove();
      await createAndAddExercise(group, name, null);
    }
  });

  overlay.querySelector('#modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) overlay.remove();
  });
}

function showCustomNamePrompt(group, displayName) {
  const name = prompt(t('ExerciseNameFor', displayName));
  if (!name || !name.trim()) return;
  createAndAddExercise(group, name.trim(), null);
}

async function createAndAddExercise(group, name, imageUrl) {
  const statusEl = document.getElementById('editday-status');
  try {
    const newEx = await api.post('/exercises', {
      name: name,
      muscleGroupId: group.id,
      imageUrl: imageUrl || '',
    });

    clearCache();

    group.exercises.push({
      exerciseId: newEx.id || newEx.Id,
      name: newEx.name || newEx.Name || name,
      imageUrl: imageUrl || newEx.imageUrl || newEx.ImageUrl || '',
      isSelected: true,
      sets: 3,
      reps: 12,
      weight: 0,
      setDetails: buildDefaultSets(3, 12, 0),
      supersetGroup: 0,
      expanded: false,
    });

    buildUI();
  } catch (err) {
    if (statusEl) statusEl.textContent = t('ErrorFmt', err.message);
  }
}
