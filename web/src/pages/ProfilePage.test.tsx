import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as endpoints from "../api/endpoints";
import type { Profile } from "../api/types";
import { apiError, renderApp } from "../test/render";

vi.mock("../api/endpoints");

const savedProfile: Profile = {
  birthDate: "1999-04-12",
  age: 27,
  sex: "Male",
  heightCm: 183,
  weightKg: 78,
  goal: "BuildMuscle",
  timeZone: "Europe/Warsaw",
  bmi: { value: 23.3, category: "Normal" },
  missingForBmi: [],
  updatedAtUtc: "2026-11-19T10:00:00Z",
};

beforeEach(() => {
  vi.mocked(endpoints.getCurrentMeasurements).mockResolvedValue([]);
});

describe("ProfilePage (WF03, WF10)", () => {
  it("T16: shows the BMI calculated by the API", async () => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(savedProfile);
    renderApp("/profile");

    expect(await screen.findByText("23.3")).toBeInTheDocument();
    expect(screen.getByText("Healthy weight")).toBeInTheDocument();
    expect(screen.getByLabelText("Weight (kg)")).toHaveValue("78");
    // The disclaimer always comes with the number.
    expect(screen.getByText(/can't tell muscle from fat/)).toBeInTheDocument();
  });

  it("T17: without height it asks for the missing data instead of a BMI", async () => {
    vi.mocked(endpoints.getProfile).mockResolvedValue({ ...savedProfile, heightCm: null, bmi: null, missingForBmi: ["heightCm"] });
    renderApp("/profile");

    expect(await screen.findByText(/Add your height to calculate your BMI/)).toBeInTheDocument();
  });

  it("T05: saves the entered data with the browser's time zone, accepting a decimal comma too", async () => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(savedProfile);
    vi.mocked(endpoints.updateProfile).mockResolvedValue({ ...savedProfile, weightKg: 80.5, updatedAtUtc: "2026-11-19T11:00:00Z" });
    renderApp("/profile");
    const user = userEvent.setup();

    const weight = await screen.findByLabelText("Weight (kg)");
    await user.clear(weight);
    await user.type(weight, "80,5");
    await user.click(screen.getByRole("button", { name: "Save profile" }));

    expect(endpoints.updateProfile).toHaveBeenCalledWith(
      expect.objectContaining({ weightKg: 80.5, heightCm: 183, timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone }),
    );
    expect(await screen.findByText("Profile saved.")).toBeInTheDocument();
  });

  it("does not ask the user for a time zone", async () => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(savedProfile);
    renderApp("/profile");

    await screen.findByLabelText("Weight (kg)");
    expect(screen.queryByLabelText(/time zone/i)).not.toBeInTheDocument();
  });

  it("T06: shows the API's validation message next to the weight field", async () => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(savedProfile);
    vi.mocked(endpoints.updateProfile).mockRejectedValue(
      apiError(400, { title: "One or more validation errors occurred.", errors: { WeightKg: ["Body weight must be between 20 and 400 kg."] } }),
    );
    renderApp("/profile");
    const user = userEvent.setup();

    const weight = await screen.findByLabelText("Weight (kg)");
    await user.clear(weight);
    await user.type(weight, "-5");
    await user.click(screen.getByRole("button", { name: "Save profile" }));

    expect(await screen.findByText("Body weight must be between 20 and 400 kg.")).toBeInTheDocument();
    expect(weight).toHaveAttribute("aria-invalid", "true");
  });

  it("rejects text that is not a number before calling the API", async () => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(savedProfile);
    renderApp("/profile");
    const user = userEvent.setup();

    const height = await screen.findByLabelText("Height (cm)");
    await user.clear(height);
    await user.type(height, "abc");
    await user.click(screen.getByRole("button", { name: "Save profile" }));

    expect(await screen.findByText("Enter a number, e.g. 180.")).toBeInTheDocument();
    expect(endpoints.updateProfile).not.toHaveBeenCalled();
  });
});

describe("ProfilePage: body measurements (WF07)", () => {
  const today = () => {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
  };

  beforeEach(() => {
    vi.mocked(endpoints.getProfile).mockResolvedValue(savedProfile);
    vi.mocked(endpoints.getCurrentMeasurements).mockResolvedValue([
      { field: "weightKg", value: 78, measuredOn: "2026-11-20" },
      { field: "waistCm", value: 84.5, measuredOn: "2026-11-10" },
    ]);
  });

  it("starts from the latest values, each with the day it was measured", async () => {
    renderApp("/profile");

    expect(await screen.findByLabelText("Waist (cm)")).toHaveValue("84.5");
    expect(screen.getByLabelText("Chest (cm)")).toHaveValue("");
    expect(screen.getByText("Updated 10 Nov 2026")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "See charts and history" })).toHaveAttribute("href", "/profile/body");
  });

  it("saves the measurements for today together with the profile", async () => {
    vi.mocked(endpoints.saveCurrentMeasurements).mockResolvedValue([]);
    vi.mocked(endpoints.updateProfile).mockResolvedValue(savedProfile);
    renderApp("/profile");
    const user = userEvent.setup();

    const waist = await screen.findByLabelText("Waist (cm)");
    await user.clear(waist);
    await user.type(waist, "83");
    await user.type(screen.getByLabelText("Chest (cm)"), "101,5");
    await user.click(screen.getByRole("button", { name: "Save profile" }));

    expect(await screen.findByText("Profile saved.")).toBeInTheDocument();
    expect(endpoints.saveCurrentMeasurements).toHaveBeenCalledWith(
      expect.objectContaining({ measuredOn: today(), weightKg: 78, waistCm: 83, chestCm: 101.5, calfCm: null }),
    );
    expect(endpoints.updateProfile).toHaveBeenCalledWith(expect.objectContaining({ weightKg: 78 }));
  });

  it("shows the API message next to a measurement out of range", async () => {
    vi.mocked(endpoints.saveCurrentMeasurements).mockRejectedValue(
      apiError(400, { title: "One or more validation errors occurred.", errors: { WaistCm: ["Waist must be between 40 and 200 cm."] } }),
    );
    renderApp("/profile");
    const user = userEvent.setup();

    const waist = await screen.findByLabelText("Waist (cm)");
    await user.clear(waist);
    await user.type(waist, "400");
    await user.click(screen.getByRole("button", { name: "Save profile" }));

    expect(await screen.findByText("Waist must be between 40 and 200 cm.")).toBeInTheDocument();
    expect(endpoints.updateProfile).not.toHaveBeenCalled();
  });
});
