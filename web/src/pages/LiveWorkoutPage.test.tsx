import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Workout, WorkoutExercise, WorkoutSet } from "../api/types";
import { apiError, renderApp } from "../test/render";

vi.mock("../api/endpoints");

const set = (setNumber: number, weightKg: number, reps: number, type: WorkoutSet["type"] = "Normal"): WorkoutSet => ({
  id: 100 + setNumber,
  setNumber,
  type,
  reps,
  weightKg,
  rpe: null,
  durationSeconds: null,
  distanceMeters: null,
  completedAtUtc: new Date().toISOString(),
});

const bench = (sets: WorkoutSet[]): WorkoutExercise => ({
  id: 10,
  order: 1,
  exerciseId: 1,
  exerciseName: "Barbell Bench Press",
  category: "Chest",
  equipment: "Barbell",
  notes: "",
  targetSets: 3,
  targetRepsMin: 6,
  targetRepsMax: 8,
  targetWeightKg: 60,
  sets,
});

const workout = (exercises: WorkoutExercise[]): Workout => ({
  id: 7,
  name: "Push day",
  type: "Strength",
  status: "InProgress",
  planId: 1000,
  scheduledAtUtc: null,
  startedAtUtc: new Date(Date.now() - 20 * 60_000).toISOString(),
  finishedAtUtc: null,
  pausedAtUtc: null,
  pausedSeconds: 0,
  durationMinutes: null,
  notes: "",
  exercises,
});

describe("LiveWorkoutPage (WF06)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.searchExercises).mockResolvedValue({ items: [], page: 1, pageSize: 30, totalCount: 0, totalPages: 0 });
  });

  it("starts the first set from the plan's target, the bottom of the rep range", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(workout([bench([])]));
    renderApp("/workouts/live");

    expect(await screen.findByLabelText("Weight (kg)")).toHaveValue("60");
    expect(screen.getByLabelText("Reps")).toHaveValue("6");
    expect(screen.getByText(/target 3 × 6–8 @ 60 kg/)).toBeInTheDocument();
  });

  it("prefills the next set from the last one; ± adjusts it before logging", async () => {
    const before = workout([bench([set(1, 62.5, 8)])]);
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(before);
    vi.mocked(endpoints.addSet).mockResolvedValue(workout([bench([set(1, 62.5, 8), set(2, 62.5, 7)])]));
    renderApp("/workouts/live");
    const user = userEvent.setup();

    expect(await screen.findByLabelText("Weight (kg)")).toHaveValue("62.5");
    expect(screen.getByLabelText("Reps")).toHaveValue("8");
    await user.click(screen.getByRole("button", { name: "Reps minus 1" }));
    await user.click(screen.getByRole("button", { name: "Log set 2" }));

    expect(endpoints.addSet).toHaveBeenCalledWith(7, 10, {
      type: "Normal",
      reps: 7,
      weightKg: 62.5,
      rpe: null,
      durationSeconds: null,
      distanceMeters: null,
    });
    // The new set shows, and the form is ready for set 3 with the same numbers.
    const sets = await screen.findByRole("list", { name: "Barbell Bench Press sets" });
    expect(within(sets).getByText("62.5 kg × 7")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Log set 3" })).toBeInTheDocument();
    expect(screen.getByLabelText("Reps")).toHaveValue("7");
  });

  it("a drop set starts 20% lighter", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(workout([bench([set(1, 60, 8)])]));
    vi.mocked(endpoints.addSet).mockResolvedValue(workout([bench([set(1, 60, 8), set(2, 47.5, 8, "Drop")])]));
    renderApp("/workouts/live");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "+ Drop set" }));
    expect(screen.getByLabelText("Weight (kg)")).toHaveValue("47.5");
    await user.click(screen.getByRole("button", { name: "Log set 2" }));

    expect(endpoints.addSet).toHaveBeenCalledWith(7, 10, expect.objectContaining({ type: "Drop", weightKg: 47.5 }));
  });

  it("T12: finishing without any set shows why and keeps the workout open", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(workout([bench([])]));
    vi.mocked(endpoints.finishWorkout).mockRejectedValue(
      apiError(400, { title: "One or more validation errors occurred.", errors: { exercises: ["Log at least one set before finishing the workout."] } }),
    );
    renderApp("/workouts/live");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Finish" }));
    const dialog = await screen.findByRole("dialog", { name: "Finish workout" });
    await user.click(within(dialog).getByRole("button", { name: "Finish workout" }));

    expect(await within(dialog).findByText("Log at least one set before finishing the workout.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Push day" })).toBeInTheDocument();
  });

  it("finishing sends the duration and opens the finished workout", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(workout([bench([set(1, 60, 8)])]));
    const finished = { ...workout([bench([set(1, 60, 8)])]), status: "Completed" as const, durationMinutes: 45 };
    vi.mocked(endpoints.finishWorkout).mockResolvedValue(finished);
    vi.mocked(endpoints.getWorkout).mockResolvedValue(finished);
    renderApp("/workouts/live");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Finish" }));
    const duration = screen.getByLabelText("Duration (min)");
    expect(duration).toHaveValue("20"); // started 20 minutes ago
    await user.clear(duration);
    await user.type(duration, "45");
    await user.click(screen.getByRole("button", { name: "Finish workout" }));

    expect(endpoints.finishWorkout).toHaveBeenCalledWith(7, 45, "");
    expect(await screen.findByText("Finished")).toBeInTheDocument();
  });

  it("without an active workout it offers to start one", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(null);
    renderApp("/workouts/live");

    expect(await screen.findByText(/No workout in progress/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Start one" })).toHaveAttribute("href", "/workouts/new");
  });

  it("pauses and resumes", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(workout([bench([])]));
    vi.mocked(endpoints.changeWorkoutStatus).mockResolvedValue({ ...workout([bench([])]), status: "Paused", pausedAtUtc: new Date().toISOString() });
    renderApp("/workouts/live");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Pause" }));

    expect(endpoints.changeWorkoutStatus).toHaveBeenCalledWith(7, "pause");
    await waitFor(() => expect(screen.getByRole("button", { name: "Resume" })).toBeInTheDocument());
  });
});
