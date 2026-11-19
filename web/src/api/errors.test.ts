import { AxiosError } from "axios";
import { describe, expect, it } from "vitest";
import { apiError } from "../test/render";
import { toApiError } from "./errors";

describe("toApiError", () => {
  it("uses the ProblemDetails title as the message", () => {
    expect(toApiError(apiError(401, { title: "Nieprawidłowy e-mail lub hasło." })).message).toBe("Nieprawidłowy e-mail lub hasło.");
  });

  it("maps C# property names and JSON paths to camelCase form fields", () => {
    const error = toApiError(apiError(400, { errors: { WeightKg: ["Za dużo."], "$.sex": ["Zła wartość."], birthDate: ["Zła data."] } }));

    expect(error.fieldErrors).toEqual({ weightKg: "Za dużo.", sex: "Zła wartość.", birthDate: "Zła data." });
  });

  it("keeps retryAfter of a locked account", () => {
    expect(toApiError(apiError(423, { title: "Konto zablokowane", retryAfter: 840 })).retryAfter).toBe(840);
  });

  it("never shows server internals for a 500", () => {
    expect(toApiError(apiError(500, { title: "NullReferenceException" })).message).toBe("Wystąpił błąd serwera. Spróbuj ponownie później.");
  });

  it("reports a missing connection", () => {
    expect(toApiError(new AxiosError("Network Error", "ERR_NETWORK")).message).toMatch(/Brak połączenia/);
  });
});
