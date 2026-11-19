import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { getProfile, updateProfile } from "../api/endpoints";
import { toApiError, type ApiError } from "../api/errors";
import type { Profile, Sex, TrainingGoal, UpdateProfileRequest } from "../api/types";
import { BmiCard } from "../components/BmiCard";
import { Alert, Button, Card, Loading, PageHeader, SelectField, TextField } from "../components/ui";
import { goalLabels, options, sexLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** WF03: the user's profile; WF10: BMI calculated by the API from weight and height. */
export function ProfilePage() {
  const profile = useQuery({ queryKey: queryKeys.profile, queryFn: getProfile });

  return (
    <>
      <PageHeader title="Profile" />
      {profile.isPending && <Loading />}
      {profile.isError && <Alert>{toApiError(profile.error).message}</Alert>}
      {profile.data && (
        <div className="grid gap-6 lg:grid-cols-[1fr_20rem]">
          {/* The form starts from the loaded profile and then keeps its own state; a save updates the cache. */}
          <ProfileForm profile={profile.data} />
          <Card className="h-fit">
            <BmiCard profile={profile.data} />
            {profile.data.age !== null && <p className="mt-4 text-sm text-slate-600">Age: {profile.data.age}</p>}
          </Card>
        </div>
      )}
    </>
  );
}

interface FormState {
  birthDate: string;
  sex: Sex | "";
  heightCm: string;
  weightKg: string;
  goal: TrainingGoal | "";
}

/**
 * The user's time zone, e.g. "Europe/Warsaw", as the browser reports it. The API needs it to put workouts on the
 * right day and to send reminders at the right local time, but the user never has to choose it (D021).
 */
const browserTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;

function toFormState(profile: Profile): FormState {
  return {
    birthDate: profile.birthDate ?? "",
    sex: profile.sex ?? "",
    heightCm: profile.heightCm?.toString() ?? "",
    weightKg: profile.weightKg?.toString() ?? "",
    goal: profile.goal ?? "",
  };
}

/** "78.5" (or "78,5", as many phones type it) → 78.5; empty → null (not filled in); anything else → NaN. */
function parseDecimal(text: string): number | null {
  const trimmed = text.trim();
  return trimmed === "" ? null : Number(trimmed.replace(",", "."));
}

function ProfileForm({ profile }: { profile: Profile }) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>(() => toFormState(profile));
  const [error, setError] = useState<ApiError | null>(null);
  const [saved, setSaved] = useState(false);

  const save = useMutation({
    mutationFn: (request: UpdateProfileRequest) => updateProfile(request),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.profile, updated);
      setSaved(true);
    },
    onError: (e) => setError(toApiError(e)),
  });

  function set<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [field]: value }));
    setSaved(false);
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const heightCm = parseDecimal(form.heightCm);
    const weightKg = parseDecimal(form.weightKg);

    const fieldErrors: Record<string, string> = {};
    if (Number.isNaN(heightCm)) fieldErrors.heightCm = "Enter a number, e.g. 180.";
    if (Number.isNaN(weightKg)) fieldErrors.weightKg = "Enter a number, e.g. 75.5.";
    if (Object.keys(fieldErrors).length > 0) {
      setError({ status: null, message: "", fieldErrors });
      return;
    }

    setError(null);
    const request: UpdateProfileRequest = {
      birthDate: form.birthDate || null,
      sex: form.sex || null,
      heightCm,
      weightKg,
      goal: form.goal || null,
      timeZone: browserTimeZone(),
    };
    save.mutate(request);
  }

  const fieldErrors = error?.fieldErrors ?? {};
  const generalError = error && Object.keys(fieldErrors).length === 0 ? error.message : null;

  return (
    <Card>
      <form onSubmit={handleSubmit} noValidate className="space-y-5">
        {generalError && <Alert>{generalError}</Alert>}
        {saved && <Alert kind="success">Profile saved.</Alert>}

        <div className="grid gap-4 sm:grid-cols-2">
          <TextField
            label="Weight (kg)"
            inputMode="decimal"
            value={form.weightKg}
            onChange={(e) => set("weightKg", e.target.value)}
            error={fieldErrors.weightKg}
            placeholder="e.g. 78.5"
          />
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
          <SelectField
            label="Training goal"
            value={form.goal}
            onChange={(e) => set("goal", e.target.value as TrainingGoal | "")}
            error={fieldErrors.goal}
            className="sm:col-span-2"
          >
            <option value="">Not chosen</option>
            {options(goalLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
        </div>

        <Button type="submit" busy={save.isPending}>
          Save profile
        </Button>
      </form>
    </Card>
  );
}
