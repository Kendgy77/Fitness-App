import { afterEach, describe, expect, it, vi } from "vitest";
import { recordServerDate, resetServerClock, serverNow } from "./serverClock";

describe("serverClock", () => {
  afterEach(() => {
    resetServerClock();
    vi.useRealTimers();
  });

  it("follows the server when this device's clock runs 40 seconds ahead", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-11-21T18:00:40Z")); // the phone thinks it is 18:00:40

    recordServerDate("Sat, 21 Nov 2026 18:00:00 GMT"); // the server says 18:00:00(.something)

    expect(serverNow()).toBe(Date.parse("2026-11-21T18:00:00.500Z"));
    vi.advanceTimersByTime(6_000);
    expect(serverNow()).toBe(Date.parse("2026-11-21T18:00:06.500Z"));
  });

  it("ignores a missing or broken header", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-11-21T18:00:00Z"));

    recordServerDate(undefined);
    recordServerDate("not a date");

    expect(serverNow()).toBe(Date.parse("2026-11-21T18:00:00Z"));
  });
});
