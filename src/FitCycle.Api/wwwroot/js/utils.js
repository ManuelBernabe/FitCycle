// FitCycle Shared Utilities — escapeHtml, calculateStreak, modal dialogs

import { t } from './l10n.js';

/**
 * Escape HTML to prevent XSS.
 */
export function escapeHtml(str) {
  if (!str) return '';
  const div = document.createElement('div');
  div.textContent = str;
  return div.innerHTML;
}

/**
 * Calculate workout streak (consecutive weekdays with workouts).
 */
export function calculateStreak(workouts) {
  if (!workouts || workouts.length === 0) return 0;

  const dates = new Set();
  workouts.forEach(w => {
    const d = new Date(w.completedAt || w.CompletedAt);
    dates.add(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`);
  });

  const sortedDates = [...dates].sort().reverse();
  if (sortedDates.length === 0) return 0;

  const today = new Date();
  const todayStr = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

  let streak = 0;
  let checkDate = new Date(today);

  if (!dates.has(todayStr)) {
    checkDate.setDate(checkDate.getDate() - 1);
    const yesterdayStr = `${checkDate.getFullYear()}-${String(checkDate.getMonth() + 1).padStart(2, '0')}-${String(checkDate.getDate()).padStart(2, '0')}`;
    if (!dates.has(yesterdayStr)) return 0;
  }

  for (let i = 0; i < 365; i++) {
    const dateStr = `${checkDate.getFullYear()}-${String(checkDate.getMonth() + 1).padStart(2, '0')}-${String(checkDate.getDate()).padStart(2, '0')}`;
    const dayOfWeek = checkDate.getDay();

    if (dayOfWeek === 0 || dayOfWeek === 6) {
      checkDate.setDate(checkDate.getDate() - 1);
      continue;
    }

    if (dates.has(dateStr)) {
      streak++;
      checkDate.setDate(checkDate.getDate() - 1);
    } else {
      break;
    }
  }

  return streak;
}

// ── Custom Modal Dialogs ──

function createModalOverlay() {
  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.style.zIndex = '150';
  return overlay;
}

/**
 * Show an alert dialog (replaces native alert).
 * @returns {Promise<void>}
 */
export function showAlert(msg) {
  return new Promise(resolve => {
    const overlay = createModalOverlay();
    overlay.innerHTML = `
      <div class="modal-content" style="max-width:340px;text-align:center;">
        <div style="font-size:14px;line-height:1.5;margin-bottom:16px;word-break:break-word;">${escapeHtml(msg)}</div>
        <button class="btn btn-primary btn-block" id="modal-alert-ok">${t('OK') || 'OK'}</button>
      </div>
    `;
    document.body.appendChild(overlay);
    overlay.querySelector('#modal-alert-ok').addEventListener('click', () => {
      overlay.remove();
      resolve();
    });
  });
}

/**
 * Show a confirm dialog (replaces native confirm).
 * @returns {Promise<boolean>}
 */
export function showConfirm(msg) {
  return new Promise(resolve => {
    const overlay = createModalOverlay();
    overlay.innerHTML = `
      <div class="modal-content" style="max-width:340px;text-align:center;">
        <div style="font-size:14px;line-height:1.5;margin-bottom:16px;word-break:break-word;">${escapeHtml(msg)}</div>
        <div style="display:flex;gap:8px;">
          <button class="btn btn-outline btn-block" id="modal-confirm-cancel">${t('Cancel') || 'Cancel'}</button>
          <button class="btn btn-primary btn-block" id="modal-confirm-ok">${t('Confirm') || 'OK'}</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
    overlay.querySelector('#modal-confirm-ok').addEventListener('click', () => {
      overlay.remove();
      resolve(true);
    });
    overlay.querySelector('#modal-confirm-cancel').addEventListener('click', () => {
      overlay.remove();
      resolve(false);
    });
    overlay.addEventListener('click', (e) => {
      if (e.target === overlay) { overlay.remove(); resolve(false); }
    });
  });
}

/**
 * Show a prompt dialog (replaces native prompt).
 * @returns {Promise<string|null>}
 */
export function showPrompt(msg, placeholder = '') {
  return new Promise(resolve => {
    const overlay = createModalOverlay();
    overlay.innerHTML = `
      <div class="modal-content" style="max-width:340px;">
        <div style="font-size:14px;line-height:1.5;margin-bottom:12px;word-break:break-word;">${escapeHtml(msg)}</div>
        <input class="form-input" id="modal-prompt-input" type="text" placeholder="${escapeHtml(placeholder)}" style="margin-bottom:12px;">
        <div style="display:flex;gap:8px;">
          <button class="btn btn-outline btn-block" id="modal-prompt-cancel">${t('Cancel') || 'Cancel'}</button>
          <button class="btn btn-primary btn-block" id="modal-prompt-ok">${t('OK') || 'OK'}</button>
        </div>
      </div>
    `;
    document.body.appendChild(overlay);
    const input = overlay.querySelector('#modal-prompt-input');
    input.focus();
    input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') { overlay.remove(); resolve(input.value); }
    });
    overlay.querySelector('#modal-prompt-ok').addEventListener('click', () => {
      overlay.remove();
      resolve(input.value);
    });
    overlay.querySelector('#modal-prompt-cancel').addEventListener('click', () => {
      overlay.remove();
      resolve(null);
    });
    overlay.addEventListener('click', (e) => {
      if (e.target === overlay) { overlay.remove(); resolve(null); }
    });
  });
}
