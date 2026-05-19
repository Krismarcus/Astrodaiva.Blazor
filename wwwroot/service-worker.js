const CACHE_NAME = "astrodaiva-shell-v20260519-mobile-overlay-v1";

const APP_SHELL = [
  "./",
  "./index.html",
  "./css/app.css?v=20260519-mobile-overlay-v1",
  "./manifest.webmanifest",
  "./img/astrodaiva-logo.png",
  "./img/pwa/apple-touch-icon.png",
  "./img/pwa/favicon-16.png",
  "./img/pwa/favicon-32.png",
  "./img/pwa/icon-192.png",
  "./img/pwa/icon-512.png"
];

function shouldBypassCache(request) {
  if (request.method !== "GET") {
    return true;
  }

  const url = new URL(request.url);

  if (url.origin !== self.location.origin) {
    return true;
  }

  return url.pathname.endsWith("/astrodb.json")
    || url.pathname.includes("/data/")
    || url.pathname.includes("/api/");
}

self.addEventListener("install", event => {
  self.skipWaiting();

  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(APP_SHELL))
      .catch(() => undefined)
  );
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key !== CACHE_NAME)
          .map(key => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", event => {
  const { request } = event;

  if (shouldBypassCache(request)) {
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(
      fetch(request)
        .then(response => {
          if (!response.ok) {
            return caches.match("./index.html").then(cached => cached || response);
          }

          const copy = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put("./index.html", copy));
          return response;
        })
        .catch(() => caches.match("./index.html"))
    );
    return;
  }

  event.respondWith(
    fetch(request)
      .then(response => {
        if (response && response.ok) {
          const copy = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
        }

        return response;
      })
      .catch(() => caches.match(request))
  );
});
