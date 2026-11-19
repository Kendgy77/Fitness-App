import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Exercise, Paged } from "../api/types";
import { renderApp } from "../test/render";

vi.mock("../api/endpoints");

const benchPress: Exercise = {
  id: 1,
  name: "Barbell Bench Press",
  description: "Lie on the bench…",
  muscles: "Chest",
  category: "Chest",
  difficulty: "Intermediate",
  equipment: "Barbell",
  isCustom: false,
  isArchived: false,
};
const squat: Exercise = { ...benchPress, id: 36, name: "Barbell Back Squat", category: "Legs", description: "Rest the bar on your upper back…" };

function page(items: Exercise[]): Paged<Exercise> {
  return { items, page: 1, pageSize: 12, totalCount: items.length, totalPages: 1 };
}

describe("ExercisesPage (WF04)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.searchExercises).mockImplementation(async (query) =>
      page([benchPress, squat].filter((e) => !query.category || e.category === query.category)),
    );
  });

  it("T07: lists exercises with category, difficulty and description", async () => {
    renderApp("/exercises");

    expect(await screen.findByText("Barbell Bench Press")).toBeInTheDocument();
    expect(screen.getByText("Lie on the bench…")).toBeInTheDocument();
    expect(screen.getByText("Found: 2")).toBeInTheDocument();
  });

  it("T08: filtering by category asks the API for that category only", async () => {
    renderApp("/exercises");
    const user = userEvent.setup();
    await screen.findByText("Barbell Back Squat");

    await user.selectOptions(screen.getByLabelText("Category"), "Legs");

    await waitFor(() => expect(screen.queryByText("Barbell Bench Press")).not.toBeInTheDocument());
    expect(screen.getByText("Barbell Back Squat")).toBeInTheDocument();
    expect(endpoints.searchExercises).toHaveBeenLastCalledWith(expect.objectContaining({ category: "Legs", page: 1 }));
  });

  it("searches after the user stops typing", async () => {
    renderApp("/exercises");
    const user = userEvent.setup();
    await screen.findByText("Barbell Back Squat");

    await user.type(screen.getByLabelText("Search"), "squat");

    await waitFor(() => expect(endpoints.searchExercises).toHaveBeenLastCalledWith(expect.objectContaining({ search: "squat" })));
  });

  it("explains an empty result", async () => {
    vi.mocked(endpoints.searchExercises).mockResolvedValue(page([]));
    renderApp("/exercises?search=xyz");

    expect(await screen.findByText(/No exercises match/)).toBeInTheDocument();
  });
});
