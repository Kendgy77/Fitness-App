import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { createPlan, getPlan, updatePlan } from "../api/endpoints";
import { FIX_FIELDS, toApiError, type ApiError } from "../api/errors";
import type { Exercise, Plan, PlanRequest } from "../api/types";
import { ExercisePicker } from "../components/ExercisePicker";
import { Alert, Button, Card, Loading, PageHeader, TextAreaField, TextField } from "../components/ui";
import { categoryLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** Create a plan (/plans/new, thesis T09) or edit one (/plans/:id/edit). */
export function PlanEditorPage() {
  const idParam = useParams().id;
  const id = idParam ? Number(idParam) : null;
  const existing = useQuery({ queryKey: queryKeys.plan(id ?? 0), queryFn: () => getPlan(id!), enabled: id !== null });

  return (
    <>
      <PageHeader title={id ? "Edit plan" : "New plan"} />
      {id === null && <PlanForm />}
      {id !== null && existing.isPending && <Loading />}
      {existing.isError && <Alert>{toApiError(existing.error).message}</Alert>}
      {existing.data && <PlanForm plan={existing.data} />}
    </>
  );
}

/** One row of the editor. `key` only tells React which row is which while rows move around. */
interface Row {
  key: number;
  exerciseId: number;
  exerciseName: string;
  category: Exercise["category"];
  sets: string;
  repsMin: string;
  /** Empty means "same as repsMin": a fixed number of reps. */
  repsMax: string;
  weight: string;
  notes: string;
}

let nextKey = 1;

function toRows(plan?: Plan): Row[] {
  return (plan?.exercises ?? []).map((e) => ({
    key: nextKey++,
    exerciseId: e.exerciseId,
    exerciseName: e.exerciseName,
    category: e.category,
    sets: String(e.targetSets),
    repsMin: String(e.targetRepsMin),
    repsMax: e.targetRepsMax === e.targetRepsMin ? "" : String(e.targetRepsMax),
    weight: e.targetWeightKg?.toString() ?? "",
    notes: e.notes,
  }));
}

function PlanForm({ plan }: { plan?: Plan }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [name, setName] = useState(plan?.name ?? "");
  const [description, setDescription] = useState(plan?.description ?? "");
  const [rows, setRows] = useState<Row[]>(() => toRows(plan));
  const [pickerOpen, setPickerOpen] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const save = useMutation({
    mutationFn: (request: PlanRequest) => (plan ? updatePlan(plan.id, request) : createPlan(request)),
    onSuccess: async (saved) => {
      queryClient.setQueryData(queryKeys.plan(saved.id), saved);
      await queryClient.invalidateQueries({ queryKey: queryKeys.plans, refetchType: "none" });
      navigate(`/plans/${saved.id}`, { replace: true });
    },
    onError: (e) => setError(toApiError(e)),
  });

  function addExercise(exercise: Exercise) {
    setRows((current) => [
      ...current,
      { key: nextKey++, exerciseId: exercise.id, exerciseName: exercise.name, category: exercise.category, sets: "3", repsMin: "8", repsMax: "12", weight: "", notes: "" },
    ]);
  }

  function updateRow(key: number, change: Partial<Row>) {
    setRows((current) => current.map((r) => (r.key === key ? { ...r, ...change } : r)));
  }

  /** Swap a row with its neighbour. Buttons instead of drag and drop: they work with a keyboard and on phones. */
  function move(index: number, by: -1 | 1) {
    setRows((current) => {
      const next = [...current];
      const [row] = next.splice(index, 1);
      next.splice(index + by, 0, row!);
      return next;
    });
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const fieldErrors: Record<string, string> = {};
    if (name.trim().length < 2) fieldErrors.name = "Enter the plan name.";

    const exercises = rows.map((r, i) => {
      const sets = Number(r.sets);
      const repsMin = Number(r.repsMin);
      const repsMax = r.repsMax.trim() === "" ? repsMin : Number(r.repsMax);
      const weight = r.weight.trim() === "" ? null : Number(r.weight.replace(",", "."));
      const isReps = (n: number) => Number.isInteger(n) && n >= 1 && n <= 100;
      if (!Number.isInteger(sets) || sets < 1 || sets > 20) fieldErrors[`exercises[${i}].targetSets`] = "1–20";
      if (r.repsMin.trim() === "" || !isReps(repsMin)) fieldErrors[`exercises[${i}].targetRepsMin`] = "1–100";
      else if (!isReps(repsMax)) fieldErrors[`exercises[${i}].targetRepsMax`] = "1–100";
      else if (repsMax < repsMin) fieldErrors[`exercises[${i}].targetRepsMax`] = `At least ${repsMin}`;
      if (weight !== null && (Number.isNaN(weight) || weight < 0 || weight > 1000)) fieldErrors[`exercises[${i}].targetWeightKg`] = "0–1000";
      return { exerciseId: r.exerciseId, targetSets: sets, targetRepsMin: repsMin, targetRepsMax: repsMax, targetWeightKg: weight, notes: r.notes };
    });

    if (Object.keys(fieldErrors).length > 0) {
      setError({ status: null, message: FIX_FIELDS, fieldErrors });
      return;
    }

    setError(null);
    save.mutate({ name: name.trim(), description, exercises });
  }

  const fieldErrors = error?.fieldErrors ?? {};
  const rowError = (i: number, field: string) => fieldErrors[`exercises[${i}].${field}`];

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-6">
      {error && (error.message !== "" || Object.keys(fieldErrors).length === 0) && <Alert>{error.message}</Alert>}

      <Card className="space-y-4">
        <TextField label="Plan name" required maxLength={100} value={name} onChange={(e) => setName(e.target.value)} error={fieldErrors.name} placeholder="e.g. Push day" />
        <TextAreaField label="Description" rows={2} maxLength={1000} value={description} onChange={(e) => setDescription(e.target.value)} error={fieldErrors.description} />
      </Card>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-bold text-slate-900">Exercises</h2>
          <Button variant="secondary" onClick={() => setPickerOpen(true)}>
            Add exercise
          </Button>
        </div>
        {rows.length > 0 && (
          <p className="mb-4 text-sm text-slate-500">
            Reps are a range, like 8 to 12: add weight once every set reaches the top. Leave "Reps to" empty for a fixed number.
          </p>
        )}
        {fieldErrors.exercises && (
          <div className="mb-4">
            <Alert>{fieldErrors.exercises}</Alert>
          </div>
        )}

        {rows.length === 0 ? (
          <Alert kind="info">No exercises yet. Use "Add exercise" to pick them from the library.</Alert>
        ) : (
          <ol className="space-y-3">
            {rows.map((row, i) => (
              <li key={row.key} className="rounded-lg border border-slate-200 p-3">
                <div className="flex items-start justify-between gap-2">
                  <p className="min-w-0 font-medium text-slate-900">
                    <span className="mr-2 text-sm text-slate-400">{i + 1}.</span>
                    {row.exerciseName}
                    <span className="ml-2 text-xs font-normal text-slate-500">{categoryLabels[row.category]}</span>
                  </p>
                  <div className="flex shrink-0 gap-1">
                    <IconButton label={`Move ${row.exerciseName} up`} disabled={i === 0} onClick={() => move(i, -1)} path="M12 19V5M5 12l7-7 7 7" />
                    <IconButton label={`Move ${row.exerciseName} down`} disabled={i === rows.length - 1} onClick={() => move(i, 1)} path="M12 5v14M19 12l-7 7-7-7" />
                    <IconButton label={`Remove ${row.exerciseName}`} onClick={() => setRows((rs) => rs.filter((r) => r.key !== row.key))} path="M6 6l12 12M18 6L6 18" />
                  </div>
                </div>
                {/* Two small boxes, not one "8–10" box: a phone's number keyboard often has no dash. */}
                <div className="mt-3 grid grid-cols-4 items-end gap-2 sm:gap-3">
                  <TextField label="Sets" inputMode="numeric" value={row.sets} onChange={(e) => updateRow(row.key, { sets: e.target.value })} error={rowError(i, "targetSets")} />
                  <TextField label="Reps from" inputMode="numeric" value={row.repsMin} onChange={(e) => updateRow(row.key, { repsMin: e.target.value })} error={rowError(i, "targetRepsMin")} />
                  <TextField label="Reps to" inputMode="numeric" placeholder="—" value={row.repsMax} onChange={(e) => updateRow(row.key, { repsMax: e.target.value })} error={rowError(i, "targetRepsMax")} />
                  <TextField label="Weight (kg)" inputMode="decimal" placeholder="—" value={row.weight} onChange={(e) => updateRow(row.key, { weight: e.target.value })} error={rowError(i, "targetWeightKg")} />
                </div>
                <TextField className="mt-3" label="Notes" maxLength={200} value={row.notes} onChange={(e) => updateRow(row.key, { notes: e.target.value })} placeholder="e.g. slow on the way down" />
              </li>
            ))}
          </ol>
        )}
      </Card>

      <div className="flex flex-wrap gap-3">
        <Button type="submit" busy={save.isPending}>
          Save plan
        </Button>
        <Link to={plan ? `/plans/${plan.id}` : "/plans"} className="inline-flex min-h-11 items-center rounded-lg px-4 text-sm font-semibold text-slate-700 hover:bg-slate-100">
          Cancel
        </Link>
      </div>

      {pickerOpen && <ExercisePicker onPick={addExercise} onClose={() => setPickerOpen(false)} />}
    </form>
  );
}

function IconButton({ label, path, disabled, onClick }: { label: string; path: string; disabled?: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      disabled={disabled}
      onClick={onClick}
      className="flex size-11 items-center justify-center rounded-lg text-slate-600 hover:bg-slate-100 disabled:opacity-30"
    >
      <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
        <path d={path} />
      </svg>
    </button>
  );
}
