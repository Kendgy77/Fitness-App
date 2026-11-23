import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Measurement, Profile, ProgressSummary } from "../api/types";
import { renderApp } from "../test/render";

vi.mock("../api/endpoints");

const summary = (over: Partial<ProgressSummary> = {}): ProgressSummary => ({
  workoutsThisWeek: 2,
  workoutsThisMonth: 7,
  workoutsTotal: 20,
  streakWeeks: 4,
  volumeThisWeekKg: 12500,
  volumeThisMonthKg: 40000,
  weightKg: 81.5,
  weightMeasuredOn: "2026-11-22",
  bmi: { value: 24.3, category: "Normal" },
  lastWorkoutAtUtc: "2026-11-22T17:00:00Z",
  ...over,
});

const profile: Profile = {
  birthDate: null,
  age: null,
  sex: null,
  heightCm: 183,
  weightKg: 81.5,
  goal: null,
  timeZone: "Europe/Warsaw",
  bmi: { value: 24.3, category: "Normal" },
  missingForBmi: [],
  updatedAtUtc: "2026-11-22T10:00:00Z",
};

const weeks = Array.from({ length: 12 }, (_, i) => ({
  weekStart: new Date(Date.UTC(2026, 8, 7 + 7 * i)).toISOString().slice(0, 10),
  volumeKg: i === 11 ? 12500 : 8000,
  workouts: i === 11 ? 2 : 3,
  sets: 30,
}));

beforeEach(() => {
  vi.mocked(endpoints.getActiveWorkout).mockResolvedValue(null);
  vi.mocked(endpoints.getProfile).mockResolvedValue(profile);
  vi.mocked(endpoints.getProgressSummary).mockResolvedValue(summary());
  vi.mocked(endpoints.getWeeklyVolume).mockImplementation(async (n) => weeks.slice(-n));
  vi.mocked(endpoints.listWorkouts).mockResolvedValue({ items: [], page: 1, pageSize: 5, totalCount: 0, totalPages: 0 });
});

describe("Dashboard (thesis Fig. 6.2, T13)", () => {
  it("T13: shows this week's statistics and the training chart", async () => {
    renderApp("/");

    expect(await screen.findByText("4-week streak", { exact: false })).toBeInTheDocument();
    expect(screen.getByText("12,500 kg")).toBeInTheDocument();
    expect(screen.getByText("81.5 kg")).toBeInTheDocument();
    expect(await screen.findByRole("img", { name: "Weekly training volume, last 8 weeks" })).toBeInTheDocument();
    expect(screen.getByText(/This week:/)).toHaveTextContent("This week: 12,500 kg in 2 workouts");
  });

  it("before the first workout, the chart's place invites you to log one", async () => {
    vi.mocked(endpoints.getProgressSummary).mockResolvedValue(summary({ workoutsTotal: 0, workoutsThisWeek: 0, streakWeeks: 0, volumeThisWeekKg: 0 }));
    renderApp("/");

    expect(await screen.findByRole("link", { name: "Log your first workout" })).toHaveAttribute("href", "/workouts/new");
    expect(screen.queryByRole("img", { name: /Weekly training volume/ })).not.toBeInTheDocument();
  });
});

describe("ProgressPage: strength (WF07)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.getExerciseRanking).mockResolvedValue([
      { exerciseId: 1, name: "Barbell Bench Press", category: "Chest", workouts: 10, volumeKg: 30000, bestOneRepMaxKg: 101.3, oneRepMaxChangePercent: 4.2, lastDoneAtUtc: "2026-11-22T17:00:00Z" },
      { exerciseId: 36, name: "Barbell Back Squat", category: "Legs", workouts: 6, volumeKg: 42000, bestOneRepMaxKg: 140, oneRepMaxChangePercent: -1.5, lastDoneAtUtc: "2026-11-20T17:00:00Z" },
      { exerciseId: 7, name: "Push-Up", category: "Chest", workouts: 4, volumeKg: 0, bestOneRepMaxKg: null, oneRepMaxChangePercent: null, lastDoneAtUtc: "2026-11-18T17:00:00Z" },
    ]);
    vi.mocked(endpoints.getMuscleGroupVolume).mockResolvedValue([{ category: "Chest", setsPerWeek: 12.5, volumePerWeekKg: 3000 }]);
    vi.mocked(endpoints.getRecords).mockResolvedValue([
      {
        exerciseId: 36,
        name: "Barbell Back Squat",
        category: "Legs",
        heaviestSet: { value: 120, weightKg: 120, reps: 5, sessionId: 3, achievedAtUtc: "2026-11-20T17:00:00Z" },
        bestOneRepMax: { value: 140, weightKg: 120, reps: 5, sessionId: 3, achievedAtUtc: "2026-11-20T17:00:00Z" },
        bestSessionVolume: { value: 4800, weightKg: null, reps: null, sessionId: 3, achievedAtUtc: "2026-11-20T17:00:00Z" },
      },
    ]);
  });

  const exerciseNames = () =>
    within(screen.getByRole("table"))
      .getAllByRole("row")
      .slice(1)
      .map((r) => within(r).getAllByRole("cell")[0]!.querySelector("a")!.textContent);

  it("ranks exercises by workouts and sorts by any column, empty values last", async () => {
    renderApp("/progress");
    const user = userEvent.setup();

    await screen.findByRole("table");
    expect(exerciseNames()).toEqual(["Barbell Bench Press", "Barbell Back Squat", "Push-Up"]);

    await user.click(screen.getByRole("button", { name: "Best 1RM" }));
    expect(exerciseNames()).toEqual(["Barbell Back Squat", "Barbell Bench Press", "Push-Up"]);
    await user.click(screen.getByRole("button", { name: /Best 1RM/ }));
    expect(exerciseNames()).toEqual(["Barbell Bench Press", "Barbell Back Squat", "Push-Up"]); // ascending; no 1RM stays last
  });

  it("the period switch asks for that period", async () => {
    renderApp("/progress");
    const user = userEvent.setup();
    await screen.findByRole("table");

    await user.click(screen.getByRole("button", { name: "1 year" }));

    await waitFor(() => expect(endpoints.getExerciseRanking).toHaveBeenLastCalledWith("Year"));
    expect(endpoints.getMuscleGroupVolume).toHaveBeenLastCalledWith("Year");
  });

  it("shows records and sets per muscle group", async () => {
    renderApp("/progress");

    expect(await screen.findByText("120 kg × 5")).toBeInTheDocument();
    expect(screen.getByText("12.5 sets")).toBeInTheDocument();
  });
});

const measurement = (id: number, measuredOn: string, weightKg: number | null, waistCm: number | null = null): Measurement => ({
  id,
  measuredOn,
  weightKg,
  bmi: weightKg ? { value: Math.round((weightKg / 1.83 / 1.83) * 10) / 10, category: "Normal" } : null,
  bodyFatPercent: null,
  muscleMassKg: null,
  neckCm: null,
  chestCm: null,
  waistCm,
  hipsCm: null,
  bicepsLeftCm: null,
  bicepsRightCm: null,
  forearmCm: null,
  thighLeftCm: null,
  thighRightCm: null,
  calfCm: null,
  notes: "",
});

describe("BodyPage (WF07)", () => {
  it("charts weight by default and offers only the measurements that have data", async () => {
    vi.mocked(endpoints.listMeasurements).mockResolvedValue({
      items: [measurement(3, "2026-11-22", 81.5, 84), measurement(2, "2026-11-15", 82.5), measurement(1, "2026-11-08", 83)],
      page: 1,
      pageSize: 100,
      totalCount: 3,
      totalPages: 1,
    });
    renderApp("/progress/body");
    const user = userEvent.setup();

    expect(await screen.findByRole("img", { name: "Weight over time" })).toBeInTheDocument();
    expect(screen.getByText(/Weight: 83 kg on/)).toHaveTextContent("now 81.5 kg (-1.5 kg)");
    const chips = within(screen.getByRole("group", { name: "Measurement to show" })).getAllByRole("button");
    expect(chips.map((c) => c.textContent)).toEqual(["Weight", "BMI", "Waist"]);

    await user.click(screen.getByRole("button", { name: "Waist" }));
    expect(screen.getByRole("img", { name: "Waist over time" })).toBeInTheDocument();
  });
});

describe("MeasurementFormPage (WF07)", () => {
  it("saves only what was filled in", async () => {
    vi.mocked(endpoints.createMeasurement).mockResolvedValue(measurement(9, "2026-11-23", 80.5));
    vi.mocked(endpoints.listMeasurements).mockResolvedValue({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 });
    renderApp("/progress/body/new");
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText("Weight (kg)"), "80,5");
    await user.type(screen.getByLabelText("Waist (cm)"), "84");
    await user.click(screen.getByRole("button", { name: "Save measurements" }));

    await waitFor(() => expect(endpoints.createMeasurement).toHaveBeenCalled());
    const request = vi.mocked(endpoints.createMeasurement).mock.calls[0]![0];
    expect(request).toMatchObject({ weightKg: 80.5, waistCm: 84, bodyFatPercent: null, calfCm: null });
  });

  it("an empty form or a wild value is not sent", async () => {
    renderApp("/progress/body/new");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Save measurements" }));
    expect(await screen.findByText("Enter at least one measurement.")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Waist (cm)"), "400");
    await user.click(screen.getByRole("button", { name: "Save measurements" }));
    expect(await screen.findByText("40–200 cm")).toBeInTheDocument();
    expect(endpoints.createMeasurement).not.toHaveBeenCalled();
  });
});
