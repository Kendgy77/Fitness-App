import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { getWorkout, logWorkout, updateWorkout } from "../api/endpoints";
import { toApiError, type ApiError } from "../api/errors";
import type { Exercise, ExerciseCategory, LogWorkoutRequest, SessionType, SetType, Workout } from "../api/types";
import { ExercisePicker } from "../components/ExercisePicker";
import { Alert, Button, Card, Loading, PageHeader, SelectField, TextAreaField, TextField } from "../components/ui";
import { categoryLabels, options, sessionTypeLabels, setTypeLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";
import { isCardio, toLocalInputValue } from "../lib/workout";

/**
 * A workout you already did (the thesis form, Fig. 6.3, test T11): date and time, type, duration, exercises and
 * their sets. /workouts/log enters a new one; /workouts/:id/edit corrects a finished one.
 */
export function WorkoutFormPage() {
  const idParam = useParams().id;
  const id = idParam ? Number(idParam) : null;
  const existing = useQuery({ queryKey: queryKeys.workout(id ?? 0), queryFn: () => getWorkout(id!), enabled: id !== null });

  return (
    <>
      <PageHeader title={id ? "Edit workout" : "Log a past workout"} />
      {id === null && <WorkoutForm />}
      {id !== null && existing.isPending && <Loading />}
      {existing.isError && <Alert>{toApiError(existing.error).message}</Alert>}
      {existing.data &&
        (existing.data.status === "Completed" ? (
          <WorkoutForm workout={existing.data} />
        ) : (
          <Alert kind="info">
            This workout isn't finished yet.{" "}
            <Link to="/workouts/live" className="font-semibold text-brand-700 hover:underline">
              Continue it
            </Link>
            .
          </Alert>
        ))}
    </>
  );
}

interface SetRow {
  key: number;
  type: SetType;
  weight: string;
  reps: string;
  minutes: string;
  km: string;
}

interface ExerciseRow {
  key: number;
  exerciseId: number;
  exerciseName: string;
  category: ExerciseCategory;
  notes: string;
  sets: SetRow[];
}

let nextKey = 1;

const emptySet = (): SetRow => ({ key: nextKey++, type: "Normal", weight: "", reps: "", minutes: "", km: "" });

function toRows(workout?: Workout): ExerciseRow[] {
  return (workout?.exercises ?? []).map((e) => ({
    key: nextKey++,
    exerciseId: e.exerciseId,
    exerciseName: e.exerciseName,
    category: e.category,
    notes: e.notes,
    sets: e.sets.map((s) => ({
      key: nextKey++,
      type: s.type,
      weight: s.weightKg?.toString() ?? "",
      reps: s.reps > 0 ? String(s.reps) : "",
      minutes: s.durationSeconds !== null ? String(Math.round(s.durationSeconds / 60)) : "",
      km: s.distanceMeters !== null ? String(s.distanceMeters / 1000) : "",
    })),
  }));
}

/** Now, rounded down to 5 minutes: a sensible default for "when did you train". */
function defaultStart() {
  const d = new Date();
  d.setMinutes(d.getMinutes() - (d.getMinutes() % 5), 0, 0);
  return toLocalInputValue(d);
}

function WorkoutForm({ workout }: { workout?: Workout }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [name, setName] = useState(workout?.name ?? "");
  const [type, setType] = useState<SessionType>(workout?.type ?? "Strength");
  const [startedAt, setStartedAt] = useState(workout?.startedAtUtc ? toLocalInputValue(new Date(workout.startedAtUtc)) : defaultStart());
  const [duration, setDuration] = useState(workout?.durationMinutes?.toString() ?? "60");
  const [notes, setNotes] = useState(workout?.notes ?? "");
  const [rows, setRows] = useState<ExerciseRow[]>(() => toRows(workout));
  const [pickerOpen, setPickerOpen] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const save = useMutation({
    mutationFn: (request: LogWorkoutRequest) => (workout ? updateWorkout(workout.id, request) : logWorkout(request)),
    onSuccess: async (saved) => {
      queryClient.setQueryData(queryKeys.workout(saved.id), saved);
      await queryClient.invalidateQueries({ queryKey: queryKeys.workouts, refetchType: "none" });
      await queryClient.invalidateQueries({ queryKey: queryKeys.progress, refetchType: "none" });
      navigate(`/workouts/${saved.id}`, { replace: true });
    },
    onError: (e) => setError(toApiError(e)),
  });

  function addExercise(exercise: Exercise) {
    setRows((current) => [
      ...current,
      { key: nextKey++, exerciseId: exercise.id, exerciseName: exercise.name, category: exercise.category, notes: "", sets: [emptySet()] },
    ]);
  }

  const updateRow = (key: number, change: (row: ExerciseRow) => ExerciseRow) =>
    setRows((current) => current.map((r) => (r.key === key ? change(r) : r)));

  const updateSet = (rowKey: number, setKey: number, change: Partial<SetRow>) =>
    updateRow(rowKey, (r) => ({ ...r, sets: r.sets.map((s) => (s.key === setKey ? { ...s, ...change } : s)) }));

  /** A new set copies the one before it: most sets repeat the same weight and reps. */
  const addSetRow = (rowKey: number) =>
    updateRow(rowKey, (r) => {
      const last = r.sets.at(-1);
      return { ...r, sets: [...r.sets, last ? { ...last, key: nextKey++, type: "Normal" } : emptySet()] };
    });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const fieldErrors: Record<string, string> = {};
    const num = (text: string) => (text.trim() === "" ? null : Number(text.replace(",", ".")));

    const minutes = Number(duration);
    if (!Number.isInteger(minutes) || minutes < 1 || minutes > 1440) fieldErrors.durationMinutes = "1–1440 minutes";
    const start = new Date(startedAt);
    if (!startedAt || Number.isNaN(start.getTime())) fieldErrors.startedAt = "Enter the date and time.";
    else if (start.getTime() > Date.now() + 5 * 60_000) fieldErrors.startedAt = "A finished workout can't be in the future.";
    // T12: at least one exercise, and every exercise with at least one set.
    if (rows.length === 0) fieldErrors.exercises = "Add at least one exercise with at least one set.";

    const exercises = rows.map((row, i) => {
      if (row.sets.length === 0) fieldErrors[`exercises[${i}].sets`] = "Add at least one set.";
      const cardio = isCardio(row.category);
      return {
        exerciseId: row.exerciseId,
        notes: row.notes,
        sets: row.sets.map((s, j) => {
          const at = `exercises[${i}].sets[${j}]`;
          const weight = num(s.weight);
          const reps = num(s.reps) ?? 0;
          const mins = num(s.minutes);
          const km = num(s.km);
          if (j === 0 && s.type === "Drop") fieldErrors[`${at}.type`] = "A drop set must follow another set.";
          if (cardio) {
            if (!mins && !km && !reps) fieldErrors[`${at}.durationSeconds`] = "Enter the time or distance.";
          } else {
            if (!Number.isInteger(reps) || reps < 1 || reps > 200) fieldErrors[`${at}.reps`] = "1–200";
            if (weight !== null && (Number.isNaN(weight) || weight < 0 || weight > 1000)) fieldErrors[`${at}.weightKg`] = "0–1000";
          }
          return {
            type: s.type,
            reps: Number.isFinite(reps) ? Math.round(reps) : 0,
            weightKg: cardio ? null : weight,
            rpe: null,
            durationSeconds: cardio && mins ? Math.round(mins * 60) : null,
            distanceMeters: cardio && km ? Math.round(km * 1000) : null,
          };
        }),
      };
    });

    if (Object.keys(fieldErrors).length > 0) {
      setError({ status: null, message: "Please fix the highlighted fields.", fieldErrors });
      return;
    }

    setError(null);
    save.mutate({ name: name.trim(), type, startedAt: start.toISOString(), durationMinutes: minutes, notes, exercises });
  }

  const fieldErrors = error?.fieldErrors ?? {};
  const errorAt = (i: number, j: number, field: string) => fieldErrors[`exercises[${i}].sets[${j}].${field}`];

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-6">
      {error && <Alert>{error.message}</Alert>}

      <Card className="grid gap-4 sm:grid-cols-2">
        <TextField label="Name" maxLength={100} value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Leg day" hint="Optional" />
        <SelectField label="Type" value={type} onChange={(e) => setType(e.target.value as SessionType)}>
          {options(sessionTypeLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </SelectField>
        <TextField label="Date and time" type="datetime-local" value={startedAt} onChange={(e) => setStartedAt(e.target.value)} error={fieldErrors.startedAt} />
        <TextField label="Duration (min)" inputMode="numeric" value={duration} onChange={(e) => setDuration(e.target.value)} error={fieldErrors.durationMinutes} />
        <TextAreaField label="Notes" className="sm:col-span-2" rows={2} maxLength={1000} value={notes} onChange={(e) => setNotes(e.target.value)} />
      </Card>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-bold text-slate-900">Exercises</h2>
          <Button variant="secondary" onClick={() => setPickerOpen(true)}>
            Add exercise
          </Button>
        </div>
        {fieldErrors.exercises && (
          <div className="mb-4">
            <Alert>{fieldErrors.exercises}</Alert>
          </div>
        )}
        {rows.length === 0 ? (
          <Alert kind="info">No exercises yet. Use "Add exercise" to pick them from the library.</Alert>
        ) : (
          <ol className="space-y-4">
            {rows.map((row, i) => {
              const cardio = isCardio(row.category);
              return (
                <li key={row.key} className="rounded-lg border border-slate-200 p-3">
                  <div className="flex items-start justify-between gap-2">
                    <p className="font-medium text-slate-900">
                      <span className="mr-2 text-sm text-slate-400">{i + 1}.</span>
                      {row.exerciseName}
                      <span className="ml-2 text-xs font-normal text-slate-500">{categoryLabels[row.category]}</span>
                    </p>
                    <button
                      type="button"
                      aria-label={`Remove ${row.exerciseName}`}
                      onClick={() => setRows((rs) => rs.filter((r) => r.key !== row.key))}
                      className="flex size-11 shrink-0 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100"
                    >
                      <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round">
                        <path d="M6 6l12 12M18 6L6 18" />
                      </svg>
                    </button>
                  </div>
                  {fieldErrors[`exercises[${i}].sets`] && (
                    <p className="mt-1 text-sm text-red-700">{fieldErrors[`exercises[${i}].sets`]}</p>
                  )}

                  <ol className="mt-2 space-y-3">
                    {row.sets.map((s, j) => (
                      // Two lines, so a phone has room: set number, type and delete; then the numbers.
                      <li key={s.key} aria-label={`${row.exerciseName} set ${j + 1}`} className="rounded-lg bg-slate-50 p-2">
                        <div className="flex items-end gap-2">
                          <span className="pb-3 text-sm font-semibold whitespace-nowrap text-slate-500">Set {j + 1}</span>
                          <SelectField
                            label="Type"
                            className="flex-1"
                            value={s.type}
                            onChange={(e) => updateSet(row.key, s.key, { type: e.target.value as SetType })}
                            error={errorAt(i, j, "type")}
                          >
                            {options(setTypeLabels).map(([value, label]) => (
                              <option key={value} value={value}>
                                {label}
                              </option>
                            ))}
                          </SelectField>
                          <button
                            type="button"
                            aria-label={`Delete ${row.exerciseName} set ${j + 1}`}
                            onClick={() => updateRow(row.key, (r) => ({ ...r, sets: r.sets.filter((x) => x.key !== s.key) }))}
                            className="flex size-11 shrink-0 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-red-700"
                          >
                            <svg viewBox="0 0 24 24" aria-hidden="true" className="size-4" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round">
                              <path d="M6 6l12 12M18 6L6 18" />
                            </svg>
                          </button>
                        </div>
                        <div className="mt-2 grid grid-cols-2 gap-2">
                        {cardio ? (
                          <>
                            <TextField label="Time (min)" inputMode="numeric" value={s.minutes} onChange={(e) => updateSet(row.key, s.key, { minutes: e.target.value })} error={errorAt(i, j, "durationSeconds")} />
                            <TextField label="Distance (km)" inputMode="decimal" value={s.km} onChange={(e) => updateSet(row.key, s.key, { km: e.target.value })} />
                          </>
                        ) : (
                          <>
                            <TextField label="Weight (kg)" inputMode="decimal" placeholder="—" value={s.weight} onChange={(e) => updateSet(row.key, s.key, { weight: e.target.value })} error={errorAt(i, j, "weightKg")} />
                            <TextField label="Reps" inputMode="numeric" value={s.reps} onChange={(e) => updateSet(row.key, s.key, { reps: e.target.value })} error={errorAt(i, j, "reps")} />
                          </>
                        )}
                        </div>
                      </li>
                    ))}
                  </ol>
                  <Button variant="secondary" className="mt-3" onClick={() => addSetRow(row.key)}>
                    Add set
                  </Button>
                </li>
              );
            })}
          </ol>
        )}
      </Card>

      <div className="flex flex-wrap gap-3">
        <Button type="submit" busy={save.isPending}>
          Save workout
        </Button>
        <Link
          to={workout ? `/workouts/${workout.id}` : "/workouts"}
          className="inline-flex min-h-11 items-center rounded-lg px-4 text-sm font-semibold text-slate-700 hover:bg-slate-100"
        >
          Cancel
        </Link>
      </div>

      {pickerOpen && <ExercisePicker showSuggestions onPick={addExercise} onClose={() => setPickerOpen(false)} />}
    </form>
  );
}
