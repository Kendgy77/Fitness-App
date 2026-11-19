import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router";
import { createExercise, getExercise, updateExercise } from "../api/endpoints";
import { toApiError, type ApiError } from "../api/errors";
import type { Difficulty, Equipment, Exercise, ExerciseCategory, ExerciseRequest } from "../api/types";
import { Alert, Button, Card, Loading, PageHeader, SelectField, TextAreaField, TextField } from "../components/ui";
import { categoryLabels, difficultyLabels, equipmentLabels, options } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** Add a custom exercise (/exercises/new) or edit one (/exercises/:id/edit). Only its author sees it. */
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
      <PageHeader title={id ? "Edit exercise" : "New exercise"} />
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
      navigate(`/exercises/${saved.id}`, { replace: true });
    },
    onError: (e) => setError(toApiError(e)),
  });

  function set<K extends keyof FormState>(field: K, value: FormState[K]) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const fieldErrors: Record<string, string> = {};
    if (form.name.trim().length < 2) fieldErrors.name = "The name must be 2 to 100 characters long.";
    if (!form.category) fieldErrors.category = "Choose a category.";
    if (!form.difficulty) fieldErrors.difficulty = "Choose a difficulty level.";
    if (!form.equipment) fieldErrors.equipment = "Choose the equipment.";
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
        <TextField label="Name" required maxLength={100} value={form.name} onChange={(e) => set("name", e.target.value)} error={fieldErrors.name} />
        <div className="grid gap-4 sm:grid-cols-3">
          <SelectField label="Category" value={form.category} onChange={(e) => set("category", e.target.value as ExerciseCategory | "")} error={fieldErrors.category}>
            <option value="">Choose…</option>
            {options(categoryLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
          <SelectField label="Difficulty" value={form.difficulty} onChange={(e) => set("difficulty", e.target.value as Difficulty | "")} error={fieldErrors.difficulty}>
            {options(difficultyLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
          <SelectField label="Equipment" value={form.equipment} onChange={(e) => set("equipment", e.target.value as Equipment | "")} error={fieldErrors.equipment}>
            <option value="">Choose…</option>
            {options(equipmentLabels).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </SelectField>
        </div>
        <TextField
          label="Muscles worked"
          maxLength={200}
          value={form.muscles}
          onChange={(e) => set("muscles", e.target.value)}
          error={fieldErrors.muscles}
          placeholder="e.g. glutes, hamstrings"
        />
        <TextAreaField
          label="How to do it"
          rows={5}
          maxLength={2000}
          value={form.description}
          onChange={(e) => set("description", e.target.value)}
          error={fieldErrors.description}
        />
        <div className="flex flex-wrap gap-3">
          <Button type="submit" busy={save.isPending}>
            Save
          </Button>
          <Link
            to={exercise ? `/exercises/${exercise.id}` : "/exercises"}
            className="inline-flex min-h-11 items-center rounded-lg px-4 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Cancel
          </Link>
        </div>
      </form>
    </Card>
  );
}
