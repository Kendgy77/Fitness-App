import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router";
import { archiveExercise, getExercise } from "../api/endpoints";
import { toApiError } from "../api/errors";
import { Alert, Badge, Button, Card, Loading } from "../components/ui";
import { categoryLabels, difficultyLabels, equipmentLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** One exercise: technique, muscles, difficulty and equipment (WF04). Own exercises can be edited or archived. */
export function ExerciseDetailPage() {
  const id = Number(useParams().id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const exercise = useQuery({ queryKey: queryKeys.exercise(id), queryFn: () => getExercise(id) });

  const archive = useMutation({
    mutationFn: () => archiveExercise(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.exercises });
      navigate("/cwiczenia", { replace: true });
    },
  });

  function handleArchive() {
    if (window.confirm("Usunąć to ćwiczenie z biblioteki? Zniknie z listy, ale zostanie w historii treningów.")) {
      archive.mutate();
    }
  }

  return (
    <>
      <Link to="/cwiczenia" className="mb-4 inline-flex min-h-11 items-center text-sm font-medium text-brand-700 hover:underline">
        ← Biblioteka ćwiczeń
      </Link>

      {exercise.isPending && <Loading />}
      {exercise.isError && <Alert>{toApiError(exercise.error).message}</Alert>}
      {exercise.data && (
        <Card>
          <div className="mb-3 flex flex-wrap gap-1.5">
            <Badge tone="brand">{categoryLabels[exercise.data.category]}</Badge>
            <Badge>{difficultyLabels[exercise.data.difficulty]}</Badge>
            <Badge>{equipmentLabels[exercise.data.equipment]}</Badge>
            {exercise.data.isCustom && <Badge tone="amber">Własne</Badge>}
            {exercise.data.isArchived && <Badge>Zarchiwizowane</Badge>}
          </div>
          <h1 className="text-2xl font-bold text-slate-900">{exercise.data.name}</h1>

          {exercise.data.muscles && (
            <>
              <h2 className="mt-5 text-sm font-semibold text-slate-900">Zaangażowane mięśnie</h2>
              <p className="mt-1 text-slate-700">{exercise.data.muscles}</p>
            </>
          )}

          {exercise.data.description && (
            <>
              <h2 className="mt-5 text-sm font-semibold text-slate-900">Technika wykonania</h2>
              <p className="mt-1 whitespace-pre-line text-slate-700">{exercise.data.description}</p>
            </>
          )}

          {exercise.data.isCustom && !exercise.data.isArchived && (
            <div className="mt-6 flex flex-wrap gap-3 border-t border-slate-100 pt-5">
              <Link
                to={`/cwiczenia/${id}/edycja`}
                className="inline-flex min-h-11 items-center rounded-lg border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-800 hover:bg-slate-50"
              >
                Edytuj
              </Link>
              <Button variant="danger" busy={archive.isPending} onClick={handleArchive}>
                Usuń z biblioteki
              </Button>
            </div>
          )}
          {archive.isError && (
            <div className="mt-4">
              <Alert>{toApiError(archive.error).message}</Alert>
            </div>
          )}
        </Card>
      )}
    </>
  );
}
