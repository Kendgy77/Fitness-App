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
- **Catalog:** 61 exercises with Polish technique descriptions, seeded by a migration (`HasData`) so every
  environment has the same catalog and ids. Seeded ids are 1–61; ids created at runtime start at 1000.
- **Search:** `unaccent` + `ILIKE` on name and muscles, so "lawka" finds "ławka" and case doesn't matter.
  Names sort in Polish alphabetical order (ICU collation `pl-PL-x-icu`).
- **Names:** unique per owner among active exercises, ignoring case; a custom exercise can't repeat a global
  name. Checked in the service (friendly 409) and by a partial unique index (guards against races).
- **Why:** plans and workout history (weeks 3–4) will point at exercises, and history must never change.
  Archiving keeps those references valid without the delete-cascade problems of the AutoCare project.
- **Consequences:** the production database must allow the `unaccent` extension and have ICU collations
  (Azure PostgreSQL and Neon both do; checked again in week 7 when hosting is chosen).
