import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router";
import { deleteWorkout, getWorkout } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { Workout } from "../api/types";
import { Alert, Badge, Button, Card, Loading } from "../components/ui";
import { categoryLabels, formatDateTime, formatMinutes, sessionStatusLabels, sessionTypeLabels, setTypeLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";
import { describeSet, describeTarget } from "../lib/workout";

/** One workout with every set, as it was saved. */
export function WorkoutDetailPage() {
  const id = Number(useParams().id);
  const workout = useQuery({ queryKey: queryKeys.workout(id), queryFn: () => getWorkout(id) });

  return (
    <>
      <Link to="/workouts" className="mb-4 inline-flex min-h-11 items-center text-sm font-medium text-brand-700 hover:underline">
        ← Workouts
      </Link>
      {workout.isPending && <Loading />}
      {workout.isError && <Alert>{toApiError(workout.error).message}</Alert>}
      {workout.data && <WorkoutView workout={workout.data} />}
    </>
  );
}

function WorkoutView({ workout }: { workout: Workout }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const active = workout.status === "InProgress" || workout.status === "Paused";
  const remove = useMutation({
    mutationFn: () => deleteWorkout(workout.id),
    onSuccess: async () => {
      queryClient.removeQueries({ queryKey: queryKeys.workout(workout.id) });
      if (active) queryClient.setQueryData(queryKeys.activeWorkout, null);
      await queryClient.invalidateQueries({ queryKey: queryKeys.workouts, refetchType: "none" });
      await queryClient.invalidateQueries({ queryKey: queryKeys.progress, refetchType: "none" });
      navigate("/workouts", { replace: true });
    },
  });
  const setCount = workout.exercises.reduce((n, e) => n + e.sets.length, 0);

  return (
    <Card>
      <div className="mb-3 flex flex-wrap gap-1.5">
        <Badge tone={workout.status === "Completed" ? "brand" : "amber"}>{sessionStatusLabels[workout.status]}</Badge>
        <Badge>{sessionTypeLabels[workout.type]}</Badge>
      </div>
      <h1 className="text-2xl font-bold text-slate-900">{workout.name}</h1>
      <p className="mt-1 text-slate-600">
        {workout.startedAtUtc && formatDateTime(workout.startedAtUtc)}
        {workout.durationMinutes !== null && ` · ${formatMinutes(workout.durationMinutes)}`}
        {` · ${setCount} sets`}
      </p>
      {workout.notes && <p className="mt-3 whitespace-pre-line text-slate-700">{workout.notes}</p>}

      <ol className="mt-5 space-y-4">
        {workout.exercises.map((e) => {
          const target = describeTarget(e);
          return (
            <li key={e.id} className="border-t border-slate-100 pt-4">
              <h2 className="font-semibold text-slate-900">
                <span className="mr-2 text-sm text-slate-400">{e.order}.</span>
                <Link to={`/exercises/${e.exerciseId}`} className="hover:underline">
                  {e.exerciseName}
                </Link>
                <span className="ml-2 text-xs font-normal text-slate-500">{categoryLabels[e.category]}</span>
              </h2>
              {(target || e.notes) && (
                <p className="text-sm text-slate-500">
                  {target && `Target ${target}`}
                  {target && e.notes && " · "}
                  {e.notes}
                </p>
              )}
              <ol className="mt-2 flex flex-wrap gap-2">
                {e.sets.map((s) => (
                  <li key={s.id} className="rounded-lg bg-slate-100 px-3 py-1.5 text-sm text-slate-800 tabular-nums">
                    <span className="mr-1.5 text-slate-400">{s.setNumber}</span>
                    {describeSet(s)}
                    {s.type !== "Normal" && <span className="ml-1.5 text-xs text-amber-800">{setTypeLabels[s.type]}</span>}
                  </li>
                ))}
                {e.sets.length === 0 && <li className="text-sm text-slate-500">No sets yet.</li>}
              </ol>
            </li>
          );
        })}
      </ol>

      <div className="mt-6 flex flex-wrap gap-3 border-t border-slate-100 pt-5">
        {remove.isError && (
          <div className="w-full">
            <Alert>{toApiError(remove.error).message}</Alert>
          </div>
        )}
        {active ? (
          <Link to="/workouts/live" className="inline-flex min-h-11 items-center rounded-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800">
            Continue workout
          </Link>
        ) : (
          workout.status === "Completed" && (
            <Link
              to={`/workouts/${workout.id}/edit`}
              className="inline-flex min-h-11 items-center rounded-lg border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-800 hover:bg-slate-50"
            >
              Edit
            </Link>
          )
        )}
        <Button variant="danger" busy={remove.isPending} onClick={() => window.confirm("Delete this workout? This can't be undone.") && remove.mutate()}>
          Delete
        </Button>
      </div>
    </Card>
  );
}
