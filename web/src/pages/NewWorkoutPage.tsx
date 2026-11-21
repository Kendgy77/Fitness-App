import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router";
import { getActiveWorkout, getStartOptions, startWorkout } from "../api/endpoints";
import { errorMessage, toApiError } from "../api/errors";
import { Alert, Badge, Loading, PageHeader } from "../components/ui";
import { formatDate } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/**
 * "New workout": start from one of your plans (big cards, most recently used first), start an empty workout, or
 * enter a workout you already did. With a workout already running, it offers to continue that one instead.
 */
export function NewWorkoutPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const active = useQuery({ queryKey: queryKeys.activeWorkout, queryFn: getActiveWorkout });
  const options = useQuery({ queryKey: queryKeys.startOptions, queryFn: getStartOptions });

  const start = useMutation({
    mutationFn: (planId: number | null) => startWorkout(planId),
    onSuccess: async (workout) => {
      queryClient.setQueryData(queryKeys.activeWorkout, workout);
      await queryClient.invalidateQueries({ queryKey: queryKeys.workouts, refetchType: "none" });
      navigate("/workouts/live");
    },
  });

  if (active.isPending) return <Loading />;

  if (active.data) {
    return (
      <>
        <PageHeader title="New workout" />
        <Alert kind="info">
          <p className="font-semibold">{active.data.name} is still in progress.</p>
          <p className="mt-1">Finish or discard it before starting another one.</p>
          <Link
            to="/workouts/live"
            className="mt-3 inline-flex min-h-11 items-center rounded-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800"
          >
            Continue workout
          </Link>
        </Alert>
      </>
    );
  }

  return (
    <>
      <PageHeader title="New workout" />
      {start.isError && (
        <div className="mb-4">
          <Alert>{errorMessage(start.error)}</Alert>
        </div>
      )}

      <div className="grid gap-3 sm:grid-cols-2">
        <button
          type="button"
          disabled={start.isPending}
          onClick={() => start.mutate(null)}
          className="flex min-h-24 flex-col items-start justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white p-4 text-left hover:border-brand-500 hover:bg-brand-50 disabled:opacity-60"
        >
          <span className="text-lg font-bold text-slate-900">Empty workout</span>
          <span className="text-sm text-slate-600">Add exercises as you go.</span>
        </button>
        <Link
          to="/workouts/log"
          className="flex min-h-24 flex-col items-start justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white p-4 hover:border-brand-500 hover:bg-brand-50"
        >
          <span className="text-lg font-bold text-slate-900">Log a past workout</span>
          <span className="text-sm text-slate-600">Enter a workout you already did.</span>
        </Link>
      </div>

      <h2 className="mt-8 mb-3 text-lg font-bold text-slate-900">Start from a plan</h2>
      {options.isPending && <Loading />}
      {options.isError && <Alert>{toApiError(options.error).message}</Alert>}
      {options.data &&
        (options.data.length === 0 ? (
          <Alert kind="info">
            No active plans yet. <Link to="/plans" className="font-semibold text-brand-700 hover:underline">Make one</Link> and activate it.
          </Alert>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {options.data.map((plan) => (
              <li key={plan.id}>
                <button
                  type="button"
                  disabled={start.isPending}
                  onClick={() => start.mutate(plan.id)}
                  aria-label={`Start ${plan.name}`}
                  className="flex min-h-24 w-full flex-col items-start justify-center gap-1 rounded-xl border border-slate-200 bg-white p-4 text-left shadow-sm hover:border-brand-500 hover:bg-brand-50 disabled:opacity-60"
                >
                  <span className="flex flex-wrap items-center gap-2">
                    <span className="text-lg font-bold text-slate-900">{plan.name}</span>
                    {plan.isTemplate && <Badge>Template</Badge>}
                  </span>
                  <span className="text-sm text-slate-600">
                    {plan.exerciseCount} exercises
                    {plan.lastUsedAtUtc ? ` · last done ${formatDate(plan.lastUsedAtUtc)}` : " · not done yet"}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        ))}
    </>
  );
}
