import axios from "axios";

/** An API failure turned into what a form needs: one message to show, plus messages per field. */
export interface ApiError {
  status: number | null;
  message: string;
  fieldErrors: Record<string, string>;
  /** Seconds until a locked account can try again (423). */
  retryAfter?: number;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  retryAfter?: number;
}

const fallbackMessages: Record<number, string> = {
  400: "Sprawdź wprowadzone dane.",
  401: "Sesja wygasła. Zaloguj się ponownie.",
  403: "Nie masz uprawnień do tej operacji.",
  404: "Nie znaleziono.",
  409: "Operacja koliduje z istniejącymi danymi.",
  429: "Zbyt wiele prób. Odczekaj chwilę i spróbuj ponownie.",
};

/**
 * The API answers errors as ProblemDetails (RFC 7807). Validation errors name fields either as the C# property
 * ("WeightKg") or as JSON paths ("$.sex"); both become the camelCase form field name ("weightKg", "sex").
 */
export function toApiError(error: unknown): ApiError {
  if (!axios.isAxiosError(error)) {
    return { status: null, message: "Wystąpił nieoczekiwany błąd.", fieldErrors: {} };
  }

  if (!error.response) {
    return { status: null, message: "Brak połączenia z serwerem. Sprawdź internet i spróbuj ponownie.", fieldErrors: {} };
  }

  const { status } = error.response;
  const problem = (error.response.data ?? {}) as ProblemDetails;

  const fieldErrors: Record<string, string> = {};
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    const field = toFieldName(key);
    if (field && messages[0] && !fieldErrors[field]) {
      fieldErrors[field] = messages[0];
    }
  }

  const message =
    status >= 500 ? "Wystąpił błąd serwera. Spróbuj ponownie później." : problem.title ?? fallbackMessages[status] ?? "Wystąpił błąd.";

  return { status, message, fieldErrors, retryAfter: problem.retryAfter };
}

function toFieldName(key: string): string {
  const name = key.startsWith("$.") ? key.slice(2) : key;
  return name.charAt(0).toLowerCase() + name.slice(1);
}
