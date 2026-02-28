const CACHE = 'fitcycle-v1';
const SHELL = ['/', '/css/app.css', '/js/app.js', '/js/api.js', '/js/auth.js', '/js/l10n.js', '/js/exercises.js',
  '/js/pages/login.js', '/js/pages/routines.js', '/js/pages/editday.js', '/js/pages/workout.js',
  '/js/pages/summary.js', '/js/pages/stats.js', '/js/pages/account.js'];

self.addEventListener('install', e => e.waitUntil(caches.open(CACHE).then(c => c.addAll(SHELL))));
self.addEventListener('fetch', e => {
  if (e.request.url.includes('/auth/') || e.request.url.includes('/routines') ||
      e.request.url.includes('/workouts') || e.request.url.includes('/exercises') ||
      e.request.url.includes('/musclegroups') || e.request.url.includes('/users')) {
    e.respondWith(fetch(e.request).catch(() => caches.match(e.request)));
  } else {
    e.respondWith(caches.match(e.request).then(r => r || fetch(e.request)));
  }
});
