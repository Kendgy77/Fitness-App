import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import { AxiosError, AxiosHeaders, type AxiosResponse } from "axios";
import { createMemoryRouter, RouterProvider } from "react-router";
import { routes } from "../App";
import type { AuthResponse } from "../api/types";
import { AuthProvider } from "../auth/AuthProvider";

export const testUser: AuthResponse = {
  accessToken: "test-token",
  expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
  user: { id: "u1", email: "ostap@test.local", roles: ["User"] },
};

/** Renders the whole app at a given address, optionally already logged in. */
export function renderApp(path: string, { loggedIn = true } = {}) {
  if (loggedIn) {
    localStorage.setItem("fitnessapp.auth", JSON.stringify(testUser));
  }
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  const router = createMemoryRouter(routes, { initialEntries: [path] });
  render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </QueryClientProvider>,
  );
  return { router };
}

/** What axios throws when the API answers with an error status. */
export function apiError(status: number, data: unknown = {}) {
  const response = { status, statusText: "", data, headers: {}, config: { headers: new AxiosHeaders() } } as AxiosResponse;
  return new AxiosError("Request failed", "ERR_BAD_RESPONSE", undefined, undefined, response);
}
