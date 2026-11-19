import { createBrowserRouter, type RouteObject } from "react-router";
import { RedirectIfLoggedIn, RequireAuth } from "./auth/RequireAuth";
import { Layout } from "./components/Layout";
import { DashboardPage } from "./pages/DashboardPage";
import { ExerciseDetailPage } from "./pages/ExerciseDetailPage";
import { ExerciseFormPage } from "./pages/ExerciseFormPage";
import { ExercisesPage } from "./pages/ExercisesPage";
import { LoginPage } from "./pages/LoginPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ProfilePage } from "./pages/ProfilePage";
import { RegisterPage } from "./pages/RegisterPage";

// Every screen of the app. Addresses are Polish, like the UI. Everything under Layout needs a logged-in user.
export const routes: RouteObject[] = [
  {
    path: "/logowanie",
    element: (
      <RedirectIfLoggedIn>
        <LoginPage />
      </RedirectIfLoggedIn>
    ),
  },
  {
    path: "/rejestracja",
    element: (
      // A new user starts by filling in the profile, so BMI and later progress have data.
      <RedirectIfLoggedIn to="/profil">
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
      { path: "profil", element: <ProfilePage /> },
      { path: "cwiczenia", element: <ExercisesPage /> },
      { path: "cwiczenia/nowe", element: <ExerciseFormPage /> },
      { path: "cwiczenia/:id", element: <ExerciseDetailPage /> },
      { path: "cwiczenia/:id/edycja", element: <ExerciseFormPage /> },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export function createAppRouter() {
  return createBrowserRouter(routes);
}
