import type {
  BmiCategory,
  Difficulty,
  Equipment,
  ExerciseCategory,
  PlanStatus,
  SessionStatus,
  SessionType,
  SetType,
  Sex,
  TrainingGoal,
} from "../api/types";

// Display names for the API's enum values. Records (not switch statements) so TypeScript reports a missing
// label the moment a value is added to the API types.

export const sexLabels: Record<Sex, string> = {
  Female: "Female",
  Male: "Male",
  Other: "Other",
};

export const goalLabels: Record<TrainingGoal, string> = {
  LoseWeight: "Lose weight",
  BuildMuscle: "Build muscle",
  IncreaseStrength: "Get stronger",
  ImproveEndurance: "Improve endurance",
  StayHealthy: "Stay fit and healthy",
};

export const bmiLabels: Record<BmiCategory, string> = {
  Underweight: "Underweight",
  Normal: "Healthy weight",
  Overweight: "Overweight",
  Obese: "Obese",
};

export const categoryLabels: Record<ExerciseCategory, string> = {
  Chest: "Chest",
  Back: "Back",
  Shoulders: "Shoulders",
  Arms: "Arms",
  Legs: "Legs and glutes",
  Core: "Abs and core",
  Cardio: "Cardio",
};

export const difficultyLabels: Record<Difficulty, string> = {
  Beginner: "Beginner",
  Intermediate: "Intermediate",
  Advanced: "Advanced",
};

export const equipmentLabels: Record<Equipment, string> = {
  Barbell: "Barbell",
  Dumbbell: "Dumbbells",
  Kettlebell: "Kettlebell",
  Machine: "Machine",
  Cable: "Cable",
  Bodyweight: "Bodyweight",
  Other: "Other equipment",
};

export const planStatusLabels: Record<PlanStatus, string> = {
  Draft: "Draft",
  Active: "Active",
  Archived: "Archived",
};

/** Entries of a label record, for building <select> options in a stable order. */
export function options<T extends string>(labels: Record<T, string>) {
  return Object.entries(labels) as [T, string][];
}

const decimal = new Intl.NumberFormat("en-GB", { maximumFractionDigits: 1 });

/** 23.31 → "23.3". */
export function formatDecimal(value: number) {
  return decimal.format(value);
}

/** "8–10" for a range, "10" when both ends are equal. */
export function formatReps(min: number, max: number) {
  return min === max ? String(min) : `${min}–${max}`;
}

export const sessionTypeLabels: Record<SessionType, string> = {
  Strength: "Strength",
  Cardio: "Cardio",
  Mixed: "Mixed",
  Other: "Other",
};

export const sessionStatusLabels: Record<SessionStatus, string> = {
  Planned: "Planned",
  InProgress: "In progress",
  Paused: "Paused",
  Completed: "Finished",
};

export const setTypeLabels: Record<SetType, string> = {
  Normal: "Normal",
  Warmup: "Warm-up",
  Drop: "Drop set",
  Failure: "To failure",
};

const dateTime = new Intl.DateTimeFormat("en-GB", { weekday: "short", day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
const dateOnly = new Intl.DateTimeFormat("en-GB", { weekday: "short", day: "numeric", month: "short", year: "numeric" });

/** "Wed 24 Jun 2026, 18:00" in the browser's time zone. */
export function formatDateTime(iso: string) {
  return dateTime.format(new Date(iso));
}

/** "Wed 24 Jun 2026". */
export function formatDate(iso: string) {
  return dateOnly.format(new Date(iso));
}

/** 62 → "1 h 2 min", 45 → "45 min". */
export function formatMinutes(minutes: number) {
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return h === 0 ? `${m} min` : m === 0 ? `${h} h` : `${h} h ${m} min`;
}
