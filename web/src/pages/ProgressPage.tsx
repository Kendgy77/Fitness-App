import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router";
import { getExerciseRanking, getMuscleGroupVolume, getProgressSummary, getRecords, getWeeklyVolume } from "../api/endpoints";
import { toApiError } from "../api/errors";
import type { ExerciseProgressRow, ProgressPeriod } from "../api/types";
import { BarChart, ValueBars } from "../components/charts";
import { PeriodSwitch } from "../components/progress";
import { formatKg } from "../lib/progress";
import { Alert, Card, Loading, PageHeader } from "../components/ui";
import { categoryLabels, formatDate } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

const weekLabel = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short" });

/** Progress (WF07): weekly volume, every exercise ranked, muscle groups and personal records. */
export function ProgressPage() {
  const [period, setPeriod] = useState<ProgressPeriod>("ThreeMonths");
  const summary = useQuery({ queryKey: queryKeys.progressSummary, queryFn: getProgressSummary });

  return (
    <>
      <PageHeader title="Progress" />
      {summary.isPending && <Loading />}
      {summary.isError && <Alert>{toApiError(summary.error).message}</Alert>}
      {summary.data &&
        (summary.data.workoutsTotal === 0 ? (
          <Alert kind="info">
            Log your first workout to see your progress here.{" "}
            <Link to="/workouts/new" className="font-semibold text-brand-700 hover:underline">
              Start a workout
            </Link>
          </Alert>
        ) : (
          <div className="space-y-4">
            <WeeklyVolumeCard />
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-lg font-bold text-slate-900">Exercises and muscle groups</h2>
              <PeriodSwitch value={period} onChange={setPeriod} />
            </div>
            <ExerciseTable period={period} />
            <MuscleGroupsCard period={period} />
            <RecordsCard />
          </div>
        ))}
    </>
  );
}

export function WeeklyVolumeCard({ weeks = 12 }: { weeks?: number }) {
  const volume = useQuery({ queryKey: queryKeys.weeklyVolume(weeks), queryFn: () => getWeeklyVolume(weeks) });
  const data = volume.data ?? [];
  const thisWeek = data.at(-1);
  const trained = data.filter((w) => w.workouts > 0);
  const average = trained.length ? trained.reduce((sum, w) => sum + w.volumeKg, 0) / trained.length : 0;

  return (
    <Card>
      <h2 className="text-lg font-bold text-slate-900">Weekly volume</h2>
      <p className="mb-3 text-sm text-slate-600">Weight × reps of every working set (warm-ups left out), per week.</p>
      {volume.isPending && <Loading />}
      {volume.isError && <Alert>{toApiError(volume.error).message}</Alert>}
      {volume.data && (
        <BarChart
          title={`Weekly training volume, last ${weeks} weeks`}
          unit="kg"
          bars={data.map((w) => ({
            label: weekLabel.format(new Date(w.weekStart)),
            value: w.volumeKg,
            detail: `Week of ${weekLabel.format(new Date(w.weekStart))}: ${formatKg(w.volumeKg)}, ${w.workouts} workouts, ${w.sets} sets`,
          }))}
          caption={
            <>
              This week: <strong>{formatKg(thisWeek?.volumeKg ?? 0)}</strong> in {thisWeek?.workouts ?? 0} workouts. Average of the weeks you
              trained: {formatKg(Math.round(average))}.
            </>
          }
        />
      )}
    </Card>
  );
}

type SortKey = "name" | "workouts" | "volumeKg" | "bestOneRepMaxKg" | "oneRepMaxChangePercent" | "lastDoneAtUtc";

const columns: { key: SortKey; label: string; numeric: boolean }[] = [
  { key: "name", label: "Exercise", numeric: false },
  { key: "workouts", label: "Workouts", numeric: true },
  { key: "volumeKg", label: "Volume", numeric: true },
  { key: "bestOneRepMaxKg", label: "Best 1RM", numeric: true },
  { key: "oneRepMaxChangePercent", label: "1RM change", numeric: true },
  { key: "lastDoneAtUtc", label: "Last done", numeric: false },
];

/** Every exercise done in the period; any column sorts it (empty values always last). */
function ExerciseTable({ period }: { period: ProgressPeriod }) {
  const ranking = useQuery({ queryKey: queryKeys.exerciseRanking(period), queryFn: () => getExerciseRanking(period), placeholderData: keepPreviousData });
  const [sort, setSort] = useState<{ key: SortKey; desc: boolean }>({ key: "workouts", desc: true });

  const rows = [...(ranking.data ?? [])].sort((a, b) => {
    const va = a[sort.key];
    const vb = b[sort.key];
    if (va === null) return 1;
    if (vb === null) return -1;
    const order = typeof va === "string" ? va.localeCompare(vb as string) : va - (vb as number);
    return sort.desc ? -order : order;
  });

  function sortBy(key: SortKey) {
    setSort((s) => (s.key === key ? { key, desc: !s.desc } : { key, desc: key !== "name" }));
  }

  return (
    <Card>
      <h3 className="mb-3 font-bold text-slate-900">Exercises</h3>
      {ranking.isPending && <Loading />}
      {ranking.isError && <Alert>{toApiError(ranking.error).message}</Alert>}
      {ranking.data &&
        (rows.length === 0 ? (
          <p className="text-sm text-slate-600">No workouts in this period.</p>
        ) : (
          // Wide tables scroll sideways on a phone instead of squeezing the numbers.
          <div className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
            <table className="w-full min-w-[36rem] text-sm">
              <thead>
                <tr className="border-b border-slate-200 text-left">
                  {columns.map((c) => (
                    <th
                      key={c.key}
                      scope="col"
                      aria-sort={sort.key === c.key ? (sort.desc ? "descending" : "ascending") : "none"}
                      className={`py-2 pr-3 font-medium text-slate-600 ${c.numeric ? "text-right" : ""}`}
                    >
                      <button type="button" onClick={() => sortBy(c.key)} className="min-h-10 hover:text-slate-900">
                        {c.label}
                        {sort.key === c.key && <span aria-hidden="true">{sort.desc ? " ↓" : " ↑"}</span>}
                      </button>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {rows.map((r) => (
                  <ExerciseRow key={r.exerciseId} row={r} />
                ))}
              </tbody>
            </table>
          </div>
        ))}
    </Card>
  );
}

function ExerciseRow({ row }: { row: ExerciseProgressRow }) {
  const change = row.oneRepMaxChangePercent;
  return (
    <tr>
      <td className="py-2 pr-3">
        <Link to={`/progress/exercises/${row.exerciseId}`} className="font-medium text-slate-900 hover:underline">
          {row.name}
        </Link>
        <span className="block text-xs text-slate-500">{categoryLabels[row.category]}</span>
      </td>
      <td className="py-2 pr-3 text-right tabular-nums">{row.workouts}</td>
      <td className="py-2 pr-3 text-right tabular-nums">{formatKg(row.volumeKg)}</td>
      <td className="py-2 pr-3 text-right tabular-nums">{row.bestOneRepMaxKg !== null ? formatKg(row.bestOneRepMaxKg) : "—"}</td>
      <td className={`py-2 pr-3 text-right tabular-nums ${change === null ? "" : change >= 0 ? "text-brand-700" : "text-red-700"}`}>
        {change === null ? "—" : `${change > 0 ? "+" : ""}${change}%`}
      </td>
      <td className="py-2 pr-3 whitespace-nowrap">{formatDate(row.lastDoneAtUtc)}</td>
    </tr>
  );
}

function MuscleGroupsCard({ period }: { period: ProgressPeriod }) {
  const groups = useQuery({ queryKey: queryKeys.muscleGroups(period), queryFn: () => getMuscleGroupVolume(period), placeholderData: keepPreviousData });
  return (
    <Card>
      <h3 className="font-bold text-slate-900">Sets per week by muscle group</h3>
      <p className="mb-3 text-sm text-slate-600">Working sets, averaged over the period. Around 10–20 a week per muscle is a common target for growth.</p>
      {groups.isPending && <Loading />}
      {groups.isError && <Alert>{toApiError(groups.error).message}</Alert>}
      {groups.data &&
        (groups.data.length === 0 ? (
          <p className="text-sm text-slate-600">No workouts in this period.</p>
        ) : (
          <ValueBars rows={groups.data.map((g) => ({ label: categoryLabels[g.category], value: g.setsPerWeek }))} format={(v) => `${v} sets`} />
        ))}
    </Card>
  );
}

function RecordsCard() {
  const records = useQuery({ queryKey: queryKeys.records, queryFn: getRecords });
  return (
    <Card>
      <h3 className="mb-3 font-bold text-slate-900">Personal records</h3>
      {records.isPending && <Loading />}
      {records.isError && <Alert>{toApiError(records.error).message}</Alert>}
      {records.data &&
        (records.data.length === 0 ? (
          <p className="text-sm text-slate-600">Records appear once you've logged a set with weight.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {records.data.map((r) => (
              <li key={r.exerciseId} className="py-3">
                <Link to={`/progress/exercises/${r.exerciseId}`} className="font-medium text-slate-900 hover:underline">
                  {r.name}
                </Link>
                <dl className="mt-1 grid grid-cols-1 gap-x-4 gap-y-0.5 text-sm sm:grid-cols-3">
                  {r.heaviestSet && (
                    <div>
                      <dt className="inline text-slate-500">Heaviest: </dt>
                      <dd className="inline font-semibold tabular-nums">
                        {formatKg(r.heaviestSet.value)} × {r.heaviestSet.reps}
                      </dd>
                    </div>
                  )}
                  {r.bestOneRepMax && (
                    <div>
                      <dt className="inline text-slate-500">Est. 1RM: </dt>
                      <dd className="inline font-semibold tabular-nums">{formatKg(r.bestOneRepMax.value)}</dd>
                    </div>
                  )}
                  {r.bestSessionVolume && (
                    <div>
                      <dt className="inline text-slate-500">Best workout: </dt>
                      <dd className="inline font-semibold tabular-nums">{formatKg(r.bestSessionVolume.value)}</dd>
                    </div>
                  )}
                </dl>
              </li>
            ))}
          </ul>
        ))}
    </Card>
  );
}
