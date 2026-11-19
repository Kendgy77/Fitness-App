import { useState, type FormEvent } from "react";
import { Link } from "react-router";
import { toApiError, type ApiError } from "../api/errors";
import { useAuth } from "../auth/authContext";
import { Alert, Button, TextField } from "../components/ui";
import { AuthLayout } from "./AuthLayout";

/** WF01: create an account. The API logs the new user in straight away (thesis T01). */
export function RegisterPage() {
  const { register } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<ApiError | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    // Only a typo check; the real password rules are enforced by the API.
    if (password !== confirm) {
      setError({ status: null, message: "", fieldErrors: { confirm: "The passwords don't match." } });
      return;
    }

    setBusy(true);
    setError(null);
    try {
      // On success the user is logged in and RedirectIfLoggedIn sends them to the profile (see App.tsx).
      await register(email, password);
    } catch (e) {
      setError(toApiError(e));
      setBusy(false);
    }
  }

  const hasFieldErrors = error && Object.keys(error.fieldErrors).length > 0;

  return (
    <AuthLayout
      title="Create an account"
      footer={
        <>
          Already have an account?{" "}
          <Link to="/login" className="font-semibold text-brand-700 hover:underline">
            Log in
          </Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} noValidate className="space-y-4">
        {error && !hasFieldErrors && <Alert>{error.message}</Alert>}
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
          autoComplete="new-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={error?.fieldErrors.password}
          hint="At least 8 characters, with an upper-case letter, a lower-case letter and a digit."
        />
        <TextField
          label="Repeat password"
          type="password"
          autoComplete="new-password"
          required
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          error={error?.fieldErrors.confirm}
        />
        <Button type="submit" busy={busy} className="w-full">
          Create account
        </Button>
      </form>
    </AuthLayout>
  );
}
