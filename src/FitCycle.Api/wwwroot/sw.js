const CACHE = 'fitcycle-v13';
const SHELL = ['/', '/css/app.css', '/js/app.js', '/js/api.js', '/js/auth.js', '/js/l10n.js', '/js/exercises.js',
  '/js/pages/login.js', '/js/pages/routines.js', '/js/pages/editday.js', '/js/pages/workout.js',
  '/js/pages/summary.js', '/js/pages/stats.js', '/js/pages/account.js', '/js/pages/measurements.js'];

self.addEventListener('install', e => {
  self.skipWaiting();
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(SHELL)));
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))
    ).then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', e => {
  // API calls: network-first
  if (e.request.url.includes('/auth/') || e.request.url.includes('/routines') ||
      e.request.url.includes('/workouts') || e.request.url.includes('/exercises') ||
      e.request.url.includes('/musclegroups') || e.request.url.includes('/users') ||
      e.request.url.includes('/measurements')) {
    e.respondWith(fetch(e.request).catch(() => caches.match(e.request)));
  } else {
    // App shell: network-first with cache fallback (ensures updates are picked up)
    e.respondWith(
      fetch(e.request).then(res => {
        const clone = res.clone();
        caches.open(CACHE).then(c => c.put(e.request, clone));
        return res;
      }).catch(() => caches.match(e.request))
    );
  }
});
