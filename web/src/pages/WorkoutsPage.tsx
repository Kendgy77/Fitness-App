import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router";
import { getActiveWorkout, listWorkouts } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { Exercise, WorkoutQuery } from "../api/types";
import { ExercisePicker } from "../components/ExercisePicker";
import { Alert, Badge, Button, Card, Loading, PageHeader } from "../components/ui";
import { formatDateTime, formatMinutes, sessionStatusLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";
import { localDayKey, monthGrid } from "../lib/workout";

const monthTitle = new Intl.DateTimeFormat("en-GB", { month: "long", year: "numeric" });
const dayLabel = new Intl.DateTimeFormat("en-GB", { weekday: "long", day: "numeric", month: "long" });
const weekdays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

/**
 * Workout history: a month calendar with the training days marked, a day and an exercise filter ("every workout
 * with bench press"), and the list, newest first. Days are the user's own local days.
 */
export function WorkoutsPage() {
  const today = new Date();
  const [month, setMonth] = useState({ year: today.getFullYear(), month: today.getMonth() });
  const [day, setDay] = useState<Date | null>(null);
  const [exercise, setExercise] = useState<Pick<Exercise, "id" | "name"> | null>(null);
  const [page, setPage] = useState(1);
  const [pickerOpen, setPickerOpen] = useState(false);

  const active = useQuery({ queryKey: queryKeys.activeWorkout, queryFn: getActiveWorkout });

  // The calendar asks for the whole month (a month never has 100 workouts); the list is paged.
  const monthQuery: WorkoutQuery = {
    from: new Date(month.year, month.month, 1).toISOString(),
    to: new Date(month.year, month.month + 1, 1).toISOString(),
    exerciseId: exercise?.id,
    pageSize: 100,
  };
  const monthData = useQuery({
    queryKey: queryKeys.workoutList(monthQuery),
    queryFn: () => listWorkouts(monthQuery),
    placeholderData: keepPreviousData,
  });
  const trainedDays = new Map<string, number>();
  for (const w of monthData.data?.items ?? []) {
    const at = w.startedAtUtc ?? w.scheduledAtUtc;
    if (at) trainedDays.set(localDayKey(at), (trainedDays.get(localDayKey(at)) ?? 0) + 1);
  }

  const listQuery: WorkoutQuery = {
    from: day ? new Date(day.getFullYear(), day.getMonth(), day.getDate()).toISOString() : undefined,
    to: day ? new Date(day.getFullYear(), day.getMonth(), day.getDate() + 1).toISOString() : undefined,
    exerciseId: exercise?.id,
    page,
    pageSize: 20,
  };
  const list = useQuery({ queryKey: queryKeys.workoutList(listQuery), queryFn: () => listWorkouts(listQuery), placeholderData: keepPreviousData });

  function moveMonth(by: number) {
    const d = new Date(month.year, month.month + by, 1);
    setMonth({ year: d.getFullYear(), month: d.getMonth() });
  }

  function chooseDay(d: Date) {
    setDay(day && localDayKey(day) === localDayKey(d) ? null : d);
    setPage(1);
  }

  return (
    <>
      <PageHeader
        title="Workouts"
        actions={
          <Link to="/workouts/new" className="inline-flex min-h-11 items-center rounded-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800">
            New workout
          </Link>
        }
      />

      {active.data && (
        <div className="mb-4">
          <Alert kind="success">
            <span className="font-semibold">{active.data.name}</span> is in progress.{" "}
            <Link to="/workouts/live" className="font-semibold underline">
              Continue workout
            </Link>
          </Alert>
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-[20rem_1fr] lg:items-start">
        <Card>
          <div className="mb-3 flex items-center justify-between">
            <Button variant="secondary" aria-label="Previous month" onClick={() => moveMonth(-1)}>
              ‹
            </Button>
            <h2 className="font-semibold text-slate-900">{monthTitle.format(new Date(month.year, month.month, 1))}</h2>
            <Button variant="secondary" aria-label="Next month" onClick={() => moveMonth(1)}>
              ›
            </Button>
          </div>
          <table className="w-full table-fixed text-center text-sm">
            <thead>
              <tr>
                {weekdays.map((w) => (
                  <th key={w} scope="col" className="pb-1 text-xs font-medium text-slate-500">
                    {w}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {monthGrid(month.year, month.month).map((week, i) => (
                <tr key={i}>
                  {week.map((d, j) => {
                    if (!d) return <td key={j} />;
                    const key = localDayKey(d);
                    const count = trainedDays.get(key) ?? 0;
                    const selected = day !== null && localDayKey(day) === key;
                    const isToday = key === localDayKey(today);
                    return (
                      <td key={j} className="p-0.5">
                        <button
                          type="button"
                          aria-pressed={selected}
                          aria-label={`${dayLabel.format(d)}${count ? `, ${count} workout${count > 1 ? "s" : ""}` : ""}`}
                          onClick={() => chooseDay(d)}
                          className={`flex aspect-square w-full items-center justify-center rounded-lg font-medium ${
                            selected
                              ? "bg-slate-900 text-white"
                              : count
                                ? "bg-brand-600 text-white hover:bg-brand-700"
                                : `text-slate-700 hover:bg-slate-100 ${isToday ? "ring-1 ring-slate-400" : ""}`
                          }`}
                        >
                          {d.getDate()}
                        </button>
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
          <p className="mt-3 text-xs text-slate-500">Green days have a workout. Tap a day to see only that day.</p>
        </Card>

        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-2">
            {day && (
              <FilterChip label={dayLabel.format(day)} onClear={() => setDay(null)} />
            )}
            {exercise ? (
              <FilterChip label={exercise.name} onClear={() => { setExercise(null); setPage(1); }} />
            ) : (
              <Button variant="secondary" onClick={() => setPickerOpen(true)}>
                Filter by exercise
              </Button>
            )}
          </div>

          {list.isPending && <Loading />}
          {list.isError && <Alert>{toApiError(list.error).message}</Alert>}
          {list.data &&
            (list.data.items.length === 0 ? (
              <Alert kind="info">
                {day || exercise ? "No workouts match." : "No workouts yet. Start your first one with \"New workout\"."}
              </Alert>
            ) : (
              <ul className="space-y-3">
                {list.data.items.map((w) => (
                  <li key={w.id}>
                    <Link to={`/workouts/${w.id}`} className="block rounded-xl border border-slate-200 bg-white p-4 shadow-sm hover:border-brand-500">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <span className="font-semibold text-slate-900">{w.name}</span>
                        {w.status !== "Completed" && <Badge tone="amber">{sessionStatusLabels[w.status]}</Badge>}
                      </div>
                      <p className="text-sm text-slate-600">
                        {(w.startedAtUtc ?? w.scheduledAtUtc) && formatDateTime((w.startedAtUtc ?? w.scheduledAtUtc)!)}
                        {w.durationMinutes !== null && ` · ${formatMinutes(w.durationMinutes)}`}
                        {` · ${w.setCount} sets`}
                      </p>
                      {w.exerciseNames.length > 0 && (
                        <p className="mt-1 truncate text-sm text-slate-500">
                          {w.exerciseNames.join(", ")}
                          {w.exerciseCount > w.exerciseNames.length && ` +${w.exerciseCount - w.exerciseNames.length} more`}
                        </p>
                      )}
                    </Link>
                  </li>
                ))}
              </ul>
            ))}

          {list.data && list.data.totalPages > 1 && (
            <div className="flex items-center justify-between gap-3">
              <Button variant="secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Newer
              </Button>
              <span className="text-sm text-slate-600">
                Page {page} of {list.data.totalPages}
              </span>
              <Button variant="secondary" disabled={page >= list.data.totalPages} onClick={() => setPage((p) => p + 1)}>
                Older
              </Button>
            </div>
          )}
        </div>
      </div>

      {pickerOpen && (
        <ExercisePicker
          title="Filter by exercise"
          showSuggestions
          onPick={(e) => {
            setExercise({ id: e.id, name: e.name });
            setPage(1);
            setPickerOpen(false);
          }}
          onClose={() => setPickerOpen(false)}
        />
      )}
    </>
  );
}

function FilterChip({ label, onClear }: { label: string; onClear: () => void }) {
  return (
    <span className="inline-flex min-h-11 items-center gap-1 rounded-full bg-slate-900 pr-1 pl-4 text-sm font-medium text-white">
      {label}
      <button type="button" aria-label={`Remove filter ${label}`} onClick={onClear} className="flex size-9 items-center justify-center rounded-full hover:bg-white/20">
        ×
      </button>
    </span>
  );
}
