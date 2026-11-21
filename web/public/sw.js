// Service worker: a small script the browser runs beside the app. It is what lets a phone install the app.
//
// Deliberately minimal (DECISIONS D024): everything comes from the network as usual, so a new version of the
// app is never hidden behind an old cached copy. The only thing kept offline is a small "You're offline" page,
// shown instead of the browser's error page when the app is opened with no connection.
// Week 6 adds push notifications to this file.

const CACHE = "fitness-logger-v1";
const OFFLINE_URL = "/offline.html";
const OFFLINE_ASSETS = [OFFLINE_URL, "/icons/icon-192.png"];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(OFFLINE_ASSETS)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  // Remove caches left by older versions of this file.
  event.waitUntil(
    caches
      .keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener("fetch", (event) => {
  // Only page loads get the offline fallback. API calls, scripts and images go straight to the network.
  if (event.request.mode !== "navigate") return;

  event.respondWith(fetch(event.request).catch(() => caches.match(OFFLINE_URL)));
});
