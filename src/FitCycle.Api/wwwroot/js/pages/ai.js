// FitCycle AI Page — AI-powered features using Gemini

import { t } from '../l10n.js';
import { api } from '../api.js';
import { escapeHtml } from '../utils.js';

export function render() {
  return `
    <div class="page">
      <div class="page-content">
        <div class="section-title">${t('AIAssistant')}</div>
        <div id="ai-content">
          <div id="ai-tabs" class="ai-tabs">
            <button class="ai-tab active" data-tab="analysis">${t('AIAnalysis')}</button>
            <button class="ai-tab" data-tab="generate">${t('AIGenerate')}</button>
            <button class="ai-tab" data-tab="suggest">${t('AISuggest')}</button>
          </div>
          <div id="ai-tab-content"></div>
        </div>
      </div>
    </div>
  `;
}

let currentTab = 'analysis';

export function mount() {
  document.querySelectorAll('.ai-tab').forEach(btn => {
    btn.addEventListener('click', () => {
      document.querySelectorAll('.ai-tab').forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      currentTab = btn.dataset.tab;
      renderTab();
    });
  });
  renderTab();
}

function renderTab() {
  const container = document.getElementById('ai-tab-content');
  if (!container) return;
  if (currentTab === 'analysis') renderAnalysis(container);
  else if (currentTab === 'generate') renderGenerate(container);
  else if (currentTab === 'suggest') renderSuggest(container);
}

async function renderAnalysis(container) {
  container.innerHTML = `<div class="loading-page"><div class="spinner"></div><span>${t('AIThinking')}</span></div>`;
  try {
    const data = await api.get('/ai/workout-analysis');
    if (!data?.analysis) { container.innerHTML = `<p>${t('AINoData')}</p>`; return; }
    if (typeof data.analysis === 'string') {
      container.innerHTML = `<div class="ai-card"><p>${escapeHtml(data.analysis)}</p></div>`;
      return;
    }
    const a = data.analysis;
    container.innerHTML = `
      <div class="ai-card">
        <h3>${t('AISummary')}</h3>
        <p>${escapeHtml(a.summary || '')}</p>
      </div>
      ${a.strengths?.length ? `<div class="ai-card ai-card-green">
        <h3>${t('AIStrengths')}</h3>
        <ul>${a.strengths.map(s => `<li>${escapeHtml(s)}</li>`).join('')}</ul>
      </div>` : ''}
      ${a.improvements?.length ? `<div class="ai-card ai-card-orange">
        <h3>${t('AIImprovements')}</h3>
        <ul>${a.improvements.map(s => `<li>${escapeHtml(s)}</li>`).join('')}</ul>
      </div>` : ''}
      ${a.plateaus?.length ? `<div class="ai-card ai-card-red">
        <h3>${t('AIPlateaus')}</h3>
        <ul>${a.plateaus.map(s => `<li>${escapeHtml(s)}</li>`).join('')}</ul>
      </div>` : ''}
      ${a.recommendations?.length ? `<div class="ai-card ai-card-blue">
        <h3>${t('AIRecommendations')}</h3>
        <ul>${a.recommendations.map(s => `<li>${escapeHtml(s)}</li>`).join('')}</ul>
      </div>` : ''}
      ${a.weeklyConsistency ? `<div class="ai-card">
        <h3>${t('AIConsistency')}</h3>
        <p>${escapeHtml(a.weeklyConsistency)}</p>
      </div>` : ''}
      ${a.muscleBalance ? `<div class="ai-card">
        <h3>${t('AIMuscleBalance')}</h3>
        <p>${escapeHtml(a.muscleBalance)}</p>
      </div>` : ''}
    `;
  } catch (e) {
    container.innerHTML = `<div class="ai-card ai-card-red"><p>${t('AIError')}: ${escapeHtml(e.message || '')}</p></div>`;
  }
}

function renderGenerate(container) {
  container.innerHTML = `
    <div class="ai-card">
      <h3>${t('AIGenerateTitle')}</h3>
      <div class="form-group">
        <label>${t('AIGoal')}</label>
        <select id="ai-goal" class="form-control">
          <option value="Hipertrofia">${t('AIGoalHypertrophy')}</option>
          <option value="Fuerza">${t('AIGoalStrength')}</option>
          <option value="Resistencia">${t('AIGoalEndurance')}</option>
          <option value="Pérdida de grasa">${t('AIGoalFatLoss')}</option>
          <option value="General fitness">${t('AIGoalGeneral')}</option>
        </select>
      </div>
      <div class="form-group">
        <label>${t('AILevel')}</label>
        <select id="ai-level" class="form-control">
          <option value="Principiante">${t('AILevelBeginner')}</option>
          <option value="Intermedio">${t('AILevelIntermediate')}</option>
          <option value="Avanzado">${t('AILevelAdvanced')}</option>
        </select>
      </div>
      <div class="form-group">
        <label>${t('AIDays')}</label>
        <select id="ai-days" class="form-control">
          <option value="3">3</option>
          <option value="4" selected>4</option>
          <option value="5">5</option>
          <option value="6">6</option>
        </select>
      </div>
      <div class="form-group">
        <label>${t('AIEquipment')}</label>
        <input type="text" id="ai-equipment" class="form-control" placeholder="${t('AIEquipmentPlaceholder')}">
      </div>
      <div class="form-group">
        <label>${t('AINotes')}</label>
        <input type="text" id="ai-notes" class="form-control" placeholder="${t('AINotesPlaceholder')}">
      </div>
      <button id="ai-generate-btn" class="btn btn-primary" style="width:100%;margin-top:12px;">${t('AIGenerateBtn')}</button>
    </div>
    <div id="ai-generate-result"></div>
  `;

  document.getElementById('ai-generate-btn')?.addEventListener('click', async () => {
    const resultDiv = document.getElementById('ai-generate-result');
    if (!resultDiv) return;
    resultDiv.innerHTML = `<div class="loading-page"><div class="spinner"></div><span>${t('AIThinking')}</span></div>`;

    try {
      const data = await api.post('/ai/generate-routine', {
        goal: document.getElementById('ai-goal')?.value || 'Hipertrofia',
        level: document.getElementById('ai-level')?.value || 'Intermedio',
        days: parseInt(document.getElementById('ai-days')?.value || '4'),
        equipment: document.getElementById('ai-equipment')?.value || null,
        notes: document.getElementById('ai-notes')?.value || null,
      });

      if (!data?.routine) { resultDiv.innerHTML = `<p>${t('AIError')}</p>`; return; }
      const r = data.routine;

      const dayNames = { 1: t('Monday'), 2: t('Tuesday'), 3: t('Wednesday'), 4: t('Thursday'), 5: t('Friday'), 6: t('Saturday') };

      let html = '';
      if (r.explanation) {
        html += `<div class="ai-card ai-card-blue"><p>${escapeHtml(r.explanation)}</p></div>`;
      }
      if (r.routines) {
        for (const day of r.routines) {
          html += `<div class="ai-card">
            <h3>${dayNames[day.dayOfWeek] || `Day ${day.dayOfWeek}`}</h3>
            <ul>`;
          for (const ex of (day.exercises || [])) {
            html += `<li><strong>${escapeHtml(ex.exerciseId ? `#${ex.exerciseId}` : '')} </strong>${escapeHtml(ex.notes || '')} — ${ex.sets}x${ex.reps}</li>`;
          }
          html += `</ul></div>`;
        }
      }

      resultDiv.innerHTML = html || `<p>${t('AINoData')}</p>`;
    } catch (e) {
      resultDiv.innerHTML = `<div class="ai-card ai-card-red"><p>${t('AIError')}: ${escapeHtml(e.message || '')}</p></div>`;
    }
  });
}

function renderSuggest(container) {
  container.innerHTML = `
    <div class="ai-card">
      <h3>${t('AISuggestTitle')}</h3>
      <div class="form-group">
        <label>${t('AISuggestLabel')}</label>
        <input type="text" id="ai-query" class="form-control" placeholder="${t('AISuggestPlaceholder')}">
      </div>
      <button id="ai-suggest-btn" class="btn btn-primary" style="width:100%;margin-top:12px;">${t('AISuggestBtn')}</button>
    </div>
    <div id="ai-suggest-result"></div>
  `;

  document.getElementById('ai-suggest-btn')?.addEventListener('click', async () => {
    const query = document.getElementById('ai-query')?.value?.trim();
    if (!query) return;
    const resultDiv = document.getElementById('ai-suggest-result');
    if (!resultDiv) return;
    resultDiv.innerHTML = `<div class="loading-page"><div class="spinner"></div><span>${t('AIThinking')}</span></div>`;

    try {
      const data = await api.post('/ai/exercise-suggestions', { query });
      if (!data?.result) { resultDiv.innerHTML = `<p>${t('AINoData')}</p>`; return; }
      const r = data.result;
      let html = '';
      if (r.suggestions?.length) {
        html += `<div class="ai-card"><h3>${t('AISuggestions')}</h3><ul>`;
        for (const s of r.suggestions) {
          html += `<li><strong>${escapeHtml(s.name)}</strong> — ${escapeHtml(s.reason)}</li>`;
        }
        html += `</ul></div>`;
      }
      if (r.tips) {
        html += `<div class="ai-card ai-card-blue"><p>${escapeHtml(r.tips)}</p></div>`;
      }
      resultDiv.innerHTML = html || `<p>${t('AINoData')}</p>`;
    } catch (e) {
      resultDiv.innerHTML = `<div class="ai-card ai-card-red"><p>${t('AIError')}: ${escapeHtml(e.message || '')}</p></div>`;
    }
  });
}
