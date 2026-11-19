import type { ReactNode } from "react";

/** The frame around the login and register forms: centred card, app name on top. */
export function AuthLayout({ title, children, footer }: { title: string; children: ReactNode; footer: ReactNode }) {
  return (
    <main className="flex min-h-dvh items-center justify-center bg-slate-50 px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-6 flex items-center justify-center gap-2 text-xl font-bold text-brand-800">
          <img src="/favicon.svg" alt="" className="size-9" />
          Fitness App
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h1 className="mb-5 text-xl font-bold text-slate-900">{title}</h1>
          {children}
        </div>
        <p className="mt-4 text-center text-sm text-slate-600">{footer}</p>
      </div>
    </main>
  );
}
