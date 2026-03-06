// FitCycle Templates Page — routine template library (Superuser only)

import { t, dayName, muscleGroup } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';

let templates = [];

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="flex items-center justify-between">
          <div class="section-title">${t('RoutineTemplates')}</div>
          <button id="save-template-btn" class="btn btn-sm btn-outline" style="color:#512BD4;font-size:12px;">+ ${t('SaveTemplate')}</button>
        </div>
        <div class="section-subtitle">${t('NoTemplates')}</div>
        <div id="templates-list">
          <div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div>
        </div>
      </div>
    </div>
  `;
}

export async function mount() {
  document.getElementById('save-template-btn')?.addEventListener('click', showSaveModal);
  await loadTemplates();
}

export function destroy() {}

async function loadTemplates() {
  const container = document.getElementById('templates-list');
  if (!container) return;

  try {
    templates = await api.get('/templates');
    renderTemplates(container);
  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
  }
}

function renderTemplates(container) {
  if (!templates || templates.length === 0) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('NoTemplates')}</div></div>`;
    return;
  }

  container.innerHTML = templates.map(tmpl => {
    const date = new Date(tmpl.createdAt).toLocaleDateString();
    let weekData = null;
    try { weekData = JSON.parse(tmpl.routineDataJson); } catch (e) { /* */ }
    const days = weekData?.Days || weekData?.days || [];
    const dayCount = days.filter(d => {
      const exs = d.exercises || d.Exercises || [];
      return exs.length > 0;
    }).length;

    return `
      <div class="card" style="margin-bottom:10px;">
        <div style="display:flex;align-items:center;justify-content:space-between;">
          <div>
            <div class="card-title">${tmpl.name}</div>
            ${tmpl.description ? `<div class="card-subtitle">${tmpl.description}</div>` : ''}
            <div style="font-size:11px;color:var(--text-light);margin-top:4px;">${date} &middot; ${dayCount} ${t('Days')}</div>
          </div>
          <div style="display:flex;gap:4px;flex-shrink:0;">
            <button class="btn btn-sm btn-ghost" style="font-size:11px;" data-view-tmpl="${tmpl.id}">${t('ViewDetails')}</button>
            <button class="btn btn-sm btn-ghost" style="color:#512BD4;font-size:11px;" data-apply-tmpl="${tmpl.id}" data-name="${tmpl.name}">${t('ApplyTemplate')}</button>
            <button class="btn btn-sm btn-ghost" style="color:var(--danger,#dc3545);font-size:11px;" data-delete-tmpl="${tmpl.id}">${t('DeleteTemplate')}</button>
          </div>
        </div>
        <div id="tmpl-detail-${tmpl.id}" style="display:none;margin-top:10px;border-top:1px solid var(--border-light);padding-top:8px;"></div>
      </div>
    `;
  }).join('');

  // View details toggle
  container.querySelectorAll('[data-view-tmpl]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      const id = e.currentTarget.dataset.viewTmpl;
      const detail = document.getElementById(`tmpl-detail-${id}`);
      if (!detail) return;

      if (detail.style.display !== 'none') {
        detail.style.display = 'none';
        return;
      }

      const tmpl = templates.find(t => String(t.id) === id);
      if (!tmpl) return;

      let weekData = null;
      try { weekData = JSON.parse(tmpl.routineDataJson); } catch (e) { /* */ }
      const days = weekData?.Days || weekData?.days || [];

      detail.innerHTML = days.map(d => {
        const dayNum = d.day ?? d.Day;
        const groups = (d.muscleGroups || d.MuscleGroups || []).map(g => muscleGroup(g.name || g.Name)).join(', ');
        const exercises = d.exercises || d.Exercises || [];

        if (exercises.length === 0) return '';

        const exLines = exercises.map(ex => {
          const name = ex.exerciseName || ex.ExerciseName || '';
          const sets = ex.sets || ex.Sets || 0;
          const reps = ex.reps || ex.Reps || 0;
          const weight = ex.weight || ex.Weight || 0;
          const rawSD = ex.setDetails || ex.SetDetails || '';
          let details = null;
          try { if (rawSD) details = typeof rawSD === 'string' ? JSON.parse(rawSD) : rawSD; } catch (e) { /* */ }

          let extraInfo = '';
          if (Array.isArray(details) && details.length > 0) {
            const tp = details[0].tempoPos || 0;
            const tn = details[0].tempoNeg || 0;
            const grip = details[0].grip || '';
            if (tp > 0 || tn > 0) extraInfo += ` <span style="font-size:10px;color:#512BD4;background:#f3f0fc;padding:1px 4px;border-radius:4px;">&#8593;${tp}s ${t('TempoAsc')} &#8595;${tn}s ${t('TempoDesc')}</span>`;
            if (grip) {
              const gripKey = 'Grip' + grip.charAt(0).toUpperCase() + grip.slice(1).toLowerCase();
              extraInfo += ` <span style="font-size:10px;color:#e67e22;background:#fff3e0;padding:1px 4px;border-radius:4px;">&#9994; ${t(gripKey) || grip}</span>`;
            }
          }

          const weightStr = weight > 0 ? ` @${weight}kg` : '';
          return `<div style="font-size:12px;padding:2px 0;">&bull; ${name} — ${sets}x${reps}${weightStr}${extraInfo}</div>`;
        }).join('');

        return `
          <div style="margin-bottom:8px;">
            <div style="font-weight:600;font-size:13px;">${dayName(dayNum)}${groups ? ` (${groups})` : ''}</div>
            ${exLines}
          </div>
        `;
      }).join('');

      detail.style.display = 'block';
    });
  });

  // Apply template
  container.querySelectorAll('[data-apply-tmpl]').forEach(btn => {
    btn.addEventListener('click', (e) => {
      const id = parseInt(e.currentTarget.dataset.applyTmpl);
      const name = e.currentTarget.dataset.name;
      showApplyModal(id, name);
    });
  });

  // Delete template
  container.querySelectorAll('[data-delete-tmpl]').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      const id = parseInt(e.currentTarget.dataset.deleteTmpl);
      if (!confirm(t('ConfirmDeleteTemplate'))) return;

      try {
        await api.del(`/templates/${id}`);
        await loadTemplates();
      } catch (err) {
        alert(t('ErrorFmt', err.message));
      }
    });
  });
}

async function showSaveModal() {
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
    return `<option value="${uid}">${uname}</option>`;
  }).join('');

  overlay.innerHTML = `
    <div class="modal-content" style="max-width:400px;">
      <div class="modal-header">
        <div class="modal-title">${t('SaveTemplate')}</div>
        <button class="modal-close" id="tmpl-modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('TemplateName')}</label>
        <input id="tmpl-name" class="form-input" type="text" placeholder="Plan de entrenamiento...">
      </div>
      <div class="form-group">
        <label class="form-label">${t('TemplateDescription')}</label>
        <input id="tmpl-desc" class="form-input" type="text">
      </div>
      <div class="form-group">
        <label class="form-label">${t('SelectSourceUser')}</label>
        <select id="tmpl-source-user" class="form-input">${userOptions}</select>
      </div>
      <button id="tmpl-save-submit" class="btn btn-primary btn-block">${t('SaveTemplate')}</button>
      <div id="tmpl-save-status" style="margin-top:8px;font-size:13px;text-align:center;"></div>
    </div>
  `;

  document.body.appendChild(overlay);
  overlay.querySelector('#tmpl-modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });

  overlay.querySelector('#tmpl-save-submit')?.addEventListener('click', async () => {
    const name = overlay.querySelector('#tmpl-name')?.value?.trim();
    const description = overlay.querySelector('#tmpl-desc')?.value?.trim();
    const sourceUserId = parseInt(overlay.querySelector('#tmpl-source-user')?.value);
    const statusEl = overlay.querySelector('#tmpl-save-status');
    const btn = overlay.querySelector('#tmpl-save-submit');

    if (!name) {
      if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = t('TemplateName'); }
      return;
    }

    if (btn) { btn.disabled = true; btn.textContent = t('SavingTemplate'); }

    try {
      const result = await api.post('/templates', { name, description, sourceUserId });
      if (result?.success) {
        if (statusEl) { statusEl.style.color = '#28a745'; statusEl.textContent = t('TemplateSaved'); }
        setTimeout(() => { overlay.remove(); loadTemplates(); }, 1500);
      } else {
        if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = result?.error || result?.message || 'Error'; }
        if (btn) { btn.disabled = false; btn.textContent = t('SaveTemplate'); }
      }
    } catch (err) {
      if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = t('ErrorFmt', err.message); }
      if (btn) { btn.disabled = false; btn.textContent = t('SaveTemplate'); }
    }
  });
}

async function showApplyModal(templateId, templateName) {
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
    return `<option value="${uid}">${uname}</option>`;
  }).join('');

  overlay.innerHTML = `
    <div class="modal-content" style="max-width:400px;">
      <div class="modal-header">
        <div class="modal-title">${t('ApplyTemplate')}: ${templateName}</div>
        <button class="modal-close" id="apply-modal-close">&times;</button>
      </div>
      <div class="form-group">
        <label class="form-label">${t('TargetUser')}</label>
        <select id="apply-target-user" class="form-input">${userOptions}</select>
      </div>
      <button id="apply-submit" class="btn btn-primary btn-block">${t('ApplyTemplate')}</button>
      <div id="apply-status" style="margin-top:8px;font-size:13px;text-align:center;"></div>
    </div>
  `;

  document.body.appendChild(overlay);
  overlay.querySelector('#apply-modal-close')?.addEventListener('click', () => overlay.remove());
  overlay.addEventListener('click', (e) => { if (e.target === overlay) overlay.remove(); });

  overlay.querySelector('#apply-submit')?.addEventListener('click', async () => {
    const targetUserId = parseInt(overlay.querySelector('#apply-target-user')?.value);
    const targetName = overlay.querySelector('#apply-target-user')?.selectedOptions[0]?.text || '';
    const statusEl = overlay.querySelector('#apply-status');
    const btn = overlay.querySelector('#apply-submit');

    if (!confirm(t('ConfirmApplyTemplate', targetName))) return;

    if (btn) { btn.disabled = true; btn.textContent = t('CopyingRoutines'); }

    try {
      const result = await api.post(`/templates/${templateId}/apply`, { targetUserId });
      if (result?.success) {
        if (statusEl) { statusEl.style.color = '#28a745'; statusEl.textContent = t('TemplateApplied'); }
        setTimeout(() => overlay.remove(), 2000);
      } else {
        if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = result?.error || 'Error'; }
        if (btn) { btn.disabled = false; btn.textContent = t('ApplyTemplate'); }
      }
    } catch (err) {
      if (statusEl) { statusEl.style.color = '#dc3545'; statusEl.textContent = t('ErrorFmt', err.message); }
      if (btn) { btn.disabled = false; btn.textContent = t('ApplyTemplate'); }
    }
  });
}
