import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { createExercise, getExercise, updateExercise } from "../api/endpoints";
import { toApiError, type ApiError } from "../api/errors";
import type { Difficulty, Equipment, Exercise, ExerciseCategory, ExerciseRequest } from "../api/types";
import { Alert, Button, Card, Loading, PageHeader, SelectField, TextAreaField, TextField } from "../components/ui";
import { categoryLabels, difficultyLabels, equipmentLabels, options } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** Add a custom exercise (/cwiczenia/nowe) or edit one (/cwiczenia/:id/edycja). Only the author sees it. */
export function ExerciseFormPage() {
  const idParam = useParams().id;
  const id = idParam ? Number(idParam) : null;
  const existing = useQuery({
    queryKey: queryKeys.exercise(id ?? 0),
    queryFn: () => getExercise(id!),
    enabled: id !== null,
  });

  return (
    <>
      <PageHeader title={id ? "Edytuj ćwiczenie" : "Nowe ćwiczenie"} />
      {id === null && <ExerciseForm />}
      {id !== null && existing.isPending && <Loading />}
      {existing.isError && <Alert>{toApiError(existing.error).message}</Alert>}
      {existing.data && <ExerciseForm exercise={existing.data} />}
    </>
  );
}

interface FormState {
  name: string;
  description: string;
  muscles: string;
  category: ExerciseCategory | "";
  difficulty: Difficulty | "";
  equipment: Equipment | "";
}

function ExerciseForm({ exercise }: { exercise?: Exercise }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<FormState>({
    name: exercise?.name ?? "",
    description: exercise?.description ?? "",
    muscles: exercise?.muscles ?? "",
    category: exercise?.category ?? "",
    difficulty: exercise?.difficulty ?? "Beginner",
    equipment: exercise?.equipment ?? "",
  });
  const [error, setError] = useState<ApiError | null>(null);

  const save = useMutation({
    mutationFn: (request: ExerciseRequest) => (exercise ? updateExercise(exercise.id, request) : createExercise(request)),
    onSuccess: async (saved) => {
      // Every cached exercise list may now be out of date.
      await queryClient.invalidateQueries({ queryKey: queryKeys.exercises });
      navigate(`/cwiczenia/${saved.id}`, { replace: true });
    },
    onError: (e) => setError(toApiError(e)),
  });

  function set<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const fieldErrors: Record<string, string> = {};
    if (form.name.trim().length < 2) fieldErrors.name = "Nazwa musi mieć od 2 do 100 znaków.";
    if (!form.category) fieldErrors.category = "Wybierz kategorię.";
    if (!form.difficulty) fieldErrors.difficulty = "Wybierz poziom trudności.";
    if (!form.equipment) fieldErrors.equipment = "Wybierz sprzęt.";
    if (Object.keys(fieldErrors).length > 0 || !form.category || !form.difficulty || !form.equipment) {
      setError({ status: null, message: "", fieldErrors });
      return;
    }

    setError(null);
    save.mutate({
      name: form.name,
      description: form.description,
      muscles: form.muscles,
      category: form.category,
      difficulty: form.difficulty,
      equipment: form.equipment,
    });
  }

  const fieldErrors = error?.fieldErrors ?? {};
  const generalError = error && Object.keys(fieldErrors).length === 0 ? error.message : null;

  return (
    <Card>
      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        {generalError && <Alert>{generalError}</Alert>}
        <TextField label="Nazwa" required maxLength={100} value={form.name} onChange={(e) => set("name", e.target.value)} error={fieldErrors.name} />
        <div className="grid gap-4 sm:grid-cols-3">
          <SelectField label="Kategoria" value={form.category} onChange={(e) => set("category", e.target.value as ExerciseCategory | "")} error={fieldErrors.category}>
            <option value="">Wybierz…</option>
            {options(categoryLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
          <SelectField label="Poziom trudności" value={form.difficulty} onChange={(e) => set("difficulty", e.target.value as Difficulty | "")} error={fieldErrors.difficulty}>
            {options(difficultyLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
          <SelectField label="Sprzęt" value={form.equipment} onChange={(e) => set("equipment", e.target.value as Equipment | "")} error={fieldErrors.equipment}>
            <option value="">Wybierz…</option>
            {options(equipmentLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
        </div>
        <TextField
          label="Zaangażowane mięśnie"
          maxLength={200}
          value={form.muscles}
          onChange={(e) => set("muscles", e.target.value)}
          error={fieldErrors.muscles}
          placeholder="np. pośladki, mięśnie dwugłowe ud"
        />
        <TextAreaField
          label="Technika wykonania"
          rows={5}
          maxLength={2000}
          value={form.description}
          onChange={(e) => set("description", e.target.value)}
          error={fieldErrors.description}
        />
        <div className="flex flex-wrap gap-3">
          <Button type="submit" busy={save.isPending}>
            Zapisz
          </Button>
          <Link
            to={exercise ? `/cwiczenia/${exercise.id}` : "/cwiczenia"}
            className="inline-flex min-h-11 items-center rounded-lg px-4 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Anuluj
          </Link>
        </div>
      </form>
    </Card>
  );
}
