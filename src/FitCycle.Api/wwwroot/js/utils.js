// FitCycle Shared Utilities — escapeHtml, calculateStreak, modal dialogs, theme

import { t } from './l10n.js';

// ── Environment Tag ──

/** Returns (D) for develop, (P) for production, (L) for localhost */
export function envTag() {
  const h = location.hostname;
  if (h === 'localhost' || h === '127.0.0.1') return ' (L)';
  if (h.includes('develop') || h.includes('-dev')) return ' (D)';
  return ' (P)';
}

// ── Font Size Management ──

const FONTSIZE_KEY = 'fitcycle_fontsize'; // percentage: 80, 90, 100, 110, 120, 130
const FONTSIZE_OPTIONS = [80, 90, 100, 110, 120, 130];

export function getFontSize() {
  return parseInt(localStorage.getItem(FONTSIZE_KEY) || '100', 10);
}

export function setFontSize(pct) {
  localStorage.setItem(FONTSIZE_KEY, String(pct));
  applyFontSize();
}

export function applyFontSize() {
  const pct = getFontSize();
  let styleEl = document.getElementById('fitcycle-zoom-style');
  if (pct === 100) {
    if (styleEl) styleEl.remove();
    return;
  }
  if (!styleEl) {
    styleEl = document.createElement('style');
    styleEl.id = 'fitcycle-zoom-style';
    document.head.appendChild(styleEl);
  }
  // Scale all text and spacing uniformly via zoom on #app
  // Fallback to transform for Firefox which doesn't support zoom
  const scale = pct / 100;
  styleEl.textContent = `
    #app { zoom: ${pct}%; }
    @supports not (zoom: 1) {
      #app { transform: scale(${scale}); transform-origin: top center; width: ${100 / scale}%; }
    }
  `;
}

export function fontSizeUp() {
  const cur = getFontSize();
  const idx = FONTSIZE_OPTIONS.indexOf(cur);
  if (idx < FONTSIZE_OPTIONS.length - 1) setFontSize(FONTSIZE_OPTIONS[idx + 1]);
}

export function fontSizeDown() {
  const cur = getFontSize();
  const idx = FONTSIZE_OPTIONS.indexOf(cur);
  if (idx > 0) setFontSize(FONTSIZE_OPTIONS[idx - 1]);
}

export function fontSizeOptions() { return FONTSIZE_OPTIONS; }

// ── Theme Management ──

const THEME_KEY = 'fitcycle_theme'; // 'auto' | 'light' | 'dark'

/** Get the saved theme preference (defaults to 'auto'). */
export function getTheme() {
  return localStorage.getItem(THEME_KEY) || 'auto';
}

/** Set theme preference and apply it immediately. */
export function setTheme(mode) {
  localStorage.setItem(THEME_KEY, mode);
  applyTheme();
}

/** Apply the current theme to <html>. Call on startup and on change. */
export function applyTheme() {
  const mode = getTheme();
  const html = document.documentElement;
  html.classList.remove('dark');

  if (mode === 'dark') {
    html.classList.add('dark');
  } else if (mode === 'auto') {
    if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
      html.classList.add('dark');
    }
  }
  // 'light' — no .dark class
}

// Listen for system theme changes (for auto mode)
if (typeof window !== 'undefined' && window.matchMedia) {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    if (getTheme() === 'auto') applyTheme();
  });
}

// Apply theme immediately on module load (before first paint)
applyTheme();

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

// ── 1RM Calculator (Epley formula) ──

/**
 * Estimate one-rep max from a working set using the Epley formula.
 * 1RM ≈ weight * (1 + reps/30). Returns 0 for invalid inputs.
 */
export function estimate1RM(weight, reps) {
  const w = parseFloat(weight) || 0;
  const r = parseInt(reps) || 0;
  if (w <= 0 || r <= 0) return 0;
  if (r === 1) return w;
  return Math.round(w * (1 + r / 30) * 10) / 10; // 1 decimal
}

// ── Haptic feedback ──

/**
 * Unified haptic feedback. Silently no-ops on iOS Safari which doesn't expose vibrate.
 * Types: 'tap' (light), 'success' (medium), 'pr' (strong), 'error' (warning), 'finish' (long).
 */
export function haptic(type = 'tap') {
  if (!navigator.vibrate) return;
  try {
    switch (type) {
      case 'tap': navigator.vibrate(20); break;
      case 'success': navigator.vibrate([30, 50, 30]); break;
      case 'pr': navigator.vibrate([200, 80, 200, 80, 300]); break;
      case 'error': navigator.vibrate([60, 60, 60]); break;
      case 'finish': navigator.vibrate([100, 50, 100, 50, 300]); break;
      default: navigator.vibrate(20);
    }
  } catch { /* ignore */ }
}

// ── Visual celebrations ──

/**
 * Pulse-success animation on an element (green flash + scale).
 * Adds .celebrate-pulse class for 600ms.
 */
export function celebrate(element) {
  if (!element) return;
  element.classList.remove('celebrate-pulse');
  // Force reflow so re-adding the class re-triggers the animation
  void element.offsetWidth;
  element.classList.add('celebrate-pulse');
  setTimeout(() => element.classList.remove('celebrate-pulse'), 700);
}

/**
 * Show confetti at the bottom of the viewport. Used on workout finish or PR.
 * Duration in ms, defaults to 2500.
 */
export function confetti(duration = 2500) {
  const container = document.createElement('div');
  container.className = 'confetti-container';
  container.setAttribute('aria-hidden', 'true');
  const colors = ['#512BD4', '#28a745', '#e67e22', '#ffc107', '#dc3545', '#17a2b8'];
  for (let i = 0; i < 40; i++) {
    const piece = document.createElement('div');
    piece.className = 'confetti-piece';
    piece.style.left = `${Math.random() * 100}%`;
    piece.style.background = colors[Math.floor(Math.random() * colors.length)];
    piece.style.animationDelay = `${Math.random() * 0.5}s`;
    piece.style.animationDuration = `${1.5 + Math.random() * 1.5}s`;
    container.appendChild(piece);
  }
  document.body.appendChild(container);
  setTimeout(() => container.remove(), duration);
}

// ── Skeleton helpers ──

/**
 * Returns HTML for a generic skeleton loader. Use instead of <div class="loading-page"><div class="spinner"></div></div>.
 * @param {('card'|'row'|'list')} variant
 */
export function skeleton(variant = 'card') {
  if (variant === 'row') {
    return `<div class="skeleton-row" aria-busy="true" aria-label="Loading">
      <div class="skeleton skeleton-circle"></div>
      <div style="flex:1;">
        <div class="skeleton skeleton-line" style="width:60%;"></div>
        <div class="skeleton skeleton-line" style="width:30%;"></div>
      </div>
    </div>`;
  }
  if (variant === 'list') {
    return Array.from({ length: 4 }).map(() => skeleton('row')).join('');
  }
  return `<div class="skeleton-card" aria-busy="true" aria-label="Loading">
    <div class="skeleton skeleton-line" style="width:40%;"></div>
    <div class="skeleton skeleton-line" style="width:80%;"></div>
    <div class="skeleton skeleton-line" style="width:70%;"></div>
  </div>`;
}

// ── Streak celebration ──

const STREAK_KEY = 'fitcycle_last_streak_celebrated';

/**
 * Show a celebration modal when the user crosses a streak milestone (3, 5, 7, 14, 30 days).
 * Persists "last celebrated" to localStorage to avoid duplicate shows.
 */
export function checkStreakMilestone(streak) {
  if (!streak || streak < 3) return;
  const milestones = [3, 5, 7, 14, 21, 30, 60, 100];
  const last = parseInt(localStorage.getItem(STREAK_KEY) || '0', 10);
  // Find the highest milestone reached that's higher than the last celebrated
  const reached = milestones.filter(m => streak >= m && m > last).pop();
  if (!reached) return;
  localStorage.setItem(STREAK_KEY, String(reached));
  showStreakModal(streak, reached);
}

// ── YouTube demo video modal ──

/**
 * Extracts the YouTube video ID from a watch / share / shorts URL.
 * Returns null if the URL doesn't look like YouTube.
 */
export function extractYoutubeId(url) {
  if (!url) return null;
  const m = url.match(/(?:youtube\.com\/(?:watch\?v=|embed\/|shorts\/)|youtu\.be\/)([\w-]{11})/);
  return m ? m[1] : null;
}

/**
 * Opens a modal with a YouTube embed.
 * If `editable` is true, shows an input for the URL and a Save callback.
 */
export function showVideoModal({ url, title, editable = false, onSave = null }) {
  const overlay = document.createElement('div');
  overlay.className = 'modal-overlay modal-centered';
  overlay.style.zIndex = '200';

  const videoId = extractYoutubeId(url);
  const iframeHtml = videoId
    ? `<div class="video-wrap">
        <iframe src="https://www.youtube.com/embed/${videoId}" frameborder="0"
          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
          allowfullscreen></iframe>
      </div>`
    : `<div class="video-placeholder">${t('NoVideoForExercise') || 'No hay vídeo configurado'}</div>`;

  const editBlock = editable
    ? `<div style="margin-top:12px;">
        <label style="font-size:12px;color:var(--text-light);display:block;margin-bottom:4px;">${t('YouTubeUrlLabel') || 'URL de YouTube'}</label>
        <input id="video-url-input" type="url" class="form-input" placeholder="https://youtu.be/..."
          value="${(url || '').replace(/"/g, '&quot;')}" style="width:100%;">
        <div style="display:flex;gap:8px;margin-top:10px;">
          <button class="btn btn-outline btn-block" id="video-cancel">${t('Cancel') || 'Cancelar'}</button>
          <button class="btn btn-primary btn-block" id="video-save">${t('Save') || 'Guardar'}</button>
        </div>
      </div>`
    : `<button class="btn btn-primary btn-block" id="video-close" style="margin-top:12px;">${t('Close') || 'Cerrar'}</button>`;

  overlay.innerHTML = `
    <div class="modal-content" style="max-width:500px;width:90%;">
      ${title ? `<div style="font-weight:700;font-size:16px;margin-bottom:10px;text-align:center;">${escapeHtml(title)}</div>` : ''}
      ${iframeHtml}
      ${editBlock}
    </div>
  `;

  document.body.appendChild(overlay);
  const close = () => overlay.remove();
  overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
  overlay.querySelector('#video-close')?.addEventListener('click', close);
  overlay.querySelector('#video-cancel')?.addEventListener('click', close);
  overlay.querySelector('#video-save')?.addEventListener('click', () => {
    const input = overlay.querySelector('#video-url-input');
    const newUrl = input?.value.trim() || '';
    close();
    if (onSave) onSave(newUrl);
  });
}

function showStreakModal(streak, milestone) {
  const overlay = createModalOverlay();
  overlay.innerHTML = `
    <div class="modal-content" style="max-width:340px;text-align:center;">
      <div style="font-size:60px;margin-bottom:8px;">&#128293;</div>
      <div style="font-size:22px;font-weight:700;color:#e67e22;margin-bottom:4px;">${milestone} ${t('StreakDays') || 'días seguidos'}</div>
      <div style="font-size:14px;color:#666;margin-bottom:16px;">${t('StreakCelebration') || '¡Sigue así! Llevas un buen ritmo.'}</div>
      <button class="btn btn-primary btn-block" id="streak-modal-ok">${t('LetsGo') || '¡A por más!'}</button>
    </div>
  `;
  document.body.appendChild(overlay);
  haptic('pr');
  confetti(2000);
  overlay.querySelector('#streak-modal-ok').addEventListener('click', () => overlay.remove());
  setTimeout(() => overlay.remove(), 10000);
}
