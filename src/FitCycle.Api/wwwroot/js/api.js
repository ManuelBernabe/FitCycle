// FitCycle API client — handles HTTP requests with JWT auth and token refresh

import { auth } from './auth.js';

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

  const res = await fetch(`${BASE}${path}`, opts);

  // On 401 — attempt token refresh (once)
  if (res.status === 401 && !isRetry) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      return request(method, path, body, true);
    }
    // refresh failed — clear auth and redirect to login
    auth.clear();
    location.hash = '#login';
    throw new Error('Unauthorized');
  }

  if (!res.ok) {
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
  if (!text) return null;
  return JSON.parse(text);
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

const api = {
  get(path)         { return request('GET',    path); },
  post(path, body)  { return request('POST',   path, body); },
  put(path, body)   { return request('PUT',    path, body); },
  del(path)         { return request('DELETE', path); },
};

export { api };
