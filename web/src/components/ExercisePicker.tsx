import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { getExerciseSuggestions, searchExercises } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { Exercise, ExerciseCategory } from "../api/types";
import { categoryLabels, equipmentLabels, options } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";
import { Alert, Button, Loading, SelectField, TextField } from "./ui";

/**
 * A searchable list of the exercise library in a dialog (thesis: creating a plan includes browsing the library,
 * UC005 «include» UC004). Picking an exercise adds it and keeps the dialog open, so several can be added in a row.
 */
export function ExercisePicker({
  onPick,
  onClose,
  title = "Add exercises",
  showSuggestions = false,
}: {
  onPick: (exercise: Exercise) => void;
  onClose: () => void;
  title?: string;
  /** Workouts: the user's most-used and recent exercises on top, before the whole library. */
  showSuggestions?: boolean;
}) {
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [category, setCategory] = useState<ExerciseCategory | "">("");
  const [added, setAdded] = useState<string | null>(null);
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(search.trim()), 300);
    return () => clearTimeout(timer);
  }, [search]);

  // Focus starts inside the dialog, so keyboard users land in the search box; Escape closes it.
  // onClose is read through a ref, so this runs once instead of on every render (which would steal focus).
  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  });
  useEffect(() => {
    dialogRef.current?.querySelector("input")?.focus();
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onCloseRef.current();
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const query = { search: debounced || undefined, category: category || undefined, page: 1, pageSize: 30 };
  const results = useQuery({
    queryKey: queryKeys.exerciseList(query),
    queryFn: () => searchExercises(query),
    placeholderData: keepPreviousData,
  });

  const browsing = !debounced && !category;
  const suggestions = useQuery({
    queryKey: queryKeys.exerciseSuggestions,
    queryFn: getExerciseSuggestions,
    enabled: showSuggestions,
  });

  function pick(exercise: Exercise) {
    onPick(exercise);
    setAdded(exercise.name);
  }

  const row = (exercise: Exercise, key: string) => (
    <li key={key} className="flex items-center justify-between gap-3 py-2">
      <div className="min-w-0">
        <p className="truncate font-medium text-slate-900">{exercise.name}</p>
        <p className="text-xs text-slate-500">
          {categoryLabels[exercise.category]} · {equipmentLabels[exercise.equipment]}
        </p>
      </div>
      <Button variant="secondary" aria-label={`Add ${exercise.name}`} onClick={() => pick(exercise)}>
        Add
      </Button>
    </li>
  );

  const section = (heading: string, exercises: Exercise[], prefix: string) =>
    exercises.length > 0 && (
      <div className="mb-3">
        <h3 className="pt-2 text-xs font-semibold tracking-wide text-slate-500 uppercase">{heading}</h3>
        <ul className="divide-y divide-slate-100">{exercises.map((e) => row(e, `${prefix}-${e.id}`))}</ul>
      </div>
    );

  return (
    <div className="fixed inset-0 z-40 flex items-end justify-center bg-slate-900/40 sm:items-center sm:p-4" onClick={onClose}>
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="picker-title"
        className="flex max-h-[90dvh] w-full max-w-lg flex-col rounded-t-2xl bg-white shadow-xl sm:rounded-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between gap-3 border-b border-slate-100 px-4 py-3">
          <h2 id="picker-title" className="text-lg font-bold text-slate-900">
            {title}
          </h2>
          <Button variant="secondary" onClick={onClose}>
            Done
          </Button>
        </div>

        <div className="grid gap-3 px-4 pt-3 sm:grid-cols-2">
          <TextField label="Search" type="search" placeholder="e.g. squat" value={search} onChange={(e) => setSearch(e.target.value)} />
          <SelectField label="Category" value={category} onChange={(e) => setCategory(e.target.value as ExerciseCategory | "")}>
            <option value="">All</option>
            {options(categoryLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
        </div>
        <p className="min-h-6 px-4 pt-2 text-sm text-brand-800" aria-live="polite">
          {added && `Added: ${added}`}
        </p>

        <div className="min-h-0 flex-1 overflow-y-auto px-4 pb-4">
          {showSuggestions && browsing && suggestions.data && (
            <>
              {section("Most used", suggestions.data.mostUsed, "most")}
              {section("Recently used", suggestions.data.recent, "recent")}
            </>
          )}
          {showSuggestions && browsing && (suggestions.data?.mostUsed.length || suggestions.data?.recent.length) ? (
            <h3 className="pt-2 text-xs font-semibold tracking-wide text-slate-500 uppercase">All exercises</h3>
          ) : null}
          {results.isPending && <Loading />}
          {results.isError && <Alert>{toApiError(results.error).message}</Alert>}
          {results.data &&
            (results.data.items.length === 0 ? (
              <Alert kind="info">No exercises match.</Alert>
            ) : (
              <ul className="divide-y divide-slate-100">{results.data.items.map((e) => row(e, `all-${e.id}`))}</ul>
            ))}
        </div>
      </div>
    </div>
  );
}
