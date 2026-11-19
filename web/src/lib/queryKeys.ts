import type { ExerciseQuery } from "../api/types";

// React Query caches each response under a key. Keeping the keys in one place means a save can refresh exactly
// the cached lists it made stale.
export const queryKeys = {
  profile: ["profile"] as const,
  exercises: ["exercises"] as const,
  exerciseList: (query: ExerciseQuery) => ["exercises", "list", query] as const,
  exercise: (id: number) => ["exercises", id] as const,
};
