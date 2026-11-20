import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Paged, Plan, PlanSummary } from "../api/types";
import { renderApp } from "../test/render";

vi.mock("../api/endpoints");

const summary = (id: number, name: string, status: PlanSummary["status"], isTemplate = false): PlanSummary => ({
  id,
  name,
  description: `${name} description`,
  status,
  isTemplate,
  exerciseCount: 6,
  updatedAtUtc: "2026-11-20T10:00:00Z",
});

const paged = (items: PlanSummary[]): Paged<PlanSummary> => ({ items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 });

const plan = (status: Plan["status"], isTemplate = false): Plan => ({
  id: 1000,
  name: "Push day",
  description: "",
  status,
  isTemplate,
  createdAtUtc: "2026-11-20T10:00:00Z",
  updatedAtUtc: "2026-11-20T10:00:00Z",
  exercises: [
    { order: 1, exerciseId: 1, exerciseName: "Barbell Bench Press", category: "Chest", equipment: "Barbell", exerciseIsArchived: false, targetSets: 4, targetReps: 8, targetWeightKg: 60, notes: "" },
  ],
});

describe("PlansPage (WF05)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.listPlans).mockImplementation(async (q) =>
      paged([summary(1000, "Push day", "Active"), summary(1001, "Leg day", "Draft")].filter((p) => !q.status || p.status === q.status)),
    );
    vi.mocked(endpoints.listPlanTemplates).mockResolvedValue(paged([summary(1, "Full Body – Beginner", "Active", true)]));
  });

  it("lists the user's plans and the templates", async () => {
    renderApp("/plans");

    expect(await screen.findByText("Push day")).toBeInTheDocument();
    expect(screen.getByText("Leg day")).toBeInTheDocument();
    expect(await screen.findByText("Full Body – Beginner")).toBeInTheDocument();
  });

  it("filters by status", async () => {
    renderApp("/plans");
    const user = userEvent.setup();
    await screen.findByText("Leg day");

    await user.click(screen.getByRole("button", { name: "Drafts" }));

    await waitFor(() => expect(screen.queryByText("Push day")).not.toBeInTheDocument());
    expect(endpoints.listPlans).toHaveBeenLastCalledWith(expect.objectContaining({ status: "Draft" }));
  });

  it("using a template copies it and opens the copy in the editor", async () => {
    vi.mocked(endpoints.copyPlan).mockResolvedValue({ ...plan("Draft"), id: 1005, name: "Full Body – Beginner" });
    vi.mocked(endpoints.getPlan).mockResolvedValue({ ...plan("Draft"), id: 1005, name: "Full Body – Beginner" });
    vi.mocked(endpoints.searchExercises).mockResolvedValue({ items: [], page: 1, pageSize: 30, totalCount: 0, totalPages: 0 });
    renderApp("/plans");
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Use template" }));

    expect(endpoints.copyPlan).toHaveBeenCalledWith(1);
    expect(await screen.findByRole("heading", { name: "Edit plan" })).toBeInTheDocument();
  });
});

describe("PlanDetailPage (WF05)", () => {
  it("a draft can be activated; the buttons follow the status rules", async () => {
    vi.mocked(endpoints.getPlan).mockResolvedValue(plan("Draft"));
    vi.mocked(endpoints.changePlanStatus).mockResolvedValue(plan("Active"));
    renderApp("/plans/1000");
    const user = userEvent.setup();

    expect(await screen.findByText("4 × 8")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Delete draft" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Activate" }));

    expect(endpoints.changePlanStatus).toHaveBeenCalledWith(1000, "Active");
    expect(await screen.findByRole("button", { name: "Archive" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete draft" })).not.toBeInTheDocument();
  });

  it("an archived plan can only be restored or copied", async () => {
    vi.mocked(endpoints.getPlan).mockResolvedValue(plan("Archived"));
    renderApp("/plans/1000");

    expect(await screen.findByRole("button", { name: "Restore as draft" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Make a copy" })).toBeInTheDocument();
  });

  it("a template offers only a copy", async () => {
    vi.mocked(endpoints.getPlan).mockResolvedValue(plan("Active", true));
    renderApp("/plans/1");

    expect(await screen.findByRole("button", { name: "Use template" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Archive" })).not.toBeInTheDocument();
  });
});
