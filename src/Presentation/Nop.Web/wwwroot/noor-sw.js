/* Noor PWA service worker.
   Purpose: make the site installable (Add to Home Screen / standalone app).
   No offline caching by design — every request goes straight to the network,
   so users never see stale store content. The empty fetch handler is enough
   to satisfy the browser's installability requirement. */

self.addEventListener('install', function () {
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(self.clients.claim());
});

// Network passthrough: a registered fetch handler is required for installability.
self.addEventListener('fetch', function () {
    // Intentionally not calling respondWith() — the browser handles the request normally.
});
