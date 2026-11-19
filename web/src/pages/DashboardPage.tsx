import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { getProfile } from "../api/endpoints";
import { toApiError } from "../api/errors";
import { useAuth } from "../auth/authContext";
import { BmiCard } from "../components/BmiCard";
import { Alert, Card, Loading, PageHeader } from "../components/ui";
import { formatDecimal, goalLabels } from "../lib/labels";
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
