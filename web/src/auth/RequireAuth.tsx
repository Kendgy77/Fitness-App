import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router";
import { useAuth } from "./authContext";

/** Pages inside it need a logged-in user; anyone else goes to the login page and comes back afterwards. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const location = useLocation();

  if (!user) {
    return <Navigate to="/logowanie" replace state={{ from: location.pathname + location.search }} />;
  }

  return children;
}

/**
 * For the login and register pages: once a user is logged in, they leave. This is also how a successful login
 * moves on: the page only logs in, and this redirect takes the user to the page they first asked for
 * (remembered by RequireAuth), or to <paramref name="to"/>.
 */
export function RedirectIfLoggedIn({ children, to = "/" }: { children: ReactNode; to?: string }) {
  const { user } = useAuth();
  const location = useLocation();

  if (user) {
    const from = (location.state as { from?: string } | null)?.from;
    return <Navigate to={from ?? to} replace />;
  }

  return children;
}
