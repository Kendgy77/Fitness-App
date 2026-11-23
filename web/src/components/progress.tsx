import { NavLink } from "react-router";
import type { ProgressPeriod } from "../api/types";
import { periodLabels } from "../lib/progress";


/** The time window of the statistics, as a row of toggle buttons. */
export function PeriodSwitch({ value, onChange }: { value: ProgressPeriod; onChange: (period: ProgressPeriod) => void }) {
  return (
    <div role="group" aria-label="Period" className="inline-flex flex-wrap gap-1 rounded-lg bg-slate-100 p-1">
      {(Object.keys(periodLabels) as ProgressPeriod[]).map((p) => (
        <button
          key={p}
          type="button"
          aria-pressed={value === p}
          onClick={() => onChange(p)}
          className={`min-h-10 rounded-md px-3 text-sm font-medium ${value === p ? "bg-white text-slate-900 shadow-sm" : "text-slate-600 hover:text-slate-900"}`}
        >
          {periodLabels[p]}
        </button>
      ))}
    </div>
  );
}

/** Strength / Body: the two halves of the progress section. */
export function ProgressTabs() {
  const tab = ({ isActive }: { isActive: boolean }) =>
    `inline-flex min-h-11 items-center border-b-2 px-4 text-sm font-semibold ${
      isActive ? "border-brand-600 text-brand-800" : "border-transparent text-slate-600 hover:text-slate-900"
    }`;
  return (
    <nav aria-label="Progress sections" className="mb-6 flex border-b border-slate-200">
      <NavLink to="/progress" end className={tab}>
        Strength
      </NavLink>
      <NavLink to="/progress/body" className={tab}>
        Body
      </NavLink>
    </nav>
  );
}

