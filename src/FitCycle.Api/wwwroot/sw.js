const CACHE = 'fitcycle-v99';
const API_CACHE = 'fitcycle-api-v1';
const IMG_CACHE = 'fitcycle-img-v1';
const IMG_CACHE_MAX = 100; // LRU cap for exercise images

const SHELL = ['/', '/css/app.css', '/js/app.js', '/js/api.js', '/js/auth.js', '/js/l10n.js', '/js/exercises.js', '/js/utils.js',
  '/js/qrcode.min.js', '/js/offline.js', '/js/version.js',
  '/js/pages/login.js', '/js/pages/home.js', '/js/pages/routines.js', '/js/pages/editday.js', '/js/pages/workout.js',
  '/js/pages/summary.js', '/js/pages/stats.js', '/js/pages/account.js', '/js/pages/measurements.js',
  '/js/pages/templates.js', '/js/pages/admin.js', '/js/pages/tutorial.js', '/js/pages/ai.js',
  '/js/pages/calendar.js', '/js/pages/onboarding.js'];

// GET API paths to cache for offline reading
const CACHEABLE_API = ['/routines', '/musclegroups', '/exercises', '/workouts', '/measurements', '/achievements'];

function isApiCall(url) {
  return url.includes('/auth/') || url.includes('/routines') ||
    url.includes('/workouts') || url.includes('/exercises') ||
    url.includes('/musclegroups') || url.includes('/users') ||
    url.includes('/measurements') || url.includes('/admin/') ||
    url.includes('/templates') || url.includes('/me/2fa') ||
    url.includes('/achievements') || url.includes('/ai/') || url.includes('/export/');
}

function isCacheableApi(request) {
  if (request.method !== 'GET') return false;
  const url = new URL(request.url);
  return CACHEABLE_API.some(p => url.pathname === p || url.pathname.startsWith(p + '/'));
}

function isImageRequest(request) {
  if (request.method !== 'GET') return false;
  const url = new URL(request.url);
  // wger.de exercise images, our own /uploads, or any image extension
  return url.hostname.includes('wger.de')
    || url.pathname.startsWith('/uploads/')
    || /\.(png|jpg|jpeg|webp|gif|svg)(\?.*)?$/i.test(url.pathname);
}

self.addEventListener('install', e => {
  self.skipWaiting();
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(SHELL)));
});

self.addEventListener('activate', e => {
  e.waitUntil((async () => {
    // Purge every cache that doesn't match the current versions — this is what removes
    // the old fitcycle-v90 / v91 / v92 / v93 entries when v94+ activates.
    const keys = await caches.keys();
    await Promise.all(keys.filter(k => k !== CACHE && k !== API_CACHE && k !== IMG_CACHE).map(k => caches.delete(k)));
    await self.clients.claim();
    // Notify every open client a new version activated. Use BOTH mechanisms:
    //   1. postMessage — picked up by the listener in index.html (v95+).
    //   2. client.navigate(url) — works even when the client is running an OLD index.html
    //      that doesn't have the message listener yet (the exact lock-in scenario where
    //      installed PWAs sit on a stale SW indefinitely).
    const clients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const client of clients) {
      try { client.postMessage({ type: 'sw-activated', cache: CACHE }); } catch { /* ignore */ }
      // navigate() forcibly reloads the document, even with no client-side listener.
      // Gate on focus state so we only reload visible windows (avoid kicking a workout in
      // the background mid-session). The next focus event will refresh it instead.
      try {
        if (client.focused && client.navigate) await client.navigate(client.url);
      } catch { /* ignore */ }
    }
  })());
});

/**
 * Trims a cache to a maximum number of entries (oldest first removed).
 */
async function trimCache(cacheName, maxItems) {
  try {
    const cache = await caches.open(cacheName);
    const keys = await cache.keys();
    if (keys.length <= maxItems) return;
    const toDelete = keys.length - maxItems;
    for (let i = 0; i < toDelete; i++) await cache.delete(keys[i]);
  } catch { /* ignore */ }
}

self.addEventListener('fetch', e => {
  // Images: cache-first with LRU eviction (long-lived assets)
  if (isImageRequest(e.request)) {
    e.respondWith(
      caches.open(IMG_CACHE).then(async cache => {
        const cached = await cache.match(e.request);
        if (cached) return cached;
        try {
          const res = await fetch(e.request);
          if (res.ok) {
            cache.put(e.request, res.clone());
            trimCache(IMG_CACHE, IMG_CACHE_MAX);
          }
          return res;
        } catch {
          return cached || new Response('', { status: 503 });
        }
      })
    );
    return;
  }

  if (isApiCall(e.request.url)) {
    if (isCacheableApi(e.request)) {
      // Stale-while-revalidate: return cache immediately, refresh in background.
      e.respondWith(
        caches.open(API_CACHE).then(async cache => {
          const cached = await cache.match(e.request);
          const networkFetch = fetch(e.request).then(res => {
            if (res.ok) cache.put(e.request, res.clone());
            return res;
          }).catch(() => null);

          if (cached) {
            networkFetch.then(() => { /* background refresh */ });
            return cached;
          }
          const fresh = await networkFetch;
          return fresh || new Response('{"error":"offline"}', { status: 503, headers: { 'Content-Type': 'application/json' } });
        })
      );
    } else {
      // Non-cacheable API (auth, POST, etc): network only
      e.respondWith(
        fetch(e.request).catch(() =>
          new Response('{"error":"offline"}', { status: 503, headers: { 'Content-Type': 'application/json' } })
        )
      );
    }
  } else {
    // App shell: cache-first for SHELL members, network-first otherwise.
    const url = new URL(e.request.url);
    const isShellMember = SHELL.includes(url.pathname) || url.pathname === '/';
    if (isShellMember) {
      e.respondWith(
        caches.match(e.request).then(cached => cached || fetch(e.request).then(res => {
          if (res.ok) caches.open(CACHE).then(c => c.put(e.request, res.clone()));
          return res;
        })).catch(() => new Response('Not found', { status: 503 }))
      );
    } else {
      e.respondWith(
        fetch(e.request).then(res => {
          if (res.ok) {
            const clone = res.clone();
            caches.open(CACHE).then(c => c.put(e.request, clone));
          }
          return res;
        }).catch(() => caches.match(e.request).then(r => r || new Response('Not found', { status: 503 })))
      );
    }
  }
});
