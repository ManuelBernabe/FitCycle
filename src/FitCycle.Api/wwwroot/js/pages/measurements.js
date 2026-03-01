// FitCycle Measurements Page — body measurements tracking

import { t } from '../l10n.js';
import { api } from '../api.js';

const FIELDS = [
  { key: 'weight', label: 'MeasWeight', step: '0.1', unit: 'kg' },
  { key: 'height', label: 'MeasHeight', step: '0.1', unit: 'cm' },
  { key: 'chest', label: 'MeasChest', step: '0.1', unit: 'cm' },
  { key: 'waist', label: 'MeasWaist', step: '0.1', unit: 'cm' },
  { key: 'hips', label: 'MeasHips', step: '0.1', unit: 'cm' },
  { key: 'bicepLeft', label: 'MeasBicepL', step: '0.1', unit: 'cm' },
  { key: 'bicepRight', label: 'MeasBicepR', step: '0.1', unit: 'cm' },
  { key: 'thighLeft', label: 'MeasThighL', step: '0.1', unit: 'cm' },
  { key: 'thighRight', label: 'MeasThighR', step: '0.1', unit: 'cm' },
  { key: 'calfLeft', label: 'MeasCalfL', step: '0.1', unit: 'cm' },
  { key: 'calfRight', label: 'MeasCalfR', step: '0.1', unit: 'cm' },
  { key: 'neck', label: 'MeasNeck', step: '0.1', unit: 'cm' },
  { key: 'bodyFat', label: 'MeasBodyFat', step: '0.1', unit: '%' },
];

// Which fields are visible in the form
let activeFields = ['weight'];

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="section-title">${t('MyMeasurements')}</div>
        <div id="meas-content">
          <div class="loading-page"><div class="spinner"></div><span>${t('Loading')}</span></div>
        </div>
      </div>
    </div>
  `;
}

export async function mount() {
  const container = document.getElementById('meas-content');
  if (!container) return;

  try {
    const measurements = await api.get('/measurements');
    // Auto-detect active fields from last measurement
    if (measurements && measurements.length > 0) {
      const last = measurements[0];
      const detected = FIELDS.filter(f => last[f.key] != null && last[f.key] > 0).map(f => f.key);
      if (detected.length > 0) activeFields = detected;
    }
    renderContent(container, measurements || []);
  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
  }
}

function renderContent(container, measurements) {
  let html = '';

  // Field selector dropdown
  const fieldOptions = FIELDS.map(f =>
    `<option value="${f.key}" ${activeFields.includes(f.key) ? 'selected' : ''}>${t(f.label)}</option>`
  ).join('');

  // Active field inputs
  const fieldInputs = FIELDS.filter(f => activeFields.includes(f.key)).map(f => `
    <div style="display:flex;align-items:center;gap:6px;margin-bottom:6px;">
      <label style="font-size:13px;color:#333;min-width:100px;font-weight:500;">${t(f.label)}</label>
      <input type="number" id="meas-${f.key}" class="form-input" placeholder="0" step="${f.step}" min="0"
        style="font-size:14px;padding:6px 8px;flex:1;">
      <span style="font-size:12px;color:#999;min-width:24px;">${f.unit}</span>
    </div>
  `).join('');

  html += `
    <div class="card mb-8">
      <div class="card-title mb-8">${t('AddMeasurement')}</div>
      <div style="margin-bottom:10px;">
        <label style="font-size:11px;color:#666;display:block;margin-bottom:4px;">${t('MeasSelectFields')}</label>
        <select id="meas-field-select" multiple size="4" style="width:100%;font-size:13px;padding:4px;border:1px solid #ccc;border-radius:8px;">
          ${fieldOptions}
        </select>
      </div>
      <div id="meas-fields">
        ${fieldInputs}
      </div>
      <div style="margin-top:8px;">
        <label style="font-size:11px;color:#666;">${t('MeasNotes')}</label>
        <input type="text" id="meas-notes" class="form-input" placeholder="..." style="font-size:14px;padding:6px 8px;">
      </div>
      <button id="meas-save" class="btn btn-primary btn-block mt-8">${t('Save')}</button>
      <div id="meas-status" class="status-text mt-4"></div>
    </div>
  `;

  // History
  if (measurements.length === 0) {
    html += `<div class="empty-state"><div class="empty-state-text">${t('NoMeasurements')}</div></div>`;
  } else {
    html += `<div class="card"><div class="card-title mb-8">${t('MeasHistory')}</div>`;
    html += measurements.map(m => {
      const date = new Date(m.measuredAt || m.MeasuredAt).toLocaleDateString();
      const values = [];
      FIELDS.forEach(f => {
        const v = m[f.key];
        if (v != null && v > 0) values.push(`${t(f.label).split(' ')[0]}: ${v}${f.unit}`);
      });
      const summary = values.length > 0 ? values.join(' | ') : '—';

      return `
        <div class="exercise-row" style="padding:8px 0;border-bottom:1px solid #f0f0f0;">
          <div style="flex:1;min-width:0;">
            <div style="font-weight:600;font-size:14px;">${date}</div>
            <div style="font-size:12px;color:#666;margin-top:2px;overflow-wrap:break-word;">${summary}</div>
            ${m.notes ? `<div style="font-size:11px;color:#999;margin-top:2px;font-style:italic;">${m.notes}</div>` : ''}
          </div>
          <button class="btn-delete-meas" data-meas-id="${m.id || m.Id}"
            style="background:none;border:none;color:#dc3545;font-size:16px;cursor:pointer;padding:4px 8px;flex-shrink:0;">&#10005;</button>
        </div>
      `;
    }).join('');
    html += '</div>';
  }

  container.innerHTML = html;

  // Pre-fill with last measurement values
  if (measurements.length > 0) {
    const last = measurements[0];
    FIELDS.forEach(f => {
      const el = document.getElementById(`meas-${f.key}`);
      if (el && last[f.key]) el.value = last[f.key];
    });
  }

  // Field selector change
  document.getElementById('meas-field-select')?.addEventListener('change', (e) => {
    const select = e.target;
    activeFields = Array.from(select.selectedOptions).map(o => o.value);
    if (activeFields.length === 0) activeFields = ['weight'];
    renderContent(container, measurements);
  });

  // Save handler
  document.getElementById('meas-save')?.addEventListener('click', async () => {
    const statusEl = document.getElementById('meas-status');
    const data = {};
    FIELDS.forEach(f => {
      const val = parseFloat(document.getElementById(`meas-${f.key}`)?.value);
      if (!isNaN(val) && val > 0) data[f.key] = val;
    });
    const notes = document.getElementById('meas-notes')?.value?.trim();
    if (notes) data.notes = notes;

    if (Object.keys(data).length === 0) return;

    try {
      if (statusEl) statusEl.textContent = t('Saving');
      await api.post('/measurements', data);
      if (statusEl) { statusEl.textContent = t('MeasSaved'); statusEl.style.color = '#28a745'; }
      const updated = await api.get('/measurements');
      renderContent(container, updated || []);
    } catch (err) {
      if (statusEl) { statusEl.textContent = t('ErrorFmt', err.message); statusEl.style.color = '#dc3545'; }
    }
  });

  // Delete handlers
  container.querySelectorAll('.btn-delete-meas').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      e.stopPropagation();
      if (!confirm(t('ConfirmDeleteMeas'))) return;
      const id = btn.dataset.measId;
      try {
        await api.delete(`/measurements/${id}`);
        const updated = await api.get('/measurements');
        renderContent(container, updated || []);
      } catch (err) {
        alert(t('ErrorFmt', err.message));
      }
    });
  });
}

export function destroy() {}
