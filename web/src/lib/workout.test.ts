import { describe, expect, it } from "vitest";
import type { WorkoutSet } from "../api/types";
import { describeSet, describeTarget, dropSetWeight, elapsedSeconds, formatClock, localDayKey, monthGrid, prefillSet, stepValue } from "./workout";

const set = (over: Partial<WorkoutSet>): WorkoutSet => ({
  id: 1,
  setNumber: 1,
  type: "Normal",
  reps: 8,
  weightKg: 60,
  rpe: null,
  durationSeconds: null,
  distanceMeters: null,
  completedAtUtc: "2026-11-21T18:00:00Z",
  ...over,
});

describe("dropSetWeight", () => {
  it.each([
    [100, 80],
    [60, 47.5], // 48 rounds to the nearest 2.5
    [22.5, 17.5], // 18 → 17.5
    [2, 2.5], // 1.6 → 2.5 (nearest step)
    [0, 0],
  ])("%s kg drops to %s kg", (from, to) => {
    expect(dropSetWeight(from)).toBe(to);
  });
});

describe("elapsedSeconds", () => {
  const start = "2026-11-21T18:00:00Z";
  const now = Date.parse("2026-11-21T18:10:00Z");

  it("counts from the start without earlier pauses", () => {
    expect(elapsedSeconds({ startedAtUtc: start, finishedAtUtc: null, pausedAtUtc: null, pausedSeconds: 120 }, now)).toBe(480);
  });

  it("stops while paused", () => {
    const paused = { startedAtUtc: start, finishedAtUtc: null, pausedAtUtc: "2026-11-21T18:05:00Z", pausedSeconds: 0 };
    expect(elapsedSeconds(paused, now)).toBe(300);
  });

  it("is zero before the start", () => {
    expect(elapsedSeconds({ startedAtUtc: null, finishedAtUtc: null, pausedAtUtc: null, pausedSeconds: 0 }, now)).toBe(0);
  });
});

describe("formatting", () => {
  it("formats a clock", () => {
    expect(formatClock(75)).toBe("1:15");
    expect(formatClock(3723)).toBe("1:02:03");
  });

  it("describes sets", () => {
    expect(describeSet(set({ weightKg: 62.5, reps: 8 }))).toBe("62.5 kg × 8");
    expect(describeSet(set({ weightKg: null, reps: 12 }))).toBe("× 12");
    expect(describeSet(set({ weightKg: null, reps: 0, durationSeconds: 600, distanceMeters: 1500 }))).toBe("10:00 · 1.5 km");
  });

  it("describes a plan's target", () => {
    expect(describeTarget({ targetSets: 3, targetRepsMin: 10, targetRepsMax: 12, targetWeightKg: 20 })).toBe("3 × 10–12 @ 20 kg");
    expect(describeTarget({ targetSets: null, targetRepsMin: null, targetRepsMax: null, targetWeightKg: null })).toBeNull();
  });
});

describe("prefillSet", () => {
  it("copies the previous set", () => {
    expect(prefillSet({ sets: [set({ weightKg: 60, reps: 8 })], targetRepsMin: 10, targetWeightKg: 50 })).toMatchObject({ weight: "60", reps: "8" });
  });

  it("after a drop set, starts again from the set it came from", () => {
    const sets = [set({ weightKg: 60, reps: 8 }), set({ setNumber: 2, type: "Drop", weightKg: 47.5, reps: 10 })];
    expect(prefillSet({ sets, targetRepsMin: null, targetWeightKg: null })).toMatchObject({ weight: "60", reps: "8" });
  });

  it("without sets uses the plan's target, the bottom of the range", () => {
    expect(prefillSet({ sets: [], targetRepsMin: 10, targetWeightKg: 20 })).toMatchObject({ weight: "20", reps: "10" });
  });

  it("with nothing to go on suggests 10 reps and no weight", () => {
    expect(prefillSet({ sets: [], targetRepsMin: null, targetWeightKg: null })).toMatchObject({ weight: "", reps: "10" });
  });
});

describe("stepValue", () => {
  it("steps within the limits and reads a decimal comma", () => {
    expect(stepValue("60", 2.5, 1000)).toBe("62.5");
    expect(stepValue("1", -2.5, 1000)).toBe("0");
    expect(stepValue("", 1, 200)).toBe("1");
    expect(stepValue("22,5", 2.5, 1000)).toBe("25");
    expect(stepValue("200", 1, 200)).toBe("200");
  });
});

describe("calendar", () => {
  it("uses the local day", () => {
    expect(localDayKey(new Date(2026, 5, 24, 23, 30))).toBe("2026-06-24");
  });

  it("lays out a month Monday first", () => {
    // June 2026 starts on a Monday and has 30 days: 5 weeks, the first cell is the 1st.
    const weeks = monthGrid(2026, 5);
    expect(weeks).toHaveLength(5);
    expect(weeks[0]![0]!.getDate()).toBe(1);
    // November 2026 starts on a Sunday: six empty cells before it.
    expect(monthGrid(2026, 10)[0]!.slice(0, 6)).toEqual([null, null, null, null, null, null]);
  });
});
