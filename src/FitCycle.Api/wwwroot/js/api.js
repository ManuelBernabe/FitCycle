// FitCycle API client — handles HTTP requests with JWT auth, token refresh, and offline support

import { auth } from './auth.js';
import { offline } from './offline.js';

const BASE = ''; // same origin

async function request(method, path, body, isRetry = false) {
  const headers = {};
  const token = auth.getAccessToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const opts = { method, headers };

  if (body !== undefined && body !== null) {
    headers['Content-Type'] = 'application/json';
    opts.body = JSON.stringify(body);
  }

  try {
    const res = await fetch(`${BASE}${path}`, opts);

    // On 401 — attempt token refresh (once)
    if (res.status === 401 && !isRetry) {
      const refreshed = await tryRefresh();
      if (refreshed) {
        return request(method, path, body, true);
      }
      // If offline, don't clear auth — let offline cache serve data
      if (!navigator.onLine && method === 'GET') {
        throw new TypeError('Failed to fetch');
      }
      auth.clear();
      location.hash = '#login';
      throw new Error('Unauthorized');
    }

    if (!res.ok) {
      // SW returns 503 with {"error":"offline"} when network is down —
      // try localStorage cache before throwing
      if (method === 'GET' && res.status === 503) {
        const cached = offline.getCached(path);
        if (cached !== null) return cached;
      }

      let errorData;
      try {
        errorData = await res.json();
      } catch (e) {
        errorData = { error: res.statusText };
      }
      const err = new Error(errorData.error || errorData.message || `HTTP ${res.status}`);
      err.status = res.status;
      err.data = errorData;
      throw err;
    }

    // 204 No Content or empty body
    const text = await res.text();
    const data = text ? JSON.parse(text) : null;

    // Cache successful GET responses for offline use
    if (method === 'GET' && offline.isCacheableGet(path)) {
      offline.setCache(path, data);
    }

    return data;
  } catch (err) {
    // Network error (offline) — serve from cache for GETs
    if (method === 'GET' && isNetworkError(err)) {
      const cached = offline.getCached(path);
      if (cached !== null) {
        return cached;
      }
    }
    throw err;
  }
}

function isNetworkError(err) {
  return err instanceof TypeError && err.message.includes('fetch');
}

async function tryRefresh() {
  const refreshToken = auth.getRefreshToken();
  if (!refreshToken) return false;

  try {
    const res = await fetch(`${BASE}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });

    if (!res.ok) return false;

    const data = await res.json();
    auth.store(data);
    return true;
  } catch (e) {
    return false;
  }
}

async function requestForm(path, formData, isRetry = false) {
  const headers = {};
  const token = auth.getAccessToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(`${BASE}${path}`, { method: 'POST', headers, body: formData });

  if (res.status === 401 && !isRetry) {
    const refreshed = await tryRefresh();
    if (refreshed) return requestForm(path, formData, true);
    auth.clear();
    location.hash = '#login';
    throw new Error('Unauthorized');
  }

  if (!res.ok) {
    let errorData;
    try { errorData = await res.json(); } catch (e) { errorData = { error: res.statusText }; }
    const err = new Error(errorData.error || errorData.message || `HTTP ${res.status}`);
    err.status = res.status;
    throw err;
  }

  const text = await res.text();
  if (!text) return null;
  return JSON.parse(text);
}

async function downloadBlob(path, filename) {
  const headers = {};
  const token = auth.getAccessToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const res = await fetch(`${BASE}${path}`, { headers });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);

  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

/**
 * Drops cached GET responses that match a substring. The service worker uses
 * stale-while-revalidate on `/routines/*` and `/exercises/*` — after the user
 * uploads a new image or saves a workout, the cached response is out of date
 * and would otherwise be served on the next navigation until the bg refresh
 * lands. Call this with e.g. `/routines` to force a fresh fetch next time.
 */
async function invalidateCache(pathSubstring) {
  if (!('caches' in window)) return;
  try {
    const cacheNames = await caches.keys();
    await Promise.all(cacheNames.map(async (name) => {
      if (!name.startsWith('fitcycle-api')) return;
      const cache = await caches.open(name);
      const keys = await cache.keys();
      await Promise.all(
        keys
          .filter(req => req.url.includes(pathSubstring))
          .map(req => cache.delete(req))
      );
    }));
  } catch { /* ignore — cache invalidation is best-effort */ }
}

const api = {
  get(path)         { return request('GET',    path); },
  post(path, body)  { return request('POST',   path, body); },
  put(path, body)   { return request('PUT',    path, body); },
  del(path)         { return request('DELETE', path); },
  postForm(path, formData) { return requestForm(path, formData); },
  downloadBlob(path, filename) { return downloadBlob(path, filename); },
  invalidateCache(pathSubstring) { return invalidateCache(pathSubstring); },
};

export { api };
