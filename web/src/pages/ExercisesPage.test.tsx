import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Exercise, Paged } from "../api/types";
import { renderApp } from "../test/render";

vi.mock("../api/endpoints");

const benchPress: Exercise = {
  id: 1,
  name: "Wyciskanie sztangi na ławce płaskiej",
  description: "Połóż się na ławce…",
  muscles: "Klatka piersiowa",
  category: "Chest",
  difficulty: "Intermediate",
  equipment: "Barbell",
  isCustom: false,
  isArchived: false,
};
const squat: Exercise = { ...benchPress, id: 36, name: "Przysiad ze sztangą na plecach", category: "Legs", description: "Połóż sztangę…" };

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
    renderApp("/cwiczenia");

    expect(await screen.findByText("Wyciskanie sztangi na ławce płaskiej")).toBeInTheDocument();
    expect(screen.getByText("Połóż się na ławce…")).toBeInTheDocument();
    expect(screen.getByText("Znaleziono: 2")).toBeInTheDocument();
  });

  it("T08: filtering by category asks the API for that category only", async () => {
    renderApp("/cwiczenia");
    const user = userEvent.setup();
    await screen.findByText("Przysiad ze sztangą na plecach");

    await user.selectOptions(screen.getByLabelText("Kategoria"), "Legs");

    await waitFor(() => expect(screen.queryByText("Wyciskanie sztangi na ławce płaskiej")).not.toBeInTheDocument());
    expect(screen.getByText("Przysiad ze sztangą na plecach")).toBeInTheDocument();
    expect(endpoints.searchExercises).toHaveBeenLastCalledWith(expect.objectContaining({ category: "Legs", page: 1 }));
  });

  it("searches after the user stops typing", async () => {
    renderApp("/cwiczenia");
    const user = userEvent.setup();
    await screen.findByText("Przysiad ze sztangą na plecach");

    await user.type(screen.getByLabelText("Szukaj"), "ławka");

    await waitFor(() => expect(endpoints.searchExercises).toHaveBeenLastCalledWith(expect.objectContaining({ search: "ławka" })));
  });

  it("explains an empty result", async () => {
    vi.mocked(endpoints.searchExercises).mockResolvedValue(page([]));
    renderApp("/cwiczenia?search=xyz");

    expect(await screen.findByText(/Brak ćwiczeń spełniających kryteria/)).toBeInTheDocument();
  });
});
