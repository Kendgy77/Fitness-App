// TypeScript mirrors of the API's DTOs (src/FitnessApp.Api/Features/*/…Dtos.cs).
// Enums travel as their C# names, so they are string unions here.

export interface UserResponse {
  id: string;
  email: string;
  roles: string[];
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: UserResponse;
}

export type Sex = "Female" | "Male" | "Other";
export type TrainingGoal = "LoseWeight" | "BuildMuscle" | "IncreaseStrength" | "ImproveEndurance" | "StayHealthy";
export type BmiCategory = "Underweight" | "Normal" | "Overweight" | "Obese";

export interface Bmi {
  value: number;
  category: BmiCategory;
}

export interface Profile {
  birthDate: string | null;
  age: number | null;
  sex: Sex | null;
  heightCm: number | null;
  weightKg: number | null;
  goal: TrainingGoal | null;
  timeZone: string;
  bmi: Bmi | null;
  missingForBmi: ("weightKg" | "heightCm")[];
  updatedAtUtc: string | null;
}

export interface UpdateProfileRequest {
  birthDate: string | null;
  sex: Sex | null;
  heightCm: number | null;
  weightKg: number | null;
  goal: TrainingGoal | null;
  timeZone: string | null;
}

export type ExerciseCategory = "Chest" | "Back" | "Shoulders" | "Arms" | "Legs" | "Core" | "Cardio";
export type Difficulty = "Beginner" | "Intermediate" | "Advanced";
export type Equipment = "Barbell" | "Dumbbell" | "Kettlebell" | "Machine" | "Cable" | "Bodyweight" | "Other";

export interface Exercise {
  id: number;
  name: string;
  description: string;
  muscles: string;
  category: ExerciseCategory;
  difficulty: Difficulty;
  equipment: Equipment;
  isCustom: boolean;
  isArchived: boolean;
}

export interface ExerciseRequest {
  name: string;
  description: string;
  muscles: string;
  category: ExerciseCategory;
  difficulty: Difficulty;
  equipment: Equipment;
}

export interface ExerciseQuery {
  search?: string;
  category?: ExerciseCategory;
  difficulty?: Difficulty;
  equipment?: Equipment;
  onlyCustom?: boolean;
  page?: number;
  pageSize?: number;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
