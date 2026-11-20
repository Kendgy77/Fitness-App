import type { BmiCategory, Difficulty, Equipment, ExerciseCategory, PlanStatus, Sex, TrainingGoal } from "../api/types";

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
