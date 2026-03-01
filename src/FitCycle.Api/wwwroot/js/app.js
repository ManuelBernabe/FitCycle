// FitCycle PWA — Main entry point (SPA router + app shell)

import { t, init as l10nInit, currentLanguage, setLanguage, availableLanguages, languageDisplayName } from './l10n.js';
import { auth } from './auth.js';

// Page modules (lazy-ish imports — all bundled but only rendered on demand)
import * as loginPage from './pages/login.js';
import * as routinesPage from './pages/routines.js';
import * as editdayPage from './pages/editday.js';
import * as workoutPage from './pages/workout.js';
import * as summaryPage from './pages/summary.js';
import * as statsPage from './pages/stats.js';
import * as accountPage from './pages/account.js';
import * as measurementsPage from './pages/measurements.js';

// ─── Init ───────────────────────────────────────────────────────────
l10nInit();

const appEl = document.getElementById('app');

// Route definitions: hash -> { page module, showHeader, showTabs }
const routes = {
  login:    { mod: loginPage,    header: false, tabs: false },
  routines: { mod: routinesPage, header: true,  tabs: true },
  stats:    { mod: statsPage,    header: true,  tabs: true },
  editday:  { mod: editdayPage,  header: true,  tabs: false },
  workout:  { mod: workoutPage,  header: true,  tabs: false },
  summary:  { mod: summaryPage,  header: true,  tabs: false },
  account:      { mod: accountPage,      header: true,  tabs: false },
  measurements: { mod: measurementsPage, header: true,  tabs: true },
};

// ─── Router ─────────────────────────────────────────────────────────
function parseHash() {
  const raw = location.hash.replace(/^#\/?/, '') || '';
  const parts = raw.split('/');
  return { name: parts[0] || '', params: parts.slice(1).join('/') };
}

function navigate() {
  let { name, params } = parseHash();

  // Auth guard
  if (!auth.isAuthenticated() && name !== 'login') {
    location.hash = '#login';
    return;
  }
  if (auth.isAuthenticated() && name === 'login') {
    location.hash = '#routines';
    return;
  }

  // Default route
  if (!name || !routes[name]) {
    location.hash = auth.isAuthenticated() ? '#routines' : '#login';
    return;
  }

  const route = routes[name];
  renderShell(route, name, params);
}

// ─── Render ─────────────────────────────────────────────────────────
function renderShell(route, routeName, params) {
  let html = '';

  // Header
  if (route.header) {
    const username = auth.getUsername() || '?';
    const initial = username.charAt(0).toUpperCase();
    const langOptions = availableLanguages
      .map(l => `<option value="${l}" ${l === currentLanguage() ? 'selected' : ''}>${languageDisplayName(l)}</option>`)
      .join('');

    html += `
      <div class="header">
        <div class="header-logo">FC</div>
        <div class="header-title">FitCycle</div>
        <select class="lang-picker" id="header-lang">${langOptions}</select>
        <div class="avatar" id="header-avatar">${initial}</div>
      </div>
    `;
  }

  // Page content
  html += `<div id="page-container">${route.mod.render(params)}</div>`;

  // Tab bar
  if (route.tabs) {
    const isRoutines = routeName === 'routines';
    const isStats = routeName === 'stats';
    const isMeas = routeName === 'measurements';

    html += `
      <div class="tab-bar">
        <button class="tab ${isRoutines ? 'active' : ''}" data-tab="routines">
          <span class="tab-icon">&#128197;</span>
          <span>${t('TabRoutines')}</span>
        </button>
        <button class="tab ${isStats ? 'active' : ''}" data-tab="stats">
          <span class="tab-icon">&#128200;</span>
          <span>${t('TabStats')}</span>
        </button>
        <button class="tab ${isMeas ? 'active' : ''}" data-tab="measurements">
          <span class="tab-icon">&#128207;</span>
          <span>${t('TabMeasurements')}</span>
        </button>
      </div>
    `;
  }

  appEl.innerHTML = html;

  // Bind header events
  if (route.header) {
    document.getElementById('header-lang')?.addEventListener('change', (e) => {
      setLanguage(e.target.value);
      renderShell(route, routeName, params);
      // Re-mount the page after re-render
      if (route.mod.mount) route.mod.mount(params);
    });

    document.getElementById('header-avatar')?.addEventListener('click', () => {
      location.hash = '#account';
    });
  }

  // Bind tab events
  if (route.tabs) {
    document.querySelectorAll('.tab-bar .tab').forEach(tab => {
      tab.addEventListener('click', () => {
        const target = tab.dataset.tab;
        if (target) location.hash = `#${target}`;
      });
    });
  }

  // Mount page (async lifecycle)
  if (route.mod.mount) {
    route.mod.mount(params);
  }
}

// ─── Event listeners ────────────────────────────────────────────────
window.addEventListener('hashchange', navigate);
window.addEventListener('app-rerender', navigate);

// Initial render
navigate();

// ─── Auto-update detection ──────────────────────────────────────────
let updatePending = false;

function showUpdatePopup() {
  if (document.getElementById('update-overlay')) return;
  updatePending = true;

  const overlay = document.createElement('div');
  overlay.id = 'update-overlay';
  overlay.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.5);z-index:999;display:flex;align-items:center;justify-content:center;animation:fadeIn 0.2s ease;';

  overlay.innerHTML = `
    <div style="background:#fff;border-radius:16px;padding:24px;max-width:320px;width:90%;text-align:center;box-shadow:0 4px 24px rgba(0,0,0,0.2);">
      <div style="font-size:36px;margin-bottom:8px;">&#128640;</div>
      <div style="font-size:18px;font-weight:700;margin-bottom:8px;">${t('AppUpdated')}</div>
      <div style="font-size:14px;color:#666;margin-bottom:16px;">${t('AppUpdatedMsg')}</div>
      <div style="display:flex;gap:10px;">
        <button id="update-later" style="flex:1;padding:10px;border:1px solid #ddd;border-radius:8px;background:#fff;color:#333;font-size:14px;cursor:pointer;">${t('Later')}</button>
        <button id="update-now" style="flex:1;padding:10px;border:none;border-radius:8px;background:#512BD4;color:#fff;font-size:14px;font-weight:600;cursor:pointer;">${t('UpdateNow')}</button>
      </div>
    </div>
  `;

  document.body.appendChild(overlay);

  document.getElementById('update-now')?.addEventListener('click', () => {
    window.location.reload();
  });

  document.getElementById('update-later')?.addEventListener('click', () => {
    overlay.remove();
    // Will reload when user finishes current action (on next navigation)
  });
}

// Listen for new service worker activation
if ('serviceWorker' in navigator) {
  navigator.serviceWorker.addEventListener('controllerchange', () => {
    showUpdatePopup();
  });

  // Also check on registration for waiting worker
  navigator.serviceWorker.getRegistration().then(reg => {
    if (!reg) return;

    // If there's already a waiting worker
    if (reg.waiting) {
      showUpdatePopup();
      return;
    }

    // Listen for new installing worker
    reg.addEventListener('updatefound', () => {
      const newWorker = reg.installing;
      if (!newWorker) return;
      newWorker.addEventListener('statechange', () => {
        if (newWorker.state === 'activated') {
          showUpdatePopup();
        }
      });
    });
  });
}

// Auto-reload on navigation if update is pending
window.addEventListener('hashchange', () => {
  if (updatePending && !document.getElementById('update-overlay')) {
    window.location.reload();
  }
});
