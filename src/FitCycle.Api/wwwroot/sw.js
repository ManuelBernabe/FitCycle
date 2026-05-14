const CACHE = 'fitcycle-v79';
const API_CACHE = 'fitcycle-api-v1';
const SHELL = ['/', '/css/app.css', '/js/app.js', '/js/api.js', '/js/auth.js', '/js/l10n.js', '/js/exercises.js', '/js/utils.js',
  '/js/qrcode.min.js', '/js/offline.js',
  '/js/pages/login.js', '/js/pages/home.js', '/js/pages/routines.js', '/js/pages/editday.js', '/js/pages/workout.js',
  '/js/pages/summary.js', '/js/pages/stats.js', '/js/pages/account.js', '/js/pages/measurements.js',
  '/js/pages/templates.js', '/js/pages/admin.js', '/js/pages/tutorial.js', '/js/pages/ai.js'];

// GET API paths to cache for offline reading
const CACHEABLE_API = ['/routines', '/musclegroups', '/exercises', '/workouts', '/measurements'];

function isApiCall(url) {
  return url.includes('/auth/') || url.includes('/routines') ||
    url.includes('/workouts') || url.includes('/exercises') ||
    url.includes('/musclegroups') || url.includes('/users') ||
    url.includes('/measurements') || url.includes('/admin/') ||
    url.includes('/templates') || url.includes('/me/2fa');
}

function isCacheableApi(request) {
  if (request.method !== 'GET') return false;
  const url = new URL(request.url);
  return CACHEABLE_API.some(p => url.pathname === p || url.pathname.startsWith(p + '/'));
}

self.addEventListener('install', e => {
  self.skipWaiting();
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(SHELL)));
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(k => k !== CACHE && k !== API_CACHE).map(k => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', e => {
  if (isApiCall(e.request.url)) {
    if (isCacheableApi(e.request)) {
      // Cacheable GET API: network-first, cache response, serve cache on failure
      e.respondWith(
        fetch(e.request).then(res => {
          if (res.ok) {
            const clone = res.clone();
            caches.open(API_CACHE).then(c => c.put(e.request, clone));
          }
          return res;
        }).catch(() =>
          caches.open(API_CACHE).then(c => c.match(e.request)).then(r =>
            r || new Response('{"error":"offline"}', { status: 503, headers: { 'Content-Type': 'application/json' } })
          )
        )
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
    // App shell: network-first with cache fallback
    e.respondWith(
      fetch(e.request).then(res => {
        const clone = res.clone();
        caches.open(CACHE).then(c => c.put(e.request, clone));
        return res;
      }).catch(() => caches.match(e.request).then(r => r || new Response('Not found', { status: 503 })))
    );
  }
});
