/**
 * Registers public/sw.js, which makes the app installable on a phone (D024).
 *
 * Only in the production build: during development Vite serves fresh files on every change, and a service worker
 * would only get in the way. Browsers also allow service workers only over HTTPS (or on localhost), so on a
 * plain http:// address this quietly does nothing.
 */
export function registerServiceWorker() {
  if (!import.meta.env.PROD || !("serviceWorker" in navigator)) return;

  window.addEventListener("load", () => {
    navigator.serviceWorker.register("/sw.js").catch((error: unknown) => {
      // The app works the same without it; it just can't be installed.
      console.warn("Service worker registration failed", error);
    });
  });
}
