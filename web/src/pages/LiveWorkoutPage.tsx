import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";
import {
  addSet,
  addWorkoutExercise,
  changeWorkoutStatus,
  deleteSet,
  deleteWorkout,
  finishWorkout,
  getActiveWorkout,
  removeWorkoutExercise,
} from "../api/endpoints";
import { errorMessage, toApiError } from "../api/errors";
import type { SetRequest, SetType, Workout, WorkoutExercise } from "../api/types";
import { ExercisePicker } from "../components/ExercisePicker";
import { Alert, Badge, Button, Card, Loading, PageHeader, TextAreaField, TextField } from "../components/ui";
import { sessionStatusLabels, setTypeLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";
import {
  WEIGHT_STEP,
  describeSet,
  describeTarget,
  dropSetWeight,
  elapsedSeconds,
  formatClock,
  isCardio,
  prefillSet,
  stepValue,
  type SetDraft,
} from "../lib/workout";

/**
 * The live workout at the gym (WF06), made for one thumb: big buttons, ± steppers, the previous set prefilled.
 * Every set is saved to the API straight away, so closing the tab or losing the phone signal between sets
 * loses nothing: opening the page again picks up the active workout.
 */
export function LiveWorkoutPage() {
  const active = useQuery({ queryKey: queryKeys.activeWorkout, queryFn: getActiveWorkout });

  if (active.isPending) return <Loading />;
  if (active.isError) return <Alert>{toApiError(active.error).message}</Alert>;
  if (!active.data) {
    return (
      <>
        <PageHeader title="Workout" />
        <Alert kind="info">
          No workout in progress.{" "}
          <Link to="/workouts/new" className="font-semibold text-brand-700 hover:underline">
            Start one
          </Link>
          .
        </Alert>
      </>
    );
  }
  return <LiveWorkout workout={active.data} />;
}

/** Ticks once a second, for the workout clock and the rest timer. */
function useNow() {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  return now;
}

function LiveWorkout({ workout }: { workout: Workout }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const now = useNow();
  const [pickerOpen, setPickerOpen] = useState(false);
  const [finishOpen, setFinishOpen] = useState(false);
  // The exercise being worked on is open; the others show a one-line summary. Starts at the first unfinished one.
  const [openId, setOpenId] = useState<number | null>(() => firstUnfinished(workout)?.id ?? null);
  const [restDismissedFor, setRestDismissedFor] = useState<number | null>(null);

  const show = (updated: Workout) => queryClient.setQueryData(queryKeys.activeWorkout, updated);

  const status = useMutation({
    mutationFn: (action: "pause" | "resume") => changeWorkoutStatus(workout.id, action),
    onSuccess: show,
  });
  const addExercise = useMutation({
    mutationFn: (exerciseId: number) => addWorkoutExercise(workout.id, exerciseId),
    onSuccess: (updated) => {
      show(updated);
      setOpenId(updated.exercises.at(-1)?.id ?? null);
    },
  });
  const removeExercise = useMutation({
    mutationFn: (entryId: number) => removeWorkoutExercise(workout.id, entryId),
    onSuccess: show,
  });
  const discard = useMutation({
    mutationFn: () => deleteWorkout(workout.id),
    onSuccess: async () => {
      queryClient.setQueryData(queryKeys.activeWorkout, null);
      await queryClient.invalidateQueries({ queryKey: queryKeys.workouts, refetchType: "none" });
      navigate("/workouts", { replace: true });
    },
  });

  const paused = workout.status === "Paused";
  const lastSet = workout.exercises
    .flatMap((e) => e.sets)
    .reduce<{ id: number; at: number } | null>((latest, s) => {
      const at = Date.parse(s.completedAtUtc);
      return !latest || at > latest.at ? { id: s.id, at } : latest;
    }, null);
  const restSeconds = lastSet ? Math.floor((now - lastSet.at) / 1000) : null;
  const showRest = !paused && lastSet && restDismissedFor !== lastSet.id && restSeconds !== null && restSeconds < 600;
  const error = status.error ?? addExercise.error ?? removeExercise.error ?? discard.error;

  return (
    // Extra room at the bottom, so the rest timer bar never hides the last button.
    <div className="space-y-4 pb-20">
      {/* The workout bar stays on screen while scrolling through the exercises. */}
      <Card className="sticky top-16 z-10">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="min-w-0">
            <h1 className="truncate text-xl font-bold text-slate-900">{workout.name}</h1>
            <p className="flex items-center gap-2 text-sm text-slate-600">
              <span className="font-mono text-2xl font-bold text-slate-900 tabular-nums" aria-label="Workout time">
                {formatClock(elapsedSeconds(workout, now))}
              </span>
              {paused && <Badge tone="amber">{sessionStatusLabels.Paused}</Badge>}
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="secondary" busy={status.isPending} onClick={() => status.mutate(paused ? "resume" : "pause")}>
              {paused ? "Resume" : "Pause"}
            </Button>
            <Button onClick={() => setFinishOpen(true)}>Finish</Button>
          </div>
        </div>
      </Card>

      {error && <Alert>{errorMessage(error)}</Alert>}

      {workout.exercises.length === 0 && <Alert kind="info">No exercises yet. Add the first one below.</Alert>}

      <ol className="space-y-3">
        {workout.exercises.map((entry) => (
          <li key={entry.id}>
            <ExerciseCard
              workoutId={workout.id}
              entry={entry}
              open={openId === entry.id}
              onToggle={() => setOpenId(openId === entry.id ? null : entry.id)}
              onUpdated={show}
              onRemove={() =>
                (entry.sets.length === 0 || window.confirm(`Remove ${entry.exerciseName} and its ${entry.sets.length} sets?`)) &&
                removeExercise.mutate(entry.id)
              }
            />
          </li>
        ))}
      </ol>

      <Button variant="secondary" className="w-full" busy={addExercise.isPending} onClick={() => setPickerOpen(true)}>
        Add exercise
      </Button>

      <div className="pt-4 text-center">
        <button
          type="button"
          className="min-h-11 px-3 text-sm font-medium text-red-700 hover:underline"
          onClick={() => window.confirm("Discard this workout? Its sets will be deleted.") && discard.mutate()}
        >
          Discard workout
        </button>
      </div>

      {showRest && (
        <div
          role="timer"
          aria-label="Rest timer"
          className="fixed inset-x-0 bottom-16 z-30 mx-auto flex max-w-md items-center justify-between gap-3 rounded-xl bg-slate-900 px-4 py-3 text-white shadow-lg md:bottom-6"
        >
          <span>
            Rest <span className="font-mono text-xl font-bold tabular-nums">{formatClock(restSeconds)}</span>
          </span>
          <button type="button" className="min-h-11 rounded-lg px-3 text-sm font-semibold hover:bg-white/10" onClick={() => setRestDismissedFor(lastSet.id)}>
            Hide
          </button>
        </div>
      )}

      {pickerOpen && (
        <ExercisePicker
          title="Add to workout"
          showSuggestions
          onPick={(exercise) => {
            addExercise.mutate(exercise.id);
            setPickerOpen(false);
          }}
          onClose={() => setPickerOpen(false)}
        />
      )}

      {finishOpen && <FinishDialog workout={workout} now={now} onClose={() => setFinishOpen(false)} />}
    </div>
  );
}

function firstUnfinished(workout: Workout) {
  return workout.exercises.find((e) => e.sets.length < (e.targetSets ?? 1)) ?? workout.exercises[0];
}

function ExerciseCard({
  workoutId,
  entry,
  open,
  onToggle,
  onUpdated,
  onRemove,
}: {
  workoutId: number;
  entry: WorkoutExercise;
  open: boolean;
  onToggle: () => void;
  onUpdated: (workout: Workout) => void;
  onRemove: () => void;
}) {
  const target = describeTarget(entry);
  const removeSet = useMutation({
    mutationFn: (setId: number) => deleteSet(workoutId, entry.id, setId),
    onSuccess: onUpdated,
  });

  return (
    <Card className={open ? "ring-2 ring-brand-500" : ""}>
      <div className="flex items-start justify-between gap-2">
        <button type="button" onClick={onToggle} aria-expanded={open} className="min-h-11 min-w-0 flex-1 text-left">
          <span className="block font-bold text-slate-900">
            <span className="mr-2 text-slate-400">{entry.order}.</span>
            {entry.exerciseName}
          </span>
          <span className="block text-sm text-slate-600">
            {entry.sets.length}
            {entry.targetSets !== null ? ` of ${entry.targetSets}` : ""} sets
            {target && <> · target {target}</>}
          </span>
        </button>
        <button
          type="button"
          onClick={onRemove}
          aria-label={`Remove ${entry.exerciseName}`}
          className="flex size-11 shrink-0 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100"
        >
          <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round">
            <path d="M6 6l12 12M18 6L6 18" />
          </svg>
        </button>
      </div>

      {open && (
        <div className="mt-3 space-y-3">
          {entry.notes && <p className="text-sm text-slate-600">{entry.notes}</p>}
          {entry.sets.length > 0 && (
            <ol aria-label={`${entry.exerciseName} sets`} className="divide-y divide-slate-100 rounded-lg border border-slate-200">
              {entry.sets.map((set) => (
                <li key={set.id} className="flex items-center justify-between gap-2 px-3 py-1.5">
                  <span className="flex items-center gap-2">
                    <span className="w-5 text-sm text-slate-400">{set.setNumber}</span>
                    <span className="font-semibold text-slate-900 tabular-nums">{describeSet(set)}</span>
                    {set.type !== "Normal" && <Badge tone={set.type === "Warmup" ? "slate" : "amber"}>{setTypeLabels[set.type]}</Badge>}
                  </span>
                  <button
                    type="button"
                    aria-label={`Delete set ${set.setNumber}`}
                    disabled={removeSet.isPending}
                    onClick={() => removeSet.mutate(set.id)}
                    className="flex size-11 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-red-700"
                  >
                    <svg viewBox="0 0 24 24" aria-hidden="true" className="size-4" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round">
                      <path d="M6 6l12 12M18 6L6 18" />
                    </svg>
                  </button>
                </li>
              ))}
            </ol>
          )}
          {/* A new key after every set: the form starts again, prefilled from the set just logged. */}
          <SetForm key={entry.sets.length} workoutId={workoutId} entry={entry} onUpdated={onUpdated} />
        </div>
      )}
    </Card>
  );
}

function SetForm({ workoutId, entry, onUpdated }: { workoutId: number; entry: WorkoutExercise; onUpdated: (workout: Workout) => void }) {
  const cardio = isCardio(entry.category);
  const [draft, setDraft] = useState<SetDraft>(() => prefillSet(entry));
  const [type, setType] = useState<SetType>("Normal");
  const [formError, setFormError] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: (request: SetRequest) => addSet(workoutId, entry.id, request),
    onSuccess: onUpdated,
    onError: (e) => setFormError(errorMessage(e)),
  });

  const change = (field: keyof SetDraft, value: string) => setDraft((d) => ({ ...d, [field]: value }));

  function startDropSet() {
    const last = entry.sets.at(-1);
    setType("Drop");
    if (last?.weightKg != null) change("weight", String(dropSetWeight(last.weightKg)));
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    const number = (text: string) => (text.trim() === "" ? null : Number(text.replace(",", ".")));
    const weight = number(draft.weight);
    const reps = number(draft.reps) ?? 0;
    const minutes = number(draft.minutes);
    const km = number(draft.km);

    if ([weight, reps, minutes, km].some((n) => n !== null && (Number.isNaN(n) || n < 0))) {
      setFormError("Enter numbers only.");
      return;
    }
    if (!cardio && (!Number.isInteger(reps) || reps < 1 || reps > 200)) {
      setFormError("Reps must be between 1 and 200.");
      return;
    }
    if (cardio && !minutes && !km && !reps) {
      setFormError("Enter the time or the distance.");
      return;
    }

    setFormError(null);
    save.mutate({
      type,
      reps: cardio ? Math.round(reps) : reps,
      weightKg: cardio ? null : weight,
      rpe: null,
      durationSeconds: cardio && minutes ? Math.round(minutes * 60) : null,
      distanceMeters: cardio && km ? Math.round(km * 1000) : null,
    });
  }

  return (
    <form onSubmit={submit} noValidate className="space-y-3">
      {cardio ? (
        <div className="grid grid-cols-2 gap-3">
          <Stepper label="Time (min)" value={draft.minutes} onChange={(v) => change("minutes", v)} step={1} max={1440} />
          <Stepper label="Distance (km)" value={draft.km} onChange={(v) => change("km", v)} step={0.5} max={1000} decimal />
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3">
          <Stepper label="Weight (kg)" value={draft.weight} onChange={(v) => change("weight", v)} step={WEIGHT_STEP} max={1000} decimal />
          <Stepper label="Reps" value={draft.reps} onChange={(v) => change("reps", v)} step={1} max={200} />
        </div>
      )}

      {!cardio && (
        <fieldset>
          <legend className="sr-only">Set type</legend>
          <div className="flex flex-wrap gap-2">
            {(["Normal", "Warmup", "Failure"] as const).map((t) => (
              <button
                key={t}
                type="button"
                aria-pressed={type === t}
                onClick={() => setType(t)}
                className={`min-h-11 rounded-full border px-3 text-sm font-medium ${
                  type === t ? "border-brand-700 bg-brand-700 text-white" : "border-slate-300 bg-white text-slate-700"
                }`}
              >
                {setTypeLabels[t]}
              </button>
            ))}
            {entry.sets.length > 0 && (
              <button
                type="button"
                aria-pressed={type === "Drop"}
                onClick={startDropSet}
                className={`min-h-11 rounded-full border px-3 text-sm font-medium ${
                  type === "Drop" ? "border-amber-600 bg-amber-500 text-white" : "border-amber-300 bg-white text-amber-800"
                }`}
              >
                + Drop set
              </button>
            )}
          </div>
          {type === "Drop" && <p className="mt-2 text-sm text-amber-800">Drop set: straight after the last set, 20% lighter.</p>}
        </fieldset>
      )}

      {formError && <Alert>{formError}</Alert>}

      <Button type="submit" busy={save.isPending} className="min-h-14 w-full text-base">
        Log set {entry.sets.length + 1}
      </Button>
    </form>
  );
}

/** A number box between − and + buttons: big targets for a thumb, and the number keyboard on a phone. */
function Stepper({
  label,
  value,
  onChange,
  step,
  max,
  decimal = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  step: number;
  max: number;
  decimal?: boolean;
}) {
  const stepButton = "flex size-12 shrink-0 items-center justify-center rounded-lg border border-slate-300 bg-white text-xl font-bold text-slate-800 active:bg-slate-100";
  return (
    <div>
      <TextField
        label={label}
        inputMode={decimal ? "decimal" : "numeric"}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="[&_input]:text-center [&_input]:text-lg [&_input]:font-semibold"
      />
      <div className="mt-2 flex gap-2">
        <button type="button" className={stepButton} aria-label={`${label} minus ${step}`} onClick={() => onChange(stepValue(value, -step, max))}>
          −
        </button>
        <button type="button" className={stepButton} aria-label={`${label} plus ${step}`} onClick={() => onChange(stepValue(value, step, max))}>
          +
        </button>
      </div>
    </div>
  );
}

function FinishDialog({ workout, now, onClose }: { workout: Workout; now: number; onClose: () => void }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [minutes, setMinutes] = useState(() => String(Math.max(1, Math.round(elapsedSeconds(workout, now) / 60))));
  const [notes, setNotes] = useState(workout.notes);

  const finish = useMutation({
    mutationFn: () => finishWorkout(workout.id, Number(minutes) || null, notes),
    onSuccess: async (finished) => {
      queryClient.setQueryData(queryKeys.workout(finished.id), finished);
      queryClient.setQueryData(queryKeys.activeWorkout, null);
      await queryClient.invalidateQueries({ queryKey: queryKeys.workouts, refetchType: "none" });
      await queryClient.invalidateQueries({ queryKey: queryKeys.exerciseSuggestions, refetchType: "none" });
      navigate(`/workouts/${finished.id}`, { replace: true });
    },
  });

  const setCount = workout.exercises.reduce((n, e) => n + e.sets.length, 0);
  const skipped = workout.exercises.filter((e) => e.sets.length === 0).length;

  return (
    <div className="fixed inset-0 z-40 flex items-end justify-center bg-slate-900/40 sm:items-center sm:p-4" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="finish-title"
        className="w-full max-w-md space-y-4 rounded-t-2xl bg-white p-5 shadow-xl sm:rounded-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 id="finish-title" className="text-lg font-bold text-slate-900">
          Finish workout
        </h2>
        <p className="text-sm text-slate-600">
          {setCount} sets logged.
          {skipped > 0 && ` ${skipped} exercise${skipped === 1 ? "" : "s"} without sets will be left out.`}
        </p>
        {finish.isError && <Alert>{errorMessage(finish.error)}</Alert>}
        <TextField label="Duration (min)" inputMode="numeric" value={minutes} onChange={(e) => setMinutes(e.target.value)} hint="Pauses are already left out." />
        <TextAreaField label="Notes" rows={2} maxLength={1000} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="How did it go?" />
        <div className="flex flex-wrap gap-3">
          <Button busy={finish.isPending} onClick={() => finish.mutate()}>
            Finish workout
          </Button>
          <Button variant="secondary" onClick={onClose}>
            Keep going
          </Button>
        </div>
      </div>
    </div>
  );
}
