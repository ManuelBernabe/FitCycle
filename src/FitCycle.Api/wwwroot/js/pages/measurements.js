// FitCycle Measurements Page — body measurements tracking

import { t } from '../l10n.js';
import { api } from '../api.js';

const FIELDS = [
  { key: 'weight', label: 'MeasWeight', step: '0.1' },
  { key: 'height', label: 'MeasHeight', step: '0.1' },
  { key: 'chest', label: 'MeasChest', step: '0.1' },
  { key: 'waist', label: 'MeasWaist', step: '0.1' },
  { key: 'hips', label: 'MeasHips', step: '0.1' },
  { key: 'bicepLeft', label: 'MeasBicepL', step: '0.1' },
  { key: 'bicepRight', label: 'MeasBicepR', step: '0.1' },
  { key: 'thighLeft', label: 'MeasThighL', step: '0.1' },
  { key: 'thighRight', label: 'MeasThighR', step: '0.1' },
  { key: 'calfLeft', label: 'MeasCalfL', step: '0.1' },
  { key: 'calfRight', label: 'MeasCalfR', step: '0.1' },
  { key: 'neck', label: 'MeasNeck', step: '0.1' },
  { key: 'bodyFat', label: 'MeasBodyFat', step: '0.1' },
];

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
    renderContent(container, measurements || []);
  } catch (err) {
    container.innerHTML = `<div class="empty-state"><div class="empty-state-text">${t('ErrorFmt', err.message)}</div></div>`;
  }
}

function renderContent(container, measurements) {
  let html = '';

  // Add new measurement form
  html += `
    <div class="card mb-8">
      <div class="card-title mb-8">${t('AddMeasurement')}</div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;">
        ${FIELDS.map(f => `
          <div>
            <label style="font-size:11px;color:#666;">${t(f.label)}</label>
            <input type="number" id="meas-${f.key}" class="form-input" placeholder="—" step="${f.step}" min="0"
              style="font-size:14px;padding:6px 8px;">
          </div>
        `).join('')}
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
      if (m.weight) values.push(`${m.weight}kg`);
      if (m.chest) values.push(`${t('MeasChest').split(' ')[0]}: ${m.chest}`);
      if (m.waist) values.push(`${t('MeasWaist').split(' ')[0]}: ${m.waist}`);
      if (m.hips) values.push(`${t('MeasHips').split(' ')[0]}: ${m.hips}`);
      if (m.bicepLeft || m.bicepRight) values.push(`Biceps: ${m.bicepLeft || '-'}/${m.bicepRight || '-'}`);
      if (m.bodyFat) values.push(`${m.bodyFat}%`);
      const summary = values.length > 0 ? values.join(' | ') : '—';

      return `
        <div class="exercise-row" style="cursor:pointer;" data-meas-id="${m.id || m.Id}">
          <div class="exercise-info" style="flex:1;">
            <div class="exercise-name">${date}</div>
            <div class="exercise-detail" style="font-size:12px;color:#666;">${summary}</div>
            ${m.notes ? `<div style="font-size:11px;color:#999;margin-top:2px;">${m.notes}</div>` : ''}
          </div>
          <button class="btn-delete-meas" data-meas-id="${m.id || m.Id}"
            style="background:none;border:none;color:#dc3545;font-size:16px;cursor:pointer;padding:4px 8px;">&#10005;</button>
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
