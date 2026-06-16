// FitCycle Offline Support — API response cache + write queue with auto-sync

const CACHE_PREFIX = 'fc_cache_';
const QUEUE_KEY = 'fc_sync_queue';
const CACHE_TTL = 24 * 60 * 60 * 1000; // 24 hours

// Cacheable GET paths (read-only data for offline)
const CACHEABLE = [
  '/routines',
  '/musclegroups',
  '/exercises',
  '/workouts',
  '/workouts/stats',
  '/measurements',
];

// --- Cache helpers ---

function cacheKey(path) {
  return CACHE_PREFIX + path;
}

function getCached(path) {
  try {
    const raw = localStorage.getItem(cacheKey(path));
    if (!raw) return null;
    const entry = JSON.parse(raw);
    if (Date.now() - entry.ts > CACHE_TTL) {
      localStorage.removeItem(cacheKey(path));
      return null;
    }
    return entry.data;
  } catch { return null; }
}

function setCache(path, data) {
  try {
    localStorage.setItem(cacheKey(path), JSON.stringify({ data, ts: Date.now() }));
  } catch { /* localStorage full — ignore */ }
}

function isCacheable(path) {
  return CACHEABLE.some(p => path === p || path.startsWith(p + '/'));
}

/**
 * Drops every localStorage entry whose key matches a substring of the cached path.
 * Used after writes that invalidate a cached read — e.g. PDF import rewrites every
 * day's routines, so call invalidateCache('/routines') so the next visit fetches
 * fresh data instead of serving the old 3x12 default from localStorage.
 */
function invalidateCache(pathSubstring) {
  try {
    const keysToRemove = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(CACHE_PREFIX) && key.includes(pathSubstring)) {
        keysToRemove.push(key);
      }
    }
    keysToRemove.forEach(k => localStorage.removeItem(k));
  } catch { /* ignore */ }
}

// Also cache /routines/{dayNum} dynamically
function isCacheableGet(path) {
  if (isCacheable(path)) return true;
  if (/^\/routines\/\d+$/.test(path)) return true;
  if (/^\/workouts\/exercise\/\d+\/progress$/.test(path)) return true;
  return false;
}

// --- Sync queue (for offline writes) ---

function getQueue() {
  try {
    return JSON.parse(localStorage.getItem(QUEUE_KEY) || '[]');
  } catch { return []; }
}

function saveQueue(queue) {
  localStorage.setItem(QUEUE_KEY, JSON.stringify(queue));
}

function enqueue(method, path, body) {
  const queue = getQueue();
  queue.push({ method, path, body, ts: Date.now(), id: Date.now() + '_' + Math.random().toString(36).slice(2, 6) });
  saveQueue(queue);
  updateSyncBadge();
  return queue.length;
}

function dequeue(id) {
  const queue = getQueue().filter(item => item.id !== id);
  saveQueue(queue);
  updateSyncBadge();
}

function pendingCount() {
  return getQueue().length;
}

// --- Sync engine ---

let syncing = false;

async function tryRefreshToken() {
  const refreshToken = localStorage.getItem('auth_refresh_token');
  if (!refreshToken) return false;
  try {
    const res = await fetch('/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) return false;
    const data = await res.json();
    if (data.accessToken) localStorage.setItem('auth_access_token', data.accessToken);
    if (data.refreshToken) localStorage.setItem('auth_refresh_token', data.refreshToken);
    return true;
  } catch { return false; }
}

async function syncAll(fetchFn) {
  if (syncing) return;
  const queue = getQueue();
  if (queue.length === 0) return;

  syncing = true;
  let synced = 0;
  let refreshed = false;

  for (const item of queue) {
    try {
      const headers = {};
      const token = localStorage.getItem('auth_access_token');
      if (token) headers['Authorization'] = `Bearer ${token}`;

      if (item.body !== undefined && item.body !== null) {
        headers['Content-Type'] = 'application/json';
      }

      const opts = { method: item.method, headers };
      if (item.body !== undefined && item.body !== null) {
        opts.body = JSON.stringify(item.body);
      }

      const res = await fetch(item.path, opts);
      if (res.ok || res.status === 409) {
        // 409 = duplicate, consider it synced
        dequeue(item.id);
        synced++;
      } else if (res.status === 401 && !refreshed) {
        // Token expired — try refresh once, then retry this item
        refreshed = true;
        const ok = await tryRefreshToken();
        if (ok) {
          // Retry this item with new token
          const newHeaders = { 'Authorization': `Bearer ${localStorage.getItem('auth_access_token')}` };
          if (item.body !== undefined && item.body !== null) newHeaders['Content-Type'] = 'application/json';
          const retryOpts = { method: item.method, headers: newHeaders };
          if (item.body !== undefined && item.body !== null) retryOpts.body = JSON.stringify(item.body);
          const retryRes = await fetch(item.path, retryOpts);
          if (retryRes.ok || retryRes.status === 409) { dequeue(item.id); synced++; }
          else break;
        } else {
          // Refresh failed — queue stays, user needs to re-login
          break;
        }
      } else {
        // Server error or second 401 — retry later
        break;
      }
    } catch {
      // Network still down — stop
      break;
    }
  }

  syncing = false;

  if (synced > 0) {
    // Invalidate relevant caches so next load gets fresh data
    invalidateAfterSync();
    updateSyncBadge();
    window.dispatchEvent(new CustomEvent('fc-synced', { detail: { synced } }));
  }

  return synced;
}

function invalidateAfterSync() {
  ['/workouts', '/workouts/stats', '/measurements'].forEach(p => {
    localStorage.removeItem(cacheKey(p));
  });
}

// --- Online/offline events ---

function isOnline() {
  return navigator.onLine;
}

let _syncFn = null;

function initListeners(fetchFn) {
  _syncFn = fetchFn;

  window.addEventListener('online', () => {
    updateOfflineBanner(false);
    syncAll(fetchFn);
  });

  window.addEventListener('offline', () => {
    updateOfflineBanner(true);
  });

  // Try sync on init if online
  if (isOnline()) {
    setTimeout(() => syncAll(fetchFn), 2000);
  } else {
    updateOfflineBanner(true);
  }
}

// --- UI helpers ---

function updateOfflineBanner(offline) {
  let banner = document.getElementById('fc-offline-banner');
  if (offline) {
    if (!banner) {
      banner = document.createElement('div');
      banner.id = 'fc-offline-banner';
      document.body.prepend(banner);
    }
    const count = pendingCount();
    const pendingText = count > 0 ? ` · ${count} pendiente${count > 1 ? 's' : ''}` : '';
    banner.innerHTML = `&#9888; Sin conexión${pendingText}`;
  } else if (banner) {
    banner.remove();
  }
}

function updateSyncBadge() {
  const count = pendingCount();
  // Update offline banner if visible
  const banner = document.getElementById('fc-offline-banner');
  if (banner && !isOnline()) {
    const pendingText = count > 0 ? ` · ${count} pendiente${count > 1 ? 's' : ''}` : '';
    banner.innerHTML = `&#9888; Sin conexión${pendingText}`;
  }

  // Dispatch event for any UI component that wants to show sync status
  window.dispatchEvent(new CustomEvent('fc-queue-update', { detail: { count } }));
}

function showSyncToast(message) {
  const toast = document.createElement('div');
  toast.className = 'toast toast-sync';
  toast.textContent = message;
  document.body.appendChild(toast);
  setTimeout(() => toast.remove(), 3000);
}

export const offline = {
  getCached,
  setCache,
  isCacheableGet,
  invalidateCache,
  enqueue,
  pendingCount,
  syncAll,
  isOnline,
  initListeners,
  showSyncToast,
  updateOfflineBanner,
};
