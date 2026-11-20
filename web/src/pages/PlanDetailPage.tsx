import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router";
import { changePlanStatus, copyPlan, deletePlan, getPlan } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { Plan, PlanStatus } from "../api/types";
import { Alert, Badge, Button, Card, Loading } from "../components/ui";
import { categoryLabels, formatDecimal, planStatusLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** One plan with its exercises, and the actions its status allows (see PlanStateMachine in the API). */
export function PlanDetailPage() {
  const id = Number(useParams().id);
  const plan = useQuery({ queryKey: queryKeys.plan(id), queryFn: () => getPlan(id) });

  return (
    <>
      <Link to="/plans" className="mb-4 inline-flex min-h-11 items-center text-sm font-medium text-brand-700 hover:underline">
        ← Training plans
      </Link>
      {plan.isPending && <Loading />}
      {plan.isError && <Alert>{toApiError(plan.error).message}</Alert>}
      {plan.data && <PlanView plan={plan.data} />}
    </>
  );
}

function PlanView({ plan }: { plan: Plan }) {
  const tone = plan.status === "Active" ? "brand" : plan.status === "Draft" ? "amber" : "slate";

  return (
    <Card>
      <div className="mb-3 flex flex-wrap gap-1.5">
        {plan.isTemplate ? <Badge>Template</Badge> : <Badge tone={tone}>{planStatusLabels[plan.status]}</Badge>}
      </div>
      <h1 className="text-2xl font-bold text-slate-900">{plan.name}</h1>
      {plan.description && <p className="mt-2 whitespace-pre-line text-slate-700">{plan.description}</p>}

      {plan.exercises.length === 0 ? (
        <div className="mt-5">
          <Alert kind="info">This plan has no exercises yet.</Alert>
        </div>
      ) : (
        <ol className="mt-5 divide-y divide-slate-100 border-y border-slate-100">
          {plan.exercises.map((e) => (
            <li key={e.order} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 py-3">
              <div className="min-w-0">
                <span className="mr-2 text-sm text-slate-400">{e.order}.</span>
                <Link to={`/exercises/${e.exerciseId}`} className="font-medium text-slate-900 hover:underline">
                  {e.exerciseName}
                </Link>
                <span className="ml-2 text-xs text-slate-500">{categoryLabels[e.category]}</span>
                {e.exerciseIsArchived && <span className="ml-2 text-xs text-amber-700">(removed from library)</span>}
                {e.notes && <p className="mt-0.5 text-sm text-slate-600">{e.notes}</p>}
              </div>
              <p className="text-sm font-semibold whitespace-nowrap text-slate-800">
                {e.targetSets} × {e.targetReps}
                {e.targetWeightKg !== null && <span className="font-normal text-slate-600"> @ {formatDecimal(e.targetWeightKg)} kg</span>}
              </p>
            </li>
          ))}
        </ol>
      )}

      <PlanActions plan={plan} />
    </Card>
  );
}

/** The buttons match the API's rules: what you can't do isn't offered. */
function PlanActions({ plan }: { plan: Plan }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const refresh = async (updated?: Plan) => {
    if (updated) queryClient.setQueryData(queryKeys.plan(updated.id), updated);
    await queryClient.invalidateQueries({ queryKey: queryKeys.plans, refetchType: "none" });
  };

  const status = useMutation({
    mutationFn: (to: PlanStatus) => changePlanStatus(plan.id, to),
    onSuccess: refresh,
  });
  const copy = useMutation({
    mutationFn: () => copyPlan(plan.id),
    onSuccess: async (created) => {
      await refresh();
      navigate(`/plans/${created.id}/edit`);
    },
  });
  const remove = useMutation({
    mutationFn: () => deletePlan(plan.id),
    onSuccess: async () => {
      queryClient.removeQueries({ queryKey: queryKeys.plan(plan.id) });
      await refresh();
      navigate("/plans", { replace: true });
    },
  });

  const error = status.error ?? copy.error ?? remove.error;
  const busy = status.isPending || copy.isPending || remove.isPending;

  return (
    <div className="mt-6 border-t border-slate-100 pt-5">
      {error && (
        <div className="mb-4">
          <Alert>{toApiError(error).message}</Alert>
        </div>
      )}
      <div className="flex flex-wrap gap-3">
        {!plan.isTemplate && plan.status !== "Archived" && (
          <Link
            to={`/plans/${plan.id}/edit`}
            className="inline-flex min-h-11 items-center rounded-lg border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-800 hover:bg-slate-50"
          >
            Edit
          </Link>
        )}
        {!plan.isTemplate && plan.status === "Draft" && (
          <Button busy={status.isPending && status.variables === "Active"} disabled={busy} onClick={() => status.mutate("Active")}>
            Activate
          </Button>
        )}
        {!plan.isTemplate && plan.status !== "Archived" && (
          <Button variant="secondary" busy={status.isPending && status.variables === "Archived"} disabled={busy} onClick={() => status.mutate("Archived")}>
            Archive
          </Button>
        )}
        {!plan.isTemplate && plan.status === "Archived" && (
          <Button variant="secondary" busy={status.isPending} disabled={busy} onClick={() => status.mutate("Draft")}>
            Restore as draft
          </Button>
        )}
        <Button variant={plan.isTemplate ? "primary" : "secondary"} busy={copy.isPending} disabled={busy} onClick={() => copy.mutate()}>
          {plan.isTemplate ? "Use template" : "Make a copy"}
        </Button>
        {!plan.isTemplate && plan.status === "Draft" && (
          <Button
            variant="danger"
            busy={remove.isPending}
            disabled={busy}
            onClick={() => window.confirm("Delete this draft plan? This can't be undone.") && remove.mutate()}
          >
            Delete draft
          </Button>
        )}
      </div>
      {!plan.isTemplate && plan.status === "Draft" && (
        <p className="mt-3 text-sm text-slate-500">Activate the plan when it's ready, so you can use it for workouts.</p>
      )}
    </div>
  );
}
