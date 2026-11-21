import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { WorkoutQuery, WorkoutSummary } from "../api/types";
import { renderApp } from "../test/render";

vi.mock("../api/endpoints");

const now = new Date();
const thisMonth = (day: number, hour = 18) => new Date(now.getFullYear(), now.getMonth(), day, hour).toISOString();

const summary = (id: number, name: string, startedAtUtc: string, exerciseNames: string[]): WorkoutSummary => ({
  id,
  name,
  type: "Strength",
  status: "Completed",
  startedAtUtc,
  scheduledAtUtc: null,
  durationMinutes: 62,
  exerciseCount: exerciseNames.length,
  setCount: 12,
  exerciseNames,
});

const legDay = summary(1, "Leg day", thisMonth(3), ["Barbell Back Squat", "Leg Press"]);
const pushDay = summary(2, "Push day", thisMonth(5), ["Barbell Bench Press"]);

describe("WorkoutsPage: history (WF06)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(null);
    vi.mocked(endpoints.listWorkouts).mockImplementation(async (q: WorkoutQuery) => {
      const items = [pushDay, legDay].filter(
        (w) => (!q.from || w.startedAtUtc! >= q.from) && (!q.to || w.startedAtUtc! < q.to) && (!q.exerciseId || (q.exerciseId === 1) === w.exerciseNames.includes("Barbell Bench Press")),
      );
      return { items, page: 1, pageSize: q.pageSize ?? 20, totalCount: items.length, totalPages: 1 };
    });
  });

  it("lists workouts newest first and marks the training days in the calendar", async () => {
    renderApp("/workouts");

    const rows = await screen.findAllByRole("link", { name: /^(Push|Leg) day/ });
    expect(rows.map((r) => r.textContent?.match(/^(Push|Leg) day/)?.[0])).toEqual(["Push day", "Leg day"]);
    expect(within(rows[1]!).getByText("Barbell Back Squat, Leg Press")).toBeInTheDocument();
    await waitFor(() => expect(screen.getAllByRole("button", { name: /, 1 workout$/ })).toHaveLength(2));
  });

  it("tapping a training day shows only that day", async () => {
    renderApp("/workouts");
    const user = userEvent.setup();
    await screen.findByText("Push day");

    const day3 = (await screen.findAllByRole("button", { name: /, 1 workout$/ })).find((b) => b.textContent === "3")!;
    await user.click(day3);

    await waitFor(() => expect(screen.queryByText("Push day")).not.toBeInTheDocument());
    expect(screen.getByText("Leg day")).toBeInTheDocument();
  });

  it("shows the workout in progress with a way back to it", async () => {
    vi.mocked(endpoints.getActiveWorkout).mockResolvedValue({
      id: 9,
      name: "Upper Body – Intermediate",
      type: "Strength",
      status: "InProgress",
      planId: 2,
      scheduledAtUtc: null,
      startedAtUtc: now.toISOString(),
      finishedAtUtc: null,
      pausedAtUtc: null,
      pausedSeconds: 0,
      durationMinutes: null,
      notes: "",
      exercises: [],
    });
    renderApp("/workouts");

    expect(await screen.findByRole("link", { name: "Continue workout" })).toHaveAttribute("href", "/workouts/live");
  });
});
