import { AxiosError } from "axios";
import { describe, expect, it } from "vitest";
import { apiError } from "../test/render";
import { FIX_FIELDS, toApiError } from "./errors";

describe("toApiError", () => {
  it("uses the ProblemDetails title as the message", () => {
    expect(toApiError(apiError(401, { title: "Invalid email or password." })).message).toBe("Invalid email or password.");
  });

  it("maps C# property names and JSON paths to camelCase form fields", () => {
    const error = toApiError(apiError(400, { errors: { WeightKg: ["Too heavy."], "$.sex": ["Bad value."], birthDate: ["Bad date."] } }));

    expect(error.fieldErrors).toEqual({ weightKg: "Too heavy.", sex: "Bad value.", birthDate: "Bad date." });
  });

  it("keeps retryAfter of a locked account", () => {
    expect(toApiError(apiError(423, { title: "Account locked", retryAfter: 840 })).retryAfter).toBe(840);
  });

  it("never shows server internals for a 500", () => {
    expect(toApiError(apiError(500, { title: "NullReferenceException" })).message).toBe("Something went wrong on our side. Try again in a bit.");
  });

  it("reports a missing connection", () => {
    expect(toApiError(new AxiosError("Network Error", "ERR_NETWORK")).message).toMatch(/Can't reach the server/);
  });
});

describe("toApiError: the message above a form", () => {
  it("points at the fields instead of repeating the API's general validation title", () => {
    const error = toApiError(apiError(400, { title: "One or more validation errors occurred.", errors: { WeightKg: ["Too heavy."] } }));

    expect(error.message).toBe(FIX_FIELDS);
    expect(error.fieldErrors.weightKg).toBe("Too heavy.");
  });

  it("keeps the API's own sentence when there is no field to show it next to", () => {
    expect(toApiError(apiError(409, { title: "Only draft plans can be deleted. Archive this plan instead." })).message).toBe(
      "Only draft plans can be deleted. Archive this plan instead.",
    );
  });
});
