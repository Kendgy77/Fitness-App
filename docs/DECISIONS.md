# Decisions

One entry per non-trivial decision: context, choice, why, consequences. Newest at the bottom.
Change a decision only by adding a new entry that replaces it.

The blueprint ([BLUEPRINT.md](BLUEPRINT.md)) is a guide, not a rulebook. Where this project does
something different from it, the reason is logged here.

---

### D001 · 2026-11-17 · Product type: personal training tracker

- **Context:** "gym app" can mean a personal tracker or a gym management system (blueprint Part 3.1).
- **Choice:** personal tracker: each user owns their own data; an admin manages only the exercise catalog.
- **Why:** matches the thesis requirements WF01–WF10 and is the right size for the deadline.

### D002 · 2026-11-17 · .NET 10 instead of .NET 8

- **Context:** the konspekt and thesis section 4.1 name C# 12 / .NET 8 and EF Core 8.
- **Choice:** .NET 10 (LTS) and EF Core 10 (C# 14).
- **Why:** .NET 10 is the current LTS release (support until Nov 2028); .NET 8 support ends in Nov 2026,
  the month this project starts. Building a new app on an expiring runtime would be a weak point at the defense.
- **Consequences:** update thesis tables 4.1 and 4.4–4.6 (".NET 8" → ".NET 10").

### D003 · 2026-11-17 · One API project with feature folders instead of four Clean Architecture projects

- **Context:** the thesis (4.1) describes Clean Architecture with Domain, Application, Infrastructure and API
  projects.
- **Choice:** one project `src/FitnessApp.Api`, organized by feature (`Features/Exercises`, `Features/Plans`, …)
  with shared `Common/` and `Infrastructure/` folders. Pure domain logic (BMI, 1RM, state machines) lives in
  static classes with no dependencies.
- **Why:** with one developer and ~10 entities, four projects mostly add ceremony (interfaces and mapping
  between layers for every feature). Feature folders keep everything about one feature in one place. The
  dependency rule of Clean Architecture still holds where it matters: domain calculations don't depend on
  EF Core or HTTP and are unit-tested in isolation.
- **Consequences:** thesis 4.1–4.2 should describe a "modular monolith with feature folders" and explain why.
  WNF06 (adding diet/GPS later) is met by adding a new feature folder. Split into projects only if a second
  executable needs the domain.

### D004 · 2026-11-17 · ASP.NET Core Identity password hashing instead of BCrypt

- **Context:** the thesis (4.1, T19) names BCrypt for password hashing.
- **Choice:** ASP.NET Core Identity, which hashes with PBKDF2-HMAC-SHA512 and 100,000 iterations (Identity v3 format).
- **Why:** Identity also gives lockout (WF02: 3 attempts, 15 min), unique e-mail, password rules and
  security stamps out of the box. Hand-rolling these with BCrypt would mean writing and testing
  security code that already exists. PBKDF2 is NIST-recommended; both are salted, slow hashes.
- **Consequences:** thesis 4.1, 4.2 and test T19 should say "PBKDF2 (ASP.NET Core Identity)" instead of BCrypt.

### D005 · 2026-11-17 · Data access: DbContext in services, explicit mapping

- **Choice:** EF Core 10 + Npgsql, PostgreSQL 16. Services use `AppDbContext` directly; no generic
  repository, no AutoMapper, no MediatR. Mapping is explicit (LINQ projections and extension methods).
- **Why:** `DbContext` is already a unit of work and `DbSet<T>` a repository. Explicit mapping is easy to
  debug and AutoMapper is now commercially licensed.

### D006 · 2026-11-17 · Auth: Identity + JWT bearer

- **Choice:** Identity (`AppUser : IdentityUser`) + JWT access tokens; roles `User` and `Admin`; global
  fallback policy requires authentication, `[AllowAnonymous]` only on register/login. Lockout:
  `MaxFailedAccessAttempts = 3`, `DefaultLockoutTimeSpan = 15 min`.
- **Why:** stateless API (fits the SPA and free hosting), proven in the AutoCare reference project.
- **Consequences:** logout is client-side (the token is discarded); tokens are short-lived (60 min).
  Server-side revocation is in the backlog.

### D007 · 2026-11-17 · Frontend stack

- **Choice:** `web/`: React + TypeScript + Vite, Tailwind CSS, React Router, Axios, Chart.js via
  react-chartjs-2. UI text in Polish. Exact versions are picked in week 2 (the current stable major of each).
- **Why:** matches the thesis (4.1.2); TypeScript catches API contract mistakes at compile time.

### D008 · 2026-11-17 · Tests

- **Choice:** xUnit + Shouldly; integration tests with `WebApplicationFactory` + Testcontainers PostgreSQL;
  Vitest + React Testing Library; Playwright for a few end-to-end flows in week 7.
- **Why:** tests run against real PostgreSQL, so constraints, indexes and SQL translation are tested too
  (EF InMemory would hide those). Shouldly is MIT-licensed (FluentAssertions v8 is commercial).
- **Consequences:** running integration tests locally needs Docker.

### D009 · 2026-11-17 · CI on GitHub Actions; every change through a pull request

- **Choice:** `.github/workflows/ci.yml` with a backend job (format check, build, test, vulnerable
  package scan) and a frontend job (lint, typecheck, test, build). From week 1 on, nothing goes to `main`
  without a pull request and a green CI run.

### D010 · 2026-11-17 · Units and time

- **Choice:** weight stored as `decimal` kg, height as cm, timestamps as UTC `DateTimeOffset`, the user's
  time zone stored in the profile.
- **Why:** one canonical unit makes math and comparisons correct; "which day did I train" depends on the
  user's local time.

### D011 · 2026-11-17 · Thesis classes → code entities

| Thesis (UML/ERD) | Code | Note |
|---|---|---|
| Uzytkownik / Administrator | `AppUser : IdentityUser` + role `Admin` | One table, role instead of inheritance (matches the ERD's `rola` column) |
| ProfilUzytkownika | `UserProfile` (1:1) | Birth date, sex, height, goal, time zone; BMI computed, never stored |
| Cwiczenie | `Exercise` | Global catalog (`UserId = null`) + user's custom exercises; archived, never deleted |
| PlanTreningowy + plan_cwiczenie | `TrainingPlan` + `PlanExercise` | Status Draft/Active/Archived; order, target sets and reps |
| SesjaTreningowa | `WorkoutSession` | Status Planned/InProgress/Paused/Completed; optional `PlanId`; plan contents are copied |
| SeriaCwiczenia | `SessionExercise` + `SetEntry` | Extra level keeps exercise order and notes per session |
| Postep | `BodyMeasurement` | Date, weight kg; BMI computed from profile height |
| (WF08) | `Goal` | Type, target value, deadline, status |
| (WF09) | `Notification` | Created by the reminder worker, read by the frontend |

- **Consequences:** thesis class diagram and ERD (3.4, 5.2) get updated from the final schema in week 7.

### D012 · 2026-11-17 · Hosting (provisional)

- **Choice:** backend on Azure App Service (Azure for Students) or Render; database on Azure PostgreSQL or
  Neon; frontend on Vercel. Final pick in week 7 after checking current free tiers.
- **Consequences:** WNF01 (500 concurrent users) and WNF03 (99.9% uptime) can't be guaranteed on free
  tiers; the thesis presents them as design targets backed by a measured load test.

### D013 · 2026-11-17 · Login answers: generic 401, but a distinct 423 when locked

- **Context:** the thesis (UC002, T04) wants a message saying the account is locked for 15 minutes; security
  practice says login errors shouldn't reveal whether an e-mail is registered.
- **Choice:** wrong password and unknown e-mail both return the same 401 "Nieprawidłowy e-mail lub hasło.".
  Only an account that really is locked returns 423 "Konto zablokowane" with `retryAfter` in seconds.
- **Why:** the user needs to know they must wait; the 423 only appears after 3 failed tries on a real account,
  and per-IP rate limiting (10 requests/min) makes probing e-mails this way slow.

### D014 · 2026-11-17 · Registration logs the user in

- **Choice:** `POST /api/auth/register` returns 201 with the same token response as login.
- **Why:** thesis T01 expects the user to be logged in and taken to the dashboard after registering.
  There is no e-mail confirmation in the MVP (no e-mail provider); it's in the backlog.

### D015 · 2026-11-17 · Tooling: tests on Microsoft Testing Platform, dotnet-ef pinned in a tool manifest

- **Choice:** xUnit v3 tests run on Microsoft Testing Platform (`global.json` → `"test": { "runner": … }`),
  which `dotnet test` on .NET 10 requires for xUnit v3. `dotnet-ef` 10.x is pinned in `dotnet-tools.json`;
  run `dotnet tool restore` once.
- **Why:** everyone (and CI) uses the same EF tool version. Migrations are created locally with
  `dotnet ef`, so the playbook's "add-migration" CI workflow isn't needed. CI checks
  `has-pending-model-changes` so a forgotten migration fails the build.

### D016 · 2026-11-19 · Profile: birth date instead of age; current weight in the profile; BMI never stored

- **Context:** the thesis (WF03, class ProfilUzytkownika) lists age, sex, body weight, height and goal, and a
  method that calculates BMI (WF10).
- **Choice:** `UserProfile` stores the **birth date** and calculates age when read. It stores the **current
  weight**; the weight history (thesis class Postep) is a separate `BodyMeasurement` feature in week 5. BMI and
  its WHO category are calculated on every read by the pure `BmiCalculator`, never saved. The profile row is
  created on the first save; until then `GET /api/profile` returns an empty profile (GETs never write).
- **Why:** a stored age goes stale on every birthday, and a stored BMI goes stale on every weight change.
  Values that can be derived are derived. When height or weight is missing, the response has `bmi: null` and
  `missingForBmi` names the fields, which is what thesis T17 expects ("ask the user to fill in the data").
- **Consequences:** in week 5, saving a weight measurement also updates the profile's current weight.

### D017 · 2026-11-19 · Enums: names only, stored as text

- **Choice:** the API accepts enum values by name only (`"Male"`); numbers are rejected with 400.
  In the database, enum columns are `varchar` holding the name.
- **Why:** with numbers allowed, `"sex": 7` would be accepted and stored as a value the enum doesn't define.
  Text columns are readable in the database, and reordering an enum in code can't change the meaning of rows
  that already exist.

### D018 · 2026-11-19 · Exercise library: one table, global + custom, archive instead of delete

- **Choice:** one `Exercises` table. `UserId = null` is the global catalog (managed by the admin, visible to
  all); otherwise it is a user's custom exercise, visible only to them. Exercises are never deleted, only
  archived: they disappear from the library but stay readable by id. A user who tries to edit a global
  exercise gets **403** (they can see it, so 404 would be misleading); someone else's custom exercise is
  **404** (it doesn't exist for them).
- **Catalog:** 61 exercises with technique descriptions (in English, see D021), seeded by a migration (`HasData`) so every
  environment has the same catalog and ids. Seeded ids are 1–61; ids created at runtime start at 1000.
- **Search:** `unaccent` + `ILIKE` on name and muscles: case and accents don't matter (a custom
  "Wiosłowanie" is found by "wioslowanie"). Names sort alphabetically with the ICU root collation
  (`und-x-icu`), so the order is the same on every database server.
- **Names:** unique per owner among active exercises, ignoring case; a custom exercise can't repeat a global
  name. Checked in the service (friendly 409) and by a partial unique index (guards against races).
- **Why:** plans and workout history (weeks 3–4) will point at exercises, and history must never change.
  Archiving keeps those references valid without the delete-cascade problems of the AutoCare project.
- **Consequences:** the production database must allow the `unaccent` extension and have ICU collations
  (Azure PostgreSQL and Neon both do; checked again in week 7 when hosting is chosen).

### D019 · 2026-11-19 · Frontend: versions, data fetching, login storage

- **Versions** (replaces "picked in week 2" from D007): React 19.3, React Router 8.5, Vite 8.3, Tailwind CSS 4.3,
  TypeScript 6.0, Vitest 5.1. Every package is pinned to an exact version that had been public for at least two
  weeks when it was added, so a freshly published, possibly compromised release can't slip in. TypeScript 7
  (the new native compiler) is not used yet: the linter (typescript-eslint) doesn't support it.
- **TanStack Query** on top of Axios for loading data. It caches server responses, shares one request between
  pages that need the same data, and handles loading and error states, so pages don't each re-implement them.
  After a save, the affected cached lists are refreshed. Axios stays the HTTP client, as in the thesis (4.1.2).
- **Login state:** the token and user are kept in `localStorage`, so a refresh doesn't log the user out. The
  token expires after 60 minutes; an expired one is dropped on start-up or on the first 401. On logout the
  data cache is cleared, so the next user of the same browser can't see the previous user's data.
  Trade-off: a script injected into the page (XSS) could read the token. React escapes all rendered text and
  the app renders no user HTML, which keeps that risk low; an httpOnly cookie is the stronger option and is
  in the backlog together with refresh tokens.
- **Addresses:** short English paths (`/exercises`, `/profile`). Filters of the exercise library are kept in
  the address (`?category=Legs&page=2`), so back/forward and bookmarks keep them.

### D020 · 2026-11-19 · UI direction

- **Colour:** one brand colour, emerald (`brand-*` tokens in `web/src/index.css`, mapped to Tailwind's emerald
  scale), slate greys for text and surfaces, red only for errors and destructive actions. Emerald 700 on white
  has a contrast ratio above 4.5:1 (WCAG 2.1 AA, thesis WNF04).
- **Type:** the system font (`system-ui`): no font download, and it looks native on every phone.
- **Spacing and shape:** Tailwind's 4 px scale; cards with `rounded-xl`, a light border and a small shadow.
- **Touch and keyboard:** every button, link and input is at least 44 px tall; a visible focus ring on every
  interactive element; a "skip to content" link; every input has a label and its error is announced.
- **Layout:** mobile first. Phones get a bottom tab bar (reachable with a thumb), wider screens the side menu
  from the thesis mock-up (6.2).
### D021 · 2026-11-19 · English UI, app name "Fitness Logger", time zone taken from the browser

- **Context:** the plan said "UI in Polish". Ostap asked for English and a clearer product name.
- **Choice:** the whole app is in English: screens, API error messages and the exercise catalog. The product
  is called **Fitness Logger**. Code names (`FitnessApp.Api`, namespaces) stay as they are: renaming them
  would touch every file and change nothing for the user. The time zone is no longer a field on the
  profile page: the web app sends the browser's time zone (e.g. "Europe/Warsaw") with every profile save.
- **Why:** English is easier to maintain next to English code, and every thesis screenshot uses the same language.
  The time zone is still needed (a workout at 00:30 belongs to the right day; a reminder set for 18:00 fires at
  18:00 local time), but users shouldn't have to know about it.
- **Consequences:** the thesis text stays in Polish, and chapter 6 (mock-ups) mentions that the final UI is
  in English. The API still accepts and validates `timeZone`, so a user who travels gets the zone of the
  browser they used last.

### D022 · 2026-11-20 · Training plans: status rules, templates, whole-plan saves

- **Model:** `TrainingPlan` (name, description, status) with an ordered list of `PlanExercise` rows (exercise,
  target sets, target reps, optional starting weight, notes). The same exercise may appear twice in a plan.
  Rows carry an explicit `Order`, unique within the plan.
- **Status** (`PlanStateMachine`, unit-tested): Draft → Active → Archived, Draft → Archived, and Archived →
  Draft to restore. An empty plan can't be activated. Active plans can still be edited: workouts (week 4)
  copy a plan's exercises, so editing a plan never changes past workouts. Archived plans are read-only.
  Only drafts can be deleted; a plan that was ever active is archived instead, because workouts may point at it.
- **Templates:** three seeded plans (`UserId = null`, ids 1–3) visible to everyone. Users can't change them
  (403) but can copy them into their own draft. Several plans may be active at once (e.g. "Upper" and "Lower").
- **Saving:** `PUT` replaces the whole plan, including the exercise list in its new order. The editor sends
  the plan as it should look, which is simpler and safer than separate add/move/remove endpoints.
- **Privacy:** a plan can only use exercises the user can see (global or their own, not archived). Another
  user's plan is 404 everywhere, including copy.
- **Delete rules:** user → plans cascade; plan → its rows cascade; plan row → exercise is NO ACTION (checked at
  the end of the statement, so deleting an account removes plans and custom exercises in any order).

### D023 · 2026-11-21 · Rep targets are ranges

- **Why:** Ostap's plan (8-week plan doc, week 3) asks for rep ranges like 8–9. A range is how most lifters
  train: work at the bottom, add weight once every set reaches the top ("double progression").
- **Model:** `PlanExercise.TargetReps` became `TargetRepsMin` + `TargetRepsMax`. A fixed number is a range with
  equal ends (10–10, shown as "10"). The API checks max ≥ min (400 on `exercises[i].targetRepsMax`) and so does
  a database check constraint.
- **Migration:** existing rows got max = min with one hand-written `UPDATE`, so no plan changed meaning. The
  templates got real ranges (e.g. bench press 6–8, curls 8–12).
- **Editor:** two small boxes, "Reps from" and "Reps to" (empty = fixed number), not one "8–10" text box,
  because a phone's number keyboard often has no dash.

### D024 · 2026-11-21 · Installable app (PWA) without a plugin; BMI disclaimer

- **Why:** the updated 8-week plan (week 2) asks for the app to be installable on a phone and for a BMI
  disclaimer.
- **How:** a hand-written `public/manifest.webmanifest` (name, colours, standalone display, 192/512 icons and a
  maskable icon), an `apple-touch-icon` for iPhones, and a minimal `public/sw.js`. The icons are the logo
  rendered to PNG. No `vite-plugin-pwa`: three small files are easier to explain than a plugin's generated
  code, and nothing here needs its caching machinery.
- **Service worker scope:** network only, so a new version is never hidden behind an old cached copy. The only
  offline content is `offline.html`, shown when the app is opened with no connection. API data is never cached:
  a fitness log must not show stale workouts as if they were current. Push notifications were dropped (D025), so this stays minimal.
- **Registered only in production builds** (`import.meta.env.PROD`), so it never interferes with Vite's dev
  server. Browsers allow service workers only over HTTPS or on localhost, so installing on a phone can first be
  tested once the app is online (week 7). Checked locally with `vite preview` on localhost.
- **BMI disclaimer:** the BMI card says BMI can't tell muscle from fat (people who lift often score higher than
  their body fat suggests) and isn't a diagnosis.

### D025 · 2026-11-21 · No push notifications; slow first load on free hosting is fine; flexible weeks

- **No phone/browser push notifications.** Push needs HTTPS, an installed app on iPhone and an always-on server,
  and free hosting sleeps, so it would be unreliable. Ostap decided it isn't needed. Reminders (WF09, test
  T15) stay, but are shown inside the app: a notification bell and a list of upcoming workouts.
- **Free hosting waking up slowly is accepted.** The app is for Ostap and friends, not commercial use, so
  about a minute for the first load after a quiet period is fine. No paid plan, no "keep awake" tricks.
- **The week plan is an estimate.** Each block starts when the previous one is merged, early or late.
