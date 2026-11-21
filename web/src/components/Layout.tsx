import { NavLink, Outlet } from "react-router";
import { useAuth } from "../auth/authContext";

// Navigation from the thesis mock-ups (6.2): a side menu on wide screens. On phones the same links sit in a
// bottom tab bar, where thumbs reach them (WNF05). Progress joins this list in week 5.
const navItems = [
  { to: "/", label: "Dashboard", icon: "M3 12l9-8 9 8M5 10v10h5v-6h4v6h5V10", end: true },
  { to: "/workouts", label: "Workouts", icon: "M12 8v4l3 2M21 12a9 9 0 11-18 0 9 9 0 0118 0z", end: false },
  { to: "/plans", label: "Plans", icon: "M9 5h10M9 12h10M9 19h10M5 5h.01M5 12h.01M5 19h.01", end: false },
  { to: "/exercises", label: "Exercises", icon: "M6 9v6M9 7v10M15 7v10M18 9v6M9 12h6", end: false },
  { to: "/profile", label: "Profile", icon: "M12 12a4 4 0 100-8 4 4 0 000 8zm-7 9a7 7 0 0114 0", end: false },
];

function NavIcon({ path }: { path: string }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="size-6 shrink-0" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
      <path d={path} />
    </svg>
  );
}

export function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-dvh bg-slate-50">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-2 focus:top-2 focus:z-50 focus:rounded focus:bg-white focus:px-3 focus:py-2">
        Skip to content
      </a>

      <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4">
          <NavLink to="/" className="flex items-center gap-2 font-bold text-brand-800">
            <img src="/favicon.svg" alt="" className="size-7" />
            Fitness Logger
          </NavLink>
          <div className="flex min-w-0 items-center gap-3">
            <span className="hidden truncate text-sm text-slate-600 sm:inline">{user?.email}</span>
            <button type="button" onClick={logout} className="min-h-11 rounded-lg px-3 text-sm font-medium text-slate-700 hover:bg-slate-100">
              Log out
            </button>
          </div>
        </div>
      </header>

      <div className="mx-auto flex max-w-6xl gap-8 px-4">
        <nav aria-label="Main menu" className="sticky top-14 hidden h-[calc(100dvh-3.5rem)] w-52 shrink-0 py-6 md:block">
          <ul className="space-y-1">
            {navItems.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    `flex min-h-11 items-center gap-3 rounded-lg px-3 text-sm font-medium ${
                      isActive ? "bg-brand-50 text-brand-800" : "text-slate-700 hover:bg-slate-100"
                    }`
                  }
                >
                  <NavIcon path={item.icon} />
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        {/* Bottom padding keeps content clear of the phone tab bar. */}
        <main id="main" className="min-w-0 flex-1 py-6 pb-24 md:pb-10">
          <Outlet />
        </main>
      </div>

      <nav aria-label="Main menu" className="fixed inset-x-0 bottom-0 z-20 border-t border-slate-200 bg-white pb-[env(safe-area-inset-bottom)] md:hidden">
        <ul className="grid grid-cols-5">
          {navItems.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  `flex min-h-14 flex-col items-center justify-center gap-0.5 text-xs font-medium ${isActive ? "text-brand-700" : "text-slate-600"}`
                }
              >
                <NavIcon path={item.icon} />
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  );
}
