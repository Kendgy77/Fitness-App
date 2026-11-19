import { AxiosError } from "axios";
import { describe, expect, it } from "vitest";
import { apiError } from "../test/render";
import { toApiError } from "./errors";

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
    expect(toApiError(apiError(500, { title: "NullReferenceException" })).message).toBe("Server error. Please try again later.");
  });

  it("reports a missing connection", () => {
    expect(toApiError(new AxiosError("Network Error", "ERR_NETWORK")).message).toMatch(/Can't reach the server/);
  });
});
