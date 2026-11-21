import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { getActiveWorkout, getProfile, listWorkouts } from "../api/endpoints";
import { toApiError } from "../api/errors";
import { useAuth } from "../auth/authContext";
import { BmiCard } from "../components/BmiCard";
import { Alert, Card, Loading, PageHeader } from "../components/ui";
import { formatDate, formatDecimal, goalLabels } from "../lib/labels";
import { queryKeys } from "../lib/queryKeys";

/**
 * The page after login (thesis mock-up 6.2). For now: profile summary and BMI. Workout statistics and progress
 * charts join it in week 5, once there are workouts to count.
 */
export function DashboardPage() {
  const { user } = useAuth();
  const profile = useQuery({ queryKey: queryKeys.profile, queryFn: getProfile });

  return (
    <>
      <PageHeader title="Dashboard" />
      <p className="-mt-4 mb-6 text-slate-600">Welcome, {user?.email}!</p>

      <WorkoutCard />

      {profile.isPending && <Loading />}
      {profile.isError && <Alert>{toApiError(profile.error).message}</Alert>}
      {profile.data && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <Card>
            <h2 className="text-sm font-medium text-slate-600">Weight</h2>
            <p className="mt-1 text-3xl font-bold text-slate-900">
              {profile.data.weightKg !== null ? `${formatDecimal(profile.data.weightKg)} kg` : "—"}
            </p>
            {profile.data.goal && <p className="mt-2 text-sm text-slate-600">Goal: {goalLabels[profile.data.goal]}</p>}
          </Card>
          <Card>
            <BmiCard profile={profile.data} showProfileLink />
          </Card>
          <Card>
            <h2 className="text-sm font-medium text-slate-600">Exercise library</h2>
            <p className="mt-2 text-sm text-slate-700">Over 60 exercises with technique tips and the muscles they work.</p>
            <Link to="/exercises" className="mt-3 inline-block text-sm font-semibold text-brand-700 hover:underline">
              Browse exercises
            </Link>
          </Card>
        </div>
      )}
    </>
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
