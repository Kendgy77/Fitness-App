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

export type PlanStatus = "Draft" | "Active" | "Archived";

export interface PlanSummary {
  id: number;
  name: string;
  description: string;
  status: PlanStatus;
  isTemplate: boolean;
  exerciseCount: number;
  updatedAtUtc: string;
}

export interface PlanExercise {
  order: number;
  exerciseId: number;
  exerciseName: string;
  category: ExerciseCategory;
  equipment: Equipment;
  exerciseIsArchived: boolean;
  targetSets: number;
  /** Reps are a range (8–10); a fixed number has both ends equal. */
  targetRepsMin: number;
  targetRepsMax: number;
  targetWeightKg: number | null;
  notes: string;
}

export interface Plan {
  id: number;
  name: string;
  description: string;
  status: PlanStatus;
  isTemplate: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  exercises: PlanExercise[];
}

export interface PlanExerciseRequest {
  exerciseId: number;
  targetSets: number;
  targetRepsMin: number;
  targetRepsMax: number;
  targetWeightKg: number | null;
  notes: string;
}

export interface PlanRequest {
  name: string;
  description: string;
  exercises: PlanExerciseRequest[];
}

export interface PlanQuery {
  status?: PlanStatus;
  page?: number;
  pageSize?: number;
}

// ---- Workouts (src/FitnessApp.Api/Features/Sessions/SessionDtos.cs) ----------------------------------------

export type SessionType = "Strength" | "Cardio" | "Mixed" | "Other";
export type SessionStatus = "Planned" | "InProgress" | "Paused" | "Completed";
export type SetType = "Normal" | "Warmup" | "Drop" | "Failure";

export interface WorkoutSet {
  id: number;
  setNumber: number;
  type: SetType;
  reps: number;
  weightKg: number | null;
  rpe: number | null;
  durationSeconds: number | null;
  distanceMeters: number | null;
  completedAtUtc: string;
}

export interface WorkoutExercise {
  id: number;
  order: number;
  exerciseId: number;
  exerciseName: string;
  category: ExerciseCategory;
  equipment: Equipment;
  notes: string;
  targetSets: number | null;
  targetRepsMin: number | null;
  targetRepsMax: number | null;
  targetWeightKg: number | null;
  sets: WorkoutSet[];
}

export interface Workout {
  id: number;
  name: string;
  type: SessionType;
  status: SessionStatus;
  planId: number | null;
  scheduledAtUtc: string | null;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  pausedAtUtc: string | null;
  pausedSeconds: number;
  durationMinutes: number | null;
  notes: string;
  exercises: WorkoutExercise[];
}

export interface WorkoutSummary {
  id: number;
  name: string;
  type: SessionType;
  status: SessionStatus;
  startedAtUtc: string | null;
  scheduledAtUtc: string | null;
  durationMinutes: number | null;
  exerciseCount: number;
  setCount: number;
  exerciseNames: string[];
}

export interface WorkoutQuery {
  from?: string;
  to?: string;
  exerciseId?: number;
  status?: SessionStatus;
  page?: number;
  pageSize?: number;
}

export interface SetRequest {
  type: SetType;
  reps: number;
  weightKg: number | null;
  rpe: number | null;
  durationSeconds: number | null;
  distanceMeters: number | null;
}

export interface LogWorkoutRequest {
  name: string;
  type: SessionType;
  startedAt: string;
  durationMinutes: number;
  notes: string;
  exercises: { exerciseId: number; notes: string; sets: SetRequest[] }[];
}

export interface StartPlanOption {
  id: number;
  name: string;
  isTemplate: boolean;
  exerciseCount: number;
  lastUsedAtUtc: string | null;
}

export interface ExerciseSuggestions {
  mostUsed: Exercise[];
  recent: Exercise[];
}

export interface ExerciseHistoryEntry {
  sessionId: number;
  sessionName: string;
  startedAtUtc: string;
  sets: WorkoutSet[];
}

// ---- Measurements and progress (Features/Measurements, Features/Progress) -----------------------------------

export interface MeasurementValues {
  weightKg: number | null;
  bodyFatPercent: number | null;
  muscleMassKg: number | null;
  neckCm: number | null;
  chestCm: number | null;
  waistCm: number | null;
  hipsCm: number | null;
  bicepsLeftCm: number | null;
  bicepsRightCm: number | null;
  forearmCm: number | null;
  thighLeftCm: number | null;
  thighRightCm: number | null;
  calfCm: number | null;
}

export interface Measurement extends MeasurementValues {
  id: number;
  measuredOn: string;
  bmi: Bmi | null;
  notes: string;
}

export interface MeasurementRequest extends MeasurementValues {
  measuredOn: string;
  notes: string;
}

/** One value's newest reading, e.g. { field: "waistCm", value: 84.5, measuredOn: "2026-11-20" }. */
export interface CurrentMeasurement {
  field: keyof MeasurementValues;
  value: number;
  measuredOn: string;
}

export type ProgressPeriod = "FourWeeks" | "ThreeMonths" | "Year" | "All";

export interface ProgressSummary {
  workoutsThisWeek: number;
  workoutsThisMonth: number;
  workoutsTotal: number;
  streakWeeks: number;
  volumeThisWeekKg: number;
  volumeThisMonthKg: number;
  weightKg: number | null;
  weightMeasuredOn: string | null;
  bmi: Bmi | null;
  lastWorkoutAtUtc: string | null;
}

export interface WeeklyVolume {
  weekStart: string;
  volumeKg: number;
  workouts: number;
  sets: number;
}

export interface ExerciseProgressRow {
  exerciseId: number;
  name: string;
  category: ExerciseCategory;
  workouts: number;
  volumeKg: number;
  bestOneRepMaxKg: number | null;
  oneRepMaxChangePercent: number | null;
  lastDoneAtUtc: string;
}

export interface RecordValue {
  value: number;
  weightKg: number | null;
  reps: number | null;
  sessionId: number;
  achievedAtUtc: string;
}

export interface ExerciseStats {
  exerciseId: number;
  name: string;
  category: ExerciseCategory;
  period: ProgressPeriod;
  workouts: {
    sessionId: number;
    startedAtUtc: string;
    bestOneRepMaxKg: number | null;
    topSetWeightKg: number | null;
    topSetReps: number | null;
    volumeKg: number;
    workingSets: number;
  }[];
  bestOneRepMaxKg: number | null;
  heaviestSet: RecordValue | null;
  oneRepMaxChangePercent: number | null;
  workoutsPerWeek: number;
  repRanges: { range: string; sets: number }[];
}

export interface ExerciseRecords {
  exerciseId: number;
  name: string;
  category: ExerciseCategory;
  heaviestSet: RecordValue | null;
  bestOneRepMax: RecordValue | null;
  bestSessionVolume: RecordValue | null;
}

export interface MuscleGroupVolume {
  category: ExerciseCategory;
  setsPerWeek: number;
  volumePerWeekKg: number;
}
