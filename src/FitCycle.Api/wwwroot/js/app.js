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
  account:  { mod: accountPage,  header: true,  tabs: false },
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
