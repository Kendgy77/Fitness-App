import type { ExerciseCategory, Workout, WorkoutExercise, WorkoutSet } from "../api/types";
import { formatDecimal, formatReps } from "./labels";

// Small pure helpers for the workout screens. No React and no API, so each one has a plain unit test.

/** The weight step of the ± buttons: the smallest jump with standard plates (2 × 1.25 kg). */
export const WEIGHT_STEP = 2.5;

/** A drop set starts 20% lighter, rounded to the nearest 2.5 kg, never below zero. */
export function dropSetWeight(previousKg: number) {
  return Math.max(0, Math.round((previousKg * 0.8) / WEIGHT_STEP) * WEIGHT_STEP);
}

/** Seconds of actual training: from the start until now (or the finish), without the pauses. */
export function elapsedSeconds(workout: Pick<Workout, "startedAtUtc" | "finishedAtUtc" | "pausedAtUtc" | "pausedSeconds">, now: number) {
  if (!workout.startedAtUtc) return 0;
  const end = workout.finishedAtUtc ? Date.parse(workout.finishedAtUtc) : now;
  const pausedNow = workout.pausedAtUtc ? Math.max(0, now - Date.parse(workout.pausedAtUtc)) / 1000 : 0;
  return Math.max(0, Math.floor((end - Date.parse(workout.startedAtUtc)) / 1000 - workout.pausedSeconds - pausedNow));
}

/** 75 → "1:15", 3723 → "1:02:03". */
export function formatClock(totalSeconds: number) {
  const s = Math.max(0, Math.floor(totalSeconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = String(s % 60).padStart(2, "0");
  return h > 0 ? `${h}:${String(m).padStart(2, "0")}:${sec}` : `${m}:${sec}`;
}

/** "60 kg × 8", "× 12" without weight, "10:00 · 1.5 km" for cardio. */
export function describeSet(set: Pick<WorkoutSet, "reps" | "weightKg" | "durationSeconds" | "distanceMeters">) {
  if (set.durationSeconds !== null || set.distanceMeters !== null) {
    const parts = [];
    if (set.durationSeconds !== null) parts.push(formatClock(set.durationSeconds));
    if (set.distanceMeters !== null) parts.push(`${formatDecimal(set.distanceMeters / 1000)} km`);
    if (set.reps > 0) parts.push(`× ${set.reps}`);
    return parts.join(" · ");
  }
  return set.weightKg !== null ? `${formatDecimal(set.weightKg)} kg × ${set.reps}` : `× ${set.reps}`;
}

/** "3 × 10–12 @ 20 kg" from the plan, or null for an exercise added during the workout. */
export function describeTarget(entry: Pick<WorkoutExercise, "targetSets" | "targetRepsMin" | "targetRepsMax" | "targetWeightKg">) {
  if (entry.targetSets === null || entry.targetRepsMin === null || entry.targetRepsMax === null) return null;
  const weight = entry.targetWeightKg !== null ? ` @ ${formatDecimal(entry.targetWeightKg)} kg` : "";
  return `${entry.targetSets} × ${formatReps(entry.targetRepsMin, entry.targetRepsMax)}${weight}`;
}

export function isCardio(category: ExerciseCategory) {
  return category === "Cardio";
}

export interface SetDraft {
  weight: string;
  reps: string;
  minutes: string;
  km: string;
}

/**
 * What the "Log set" boxes start with: the previous set of this exercise (so the usual case is one tap), else
 * the plan's target (the bottom of the rep range), else 10 reps with no weight. After a drop set, the next set
 * starts from the set the drop came from, not from the lighter weight.
 */
export function prefillSet(entry: Pick<WorkoutExercise, "sets" | "targetRepsMin" | "targetWeightKg">): SetDraft {
  const previous = [...entry.sets].reverse().find((s) => s.type !== "Drop") ?? entry.sets.at(-1);
  if (previous) {
    return {
      weight: previous.weightKg !== null ? String(previous.weightKg) : "",
      reps: previous.reps > 0 ? String(previous.reps) : "",
      minutes: previous.durationSeconds !== null ? String(Math.round(previous.durationSeconds / 60)) : "",
      km: previous.distanceMeters !== null ? String(previous.distanceMeters / 1000) : "",
    };
  }
  return {
    weight: entry.targetWeightKg !== null ? String(entry.targetWeightKg) : "",
    reps: String(entry.targetRepsMin ?? 10),
    minutes: "",
    km: "",
  };
}

/** Adds `step` to a number typed as text ("" counts as 0), keeping it within [0, max]. */
export function stepValue(value: string, step: number, max: number) {
  const current = Number(value.replace(",", ".")) || 0;
  const next = Math.min(max, Math.max(0, Math.round((current + step) * 100) / 100));
  return String(next);
}

/** "2026-06-24" for a moment, in the browser's time zone: the calendar day a workout belongs to. */
export function localDayKey(date: Date | string) {
  const d = typeof date === "string" ? new Date(date) : date;
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

/** The value a <input type="datetime-local"> expects: "2026-06-24T18:00" in local time. */
export function toLocalInputValue(date: Date) {
  return `${localDayKey(date)}T${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

/** The weeks of a month for a calendar, Monday first; days outside the month are null. `month` is 0–11. */
export function monthGrid(year: number, month: number): (Date | null)[][] {
  const first = new Date(year, month, 1);
  const days = new Date(year, month + 1, 0).getDate();
  const leading = (first.getDay() + 6) % 7; // Sunday is 0 in JavaScript, Monday is 0 here
  const cells: (Date | null)[] = [...Array<null>(leading).fill(null)];
  for (let day = 1; day <= days; day++) cells.push(new Date(year, month, day));
  while (cells.length % 7 !== 0) cells.push(null);
  const weeks = [];
  for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
  return weeks;
}
