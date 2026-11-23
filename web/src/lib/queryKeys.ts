import type { ExerciseQuery, PlanQuery, ProgressPeriod, WorkoutQuery } from "../api/types";

// React Query caches each response under a key. Keeping the keys in one place means a save can refresh exactly
// the cached lists it made stale.
export const queryKeys = {
  profile: ["profile"] as const,
  exercises: ["exercises"] as const,
  exerciseList: (query: ExerciseQuery) => ["exercises", "list", query] as const,
  exercise: (id: number) => ["exercises", id] as const,
  plans: ["plans"] as const,
  planList: (query: PlanQuery) => ["plans", "list", query] as const,
  planTemplates: ["plans", "templates"] as const,
  plan: (id: number) => ["plans", id] as const,
  workouts: ["workouts"] as const,
  workoutList: (query: WorkoutQuery) => ["workouts", "list", query] as const,
  workout: (id: number) => ["workouts", id] as const,
  activeWorkout: ["workouts", "active"] as const,
  startOptions: ["workouts", "start-options"] as const,
  exerciseSuggestions: ["exercises", "suggestions"] as const,
  exerciseHistory: (id: number) => ["exercises", id, "history"] as const,
  measurements: ["measurements"] as const,
  measurement: (id: number) => ["measurements", id] as const,
  progress: ["progress"] as const,
  progressSummary: ["progress", "summary"] as const,
  weeklyVolume: (weeks: number) => ["progress", "volume", weeks] as const,
  exerciseRanking: (period: ProgressPeriod) => ["progress", "exercises", period] as const,
  exerciseStats: (id: number, period: ProgressPeriod) => ["progress", "exercises", id, period] as const,
  records: ["progress", "records"] as const,
  muscleGroups: (period: ProgressPeriod) => ["progress", "muscle-groups", period] as const,
};
