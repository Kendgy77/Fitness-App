import { Link } from "react-router";
import { Card, PageHeader } from "../components/ui";
import { metrics } from "../lib/measurements";

/** Where each tape measurement goes: a drawing with numbered points, and how to take each one. */
export function MeasureGuidePage() {
  const tape = metrics.filter((m) => m.point !== undefined);

  return (
    <>
      <Link to="/progress/body" className="mb-4 inline-flex min-h-11 items-center text-sm font-medium text-brand-700 hover:underline">
        ← Body progress
      </Link>
      <PageHeader title="How to measure" />

      <div className="grid gap-4 lg:grid-cols-[18rem_1fr] lg:items-start">
        <Card className="mx-auto w-full max-w-xs">
          <BodyDiagram />
        </Card>

        <div className="space-y-4">
          <Card>
            <h2 className="mb-2 text-lg font-bold text-slate-900">Every time</h2>
            <ul className="list-disc space-y-1 pl-5 text-slate-700">
              <li>Measure in the morning, before eating, and on the same day of the week if you can.</li>
              <li>Keep the muscles relaxed unless it says otherwise.</li>
              <li>Keep the tape level all the way round and snug on the skin, without squeezing.</li>
              <li>Measure twice and write down the average.</li>
              <li>For arms and legs, always use the same side (or measure both).</li>
            </ul>
          </Card>

          <Card>
            <h2 className="mb-3 text-lg font-bold text-slate-900">Where</h2>
            <ol className="space-y-3">
              {tape.map((m) => (
                <li key={m.key} className="flex gap-3">
                  <span className="flex size-7 shrink-0 items-center justify-center rounded-full bg-brand-700 text-sm font-bold text-white">{m.point}</span>
                  <div>
                    <p className="font-semibold text-slate-900">{m.label}</p>
                    <p className="text-sm text-slate-700">{m.how}</p>
                  </div>
                </li>
              ))}
            </ol>
          </Card>

          <Card>
            <h2 className="mb-2 text-lg font-bold text-slate-900">Weight, body fat and muscle</h2>
            <ul className="space-y-2 text-slate-700">
              {metrics
                .filter((m) => m.point === undefined)
                .map((m) => (
                  <li key={m.key}>
                    <span className="font-semibold text-slate-900">{m.label}:</span> {m.how}
                  </li>
                ))}
            </ul>
          </Card>
        </div>
      </div>
    </>
  );
}

/** A simple front view of a body, drawn for this app, with the tape lines numbered as in the list. */
function BodyDiagram() {
  const body = "fill-slate-200";
  const limb = "stroke-slate-200";
  // y, from x, to x, number, number on the left (arms and legs) or right (trunk)
  const lines: [number, number, number, number, "left" | "right"][] = [
    [60, 90, 110, 1, "right"],
    [100, 68, 132, 2, "right"],
    [148, 70, 130, 3, "right"],
    [188, 64, 136, 4, "right"],
    [112, 39, 60, 5, "left"],
    [166, 30, 50, 6, "left"],
    [250, 66, 96, 7, "left"],
    [330, 70, 93, 8, "left"],
  ];

  return (
    <svg viewBox="0 0 200 410" role="img" aria-label="Body outline with eight numbered tape lines: neck, chest, waist, hips, biceps, forearm, thigh and calf" className="h-auto w-full">
      {/* Head, neck and trunk */}
      <circle cx="100" cy="30" r="20" className={body} />
      <rect x="91" y="46" width="18" height="18" rx="4" className={body} />
      <path d="M66 74 Q100 60 134 74 L131 140 Q128 168 136 202 L64 202 Q72 168 69 140 Z" className={body} />
      {/* Arms and legs as thick round lines */}
      <g fill="none" strokeLinecap="round" strokeLinejoin="round">
        <path d="M56 82 L44 148 L34 212" strokeWidth="17" className={limb} />
        <path d="M144 82 L156 148 L166 212" strokeWidth="17" className={limb} />
        <path d="M82 200 L81 300 L82 392" strokeWidth="28" className={limb} />
        <path d="M118 200 L119 300 L118 392" strokeWidth="28" className={limb} />
      </g>
      {/* Tape lines */}
      {lines.map(([y, x1, x2, n, side]) => {
        const cx = side === "right" ? x2 + 14 : x1 - 14;
        return (
          <g key={`${n}-${y}`}>
            <line x1={x1} x2={x2} y1={y} y2={y} strokeWidth="2.5" strokeDasharray="5 3" className="stroke-brand-600" />
            <circle cx={cx} cy={y} r="9" className="fill-brand-700" />
            <text x={cx} y={y + 4} textAnchor="middle" fontSize="11" fontWeight="700" className="fill-white">
              {n}
            </text>
          </g>
        );
      })}
    </svg>
  );
}
