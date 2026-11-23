import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { getActiveWorkout, getProfile, getProgressSummary, listWorkouts } from "../api/endpoints";
import { toApiError } from "../api/errors";
import { useAuth } from "../auth/authContext";
import { BmiCard } from "../components/BmiCard";
import { formatKg } from "../lib/progress";
import { Alert, Card, Loading, PageHeader } from "../components/ui";
import { formatDate, formatDecimal, formatMinutes, goalLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";
import { WeeklyVolumeCard } from "./ProgressPage";

/**
 * The page after login (thesis mock-up 6.2, WF07): start or continue a workout, this week's numbers, weight and
 * BMI, the weekly volume chart and the last workouts.
 */
export function DashboardPage() {
  const { user } = useAuth();
  const profile = useQuery({ queryKey: queryKeys.profile, queryFn: getProfile });
  const summary = useQuery({ queryKey: queryKeys.progressSummary, queryFn: getProgressSummary });
  const s = summary.data;

  return (
    <>
      <PageHeader title="Dashboard" />
      <p className="-mt-4 mb-6 text-slate-600">Welcome, {user?.email}!</p>

      <WorkoutCard />

      {(profile.isPending || summary.isPending) && <Loading />}
      {profile.isError && <Alert>{toApiError(profile.error).message}</Alert>}
      {summary.isError && <Alert>{toApiError(summary.error).message}</Alert>}
      {profile.data && s && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            <Card>
              <h2 className="text-sm font-medium text-slate-600">This week</h2>
              <p className="mt-1 text-3xl font-bold text-slate-900">{s.workoutsThisWeek}</p>
              <p className="text-sm text-slate-600">
                {s.workoutsThisWeek === 1 ? "workout" : "workouts"}
                {s.streakWeeks > 1 && ` · ${s.streakWeeks}-week streak`}
              </p>
            </Card>
            <Card>
              <h2 className="text-sm font-medium text-slate-600">This month</h2>
              <p className="mt-1 text-3xl font-bold text-slate-900">{s.workoutsThisMonth}</p>
              <p className="text-sm text-slate-600">{s.workoutsThisMonth === 1 ? "workout" : "workouts"}</p>
            </Card>
            <Card>
              <h2 className="text-sm font-medium text-slate-600">Volume this week</h2>
              <p className="mt-1 text-3xl font-bold text-slate-900 tabular-nums">{formatKg(s.volumeThisWeekKg)}</p>
              <Link to="/progress" className="text-sm font-semibold text-brand-700 hover:underline">
                See progress
              </Link>
            </Card>
            <Card>
              <h2 className="text-sm font-medium text-slate-600">Weight</h2>
              <p className="mt-1 text-3xl font-bold text-slate-900">{s.weightKg !== null ? `${formatDecimal(s.weightKg)} kg` : "—"}</p>
              <p className="text-sm text-slate-600">
                {s.weightMeasuredOn ? `${formatDate(s.weightMeasuredOn)} · ` : ""}
                <Link to="/progress/body/new" className="font-semibold text-brand-700 hover:underline">
                  Add measurements
                </Link>
              </p>
              {profile.data.goal && <p className="mt-1 text-sm text-slate-600">Goal: {goalLabels[profile.data.goal]}</p>}
            </Card>
          </div>

          <div className="grid gap-4 lg:grid-cols-3">
            <div className="lg:col-span-2">
              {s.workoutsTotal > 0 ? (
                <WeeklyVolumeCard weeks={8} />
              ) : (
                <Card>
                  <h2 className="text-lg font-bold text-slate-900">Weekly volume</h2>
                  <p className="mt-2 text-slate-600">Your training chart appears here after your first workout.</p>
                  <Link to="/workouts/new" className="mt-3 inline-block font-semibold text-brand-700 hover:underline">
                    Log your first workout
                  </Link>
                </Card>
              )}
            </div>
            <Card>
              <BmiCard profile={profile.data} showProfileLink />
            </Card>
          </div>

          <RecentWorkouts />
        </div>
      )}
    </>
  );
}

function RecentWorkouts() {
  const recentQuery = { status: "Completed", pageSize: 5 } as const;
  const recent = useQuery({ queryKey: queryKeys.workoutList(recentQuery), queryFn: () => listWorkouts(recentQuery) });
  if (!recent.data || recent.data.items.length === 0) return null;

  return (
    <Card>
      <div className="mb-2 flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-slate-900">Last workouts</h2>
        <Link to="/workouts" className="text-sm font-semibold text-brand-700 hover:underline">
          All workouts
        </Link>
      </div>
      <ul className="divide-y divide-slate-100">
        {recent.data.items.map((w) => (
          <li key={w.id}>
            <Link to={`/workouts/${w.id}`} className="flex min-h-11 flex-wrap items-baseline justify-between gap-x-4 py-2 hover:underline">
              <span className="font-medium text-slate-900">{w.name}</span>
              <span className="text-sm text-slate-600">
                {w.startedAtUtc && formatDate(w.startedAtUtc)}
                {w.durationMinutes !== null && ` · ${formatMinutes(w.durationMinutes)}`} · {w.setCount} sets
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </Card>
  );
}

/** The first thing at the gym: continue the workout in progress, or start one; plus the last workout done. */
function WorkoutCard() {
  const active = useQuery({ queryKey: queryKeys.activeWorkout, queryFn: getActiveWorkout });
  const lastQuery = { status: "Completed", pageSize: 1 } as const;
  const last = useQuery({ queryKey: queryKeys.workoutList(lastQuery), queryFn: () => listWorkouts(lastQuery) });
  const lastWorkout = last.data?.items[0];

  return (
    <Card className="mb-4 flex flex-wrap items-center justify-between gap-4">
      <div className="min-w-0">
        {active.data ? (
          <>
            <h2 className="text-lg font-bold text-slate-900">{active.data.name}</h2>
            <p className="text-sm text-slate-600">Workout in progress.</p>
          </>
        ) : (
          <>
            <h2 className="text-lg font-bold text-slate-900">Ready to train?</h2>
            <p className="text-sm text-slate-600">
              {lastWorkout?.startedAtUtc ? (
                <>
                  Last workout:{" "}
                  <Link to={`/workouts/${lastWorkout.id}`} className="font-medium text-brand-700 hover:underline">
                    {lastWorkout.name}
                  </Link>
                  , {formatDate(lastWorkout.startedAtUtc)}
                </>
              ) : (
                "No workouts logged yet."
              )}
            </p>
          </>
        )}
      </div>
      <Link
        to={active.data ? "/workouts/live" : "/workouts/new"}
        className="inline-flex min-h-12 items-center rounded-lg bg-brand-700 px-5 text-base font-semibold text-white hover:bg-brand-800"
      >
        {active.data ? "Continue workout" : "Start workout"}
      </Link>
    </Card>
  );
}
