import { useEffect, useRef, useState, type ReactNode, type RefObject } from "react";
import { formatDecimal } from "../lib/labels";

// Two small SVG charts drawn by hand instead of a chart library: a few dozen lines each, nothing extra to
// download or keep up to date, and every part is plain to explain. Each chart is a <figure> whose caption says
// in words what it shows, so a screen reader user gets the same information (WCAG 2.1, thesis WNF04).

const MARGIN = { left: 52, right: 16, top: 16, bottom: 30 };

/**
 * The chart's real width in pixels. Drawing at the real width (instead of a fixed size scaled to fit) keeps the
 * text at 12 px on a phone and on a big screen alike.
 */
function useChartWidth(): [RefObject<HTMLElement | null>, number] {
  const ref = useRef<HTMLElement>(null);
  const [width, setWidth] = useState(600);
  useEffect(() => {
    const element = ref.current;
    if (!element || typeof ResizeObserver === "undefined") return;
    const observer = new ResizeObserver(([entry]) => entry && setWidth(Math.max(240, Math.round(entry.contentRect.width))));
    observer.observe(element);
    return () => observer.disconnect();
  }, []);
  return [ref, width];
}

const shortDate = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short" });

/** Up to `count` evenly spaced, rounded tick values covering [min, max]. */
function ticks(min: number, max: number, count = 4) {
  if (min === max) return [min];
  const rawStep = (max - min) / count;
  const magnitude = 10 ** Math.floor(Math.log10(rawStep));
  const step = [1, 2, 2.5, 5, 10].map((m) => m * magnitude).find((s) => s >= rawStep) ?? rawStep;
  const first = Math.ceil(min / step) * step;
  const values = [];
  for (let v = first; v <= max + step / 1000; v += step) values.push(Math.round(v * 1000) / 1000);
  return values;
}

function Frame({
  boxRef,
  width,
  title,
  caption,
  height,
  children,
}: {
  boxRef: RefObject<HTMLElement | null>;
  width: number;
  title: string;
  caption: ReactNode;
  height: number;
  children: ReactNode;
}) {
  return (
    <figure ref={boxRef}>
      <svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label={title} className="h-auto w-full overflow-visible text-slate-500">
        {children}
      </svg>
      <figcaption className="mt-2 text-sm text-slate-600">{caption}</figcaption>
    </figure>
  );
}

export interface LinePoint {
  /** ISO date or date-time. */
  date: string;
  value: number;
}

/** A value over time (estimated 1RM, body weight), dates to scale on the x axis. */
export function LineChart({
  points,
  unit,
  title,
  caption,
  height = 220,
}: {
  points: LinePoint[];
  unit: string;
  title: string;
  caption: ReactNode;
  height?: number;
}) {
  const [boxRef, WIDTH] = useChartWidth();
  const plotW = WIDTH - MARGIN.left - MARGIN.right;
  const plotH = height - MARGIN.top - MARGIN.bottom;
  const times = points.map((p) => Date.parse(p.date));
  const values = points.map((p) => p.value);
  const tMin = Math.min(...times);
  const tMax = Math.max(...times);
  // A little room above and below, so the line never touches the frame.
  const spread = Math.max(...values) - Math.min(...values) || Math.max(1, Math.abs(values[0] ?? 1) * 0.1);
  const yMin = Math.min(...values) - spread * 0.15;
  const yMax = Math.max(...values) + spread * 0.15;

  const x = (t: number) => MARGIN.left + (tMax === tMin ? plotW / 2 : ((t - tMin) / (tMax - tMin)) * plotW);
  const y = (v: number) => MARGIN.top + plotH - ((v - yMin) / (yMax - yMin)) * plotH;
  const path = points.map((p, i) => `${i === 0 ? "M" : "L"}${x(times[i]!).toFixed(1)} ${y(p.value).toFixed(1)}`).join(" ");
  const xLabels = points.length <= 1 ? points : [points[0]!, ...(points.length > 2 ? [points[Math.floor(points.length / 2)]!] : []), points.at(-1)!];

  return (
    <Frame boxRef={boxRef} width={WIDTH} title={title} caption={caption} height={height}>
      <g fontSize="12" fill="currentColor">
        {ticks(yMin, yMax).map((v) => (
          <g key={v}>
            <line x1={MARGIN.left} x2={WIDTH - MARGIN.right} y1={y(v)} y2={y(v)} className="stroke-slate-200" />
            <text x={MARGIN.left - 6} y={y(v) + 4} textAnchor="end">
              {formatDecimal(v)}
            </text>
          </g>
        ))}
        <text x={4} y={MARGIN.top - 4}>
          {unit}
        </text>
        {xLabels.map((p) => (
          <text key={p.date} x={x(Date.parse(p.date))} y={height - 8} textAnchor="middle">
            {shortDate.format(new Date(p.date))}
          </text>
        ))}
      </g>
      <path d={path} fill="none" className="stroke-brand-600" strokeWidth={2.5} strokeLinejoin="round" strokeLinecap="round" />
      {points.map((p, i) => (
        <circle key={`${p.date}-${i}`} cx={x(times[i]!)} cy={y(p.value)} r={4.5} className="fill-white stroke-brand-700" strokeWidth={2}>
          <title>{`${shortDate.format(new Date(p.date))}: ${formatDecimal(p.value)} ${unit}`}</title>
        </circle>
      ))}
    </Frame>
  );
}

export interface Bar {
  label: string;
  value: number;
  /** The tooltip, e.g. "Week of 16 Nov: 5,200 kg in 3 workouts". */
  detail: string;
}

/** Amounts side by side (weekly volume). Bars start at zero, as bars must, so their heights compare honestly. */
export function BarChart({ bars, unit, title, caption, height = 200 }: { bars: Bar[]; unit: string; title: string; caption: ReactNode; height?: number }) {
  const [boxRef, WIDTH] = useChartWidth();
  const plotW = WIDTH - MARGIN.left - MARGIN.right;
  const plotH = height - MARGIN.top - MARGIN.bottom;
  const max = Math.max(1, ...bars.map((b) => b.value));
  const yTicks = ticks(0, max);
  const yMax = Math.max(max, yTicks.at(-1) ?? max);
  const slot = plotW / Math.max(1, bars.length);
  const barW = Math.min(40, slot * 0.7);
  // Label every bar when there's room, otherwise every second or third, counting back from the newest.
  const labelEvery = Math.ceil(bars.length / Math.max(2, Math.floor(plotW / 60)));
  const y = (v: number) => MARGIN.top + plotH - (v / yMax) * plotH;

  return (
    <Frame boxRef={boxRef} width={WIDTH} title={title} caption={caption} height={height}>
      <g fontSize="12" fill="currentColor">
        {yTicks.map((v) => (
          <g key={v}>
            <line x1={MARGIN.left} x2={WIDTH - MARGIN.right} y1={y(v)} y2={y(v)} className="stroke-slate-200" />
            <text x={MARGIN.left - 6} y={y(v) + 4} textAnchor="end">
              {v >= 10_000 ? `${formatDecimal(v / 1000)}k` : formatDecimal(v)}
            </text>
          </g>
        ))}
        <text x={4} y={MARGIN.top - 4}>
          {unit}
        </text>
        {bars.map((b, i) =>
          (bars.length - 1 - i) % labelEvery === 0 ? (
            <text key={b.label} x={MARGIN.left + slot * i + slot / 2} y={height - 8} textAnchor="middle">
              {b.label}
            </text>
          ) : null,
        )}
      </g>
      {bars.map((b, i) => (
        <rect
          key={b.label}
          x={MARGIN.left + slot * i + (slot - barW) / 2}
          y={y(b.value)}
          width={barW}
          height={Math.max(0, MARGIN.top + plotH - y(b.value))}
          rx={4}
          className={i === bars.length - 1 ? "fill-brand-600" : "fill-brand-300"}
        >
          <title>{b.detail}</title>
        </rect>
      ))}
    </Frame>
  );
}

/** Horizontal bars with their numbers written out: easier than a chart for a handful of named values. */
export function ValueBars({ rows, format }: { rows: { label: string; value: number }[]; format: (value: number) => string }) {
  const max = Math.max(1, ...rows.map((r) => r.value));
  return (
    <ul className="space-y-2">
      {rows.map((r) => (
        <li key={r.label} className="grid grid-cols-[7rem_1fr_auto] items-center gap-3 text-sm">
          <span className="truncate text-slate-700">{r.label}</span>
          <span className="h-3 overflow-hidden rounded-full bg-slate-100" aria-hidden="true">
            <span className="block h-full rounded-full bg-brand-500" style={{ width: `${(r.value / max) * 100}%` }} />
          </span>
          <span className="font-semibold text-slate-900 tabular-nums">{format(r.value)}</span>
        </li>
      ))}
    </ul>
  );
}
