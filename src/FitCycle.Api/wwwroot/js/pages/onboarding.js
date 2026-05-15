// FitCycle Onboarding Wizard — shown to first-time users after login.
// Persists completion in localStorage; no DB migration required.

import { t } from '../l10n.js';
import { api } from '../api.js';
import { auth } from '../auth.js';
import { haptic, confetti, showAlert } from '../utils.js';

const ONBOARDING_KEY = 'fitcycle_onboarding_done';

export function isOnboardingDone() {
  return localStorage.getItem(ONBOARDING_KEY) === 'true';
}

export function markOnboardingDone() {
  localStorage.setItem(ONBOARDING_KEY, 'true');
}

let step = 1;
const answers = {
  goal: 'Hipertrofia',
  level: 'Intermedio',
  days: 4,
  equipment: 'Gimnasio completo',
};

export function render() {
  step = 1; // reset on entry
  return `
    <div class="page no-tabs">
      <div class="page-content" style="max-width:480px;margin:0 auto;">
        <div id="onboarding-progress" style="display:flex;gap:4px;margin-bottom:18px;">
          ${[1, 2, 3, 4, 5].map(s => `<div class="onb-bar" data-s="${s}" style="flex:1;height:4px;border-radius:2px;background:#e0e0e0;"></div>`).join('')}
        </div>
        <div id="onboarding-step"></div>
      </div>
    </div>
  `;
}

export function mount() {
  renderStep();
}

export function destroy() {}

function setProgress() {
  document.querySelectorAll('.onb-bar').forEach((bar) => {
    const s = parseInt(bar.dataset.s);
    bar.style.background = s <= step ? 'var(--primary, #512BD4)' : '#e0e0e0';
  });
}

function renderStep() {
  setProgress();
  const container = document.getElementById('onboarding-step');
  if (!container) return;

  if (step === 1) {
    container.innerHTML = `
      <div style="text-align:center;">
        <div style="font-size:64px;margin-bottom:12px;">👋</div>
        <h2 style="margin:0 0 8px;font-size:24px;">${t('OnbWelcomeTitle')}</h2>
        <p style="color:var(--text-light);margin-bottom:24px;">${t('OnbWelcomeDesc')}</p>
        <button class="btn btn-primary btn-block btn-lg" id="onb-next">${t('OnbStart')}</button>
        <button class="btn btn-ghost btn-block" id="onb-skip" style="margin-top:8px;color:var(--text-light);">${t('OnbSkip')}</button>
      </div>
    `;
    document.getElementById('onb-next').onclick = () => { step = 2; renderStep(); haptic('tap'); };
    document.getElementById('onb-skip').onclick = finish;
  } else if (step === 2) {
    container.innerHTML = `
      <h2 style="font-size:20px;margin:0 0 12px;">${t('OnbGoalTitle')}</h2>
      <p style="color:var(--text-light);margin-bottom:16px;font-size:13px;">${t('OnbGoalDesc')}</p>
      ${optionGrid([
        { v: 'Hipertrofia', icon: '💪', label: t('AIGoalHypertrophy') },
        { v: 'Fuerza', icon: '🏋️', label: t('AIGoalStrength') },
        { v: 'Resistencia', icon: '🏃', label: t('AIGoalEndurance') },
        { v: 'Pérdida de grasa', icon: '🔥', label: t('AIGoalFatLoss') },
        { v: 'Fitness general', icon: '⚡', label: t('AIGoalGeneral') },
      ], 'goal')}
      ${navButtons()}
    `;
    bindOptions('goal');
    bindNav();
  } else if (step === 3) {
    container.innerHTML = `
      <h2 style="font-size:20px;margin:0 0 12px;">${t('OnbLevelTitle')}</h2>
      <p style="color:var(--text-light);margin-bottom:16px;font-size:13px;">${t('OnbLevelDesc')}</p>
      ${optionGrid([
        { v: 'Principiante', icon: '🌱', label: t('AILevelBeginner') },
        { v: 'Intermedio', icon: '🌿', label: t('AILevelIntermediate') },
        { v: 'Avanzado', icon: '🌳', label: t('AILevelAdvanced') },
      ], 'level')}
      ${navButtons()}
    `;
    bindOptions('level');
    bindNav();
  } else if (step === 4) {
    container.innerHTML = `
      <h2 style="font-size:20px;margin:0 0 12px;">${t('OnbDaysTitle')}</h2>
      <p style="color:var(--text-light);margin-bottom:16px;font-size:13px;">${t('OnbDaysDesc')}</p>
      ${optionGrid([
        { v: 3, icon: '3️⃣', label: '3 ' + t('OnbDaysShort') },
        { v: 4, icon: '4️⃣', label: '4 ' + t('OnbDaysShort') },
        { v: 5, icon: '5️⃣', label: '5 ' + t('OnbDaysShort') },
        { v: 6, icon: '6️⃣', label: '6 ' + t('OnbDaysShort') },
      ], 'days')}
      ${navButtons()}
    `;
    bindOptions('days');
    bindNav();
  } else if (step === 5) {
    container.innerHTML = `
      <h2 style="font-size:20px;margin:0 0 12px;">${t('OnbReadyTitle')}</h2>
      <p style="color:var(--text-light);margin-bottom:16px;font-size:13px;">${t('OnbReadyDesc')}</p>
      <div class="card" style="margin-bottom:16px;">
        <div style="display:flex;justify-content:space-between;padding:6px 0;">
          <span>${t('AIGoal')}</span><strong>${answers.goal}</strong>
        </div>
        <div style="display:flex;justify-content:space-between;padding:6px 0;border-top:1px solid var(--border-light);">
          <span>${t('AILevel')}</span><strong>${answers.level}</strong>
        </div>
        <div style="display:flex;justify-content:space-between;padding:6px 0;border-top:1px solid var(--border-light);">
          <span>${t('AIDays')}</span><strong>${answers.days}</strong>
        </div>
      </div>
      <button class="btn btn-primary btn-block btn-lg" id="onb-generate">🤖 ${t('OnbGenerate')}</button>
      <button class="btn btn-outline btn-block" id="onb-finish" style="margin-top:8px;">${t('OnbFinishNoGen')}</button>
      <div id="onb-status" style="text-align:center;margin-top:10px;color:var(--text-light);font-size:13px;"></div>
    `;
    document.getElementById('onb-generate').onclick = generateRoutine;
    document.getElementById('onb-finish').onclick = finish;
  }
}

function optionGrid(options, key) {
  return `<div style="display:grid;grid-template-columns:repeat(2,1fr);gap:8px;margin-bottom:16px;">
    ${options.map(o => `
      <button class="onb-option" data-key="${key}" data-value="${o.v}"
        style="background:${answers[key] === o.v ? 'var(--primary-light)' : 'var(--card-bg)'};
               border:2px solid ${answers[key] === o.v ? 'var(--primary)' : 'var(--border)'};
               border-radius:12px;padding:14px 8px;text-align:center;cursor:pointer;font-size:13px;">
        <div style="font-size:24px;margin-bottom:4px;">${o.icon}</div>
        <div style="font-weight:600;">${o.label}</div>
      </button>
    `).join('')}
  </div>`;
}

function bindOptions(key) {
  document.querySelectorAll(`.onb-option[data-key="${key}"]`).forEach(btn => {
    btn.addEventListener('click', () => {
      const raw = btn.dataset.value;
      answers[key] = isNaN(parseInt(raw)) ? raw : parseInt(raw);
      haptic('tap');
      renderStep();
    });
  });
}

function navButtons() {
  return `
    <div style="display:flex;gap:8px;">
      <button class="btn btn-outline btn-block" id="onb-back">${t('Back')}</button>
      <button class="btn btn-primary btn-block" id="onb-next">${t('Next')}</button>
    </div>
  `;
}

function bindNav() {
  document.getElementById('onb-back')?.addEventListener('click', () => { step--; renderStep(); haptic('tap'); });
  document.getElementById('onb-next')?.addEventListener('click', () => { step++; renderStep(); haptic('tap'); });
}

async function generateRoutine() {
  const btn = document.getElementById('onb-generate');
  const status = document.getElementById('onb-status');
  if (btn) { btn.disabled = true; btn.textContent = '🤖 ' + (t('AIThinking') || 'Procesando...'); }
  if (status) status.textContent = '';
  try {
    await api.post('/ai/generate-routine', answers);
    // The AI just returns the suggestion; the routine is not saved automatically.
    // Take the user to the AI page so they can review and apply.
    markOnboardingDone();
    haptic('success');
    confetti(2000);
    await showAlert(t('OnbGenerateDone'));
    location.hash = '#ai';
  } catch (err) {
    if (status) status.textContent = t('ErrorFmt', err.message || err);
    if (btn) { btn.disabled = false; btn.textContent = '🤖 ' + t('OnbGenerate'); }
  }
}

function finish() {
  markOnboardingDone();
  haptic('success');
  location.hash = '#home';
}
