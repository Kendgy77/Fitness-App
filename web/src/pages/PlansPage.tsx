import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate, useSearchParams } from "react-router";
import { copyPlan, listPlans, listPlanTemplates } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { PlanStatus, PlanSummary } from "../api/types";
import { Alert, Badge, Button, Card, Loading, PageHeader } from "../components/ui";
import { planStatusLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

const filters: { value: PlanStatus | ""; label: string }[] = [
  { value: "", label: "All" },
  { value: "Active", label: "Active" },
  { value: "Draft", label: "Drafts" },
  { value: "Archived", label: "Archived" },
];

/** WF05: the user's training plans, plus templates to start from. */
export function PlansPage() {
  const [params, setParams] = useSearchParams();
  const status = (params.get("status") as PlanStatus | null) || undefined;
  const page = Number(params.get("page")) || 1;
  const query = { status, page, pageSize: 20 };

  const plans = useQuery({
    queryKey: queryKeys.planList(query),
    queryFn: () => listPlans(query),
    placeholderData: keepPreviousData,
  });

  function setFilter(value: string) {
    setParams(value ? { status: value } : {});
  }

  const data = plans.data;

  return (
    <>
      <PageHeader
        title="Training plans"
        actions={
          <Link
            to="/plans/new"
            className="inline-flex min-h-11 items-center rounded-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800"
          >
            New plan
          </Link>
        }
      />

      <div role="group" aria-label="Show plans" className="mb-4 flex flex-wrap gap-2">
        {filters.map((f) => (
          <button
            key={f.label}
            type="button"
            aria-pressed={(status ?? "") === f.value}
            onClick={() => setFilter(f.value)}
            className="min-h-11 rounded-full border border-slate-300 bg-white px-4 text-sm font-medium text-slate-700 aria-pressed:border-brand-700 aria-pressed:bg-brand-700 aria-pressed:text-white"
          >
            {f.label}
          </button>
        ))}
      </div>

      {plans.isPending && <Loading />}
      {plans.isError && <Alert>{toApiError(plans.error).message}</Alert>}
      {data &&
        (data.items.length === 0 ? (
          <Alert kind="info">
            {status ? "No plans with this status." : "You don't have any plans yet. Create one, or start from a template below."}
          </Alert>
        ) : (
          <ul className="grid gap-4 sm:grid-cols-2">
            {data.items.map((plan) => (
              <li key={plan.id}>
                <PlanCard plan={plan} />
              </li>
            ))}
          </ul>
        ))}
      {data && data.totalPages > 1 && (
        <nav aria-label="Pages" className="mt-6 flex items-center justify-between gap-3">
          <Button variant="secondary" disabled={page <= 1} onClick={() => setParams({ ...(status && { status }), page: String(page - 1) })}>
            Previous
          </Button>
          <span className="text-sm text-slate-600">
            Page {data.page} of {data.totalPages}
          </span>
          <Button
            variant="secondary"
            disabled={page >= data.totalPages}
            onClick={() => setParams({ ...(status && { status }), page: String(page + 1) })}
          >
            Next
          </Button>
        </nav>
      )}

      <Templates />
    </>
  );
}

function PlanCard({ plan }: { plan: PlanSummary }) {
  const tone = plan.status === "Active" ? "brand" : plan.status === "Draft" ? "amber" : "slate";
  return (
    <Link
      to={`/plans/${plan.id}`}
      className="flex h-full flex-col rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand-500 hover:shadow"
    >
      <div className="mb-2">
        <Badge tone={tone}>{planStatusLabels[plan.status]}</Badge>
      </div>
      <h2 className="font-semibold text-slate-900">{plan.name}</h2>
      {plan.description && <p className="mt-1 line-clamp-2 text-sm text-slate-600">{plan.description}</p>}
      <p className="mt-auto pt-3 text-xs text-slate-500">
        {plan.exerciseCount} {plan.exerciseCount === 1 ? "exercise" : "exercises"}
      </p>
    </Link>
  );
}

/** Ready-made plans. "Use template" copies one into the user's own draft and opens it for editing. */
function Templates() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const templates = useQuery({ queryKey: queryKeys.planTemplates, queryFn: listPlanTemplates });
  const copy = useMutation({
    mutationFn: (id: number) => copyPlan(id),
    onSuccess: async (plan) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.plans });
      navigate(`/plans/${plan.id}/edit`);
    },
  });

  if (!templates.data || templates.data.items.length === 0) return null;

  return (
    <section className="mt-10" aria-labelledby="templates-heading">
      <h2 id="templates-heading" className="mb-1 text-lg font-bold text-slate-900">
        Start from a template
      </h2>
      <p className="mb-4 text-sm text-slate-600">Using a template makes your own copy, which you can then change.</p>
      {copy.isError && (
        <div className="mb-4">
          <Alert>{toApiError(copy.error).message}</Alert>
        </div>
      )}
      <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {templates.data.items.map((t) => (
          <li key={t.id}>
            <Card className="flex h-full flex-col">
              <h3 className="font-semibold text-slate-900">{t.name}</h3>
              <p className="mt-1 text-sm text-slate-600">{t.description}</p>
              <div className="mt-auto flex flex-wrap items-center gap-3 pt-4">
                <Button busy={copy.isPending && copy.variables === t.id} onClick={() => copy.mutate(t.id)}>
                  Use template
                </Button>
                <Link to={`/plans/${t.id}`} className="text-sm font-semibold text-brand-700 hover:underline">
                  View {t.exerciseCount} exercises
                </Link>
              </div>
            </Card>
          </li>
        ))}
      </ul>
    </section>
  );
}
