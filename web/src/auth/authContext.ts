import { createContext, useContext } from "react";
import type { UserResponse } from "../api/types";

export interface AuthState {
  user: UserResponse | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthState | null>(null);

/** The logged-in user and the login/logout actions. Works anywhere inside <AuthProvider>. */
export function useAuth() {
  const auth = useContext(AuthContext);
  if (!auth) {
    throw new Error("useAuth must be used inside <AuthProvider>.");
  }
  return auth;
}
