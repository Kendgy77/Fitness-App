import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Profile } from "../api/types";
import { apiError, renderApp, testUser } from "../test/render";

vi.mock("../api/endpoints");

const emptyProfile: Profile = {
  birthDate: null,
  age: null,
  sex: null,
  heightCm: null,
  weightKg: null,
  goal: null,
  timeZone: "Europe/Warsaw",
  bmi: null,
  missingForBmi: ["weightKg", "heightCm"],
  updatedAtUtc: null,
};

async function submitLogin(password: string) {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText("Email"), "ostap@test.local");
  await user.type(screen.getByLabelText("Password"), password);
  await user.click(screen.getByRole("button", { name: "Log in" }));
}

describe("LoginPage (WF02)", () => {
  beforeEach(() => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(emptyProfile);
  });

  it("sends anonymous users from a protected page to the login page", async () => {
    renderApp("/profile", { loggedIn: false });

    expect(await screen.findByRole("heading", { name: "Log in" })).toBeInTheDocument();
  });

  it("T03: logs in and opens the page the user wanted", async () => {
    vi.mocked(endpoints.login).mockResolvedValue(testUser);
    renderApp("/profile", { loggedIn: false });

    await submitLogin("Trening123!");

    expect(await screen.findByRole("heading", { name: "Profile" })).toBeInTheDocument();
    expect(endpoints.login).toHaveBeenCalledWith("ostap@test.local", "Trening123!");
  });

  it("shows the API's message for a wrong password", async () => {
    vi.mocked(endpoints.login).mockRejectedValue(apiError(401, { title: "Invalid email or password." }));
    renderApp("/login", { loggedIn: false });

    await submitLogin("wrong-password");

    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid email or password.");
  });

  it("T04: tells a locked-out user how many minutes to wait", async () => {
    vi.mocked(endpoints.login).mockRejectedValue(apiError(423, { title: "Account locked", retryAfter: 840 }));
    renderApp("/login", { loggedIn: false });

    await submitLogin("wrong-password");

    expect(await screen.findByRole("alert")).toHaveTextContent("Try again in 14 min.");
  });
});
