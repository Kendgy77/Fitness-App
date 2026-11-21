/**
 * The server's clock, as seen from this device.
 *
 * Workout times (started, paused, sets) are stamped by the server. A phone's own clock can be off by many
 * seconds, so "now − started" on the phone made a new workout look 40 seconds old. Every API response carries
 * the server's time in its `Date` header; the difference to this device's clock is kept here, and timers count
 * with `serverNow()` instead of `Date.now()`.
 */
let offsetMs = 0;

/**
 * Records the server's time from a response's `Date` header. The header has whole seconds only, so the real
 * server time is somewhere in the following second: half a second is added to land in the middle.
 */
export function recordServerDate(dateHeader: unknown, receivedAt = Date.now()) {
  if (typeof dateHeader !== "string") return;
  const serverMs = Date.parse(dateHeader);
  if (Number.isNaN(serverMs)) return;
  offsetMs = serverMs + 500 - receivedAt;
}

/** The current time on the server's clock, in milliseconds. */
export function serverNow() {
  return Date.now() + offsetMs;
}

/** For tests. */
export function resetServerClock() {
  offsetMs = 0;
}
