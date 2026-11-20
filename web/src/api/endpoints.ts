import { api } from "./client";
import type {
  AuthResponse,
  Exercise,
  ExerciseQuery,
  ExerciseRequest,
  Paged,
  Plan,
  PlanQuery,
  PlanRequest,
  PlanStatus,
  PlanSummary,
  Profile,
  UpdateProfileRequest,
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
