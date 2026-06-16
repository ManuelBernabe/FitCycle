// FitCycle PWA — Main entry point (SPA router + app shell)

import { t, init as l10nInit, currentLanguage, setLanguage, availableLanguages, languageDisplayName } from './l10n.js';
import { auth } from './auth.js';
import { applyTheme, applyFontSize, fontSizeUp, fontSizeDown, getFontSize, envTag } from './utils.js';
import { offline } from './offline.js';
import { APP_VERSION } from './version.js';

// Page modules (lazy-ish imports — all bundled but only rendered on demand)
import * as loginPage from './pages/login.js';
import * as routinesPage from './pages/routines.js';
import * as editdayPage from './pages/editday.js';
import * as workoutPage from './pages/workout.js';
import * as summaryPage from './pages/summary.js';
import * as statsPage from './pages/stats.js';
import * as accountPage from './pages/account.js';
import * as measurementsPage from './pages/measurements.js';
import * as templatesPage from './pages/templates.js';
import * as adminPage from './pages/admin.js';
import * as homePage from './pages/home.js';
import * as tutorialPage from './pages/tutorial.js';
import * as aiPage from './pages/ai.js';
import * as calendarPage from './pages/calendar.js';
import * as onboardingPage from './pages/onboarding.js';
import { isOnboardingDone } from './pages/onboarding.js';

// ─── Init ───────────────────────────────────────────────────────────
l10nInit();
applyTheme();
applyFontSize();

const appEl = document.getElementById('app');
document.title = 'FitCycle' + envTag();

// Route definitions: hash -> { page module, showHeader, showTabs }
const routes = {
  home:     { mod: homePage,     header: false, tabs: false },
  login:    { mod: loginPage,    header: false, tabs: false },
  routines: { mod: routinesPage, header: true,  tabs: true },
  stats:    { mod: statsPage,    header: true,  tabs: true },
  editday:  { mod: editdayPage,  header: true,  tabs: false },
  workout:  { mod: workoutPage,  header: true,  tabs: false },
  summary:  { mod: summaryPage,  header: true,  tabs: false },
  account:      { mod: accountPage,      header: true,  tabs: false },
  measurements: { mod: measurementsPage, header: true,  tabs: true },
  templates:    { mod: templatesPage,    header: true,  tabs: true },
  admin:        { mod: adminPage,       header: true,  tabs: false },
  tutorial:     { mod: tutorialPage,   header: true,  tabs: false },
  ai:           { mod: aiPage,        header: true,  tabs: true },
  calendar:     { mod: calendarPage,  header: true,  tabs: true },
  onboarding:   { mod: onboardingPage, header: false, tabs: false },
};

// ─── Router ─────────────────────────────────────────────────────────
function parseHash() {
  const raw = location.hash.replace(/^#\/?/, '') || '';
  const parts = raw.split('/');
  return { name: parts[0] || '', params: parts.slice(1).join('/') };
}

function navigate() {
  let { name, params } = parseHash();

  // Auth guard — allow offline navigation if user has a previous session
  const authenticated = auth.isAuthenticated();
  const hasOfflineSession = !authenticated && !offline.isOnline() && auth.hasSession();

  if (!authenticated && !hasOfflineSession && name !== 'login') {
    location.hash = '#login';
    return;
  }
  if (authenticated && name === 'login') {
    // First-time login: send to onboarding wizard
    location.hash = isOnboardingDone() ? '#home' : '#onboarding';
    return;
  }

  // Default route
  if (!name || !routes[name]) {
    location.hash = auth.isAuthenticated() ? '#home' : '#login';
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
        <div class="header-logo" id="header-logo" style="cursor:pointer;" role="button" tabindex="0" aria-label="Inicio">FC</div>
        <div class="header-title">FitCycle${envTag()} <span class="header-version" id="header-version" role="button" tabindex="0" title="Versión de la app — toca para buscar actualización">v${APP_VERSION}</span></div>
        <select class="lang-picker" id="header-lang">${langOptions}</select>
        <a href="https://eathealthycycle-production.up.railway.app/portal.html" target="_blank" style="background:rgba(255,255,255,0.2);color:white;border:none;border-radius:8px;padding:6px 12px;font-size:13px;font-weight:600;text-decoration:none;display:flex;align-items:center;">&#127968; Apps</a>
        <div class="avatar" id="header-avatar" role="button" tabindex="0" aria-label="Cuenta">${initial}</div>
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
    const isTmpl = routeName === 'templates';
    const isAI = routeName === 'ai';

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
        <button class="tab ${isAI ? 'active' : ''}" data-tab="ai">
          <span class="tab-icon">&#129302;</span>
          <span>${t('TabAI')}</span>
        </button>
        ${auth.isAdmin() ? `
        <button class="tab ${isTmpl ? 'active' : ''}" data-tab="templates">
          <span class="tab-icon">&#128218;</span>
          <span>${t('TabTemplates')}</span>
        </button>
        ` : ''}
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

    // Logo click -> home
    document.getElementById('header-logo')?.addEventListener('click', () => {
      location.hash = '#home';
    });

    // Version pill click -> force check for an updated service worker, then hard reload.
    // Lets the user pull the latest version on demand when iOS keeps the PWA on a stale
    // shell. Shows a tiny toast so they know something happened.
    document.getElementById('header-version')?.addEventListener('click', async () => {
      const pill = document.getElementById('header-version');
      const original = pill?.textContent;
      if (pill) pill.textContent = 'Buscando...';
      try {
        if ('serviceWorker' in navigator) {
          const reg = await navigator.serviceWorker.getRegistration();
          if (reg) await reg.update();
        }
      } catch { /* ignore */ }
      // Best-effort: clear the SW caches so the next fetch goes to the network.
      try {
        if ('caches' in window) {
          const keys = await caches.keys();
          await Promise.all(keys.filter(k => k.startsWith('fitcycle-')).map(k => caches.delete(k)));
        }
      } catch { /* ignore */ }
      // sessionStorage flag prevents the activate handler's auto-reload from looping
      // with our manual one. Clear it so the next sw-activated message reloads cleanly.
      sessionStorage.removeItem('fc_sw_reloaded');
      if (pill) pill.textContent = original || ('v' + APP_VERSION);
      setTimeout(() => location.reload(), 200);
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

// ─── Floating zoom control ──────────────────────────────────────────
(function initZoomFab() {
  // Render outside #app so zoom doesn't affect the FAB itself
  const fab = document.createElement('div');
  fab.id = 'zoom-fab';
  fab.innerHTML = `
    <button id="zoom-fab-toggle" class="zoom-fab-btn" aria-label="Cambiar tamaño de letra" aria-expanded="false">Aa</button>
    <div id="zoom-fab-panel" class="zoom-fab-panel" style="display:none;" role="group" aria-label="Tamaño de letra">
      <button id="zoom-fab-down" class="zoom-fab-ctrl" aria-label="Reducir tamaño">A-</button>
      <span id="zoom-fab-label" class="zoom-fab-label" aria-live="polite">${getFontSize()}%</span>
      <button id="zoom-fab-up" class="zoom-fab-ctrl" aria-label="Aumentar tamaño">A+</button>
    </div>
  `;
  document.body.appendChild(fab);

  document.getElementById('zoom-fab-toggle').addEventListener('click', () => {
    const panel = document.getElementById('zoom-fab-panel');
    panel.style.display = panel.style.display === 'none' ? 'flex' : 'none';
  });
  document.getElementById('zoom-fab-up').addEventListener('click', () => {
    fontSizeUp();
    document.getElementById('zoom-fab-label').textContent = `${getFontSize()}%`;
  });
  document.getElementById('zoom-fab-down').addEventListener('click', () => {
    fontSizeDown();
    document.getElementById('zoom-fab-label').textContent = `${getFontSize()}%`;
  });
  // Close panel on outside click
  document.addEventListener('click', (e) => {
    if (!e.target.closest('#zoom-fab')) {
      const panel = document.getElementById('zoom-fab-panel');
      if (panel) panel.style.display = 'none';
    }
  });
})();

// ─── Offline support ─────────────────────────────────────────────────
offline.initListeners();

// Re-render current page when data syncs successfully
window.addEventListener('fc-synced', () => {
  offline.showSyncToast(t('SyncComplete'));
  navigate();
});

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
