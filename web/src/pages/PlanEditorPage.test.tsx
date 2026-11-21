import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Exercise, Plan } from "../api/types";
import { apiError, renderApp } from "../test/render";

vi.mock("../api/endpoints");

const exercise = (id: number, name: string): Exercise => ({
  id,
  name,
  description: "",
  muscles: "",
  category: "Chest",
  difficulty: "Beginner",
  equipment: "Barbell",
  isCustom: false,
  isArchived: false,
});
const benchPress = exercise(1, "Barbell Bench Press");
const squat = exercise(36, "Barbell Back Squat");

const savedPlan: Plan = {
  id: 1000,
  name: "Push day",
  description: "",
  status: "Draft",
  isTemplate: false,
  createdAtUtc: "2026-11-20T10:00:00Z",
  updatedAtUtc: "2026-11-20T10:00:00Z",
  exercises: [],
};

async function addFromPicker(user: ReturnType<typeof userEvent.setup>, ...names: string[]) {
  await user.click(screen.getByRole("button", { name: "Add exercise" }));
  const dialog = await screen.findByRole("dialog", { name: "Add exercises" });
  for (const name of names) {
    await user.click(await within(dialog).findByRole("button", { name: `Add ${name}` }));
  }
  await user.click(within(dialog).getByRole("button", { name: "Done" }));
}

describe("PlanEditorPage (WF05)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.searchExercises).mockResolvedValue({ items: [benchPress, squat], page: 1, pageSize: 30, totalCount: 2, totalPages: 1 });
    vi.mocked(endpoints.createPlan).mockResolvedValue(savedPlan);
    vi.mocked(endpoints.getPlan).mockResolvedValue(savedPlan);
  });

  it("T09: creates a plan with the exercises picked from the library, in order", async () => {
    renderApp("/plans/new");
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText("Plan name"), "Push day");
    await addFromPicker(user, "Barbell Bench Press", "Barbell Back Squat");
    await user.click(screen.getByRole("button", { name: "Save plan" }));

    await waitFor(() =>
      expect(endpoints.createPlan).toHaveBeenCalledWith({
        name: "Push day",
        description: "",
        exercises: [
          { exerciseId: 1, targetSets: 3, targetRepsMin: 8, targetRepsMax: 12, targetWeightKg: null, notes: "" },
          { exerciseId: 36, targetSets: 3, targetRepsMin: 8, targetRepsMax: 12, targetWeightKg: null, notes: "" },
        ],
      }),
    );
  });

  it("T10: a plan without a name is not sent and the field says why", async () => {
    renderApp("/plans/new");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Save plan" }));

    expect(await screen.findByText("Enter the plan name.")).toBeInTheDocument();
    expect(screen.getByLabelText("Plan name")).toHaveAttribute("aria-invalid", "true");
    expect(endpoints.createPlan).not.toHaveBeenCalled();
  });

  it("moves an exercise up and removes one", async () => {
    renderApp("/plans/new");
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText("Plan name"), "Day A");
    await addFromPicker(user, "Barbell Bench Press", "Barbell Back Squat");

    await user.click(screen.getByRole("button", { name: "Move Barbell Back Squat up" }));
    await user.click(screen.getByRole("button", { name: "Remove Barbell Bench Press" }));
    await user.click(screen.getByRole("button", { name: "Save plan" }));

    await waitFor(() =>
      expect(endpoints.createPlan).toHaveBeenCalledWith(expect.objectContaining({ exercises: [expect.objectContaining({ exerciseId: 36 })] })),
    );
  });

  it("checks sets and reps before sending", async () => {
    renderApp("/plans/new");
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText("Plan name"), "Day A");
    await addFromPicker(user, "Barbell Bench Press");

    const sets = screen.getByLabelText("Sets");
    await user.clear(sets);
    await user.type(sets, "0");
    await user.click(screen.getByRole("button", { name: "Save plan" }));

    expect(await screen.findByText("1–20")).toBeInTheDocument();
    expect(endpoints.createPlan).not.toHaveBeenCalled();
  });

  it("shows the API's error next to the right row", async () => {
    vi.mocked(endpoints.createPlan).mockRejectedValue(
      apiError(400, { title: "One or more validation errors occurred.", errors: { "Exercises[0].TargetRepsMax": ["The top of the rep range can't be lower than the bottom."] } }),
    );
    renderApp("/plans/new");
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText("Plan name"), "Day A");
    await addFromPicker(user, "Barbell Bench Press");

    await user.click(screen.getByRole("button", { name: "Save plan" }));

    expect(await screen.findByText("The top of the rep range can't be lower than the bottom.")).toBeInTheDocument();
  });

  it("loads an existing plan for editing", async () => {
    vi.mocked(endpoints.getPlan).mockResolvedValue({
      ...savedPlan,
      exercises: [
        { order: 1, exerciseId: 1, exerciseName: "Barbell Bench Press", category: "Chest", equipment: "Barbell", exerciseIsArchived: false, targetSets: 5, targetRepsMin: 5, targetRepsMax: 5, targetWeightKg: 80, notes: "" },
      ],
    });
    renderApp("/plans/1000/edit");

    expect(await screen.findByDisplayValue("Push day")).toBeInTheDocument();
    expect(screen.getByLabelText("Weight (kg)")).toHaveValue("80");
    // A fixed number shows in "from" only.
    expect(screen.getByLabelText("Reps from")).toHaveValue("5");
    expect(screen.getByLabelText("Reps to")).toHaveValue("");
  });

  it("an empty 'Reps to' saves a fixed number; an upside-down range is not sent", async () => {
    renderApp("/plans/new");
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText("Plan name"), "Day A");
    await addFromPicker(user, "Barbell Bench Press");

    await user.clear(screen.getByLabelText("Reps from"));
    await user.type(screen.getByLabelText("Reps from"), "12");
    await user.clear(screen.getByLabelText("Reps to"));
    await user.type(screen.getByLabelText("Reps to"), "8");
    await user.click(screen.getByRole("button", { name: "Save plan" }));

    expect(await screen.findByText("At least 12")).toBeInTheDocument();
    expect(endpoints.createPlan).not.toHaveBeenCalled();

    await user.clear(screen.getByLabelText("Reps to"));
    await user.click(screen.getByRole("button", { name: "Save plan" }));

    await waitFor(() =>
      expect(endpoints.createPlan).toHaveBeenCalledWith(
        expect.objectContaining({ exercises: [expect.objectContaining({ targetRepsMin: 12, targetRepsMax: 12 })] }),
      ),
    );
  });
});
