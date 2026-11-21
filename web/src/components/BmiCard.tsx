import { Link } from "react-router";
import type { Profile } from "../api/types";
import { bmiLabels, formatDecimal } from "../lib/labels";

const missingLabels = { weightKg: "your weight", heightCm: "your height" } as const;

// Position on the 15–35 scale, as a percentage of the bar.
function scalePosition(bmi: number) {
  const clamped = Math.min(35, Math.max(15, bmi));
  return ((clamped - 15) / 20) * 100;
}

/** WF10: BMI from the profile. Without weight or height it asks for the missing data instead (thesis T17). */
export function BmiCard({ profile, showProfileLink = false }: { profile: Profile; showProfileLink?: boolean }) {
  const { bmi } = profile;

  return (
    <div>
      <h2 className="text-sm font-medium text-slate-600">Body Mass Index (BMI)</h2>
      {bmi ? (
        <>
          <p className="mt-1 flex items-baseline gap-3">
            <span className="text-3xl font-bold text-slate-900">{formatDecimal(bmi.value)}</span>
            <span className="font-medium text-slate-700">{bmiLabels[bmi.category]}</span>
          </p>
          <div className="relative mt-4" aria-hidden="true">
            {/* WHO bands on a 15–35 scale: <18.5, 18.5–25, 25–30, ≥30. */}
            <div className="flex h-2 overflow-hidden rounded-full">
              <div className="w-[17.5%] bg-sky-300" />
              <div className="w-[32.5%] bg-brand-500" />
              <div className="w-[25%] bg-amber-400" />
              <div className="w-[25%] bg-red-400" />
            </div>
            <div
              className="absolute -top-1 size-4 -translate-x-1/2 rounded-full border-2 border-white bg-slate-900 shadow"
              style={{ left: `${scalePosition(bmi.value)}%` }}
            />
          </div>
          <p className="mt-3 text-xs text-slate-500">
            BMI is body weight (kg) divided by height (m) squared. Categories follow the WHO ranges for adults.
          </p>
          {/* Week 2 of the plan asks for a disclaimer: BMI is a screening number, and lifters are the classic case
              where it misleads, because muscle weighs as much as fat. */}
          <p className="mt-2 text-xs text-slate-500">
            <span className="font-semibold text-slate-600">Only a rough guide.</span> BMI can't tell muscle from fat, so
            people who train often get a higher number than their body fat suggests. It isn't a diagnosis: for health
            questions, ask a doctor.
          </p>
        </>
      ) : (
        <p className="mt-2 text-sm text-slate-700">
          Add {profile.missingForBmi.map((f) => missingLabels[f]).join(" and ")} to calculate your BMI.
          {showProfileLink && (
            <>
              {" "}
              <Link to="/profile" className="font-semibold text-brand-700 hover:underline">
                Go to profile
              </Link>
            </>
          )}
        </p>
      )}
    </div>
  );
}
