import { api } from "./client";
import type {
  AuthResponse,
  Exercise,
  ExerciseHistoryEntry,
  ExerciseSuggestions,
  ExerciseQuery,
  ExerciseRequest,
  LogWorkoutRequest,
  Paged,
  Plan,
  PlanQuery,
  PlanRequest,
  PlanStatus,
  PlanSummary,
  Profile,
  SetRequest,
  StartPlanOption,
  UpdateProfileRequest,
  Workout,
  WorkoutQuery,
  WorkoutSummary,
} from "./types";

// One function per API endpoint. Pages call these (through React Query), never axios directly.

export async function login(email: string, password: string) {
  const { data } = await api.post<AuthResponse>("/auth/login", { email, password });
  return data;
}

export async function register(email: string, password: string) {
  const { data } = await api.post<AuthResponse>("/auth/register", { email, password });
  return data;
}

export async function logout() {
  await api.post("/auth/logout");
}

export async function getProfile() {
  const { data } = await api.get<Profile>("/profile");
  return data;
}

export async function updateProfile(request: UpdateProfileRequest) {
  const { data } = await api.put<Profile>("/profile", request);
  return data;
}

export async function searchExercises(query: ExerciseQuery) {
  const { data } = await api.get<Paged<Exercise>>("/exercises", { params: query });
  return data;
}

export async function getExercise(id: number) {
  const { data } = await api.get<Exercise>(`/exercises/${id}`);
  return data;
}

export async function createExercise(request: ExerciseRequest) {
  const { data } = await api.post<Exercise>("/exercises", request);
  return data;
}

export async function updateExercise(id: number, request: ExerciseRequest) {
  const { data } = await api.put<Exercise>(`/exercises/${id}`, request);
  return data;
}

export async function archiveExercise(id: number) {
  await api.delete(`/exercises/${id}`);
}

export async function listPlans(query: PlanQuery) {
  const { data } = await api.get<Paged<PlanSummary>>("/plans", { params: query });
  return data;
}

export async function listPlanTemplates() {
  const { data } = await api.get<Paged<PlanSummary>>("/plans/templates", { params: { pageSize: 50 } });
  return data;
}

export async function getPlan(id: number) {
  const { data } = await api.get<Plan>(`/plans/${id}`);
  return data;
}

export async function createPlan(request: PlanRequest) {
  const { data } = await api.post<Plan>("/plans", request);
  return data;
}

export async function updatePlan(id: number, request: PlanRequest) {
  const { data } = await api.put<Plan>(`/plans/${id}`, request);
  return data;
}

export async function changePlanStatus(id: number, status: PlanStatus) {
  const { data } = await api.post<Plan>(`/plans/${id}/status`, { status });
  return data;
}

export async function copyPlan(id: number) {
  const { data } = await api.post<Plan>(`/plans/${id}/copy`);
  return data;
}

export async function deletePlan(id: number) {
  await api.delete(`/plans/${id}`);
}

// ---- Workouts ----------------------------------------------------------------------------------------------

export async function listWorkouts(query: WorkoutQuery) {
  const { data } = await api.get<Paged<WorkoutSummary>>("/sessions", { params: query });
  return data;
}

export async function getWorkout(id: number) {
  const { data } = await api.get<Workout>(`/sessions/${id}`);
  return data;
}

/** The workout in progress or paused, or null (the API answers 204). */
export async function getActiveWorkout() {
  const { data, status } = await api.get<Workout>("/sessions/active");
  return status === 204 ? null : data;
}

export async function getStartOptions() {
  const { data } = await api.get<{ plans: StartPlanOption[] }>("/sessions/start-options");
  return data.plans;
}

export async function getExerciseSuggestions() {
  const { data } = await api.get<ExerciseSuggestions>("/exercises/suggestions");
  return data;
}

export async function getExerciseHistory(exerciseId: number) {
  const { data } = await api.get<Paged<ExerciseHistoryEntry>>(`/exercises/${exerciseId}/history`, { params: { pageSize: 10 } });
  return data;
}

export async function logWorkout(request: LogWorkoutRequest) {
  const { data } = await api.post<Workout>("/sessions", request);
  return data;
}

export async function updateWorkout(id: number, request: LogWorkoutRequest) {
  const { data } = await api.put<Workout>(`/sessions/${id}`, request);
  return data;
}

export async function startWorkout(planId: number | null) {
  const { data } = await api.post<Workout>("/sessions/start", { planId });
  return data;
}

export async function changeWorkoutStatus(id: number, action: "pause" | "resume" | "start") {
  const { data } = await api.post<Workout>(`/sessions/${id}/${action}`);
  return data;
}

export async function finishWorkout(id: number, durationMinutes: number | null, notes: string | null) {
  const { data } = await api.post<Workout>(`/sessions/${id}/finish`, { durationMinutes, notes });
  return data;
}

export async function addWorkoutExercise(id: number, exerciseId: number) {
  const { data } = await api.post<Workout>(`/sessions/${id}/exercises`, { exerciseId });
  return data;
}

export async function removeWorkoutExercise(id: number, entryId: number) {
  const { data } = await api.delete<Workout>(`/sessions/${id}/exercises/${entryId}`);
  return data;
}

export async function addSet(id: number, entryId: number, request: SetRequest) {
  const { data } = await api.post<Workout>(`/sessions/${id}/exercises/${entryId}/sets`, request);
  return data;
}

export async function deleteSet(id: number, entryId: number, setId: number) {
  const { data } = await api.delete<Workout>(`/sessions/${id}/exercises/${entryId}/sets/${setId}`);
  return data;
}

export async function deleteWorkout(id: number) {
  await api.delete(`/sessions/${id}`);
}
