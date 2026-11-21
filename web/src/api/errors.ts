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

/** Shown above a form when the problems are listed next to the fields themselves. */
export const FIX_FIELDS = "A couple of things need fixing. Check the fields below.";

const fallbackMessages: Record<number, string> = {
  400: "Something doesn't look right. Check what you entered and try again.",
  401: "You've been logged out. Log in again to keep going.",
  403: "You can't change this one.",
  404: "Couldn't find that. It may have been deleted.",
  409: "That clashes with something you already have.",
  429: "Too many tries. Give it a minute and try again.",
};

/**
 * The API answers errors as ProblemDetails (RFC 7807). Validation errors name fields either as the C# property
 * ("WeightKg") or as JSON paths ("$.sex"); both become the camelCase form field name ("weightKg", "sex").
 */
export function toApiError(error: unknown): ApiError {
  if (!axios.isAxiosError(error)) {
    return { status: null, message: "Something went wrong. Try again.", fieldErrors: {} };
  }

  if (!error.response) {
    return { status: null, message: "Can't reach the server. Check your connection and try again.", fieldErrors: {} };
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
    status >= 500
      ? "Something went wrong on our side. Try again in a bit."
      : // With messages next to the fields, the API's general "One or more validation errors occurred." adds nothing.
        Object.keys(fieldErrors).length > 0
        ? FIX_FIELDS
        : (problem.title ?? fallbackMessages[status] ?? "Something went wrong. Try again.");

  return { status, message, fieldErrors, retryAfter: problem.retryAfter };
}

/**
 * One sentence for screens without a form field to put it next to (the live workout): the API's own message
 * about the problem ("Log at least one set…") rather than the general "One or more validation errors occurred."
 */
export function errorMessage(error: unknown): string {
  const apiError = toApiError(error);
  return Object.values(apiError.fieldErrors)[0] ?? apiError.message;
}

// "Exercises[0].TargetSets" → "exercises[0].targetSets": every part of the path in camelCase.
function toFieldName(key: string): string {
  const path = key.startsWith("$.") ? key.slice(2) : key;
  return path
    .split(".")
    .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
    .join(".");
}
