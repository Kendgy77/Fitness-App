import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { getExerciseStats } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { ExerciseStats, ProgressPeriod } from "../api/types";
import { LineChart, ValueBars } from "../components/charts";
import { PeriodSwitch } from "../components/progress";
import { formatKg, periodLabels } from "../lib/progress";
import { Alert, Card, Loading } from "../components/ui";
import { categoryLabels, formatDate } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/** One exercise in detail for a period (Orest's idea): 1RM over time, top sets, rep ranges, how often. */
export function ExerciseStatsPage() {
  const id = Number(useParams().id);
  const [period, setPeriod] = useState<ProgressPeriod>("ThreeMonths");
  const stats = useQuery({ queryKey: queryKeys.exerciseStats(id, period), queryFn: () => getExerciseStats(id, period), placeholderData: keepPreviousData });

  return (
    <>
      <Link to="/progress" className="mb-4 inline-flex min-h-11 items-center text-sm font-medium text-brand-700 hover:underline">
        ← Progress
      </Link>
      {stats.isPending && <Loading />}
      {stats.isError && <Alert>{toApiError(stats.error).message}</Alert>}
      {stats.data && <StatsView stats={stats.data} period={period} onPeriod={setPeriod} />}
    </>
  );
}

function StatsView({ stats, period, onPeriod }: { stats: ExerciseStats; period: ProgressPeriod; onPeriod: (p: ProgressPeriod) => void }) {
  const oneRepMax = stats.workouts.filter((w) => w.bestOneRepMaxKg !== null).map((w) => ({ date: w.startedAtUtc, value: w.bestOneRepMaxKg! }));
  const change = stats.oneRepMaxChangePercent;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">{stats.name}</h1>
          <p className="text-sm text-slate-600">
            {categoryLabels[stats.category]} ·{" "}
            <Link to={`/exercises/${stats.exerciseId}`} className="font-medium text-brand-700 hover:underline">
              About this exercise
            </Link>
          </p>
        </div>
        <PeriodSwitch value={period} onChange={onPeriod} />
      </div>

      {stats.workouts.length === 0 ? (
        <Alert kind="info">No finished workouts with this exercise in the last {periodLabels[period].toLowerCase()}.</Alert>
      ) : (
        <>
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            <Stat label="Best estimated 1RM" value={stats.bestOneRepMaxKg !== null ? formatKg(stats.bestOneRepMaxKg) : "—"} />
            <Stat
              label="1RM change"
              value={change === null ? "—" : `${change > 0 ? "+" : ""}${change}%`}
              tone={change === null ? undefined : change >= 0 ? "up" : "down"}
            />
            <Stat label="Heaviest set" value={stats.heaviestSet ? `${formatKg(stats.heaviestSet.value)} × ${stats.heaviestSet.reps}` : "—"} />
            <Stat label="Workouts per week" value={String(stats.workoutsPerWeek)} />
          </div>

          <Card>
            <h2 className="font-bold text-slate-900">Estimated 1RM</h2>
            <p className="mb-3 text-sm text-slate-600">The heaviest single rep you could probably lift, worked out from your best set each workout (sets up to 12 reps).</p>
            {oneRepMax.length > 0 ? (
              <LineChart
                title={`${stats.name}: estimated one-rep max per workout`}
                unit="kg"
                points={oneRepMax}
                caption={
                  oneRepMax.length > 1
                    ? `From ${formatKg(oneRepMax[0]!.value)} on ${formatDate(oneRepMax[0]!.date)} to ${formatKg(oneRepMax.at(-1)!.value)} on ${formatDate(oneRepMax.at(-1)!.date)}.`
                    : `${formatKg(oneRepMax[0]!.value)} on ${formatDate(oneRepMax[0]!.date)}. One more workout and this becomes a line.`
                }
              />
            ) : (
              <p className="text-sm text-slate-600">No estimate: this exercise has no sets with weight and up to 12 reps.</p>
            )}
          </Card>

          <Card>
            <h2 className="mb-3 font-bold text-slate-900">Reps per set</h2>
            <ValueBars rows={stats.repRanges.map((r) => ({ label: `${r.range} reps`, value: r.sets }))} format={(v) => `${v} sets`} />
          </Card>

          <Card>
            <h2 className="mb-3 font-bold text-slate-900">Workouts</h2>
            <ul className="divide-y divide-slate-100">
              {[...stats.workouts].reverse().map((w) => (
                <li key={w.sessionId} className="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-1 py-2 text-sm">
                  <Link to={`/workouts/${w.sessionId}`} className="font-medium text-slate-900 hover:underline">
                    {formatDate(w.startedAtUtc)}
                  </Link>
                  <span className="text-slate-700 tabular-nums">
                    {w.topSetWeightKg !== null && `top ${formatKg(w.topSetWeightKg)} × ${w.topSetReps} · `}
                    {w.workingSets} sets · {formatKg(w.volumeKg)}
                  </span>
                </li>
              ))}
            </ul>
          </Card>
        </>
      )}
    </div>
  );
}

function Stat({ label, value, tone }: { label: string; value: string; tone?: "up" | "down" }) {
  return (
    <Card>
      <p className="text-sm text-slate-600">{label}</p>
      <p className={`mt-1 text-xl font-bold tabular-nums ${tone === "up" ? "text-brand-700" : tone === "down" ? "text-red-700" : "text-slate-900"}`}>{value}</p>
    </Card>
  );
}
