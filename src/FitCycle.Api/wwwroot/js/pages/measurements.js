// FitCycle Measurements Page — body measurements tracking

import { t } from '../l10n.js';
import { api } from '../api.js';
import { showAlert, showConfirm, escapeHtml } from '../utils.js';

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

  // BMI card (if weight + height available)
  if (measurements.length > 0) {
    const last = measurements[0];
    const w = last.weight || 0;
    const h = last.height || 0;
    if (w > 0 && h > 0) {
      const bmi = (w / ((h / 100) ** 2)).toFixed(1);
      let bmiColor = '#28a745'; // normal
      let bmiLabel = t('NormalWeight');
      if (bmi < 18.5) { bmiColor = '#ff8c00'; bmiLabel = t('Underweight'); }
      else if (bmi >= 25 && bmi < 30) { bmiColor = '#ff8c00'; bmiLabel = t('Overweight'); }
      else if (bmi >= 30) { bmiColor = '#dc3545'; bmiLabel = t('Obese'); }

      html += `
        <div class="card mb-8" style="text-align:center;">
          <div style="font-size:13px;color:var(--text-light);margin-bottom:4px;">${t('BMI')}</div>
          <div style="font-size:32px;font-weight:800;color:${bmiColor};">${bmi}</div>
          <div style="font-size:13px;color:${bmiColor};font-weight:600;">${bmiLabel}</div>
        </div>
      `;
    }
  }

  // Trend chart
  if (measurements.length >= 2) {
    const trendFields = FIELDS.filter(f => {
      return measurements.some(m => m[f.key] > 0);
    });
    if (trendFields.length > 0) {
      const trendOptions = trendFields.map(f =>
        `<option value="${f.key}">${t(f.label)}</option>`
      ).join('');
      html += `
        <div class="card mb-8">
          <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px;">
            <div class="card-title">${t('MeasTrend')}</div>
            <select id="meas-trend-field" style="font-size:12px;padding:4px 8px;border:1px solid var(--border);border-radius:6px;">${trendOptions}</select>
          </div>
          <div id="meas-trend-chart"></div>
        </div>
      `;
    }
  }

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
            ${m.notes ? `<div style="font-size:11px;color:#999;margin-top:2px;font-style:italic;">${escapeHtml(m.notes)}</div>` : ''}
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

  // Trend chart
  const trendSelect = document.getElementById('meas-trend-field');
  if (trendSelect) {
    const renderTrend = () => {
      const field = trendSelect.value;
      const chartEl = document.getElementById('meas-trend-chart');
      if (!chartEl) return;
      // Get data points (reverse to chronological order)
      const points = measurements.filter(m => m[field] > 0).reverse().map(m => ({
        value: m[field],
        date: new Date(m.measuredAt || m.MeasuredAt),
      }));
      if (points.length < 2) { chartEl.innerHTML = '<div style="text-align:center;color:var(--text-muted);padding:16px;font-size:13px;">-</div>'; return; }

      const maxVal = Math.max(...points.map(p => p.value));
      const minVal = Math.min(...points.map(p => p.value));
      const range = maxVal - minVal || 1;
      const W = 360, H = 130, pad = 28;
      const iW = W - pad * 2, iH = H - pad * 2;

      const svgPts = points.map((p, i) => {
        const x = pad + (points.length > 1 ? (i / (points.length - 1)) * iW : iW / 2);
        const y = pad + iH - ((p.value - minVal) / range) * iH;
        return { x, y, v: p.value, d: p.date };
      });

      const polyline = svgPts.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
      const circles = svgPts.map(p => `<circle cx="${p.x.toFixed(1)}" cy="${p.y.toFixed(1)}" r="3.5" fill="#512BD4" />`).join('');
      const labels = svgPts.filter((_, i) => i === 0 || i === svgPts.length - 1 || points.length <= 6).map(p =>
        `<text x="${p.x.toFixed(1)}" y="${(p.y - 7).toFixed(1)}" text-anchor="middle" font-size="10" fill="#333">${p.v}</text>`
      ).join('');
      const dateLbls = svgPts.filter((_, i) => i === 0 || i === svgPts.length - 1).map(p => {
        const ds = `${p.d.getDate()}/${p.d.getMonth() + 1}`;
        return `<text x="${p.x.toFixed(1)}" y="${(H - 4).toFixed(1)}" text-anchor="middle" font-size="9" fill="gray">${ds}</text>`;
      }).join('');

      chartEl.innerHTML = `<svg width="100%" viewBox="0 0 ${W} ${H}" style="max-width:${W}px;"><polyline points="${polyline}" fill="none" stroke="#512BD4" stroke-width="2" />${circles}${labels}${dateLbls}</svg>`;
    };
    trendSelect.addEventListener('change', renderTrend);
    renderTrend();
  }

  // Delete handlers
  container.querySelectorAll('.btn-delete-meas').forEach(btn => {
    btn.addEventListener('click', async (e) => {
      e.stopPropagation();
      if (!await showConfirm(t('ConfirmDeleteMeas'))) return;
      const id = btn.dataset.measId;
      try {
        await api.del(`/measurements/${id}`);
        const updated = await api.get('/measurements');
        renderContent(container, updated || []);
      } catch (err) {
        await showAlert(t('ErrorFmt', err.message));
      }
    });
  });
}

export function destroy() {}
