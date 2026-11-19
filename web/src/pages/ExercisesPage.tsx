import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router";
import { searchExercises } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { Difficulty, Equipment, Exercise, ExerciseCategory, ExerciseQuery } from "../api/types";
import { Alert, Badge, Button, Loading, PageHeader, SelectField, TextField } from "../components/ui";
import { categoryLabels, difficultyLabels, equipmentLabels, options } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

const pageSize = 12;

/**
 * WF04: the exercise library (thesis mock-up 6.4) with search and filters (T07, T08). The filters live in the
 * URL (?category=Legs&page=2), so the back button and bookmarks keep them.
 */
export function ExercisesPage() {
  const [params, setParams] = useSearchParams();
  const query: ExerciseQuery = {
    search: params.get("search") || undefined,
    category: (params.get("category") as ExerciseCategory | null) || undefined,
    difficulty: (params.get("difficulty") as Difficulty | null) || undefined,
    equipment: (params.get("equipment") as Equipment | null) || undefined,
    onlyCustom: params.get("onlyCustom") === "true" || undefined,
    page: Number(params.get("page")) || 1,
    pageSize,
  };

  const exercises = useQuery({
    queryKey: queryKeys.exerciseList(query),
    queryFn: () => searchExercises(query),
    // Keep showing the current page while the next one loads, instead of flashing a spinner.
    placeholderData: keepPreviousData,
  });

  /** Changing a filter starts again from page 1; an empty value removes the filter. */
  function setFilter(name: string, value: string) {
    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        if (value) next.set(name, value);
        else next.delete(name);
        if (name !== "page") next.delete("page");
        return next;
      },
      { replace: name === "search" },
    );
  }

  const data = exercises.data;

  return (
    <>
      <PageHeader
        title="Exercise library"
        actions={
          <Link
            to="/exercises/new"
            className="inline-flex min-h-11 items-center rounded-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800"
          >
            Add your own exercise
          </Link>
        }
      />

      <div className="mb-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <SearchBox initial={query.search ?? ""} onSearch={(text) => setFilter("search", text)} />
        <SelectField label="Category" value={query.category ?? ""} onChange={(e) => setFilter("category", e.target.value)}>
          <option value="">All</option>
          {options(categoryLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </SelectField>
        <SelectField label="Difficulty" value={query.difficulty ?? ""} onChange={(e) => setFilter("difficulty", e.target.value)}>
          <option value="">All</option>
          {options(difficultyLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </SelectField>
        <SelectField label="Equipment" value={query.equipment ?? ""} onChange={(e) => setFilter("equipment", e.target.value)}>
          <option value="">Any</option>
          {options(equipmentLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </SelectField>
        <label className="flex min-h-11 items-center gap-2 text-sm text-slate-700 sm:col-span-2 lg:col-span-4">
          <input
            type="checkbox"
            className="size-5 accent-brand-700"
            checked={query.onlyCustom ?? false}
            onChange={(e) => setFilter("onlyCustom", e.target.checked ? "true" : "")}
          />
          Only my exercises
        </label>
      </div>

      {exercises.isPending && <Loading />}
      {exercises.isError && <Alert>{toApiError(exercises.error).message}</Alert>}
      {data && (
        <>
          <p className="mb-3 text-sm text-slate-600" aria-live="polite">
            Found: {data.totalCount}
          </p>
          {data.items.length === 0 ? (
            <Alert kind="info">No exercises match. Change the filters or add your own exercise.</Alert>
          ) : (
            <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              {data.items.map((exercise) => (
                <li key={exercise.id}>
                  <ExerciseCard exercise={exercise} />
                </li>
              ))}
            </ul>
          )}
          {data.totalPages > 1 && (
            <nav aria-label="Pages" className="mt-6 flex items-center justify-between gap-3">
              <Button variant="secondary" disabled={data.page <= 1} onClick={() => setFilter("page", String(data.page - 1))}>
                Previous
              </Button>
              <span className="text-sm text-slate-600">
                Page {data.page} of {data.totalPages}
              </span>
              <Button
                variant="secondary"
                disabled={data.page >= data.totalPages}
                onClick={() => setFilter("page", String(data.page + 1))}
              >
                Next
              </Button>
            </nav>
          )}
        </>
      )}
    </>
  );
}

/** Searches 300 ms after the user stops typing, so not every keystroke is a request. */
function SearchBox({ initial, onSearch }: { initial: string; onSearch: (text: string) => void }) {
  const [text, setText] = useState(initial);

  useEffect(() => {
    if (text.trim() === initial) return;
    const timer = setTimeout(() => onSearch(text.trim()), 300);
    return () => clearTimeout(timer);
  }, [text, initial, onSearch]);

  return (
    <TextField
      label="Search"
      type="search"
      placeholder="Name or muscle, e.g. squat"
      value={text}
      onChange={(e) => setText(e.target.value)}
    />
  );
}

function ExerciseCard({ exercise }: { exercise: Exercise }) {
  return (
    <Link
      to={`/exercises/${exercise.id}`}
      className="flex h-full flex-col rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand-500 hover:shadow"
    >
      <div className="mb-2 flex flex-wrap gap-1.5">
        <Badge tone="brand">{categoryLabels[exercise.category]}</Badge>
        <Badge>{difficultyLabels[exercise.difficulty]}</Badge>
        {exercise.isCustom && <Badge tone="amber">Custom</Badge>}
      </div>
      <h2 className="font-semibold text-slate-900">{exercise.name}</h2>
      <p className="mt-1 line-clamp-3 text-sm text-slate-600">{exercise.description || exercise.muscles}</p>
      <p className="mt-auto pt-3 text-xs text-slate-500">{equipmentLabels[exercise.equipment]}</p>
    </Link>
  );
}
