import type { Measurement, MeasurementValues } from "../api/types";

/** Every measurement the app knows, in the order the form and the charts show them. */
export interface MetricInfo {
  key: keyof MeasurementValues;
  label: string;
  unit: "kg" | "%" | "cm";
  min: number;
  max: number;
  /** How to measure it, one sentence (the guide page shows them all with the drawing). */
  how: string;
  /** The number on the body drawing in the guide; tape measurements only. */
  point?: number;
}

// Ranges match MeasurementRequest in the API (MeasurementDtos.cs).
export const metrics: MetricInfo[] = [
  { key: "weightKg", label: "Weight", unit: "kg", min: 20, max: 400, how: "In the morning, after the toilet and before eating or drinking." },
  { key: "bodyFatPercent", label: "Body fat", unit: "%", min: 2, max: 70, how: "From a smart scale, calipers or an InBody test. Use the same method every time." },
  { key: "muscleMassKg", label: "Muscle mass", unit: "kg", min: 5, max: 200, how: "From a smart scale or an InBody test." },
  { key: "neckCm", label: "Neck", unit: "cm", min: 20, max: 80, point: 1, how: "Just below the Adam's apple, tape sloping slightly down to the front." },
  { key: "chestCm", label: "Chest", unit: "cm", min: 50, max: 200, point: 2, how: "Around the nipples, arms down, after a normal breath out." },
  { key: "waistCm", label: "Waist", unit: "cm", min: 40, max: 200, point: 3, how: "At the navel, standing relaxed, after a normal breath out. Don't pull the belly in." },
  { key: "hipsCm", label: "Hips", unit: "cm", min: 50, max: 200, point: 4, how: "Around the widest part of the buttocks, feet together." },
  { key: "bicepsLeftCm", label: "Biceps (left)", unit: "cm", min: 15, max: 80, point: 5, how: "Halfway between shoulder and elbow, arm hanging relaxed." },
  { key: "bicepsRightCm", label: "Biceps (right)", unit: "cm", min: 15, max: 80, point: 5, how: "Same place on the right arm." },
  { key: "forearmCm", label: "Forearm", unit: "cm", min: 15, max: 60, point: 6, how: "The widest part, just below the elbow, hand open and relaxed." },
  { key: "thighLeftCm", label: "Thigh (left)", unit: "cm", min: 25, max: 120, point: 7, how: "Halfway between the hip crease and the top of the knee, standing, weight on both legs." },
  { key: "thighRightCm", label: "Thigh (right)", unit: "cm", min: 25, max: 120, point: 7, how: "Same place on the right leg." },
  { key: "calfCm", label: "Calf", unit: "cm", min: 20, max: 80, point: 8, how: "The widest part of the calf, standing, weight on both legs." },
];

/** The BMI isn't typed in but calculated, and it can be charted like the rest. */
export type ChartMetric = MetricInfo["key"] | "bmi";

export function metricValue(m: Measurement, key: ChartMetric) {
  return key === "bmi" ? (m.bmi?.value ?? null) : m[key];
}

/** "82.5 kg", "15.2 %", "84 cm". */
export function formatMetric(value: number, unit: string) {
  return `${new Intl.NumberFormat("en-GB", { maximumFractionDigits: 1 }).format(value)} ${unit}`;
}
