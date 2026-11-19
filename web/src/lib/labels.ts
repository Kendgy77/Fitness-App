import type { BmiCategory, Difficulty, Equipment, ExerciseCategory, Sex, TrainingGoal } from "../api/types";

// Polish names for the API's enum values. Records (not switch statements) so TypeScript reports a missing
// translation the moment a value is added to the API types.

export const sexLabels: Record<Sex, string> = {
  Female: "Kobieta",
  Male: "Mężczyzna",
  Other: "Inna",
};

export const goalLabels: Record<TrainingGoal, string> = {
  LoseWeight: "Redukcja masy ciała",
  BuildMuscle: "Budowa masy mięśniowej",
  IncreaseStrength: "Zwiększenie siły",
  ImproveEndurance: "Poprawa wytrzymałości",
  StayHealthy: "Utrzymanie formy i zdrowia",
};

export const bmiLabels: Record<BmiCategory, string> = {
  Underweight: "Niedowaga",
  Normal: "Waga prawidłowa",
  Overweight: "Nadwaga",
  Obese: "Otyłość",
};

export const categoryLabels: Record<ExerciseCategory, string> = {
  Chest: "Klatka piersiowa",
  Back: "Plecy",
  Shoulders: "Barki",
  Arms: "Ramiona",
  Legs: "Nogi i pośladki",
  Core: "Brzuch i core",
  Cardio: "Cardio",
};

export const difficultyLabels: Record<Difficulty, string> = {
  Beginner: "Początkujący",
  Intermediate: "Średniozaawansowany",
  Advanced: "Zaawansowany",
};

export const equipmentLabels: Record<Equipment, string> = {
  Barbell: "Sztanga",
  Dumbbell: "Hantle",
  Kettlebell: "Kettlebell",
  Machine: "Maszyna",
  Cable: "Wyciąg",
  Bodyweight: "Masa ciała",
  Other: "Inny sprzęt",
};

/** Entries of a label record, for building <select> options in a stable order. */
export function options<T extends string>(labels: Record<T, string>) {
  return Object.entries(labels) as [T, string][];
}

const decimal = new Intl.NumberFormat("pl-PL", { maximumFractionDigits: 1 });

/** 23.3 → "23,3" (Polish decimal comma). */
export function formatDecimal(value: number) {
  return decimal.format(value);
}
