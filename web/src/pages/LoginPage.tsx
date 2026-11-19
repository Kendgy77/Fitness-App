import { useState, type FormEvent } from "react";
import { Link } from "react-router";
import { toApiError, type ApiError } from "../api/errors";
import { useAuth } from "../auth/authContext";
import { Alert, Button, TextField } from "../components/ui";
import { AuthLayout } from "./AuthLayout";

/** WF02: log in. After three wrong passwords the API locks the account for 15 minutes (423). */
export function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      // On success the user is logged in and RedirectIfLoggedIn takes them on (see App.tsx).
      await login(email, password);
    } catch (e) {
      setError(toApiError(e));
      setBusy(false);
    }
  }

  return (
    <AuthLayout
      title="Log in"
      footer={
        <>
          No account yet?{" "}
          <Link to="/register" className="font-semibold text-brand-700 hover:underline">
            Sign up
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        {error && <Alert>{lockedMessage(error) ?? error.message}</Alert>}
        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          error={error?.fieldErrors.email}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={error?.fieldErrors.password}
        />
        <Button type="submit" busy={busy} className="w-full">
          Log in
        </Button>
      </form>
    </AuthLayout>
  );
}

/** 423: tell the user how long to wait, rounded up to whole minutes (thesis T04). */
function lockedMessage(error: ApiError) {
  if (error.status !== 423) return null;
  const minutes = Math.max(1, Math.ceil((error.retryAfter ?? 900) / 60));
  return `Your account is locked after three failed login attempts. Try again in ${minutes} min.`;
}
