import { createBrowserRouter, type RouteObject } from "react-router";
import { RedirectIfLoggedIn, RequireAuth } from "./auth/RequireAuth";
import { Layout } from "./components/Layout";
import { DashboardPage } from "./pages/DashboardPage";
import { ExerciseDetailPage } from "./pages/ExerciseDetailPage";
import { ExerciseFormPage } from "./pages/ExerciseFormPage";
import { ExercisesPage } from "./pages/ExercisesPage";
import { LiveWorkoutPage } from "./pages/LiveWorkoutPage";
import { LoginPage } from "./pages/LoginPage";
import { NewWorkoutPage } from "./pages/NewWorkoutPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { PlanDetailPage } from "./pages/PlanDetailPage";
import { PlanEditorPage } from "./pages/PlanEditorPage";
import { PlansPage } from "./pages/PlansPage";
import { ProfilePage } from "./pages/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage";
import { WorkoutDetailPage } from "./pages/WorkoutDetailPage";
import { WorkoutFormPage } from "./pages/WorkoutFormPage";
import { WorkoutsPage } from "./pages/WorkoutsPage";

// Every screen of the app. Everything under Layout needs a logged-in user.
export const routes: RouteObject[] = [
  {
    path: "/login",
    element: (
      <RedirectIfLoggedIn>
        <LoginPage />
      </RedirectIfLoggedIn>
    ),
  },
  {
    path: "/register",
    element: (
      // A new user starts by filling in the profile, so BMI and later progress have data.
      <RedirectIfLoggedIn to="/profile">
        <RegisterPage />
      </RedirectIfLoggedIn>
    ),
  },
  {
    element: (
      <RequireAuth>
        <Layout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: "profile", element: <ProfilePage /> },
      { path: "exercises", element: <ExercisesPage /> },
      { path: "exercises/new", element: <ExerciseFormPage /> },
      { path: "exercises/:id", element: <ExerciseDetailPage /> },
      { path: "exercises/:id/edit", element: <ExerciseFormPage /> },
      { path: "plans", element: <PlansPage /> },
      { path: "plans/new", element: <PlanEditorPage /> },
      { path: "plans/:id", element: <PlanDetailPage /> },
      { path: "plans/:id/edit", element: <PlanEditorPage /> },
      { path: "workouts", element: <WorkoutsPage /> },
      { path: "workouts/new", element: <NewWorkoutPage /> },
      { path: "workouts/live", element: <LiveWorkoutPage /> },
      { path: "workouts/log", element: <WorkoutFormPage /> },
      { path: "workouts/:id", element: <WorkoutDetailPage /> },
      { path: "workouts/:id/edit", element: <WorkoutFormPage /> },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export function createAppRouter() {
  return createBrowserRouter(routes);
}
