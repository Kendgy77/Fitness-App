import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Exercise, Workout } from "../api/types";
import { renderApp } from "../test/render";

vi.mock("../api/endpoints");

const squat: Exercise = {
  id: 36,
  name: "Barbell Back Squat",
  description: "",
  muscles: "",
  category: "Legs",
  difficulty: "Intermediate",
  equipment: "Barbell",
  isCustom: false,
  isArchived: false,
};

const saved: Workout = {
  id: 50,
  name: "Leg day",
  type: "Strength",
  status: "Completed",
  planId: null,
  scheduledAtUtc: null,
  startedAtUtc: "2026-06-24T16:00:00Z",
  finishedAtUtc: "2026-06-24T17:02:00Z",
  pausedAtUtc: null,
  pausedSeconds: 0,
  durationMinutes: 62,
  notes: "",
  exercises: [],
};

describe("WorkoutFormPage: a past workout (WF06)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.searchExercises).mockResolvedValue({ items: [squat], page: 1, pageSize: 30, totalCount: 1, totalPages: 1 });
    vi.mocked(endpoints.getExerciseSuggestions).mockResolvedValue({ mostUsed: [], recent: [] });
    vi.mocked(endpoints.logWorkout).mockResolvedValue(saved);
    vi.mocked(endpoints.getWorkout).mockResolvedValue(saved);
  });

  it("T11: saves the date, type, duration and the squat sets 4 × 8 × 80 kg", async () => {
    renderApp("/workouts/log");
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText("Name"), "Leg day");
    const when = screen.getByLabelText("Date and time");

    fireEvent.change(when, { target: { value: "2026-06-24T18:00" } });
    const duration = screen.getByLabelText("Duration (min)");
    await user.clear(duration);
    await user.type(duration, "62");

    await user.click(screen.getByRole("button", { name: "Add exercise" }));
    const dialog = await screen.findByRole("dialog", { name: "Add exercises" });
    await user.click(await within(dialog).findByRole("button", { name: "Add Barbell Back Squat" }));
    await user.click(within(dialog).getByRole("button", { name: "Done" }));

    const first = screen.getByRole("listitem", { name: "Barbell Back Squat set 1" });
    await user.type(within(first).getByLabelText("Weight (kg)"), "80");
    await user.type(within(first).getByLabelText("Reps"), "8");
    // "Add set" copies the previous set, so the next three are one tap each.
    for (let i = 0; i < 3; i++) await user.click(screen.getByRole("button", { name: "Add set" }));
    await user.click(screen.getByRole("button", { name: "Save workout" }));

    await waitFor(() => expect(endpoints.logWorkout).toHaveBeenCalled());
    const request = vi.mocked(endpoints.logWorkout).mock.calls[0]![0];
    expect(request).toMatchObject({ name: "Leg day", type: "Strength", durationMinutes: 62 });
    expect(new Date(request.startedAt).getTime()).toBe(new Date(2026, 5, 24, 18, 0).getTime());
    expect(request.exercises).toEqual([
      {
        exerciseId: 36,
        notes: "",
        sets: Array(4).fill({ type: "Normal", reps: 8, weightKg: 80, rpe: null, durationSeconds: null, distanceMeters: null }),
      },
    ]);
    expect(await screen.findByRole("heading", { name: "Leg day" })).toBeInTheDocument();
  });

  it("T12: a workout without exercises is not sent and the form says why", async () => {
    renderApp("/workouts/log");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Save workout" }));

    expect(await screen.findByText("Add at least one exercise with at least one set.")).toBeInTheDocument();
    expect(endpoints.logWorkout).not.toHaveBeenCalled();
  });

  it("T12: an exercise whose sets were all removed is not sent", async () => {
    renderApp("/workouts/log");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Add exercise" }));
    const dialog = await screen.findByRole("dialog", { name: "Add exercises" });
    await user.click(await within(dialog).findByRole("button", { name: "Add Barbell Back Squat" }));
    await user.click(within(dialog).getByRole("button", { name: "Done" }));
    await user.click(screen.getByRole("button", { name: "Delete Barbell Back Squat set 1" }));
    await user.click(screen.getByRole("button", { name: "Save workout" }));

    expect(await screen.findByText("Add at least one set.")).toBeInTheDocument();
    expect(endpoints.logWorkout).not.toHaveBeenCalled();
  });
});
