import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link } from "react-router";
import { getCurrentMeasurements, getProfile, saveCurrentMeasurements, updateProfile } from "../api/endpoints";
import { toApiError, type ApiError } from "../api/errors";
import type { CurrentMeasurement, MeasurementValues, Profile, Sex, TrainingGoal, UpdateProfileRequest } from "../api/types";
import { BmiCard } from "../components/BmiCard";
import { Alert, Button, Card, Loading, PageHeader, SelectField, TextField } from "../components/ui";
import { goalLabels, options, sexLabels } from "../lib/labels";
import { metrics, type MetricInfo } from "../lib/measurements";
import { queryKeys } from "../lib/queryKeys";
import { localDayKey } from "../lib/workout";

/**
 * WF03: the user's profile; WF10: BMI calculated by the API from weight and height; WF07: body measurements.
 * The measurements live here, filled in with their latest values, so the user only changes what changed. Each
 * save records the changed values for today, which builds the history behind the charts (Profile → Body history).
 */
export function ProfilePage() {
  const profile = useQuery({ queryKey: queryKeys.profile, queryFn: getProfile });
  const current = useQuery({ queryKey: queryKeys.currentMeasurements, queryFn: getCurrentMeasurements });
  const error = profile.error ?? current.error;

  return (
    <>
      <PageHeader title="Profile" />
      {(profile.isPending || current.isPending) && !error && <Loading />}
      {error && <Alert>{toApiError(error).message}</Alert>}
      {profile.data && current.data && (
        <div className="grid gap-6 lg:grid-cols-[1fr_20rem] lg:items-start">
          {/* The form starts from the loaded data and then keeps its own state; a save updates the cache. */}
          <ProfileForm profile={profile.data} current={current.data} />
          <div className="space-y-4">
            <Card>
              <BmiCard profile={profile.data} />
              {profile.data.age !== null && <p className="mt-4 text-sm text-slate-600">Age: {profile.data.age}</p>}
            </Card>
            <Card>
              <h2 className="text-lg font-bold text-slate-900">Body history</h2>
              <p className="mt-1 text-sm text-slate-600">Every change you save here is kept, so you can see how your weight and measurements change over time.</p>
              <Link to="/profile/body" className="mt-3 inline-flex min-h-11 items-center text-sm font-semibold text-brand-700 hover:underline">
                See charts and history
              </Link>
            </Card>
          </div>
        </div>
      )}
    </>
  );
}

type BodyKey = keyof MeasurementValues;

interface FormState {
  birthDate: string;
  sex: Sex | "";
  heightCm: string;
  goal: TrainingGoal | "";
  body: Record<BodyKey, string>;
}

/**
 * The user's time zone, e.g. "Europe/Warsaw", as the browser reports it. The API needs it to put workouts on the
 * right day and to send reminders at the right local time, but the user never has to choose it (D021).
 */
const browserTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;

function toFormState(profile: Profile, current: CurrentMeasurement[]): FormState {
  const latest = (key: BodyKey) => current.find((c) => c.field === key)?.value;
  return {
    birthDate: profile.birthDate ?? "",
    sex: profile.sex ?? "",
    heightCm: profile.heightCm?.toString() ?? "",
    goal: profile.goal ?? "",
    body: Object.fromEntries(
      metrics.map((m) => [m.key, (m.key === "weightKg" ? (profile.weightKg ?? latest(m.key)) : latest(m.key))?.toString() ?? ""]),
    ) as Record<BodyKey, string>,
  };
}

/** "10 Nov 2026": short enough to sit under a box on a phone. */
const shortDate = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric" });

/** "78.5" (or "78,5", as many phones type it) → 78.5; empty → null (not filled in); anything else → NaN. */
function parseDecimal(text: string): number | null {
  const trimmed = text.trim();
  return trimmed === "" ? null : Number(trimmed.replace(",", "."));
}

function ProfileForm({ profile, current }: { profile: Profile; current: CurrentMeasurement[] }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>(() => toFormState(profile, current));
  const [error, setError] = useState<ApiError | null>(null);
  const [saved, setSaved] = useState(false);

  const save = useMutation({
    mutationFn: async ({ body, profileRequest }: { body: MeasurementValues | null; profileRequest: UpdateProfileRequest }) => {
      // Measurements first: a new weight there also becomes the profile's weight, and the profile save after it
      // returns the final profile (with the BMI) for the page.
      const newCurrent = body ? await saveCurrentMeasurements({ measuredOn: localDayKey(new Date()), notes: "", ...body }) : null;
      const updated = await updateProfile(profileRequest);
      return { newCurrent, updated };
    },
    onSuccess: async ({ newCurrent, updated }) => {
      queryClient.setQueryData(queryKeys.profile, updated);
      if (newCurrent) queryClient.setQueryData(queryKeys.currentMeasurements, newCurrent);
      await queryClient.invalidateQueries({ queryKey: queryKeys.measurements, refetchType: "none" });
      await queryClient.invalidateQueries({ queryKey: queryKeys.progress, refetchType: "none" });
      setSaved(true);
    },
    onError: (e) => setError(toApiError(e)),
  });

  function set<K extends Exclude<keyof FormState, "body">>(field: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [field]: value }));
    setSaved(false);
  }

  function setBody(key: BodyKey, value: string) {
    setForm((f) => ({ ...f, body: { ...f.body, [key]: value } }));
    setSaved(false);
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const heightCm = parseDecimal(form.heightCm);
    const body = Object.fromEntries(metrics.map((m) => [m.key, parseDecimal(form.body[m.key])])) as unknown as MeasurementValues;

    const fieldErrors: Record<string, string> = {};
    if (Number.isNaN(heightCm)) fieldErrors.heightCm = "Enter a number, e.g. 180.";
    for (const m of metrics) {
      if (Number.isNaN(body[m.key])) fieldErrors[m.key] = m.key === "weightKg" ? "Enter a number, e.g. 75.5." : `Enter a number, e.g. ${m.min + 10}.`;
    }
    if (Object.keys(fieldErrors).length > 0) {
      setError({ status: null, message: "Please fix the highlighted fields.", fieldErrors });
      return;
    }

    setError(null);
    save.mutate({
      // The API keeps only the values that changed, so sending them all is safe.
      body: Object.values(body).some((v) => v !== null) ? body : null,
      profileRequest: {
        birthDate: form.birthDate || null,
        sex: form.sex || null,
        heightCm,
        weightKg: body.weightKg,
        goal: form.goal || null,
        timeZone: browserTimeZone(),
      },
    });
  }

  const fieldErrors = error?.fieldErrors ?? {};
  const generalError = error && (Object.keys(fieldErrors).length === 0 || error.status === null) ? error.message : null;
  const latestOn = (key: BodyKey) => current.find((c) => c.field === key)?.measuredOn;

  const bodyField = (m: MetricInfo) => {
    const on = latestOn(m.key);
    return (
      <TextField
        key={m.key}
        label={`${m.label} (${m.unit})`}
        inputMode="decimal"
        value={form.body[m.key]}
        onChange={(e) => setBody(m.key, e.target.value)}
        error={fieldErrors[m.key]}
        hint={on ? `Updated ${shortDate.format(new Date(`${on}T00:00`))}` : undefined}
        placeholder={m.key === "weightKg" ? "e.g. 78.5" : undefined}
      />
    );
  };

  return (
    <form onSubmit={handleSubmit} noValidate className="space-y-4">
      {generalError && <Alert>{generalError}</Alert>}
      {saved && <Alert kind="success">Profile saved.</Alert>}

      <Card>
        <h2 className="mb-4 text-lg font-bold text-slate-900">About you</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField
            label="Height (cm)"
            inputMode="decimal"
            value={form.heightCm}
            onChange={(e) => set("heightCm", e.target.value)}
            error={fieldErrors.heightCm}
            placeholder="e.g. 183"
          />
          <TextField
            label="Date of birth"
            type="date"
            max={new Date().toISOString().slice(0, 10)}
            value={form.birthDate}
            onChange={(e) => set("birthDate", e.target.value)}
            error={fieldErrors.birthDate}
          />
          <SelectField label="Sex" value={form.sex} onChange={(e) => set("sex", e.target.value as Sex | "")} error={fieldErrors.sex}>
            <option value="">Not specified</option>
            {options(sexLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
          <SelectField label="Training goal" value={form.goal} onChange={(e) => set("goal", e.target.value as TrainingGoal | "")} error={fieldErrors.goal}>
            <option value="">Not chosen</option>
            {options(goalLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
        </div>
      </Card>

      <Card>
        <h2 className="text-lg font-bold text-slate-900">Body</h2>
        <p className="mb-4 text-sm text-slate-600">Change only what changed, whenever you like. Every value is optional.</p>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">{metrics.filter((m) => m.unit !== "cm").map(bodyField)}</div>
      </Card>

      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-bold text-slate-900">Tape measurements</h2>
          <Link to="/profile/body/guide" className="text-sm font-semibold text-brand-700 hover:underline">
            How to measure
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3">{metrics.filter((m) => m.unit === "cm").map(bodyField)}</div>
      </Card>

      <Button type="submit" busy={save.isPending}>
        Save profile
      </Button>
    </form>
  );
}
