import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { setAccessToken, setUnauthorizedHandler } from "../api/client";
import * as endpoints from "../api/endpoints";
import type { AuthResponse } from "../api/types";
import { AuthContext, type AuthState } from "./authContext";

const storageKey = "fitnessapp.auth";

/**
 * Keeps the login (token + user) in localStorage, so a page refresh doesn't log the user out. An expired token is
 * dropped on start-up; one that expires while the app is open is dropped on the first 401 from the API.
 */
function loadSession(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(storageKey);
    if (!raw) return null;
    const session = JSON.parse(raw) as AuthResponse;
    if (new Date(session.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(storageKey);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

function saveSession(session: AuthResponse | null) {
  try {
    if (session) localStorage.setItem(storageKey, JSON.stringify(session));
    else localStorage.removeItem(storageKey);
  } catch {
    // Storage can be unavailable (private mode); the session then lasts until the tab closes.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [session, setSession] = useState<AuthResponse | null>(() => {
    const loaded = loadSession();
    setAccessToken(loaded?.accessToken ?? null);
    return loaded;
  });

  const startSession = useCallback((next: AuthResponse) => {
    setAccessToken(next.accessToken);
    saveSession(next);
    setSession(next);
  }, []);

  const endSession = useCallback(() => {
    setAccessToken(null);
    saveSession(null);
    setSession(null);
    // Cached data belongs to the user who just left; the next user must never see it.
    queryClient.clear();
  }, [queryClient]);

  useEffect(() => {
    setUnauthorizedHandler(endSession);
    return () => setUnauthorizedHandler(null);
  }, [endSession]);

  const value = useMemo<AuthState>(
    () => ({
      user: session?.user ?? null,
      login: async (email, password) => startSession(await endpoints.login(email, password)),
      register: async (email, password) => startSession(await endpoints.register(email, password)),
      logout: () => {
        // The token is stateless, so logging out is forgetting it; telling the API is best effort.
        void endpoints.logout().catch(() => undefined);
        endSession();
      },
    }),
    [session, startSession, endSession],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
