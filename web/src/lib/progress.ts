import type { ProgressPeriod } from "../api/types";

export const periodLabels: Record<ProgressPeriod, string> = {
  FourWeeks: "4 weeks",
  ThreeMonths: "3 months",
  Year: "1 year",
  All: "All",
};

const kg = new Intl.NumberFormat("en-GB", { maximumFractionDigits: 1 });

/** 12500 → "12,500 kg", 101.3 → "101.3 kg". */
export function formatKg(value: number) {
  return `${kg.format(value)} kg`;
}
