import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router";
import { listMeasurements } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { Measurement } from "../api/types";
import { LineChart } from "../components/charts";
import { ProgressTabs } from "../components/progress";
import { Alert, Card, Loading, PageHeader } from "../components/ui";
import { formatDate } from "../lib/labels";
import { formatMetric, metrics, metricValue, type ChartMetric } from "../lib/measurements";
import { queryKeys } from "../lib/queryKeys";

const chartable: { key: ChartMetric; label: string; unit: string }[] = [
  { key: "weightKg", label: "Weight", unit: "kg" },
  { key: "bmi", label: "BMI", unit: "" },
  ...metrics.filter((m) => m.key !== "weightKg").map((m) => ({ key: m.key, label: m.label, unit: m.unit })),
];

/** Progress, body half (WF07): a chart for any measurement over time, and every entry. */
export function BodyPage() {
  const measurements = useQuery({ queryKey: queryKeys.measurements, queryFn: () => listMeasurements() });

  return (
    <>
      <PageHeader
        title="Progress"
        actions={
          <Link to="/progress/body/new" className="inline-flex min-h-11 items-center rounded-lg bg-brand-700 px-4 text-sm font-semibold text-white hover:bg-brand-800">
            Add measurements
          </Link>
        }
      />
      <ProgressTabs />
      {measurements.isPending && <Loading />}
      {measurements.isError && <Alert>{toApiError(measurements.error).message}</Alert>}
      {measurements.data &&
        (measurements.data.items.length === 0 ? (
          <Alert kind="info">
            No measurements yet. Add your weight, and the tape measurements if you like, to follow how your body changes.{" "}
            <Link to="/progress/body/guide" className="font-semibold text-brand-700 hover:underline">
              How to measure
            </Link>
          </Alert>
        ) : (
          <BodyView items={measurements.data.items} />
        ))}
    </>
  );
}

function BodyView({ items }: { items: Measurement[] }) {
  // Oldest first for the charts; the list below stays newest first.
  const oldestFirst = [...items].reverse();
  const available = chartable.filter((c) => items.some((m) => metricValue(m, c.key) !== null));
  const [selected, setSelected] = useState<ChartMetric>(available[0]?.key ?? "weightKg");
  const metric = available.find((c) => c.key === selected) ?? available[0]!;
  const points = oldestFirst.flatMap((m) => {
    const value = metricValue(m, metric.key);
    return value === null ? [] : [{ date: m.measuredOn, value }];
  });
  const first = points[0];
  const last = points.at(-1);
  const delta = first && last ? Math.round((last.value - first.value) * 10) / 10 : 0;

  return (
    <div className="space-y-4">
      <Card>
        <div role="group" aria-label="Measurement to show" className="mb-4 flex flex-wrap gap-2">
          {available.map((c) => (
            <button
              key={c.key}
              type="button"
              aria-pressed={metric.key === c.key}
              onClick={() => setSelected(c.key)}
              className={`min-h-10 rounded-full border px-3 text-sm font-medium ${
                metric.key === c.key ? "border-brand-700 bg-brand-700 text-white" : "border-slate-300 bg-white text-slate-700"
              }`}
            >
              {c.label}
            </button>
          ))}
        </div>
        <LineChart
          title={`${metric.label} over time`}
          unit={metric.unit || "BMI"}
          points={points}
          caption={
            first && last && points.length > 1 ? (
              <>
                {metric.label}: {formatMetric(first.value, metric.unit)} on {formatDate(first.date)}, now{" "}
                <strong>{formatMetric(last.value, metric.unit)}</strong> ({delta > 0 ? "+" : ""}
                {formatMetric(delta, metric.unit)}).
              </>
            ) : (
              `${metric.label}: ${formatMetric(last!.value, metric.unit)} on ${formatDate(last!.date)}. Measure again later to see a trend.`
            )
          }
        />
        {metric.key === "bmi" && <p className="mt-2 text-xs text-slate-500">BMI uses your profile height. Only a rough guide: it can't tell muscle from fat.</p>}
      </Card>

      <Card>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-bold text-slate-900">All entries</h2>
          <Link to="/progress/body/guide" className="text-sm font-semibold text-brand-700 hover:underline">
            How to measure
          </Link>
        </div>
        <ul className="divide-y divide-slate-100">
          {items.map((m) => {
            const filled = metrics.filter((x) => m[x.key] !== null);
            return (
              <li key={m.id} className="flex items-center justify-between gap-3 py-3">
                <div className="min-w-0">
                  <p className="font-medium text-slate-900">{formatDate(m.measuredOn)}</p>
                  <p className="truncate text-sm text-slate-600">
                    {filled
                      .slice(0, 3)
                      .map((x) => `${x.label.toLowerCase()} ${formatMetric(m[x.key]!, x.unit)}`)
                      .join(" · ")}
                    {filled.length > 3 && ` · +${filled.length - 3} more`}
                  </p>
                </div>
                <Link
                  to={`/progress/body/${m.id}/edit`}
                  aria-label={`Edit measurements of ${formatDate(m.measuredOn)}`}
                  className="inline-flex min-h-11 shrink-0 items-center rounded-lg border border-slate-300 px-3 text-sm font-semibold text-slate-800 hover:bg-slate-50"
                >
                  Edit
                </Link>
              </li>
            );
          })}
        </ul>
      </Card>
    </div>
  );
}
