import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { deleteMeasurement, getMeasurement, updateMeasurement } from "../api/endpoints";
import { toApiError, type ApiError } from "../api/errors";
import type { Measurement, MeasurementRequest, MeasurementValues } from "../api/types";
import { Alert, Button, Card, Loading, PageHeader, TextAreaField, TextField } from "../components/ui";
import { metrics } from "../lib/measurements";
import { queryKeys } from "../lib/queryKeys";
import { localDayKey } from "../lib/workout";

/**
 * Correct or delete one day of the body history (/profile/body/:id/edit). New values are entered in the profile,
 * which records them for today.
 */
export function MeasurementFormPage() {
  const id = Number(useParams().id);
  const existing = useQuery({ queryKey: queryKeys.measurement(id), queryFn: () => getMeasurement(id) });

  return (
    <>
      <PageHeader title="Edit measurements" />
      {existing.isPending && <Loading />}
      {existing.isError && <Alert>{toApiError(existing.error).message}</Alert>}
      {existing.data && <MeasurementForm measurement={existing.data} />}
    </>
  );
}

type Values = Record<keyof MeasurementValues, string>;

function MeasurementForm({ measurement }: { measurement: Measurement }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [day, setDay] = useState(measurement.measuredOn);
  const [values, setValues] = useState<Values>(
    () => Object.fromEntries(metrics.map((m) => [m.key, measurement[m.key]?.toString() ?? ""])) as Values,
  );
  const [notes, setNotes] = useState(measurement.notes);
  const [error, setError] = useState<ApiError | null>(null);

  const done = async () => {
    await queryClient.invalidateQueries({ queryKey: queryKeys.measurements });
    await queryClient.invalidateQueries({ queryKey: queryKeys.progress, refetchType: "none" });
    // Saving a newer weight also changes the profile's weight.
    await queryClient.invalidateQueries({ queryKey: queryKeys.profile, refetchType: "none" });
    navigate("/profile/body", { replace: true });
  };

  const save = useMutation({
    mutationFn: (request: MeasurementRequest) => updateMeasurement(measurement.id, request),
    onSuccess: done,
    onError: (e) => setError(toApiError(e)),
  });
  const remove = useMutation({ mutationFn: () => deleteMeasurement(measurement.id), onSuccess: done });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const fieldErrors: Record<string, string> = {};
    const parsed = {} as Record<keyof MeasurementValues, number | null>;

    for (const m of metrics) {
      const text = values[m.key].trim().replace(",", ".");
      const value = text === "" ? null : Number(text);
      if (value !== null && (Number.isNaN(value) || value < m.min || value > m.max)) {
        fieldErrors[m.key] = `${m.min}–${m.max} ${m.unit}`;
      }
      parsed[m.key] = value;
    }
    if (!day) fieldErrors.measuredOn = "Enter the date.";
    else if (day > localDayKey(new Date())) fieldErrors.measuredOn = "Measurements can't be in the future.";
    if (Object.values(parsed).every((v) => v === null)) fieldErrors.weightKg = "Enter at least one measurement.";

    if (Object.keys(fieldErrors).length > 0) {
      setError({ status: null, message: "Please fix the highlighted fields.", fieldErrors });
      return;
    }
    setError(null);
    save.mutate({ measuredOn: day, notes, ...parsed });
  }

  const fieldErrors = error?.fieldErrors ?? {};
  const field = (m: (typeof metrics)[number]) => (
    <TextField
      key={m.key}
      label={`${m.label} (${m.unit})`}
      inputMode="decimal"
      value={values[m.key]}
      onChange={(e) => setValues((v) => ({ ...v, [m.key]: e.target.value }))}
      error={fieldErrors[m.key]}
    />
  );

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-6">
      {error && <Alert>{error.message}</Alert>}

      <Card className="grid gap-4 sm:grid-cols-2">
        <TextField label="Date" type="date" value={day} max={localDayKey(new Date())} onChange={(e) => setDay(e.target.value)} error={fieldErrors.measuredOn} />
        <p className="text-sm text-slate-600 sm:self-end sm:pb-3">Fill in only what you measured. Every value is optional.</p>
      </Card>

      <Card>
        <h2 className="mb-4 text-lg font-bold text-slate-900">Body</h2>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">{metrics.filter((m) => m.unit !== "cm").map(field)}</div>
      </Card>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-bold text-slate-900">Tape measurements</h2>
          <Link to="/profile/body/guide" className="text-sm font-semibold text-brand-700 hover:underline">
            How to measure
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">{metrics.filter((m) => m.unit === "cm").map(field)}</div>
      </Card>

      <Card>
        <TextAreaField label="Notes" rows={2} maxLength={200} value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="e.g. after holidays" />
      </Card>

      <div className="flex flex-wrap gap-3">
        <Button type="submit" busy={save.isPending}>
          Save measurements
        </Button>
        <Link to="/profile/body" className="inline-flex min-h-11 items-center rounded-lg px-4 text-sm font-semibold text-slate-700 hover:bg-slate-100">
          Cancel
        </Link>
        <Button
          variant="danger"
          className="ml-auto"
          busy={remove.isPending}
          onClick={() => window.confirm("Delete this day's measurements?") && remove.mutate()}
        >
          Delete
        </Button>
      </div>
    </form>
  );
}
