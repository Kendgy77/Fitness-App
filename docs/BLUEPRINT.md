# Gym App Blueprint: building a clean, maintainable .NET web app

> **What this is.** A playbook for building a gym web app in .NET. It has two readers:
>
> 1. **The AI agent** that will pair-program on the project. Read Part 1 first: it sets how you work.
> 2. **The developer** (new to clean code) who owns the project. Read Parts 2–4 to learn *why* things
>    are done a certain way, and use the checklists in Parts 8–9 on every feature.
>
> The guidance comes from a working production-style project, **AutoCare** (`Persay23/VehicleMaintenance`):
> a .NET 10 Web API with JWT auth, EF Core, per-user data isolation, rate limiting, AI integration and
> tests. Part 2 lists what AutoCare does well (copy it) and what it does inconsistently (don't repeat it).
> Every rule in this document exists because of a real decision or a real mistake in that codebase.
>
> **Recommended use:** copy this file into the new repository as `docs/BLUEPRINT.md` and reference it from
> the new repo's `CLAUDE.md` / `AGENTS.md` (template in [Appendix A](#appendix-a--claudemd-template-for-the-new-repo)).

---

## Table of contents

- [Part 1: Operating instructions for the AI agent](#part-1--operating-instructions-for-the-ai-agent)
- [Part 2: Lessons from AutoCare](#part-2--lessons-from-autocare)
- [Part 3: Phase 0: discovery and challenging the idea](#part-3--phase-0-discovery-and-challenging-the-idea)
- [Part 4: Architecture and project structure](#part-4--architecture-and-project-structure)
- [Part 5: Domain model guidance for a gym app](#part-5--domain-model-guidance-for-a-gym-app)
- [Part 6: Coding rules (with examples)](#part-6--coding-rules-with-examples)
- [Part 7: Testing strategy](#part-7--testing-strategy)
- [Part 8: Security checklist](#part-8--security-checklist)
- [Part 9: Workflow, Definition of Done, maintenance](#part-9--workflow-definition-of-done-maintenance)
- [Part 10: Roadmap: phases and milestones](#part-10--roadmap-phases-and-milestones)
- [Appendix A: CLAUDE.md template for the new repo](#appendix-a--claudemd-template-for-the-new-repo)
- [Appendix B: Glossary](#appendix-b--glossary)

---

## Part 1: Operating instructions for the AI agent

You are a **senior .NET engineer and mentor** pair-programming with a developer who has little experience
with clean, maintainable code. Success is not just "the app works". Success is:

1. The app works, and it is **secure, tested and easy to change**.
2. The developer **understands every part of it** and could explain or extend it without you.
3. Product decisions were **challenged and made deliberately**, not by accident.

### 1.1 How you work

| Rule | Why |
|---|---|
| **Ask before you build.** Before each phase or feature, ask the clarifying questions from Part 3 / Part 5. Ask at most 3–5 at a time, and give your recommendation with each one. | The developer doesn't know what they don't know. Unasked questions turn into rewrites. |
| **Challenge ideas respectfully.** When an idea adds complexity, risk or scope, say so, explain the cost, and offer a simpler alternative. Then let the developer decide and record the decision. | You protect the project from scope creep and overengineering. The developer still owns it. |
| **Work in thin vertical slices.** One feature end-to-end (entity → migration → service → endpoint → tests) before starting the next. | Small, finished things beat large, half-finished things. Each slice is reviewable. |
| **Explain new concepts briefly.** First time a concept appears (DI, DTO, AsNoTracking, migration…), explain it in 2–3 sentences and link it to the glossary. | The goal is learning, not just code. |
| **Make the developer do part of it.** Regularly ask them to write the next endpoint, test or DTO themselves following an existing example, then review it. | Copying a pattern once is how a pattern is learned. |
| **Never skip the Definition of Done** (Part 9.2): tests, auth check, no warnings, docs updated. | Skipped steps become permanent debt. |
| **Record decisions** in `docs/DECISIONS.md` (one short entry per decision: context, options, choice, why). | Six months later "why is it like this?" has an answer. |
| **Keep consistency above cleverness.** If a pattern already exists in the codebase, follow it. If it's wrong, change it everywhere in a dedicated refactor commit, not just in one place. | AutoCare's biggest weakness is not bad code; it is *several different ways of doing the same thing* (Part 2.2). |
| **No placeholder code.** No empty tests, no `// TODO dive into`, no commented-out code. Open an issue instead. | Leftovers rot. |
| **Say when you are unsure.** If a library version, API or behavior may have changed, say so and verify (docs, a quick test) rather than guess. | Wrong confident answers are the most expensive kind. |

### 1.2 Red flags: push back when you hear these

| The developer says… | Your response |
|---|---|
| "Let's use microservices / Kubernetes / event sourcing." | Explain the operational cost. A modular monolith (Part 4) is the right default for one developer. Revisit only with a concrete scaling problem. |
| "Let's add an AI coach first." | AI is a feature on top of good data. Without logged workouts there is nothing to analyze. Put it in Phase 5. |
| "Let's build it like [big existing app] with everything." | Ask: *who is the first user, and what is the one thing they must be able to do in week one?* Build that. |
| "We can add tests / security later." | Later never comes, and retrofitting auth is expensive. Ownership checks and tests are part of every slice. |
| "Let me just return the entity from the controller, it's faster." | Entities leak internal fields, create JSON cycles and couple the API to the database. Always return DTOs. |
| "Let's make a generic repository for everything." | EF Core's `DbContext` already *is* a repository + unit of work. A generic wrapper adds code without adding value (Part 4.3). |
| "I'll store weight as a string" / "I'll store dates as local time." | Store canonical units (kg) and UTC timestamps; convert at the edges (Part 5.4). |
| "Let's catch the exception and return `ex.Message`." | That leaks internals (stack details, SQL, API keys in URLs). Log it, return a generic ProblemDetails. |

### 1.3 Your session loop

```
1. Re-read docs/DECISIONS.md and the current phase in Part 10.
2. State what we're building in this session (one slice) and the done-criteria.
3. Ask open questions (≤5, each with a recommendation). Wait for answers.
4. Implement in small commits. After each step: build, run tests, explain what changed.
5. Run the Definition of Done checklist (Part 9.2). Fix gaps.
6. Summarize: what was built, what was learned, what's next, any new decisions logged.
```

---

## Part 2: Lessons from AutoCare

### 2.1 Keep these: patterns AutoCare gets right

| Practice | Where in AutoCare | Why it matters |
|---|---|---|
| **Layered flow: Controller → Service → EF Core** | `Controllers/`, `Services/`, `Data/AppDbContext` | Controllers deal with HTTP; services hold the logic; EF handles data. Each layer has one job. |
| **Interface per service + DI registration** | `IVehicleService` / `VehicleService`, registered in `Program.cs` | Makes services replaceable and mockable in tests. |
| **Separate DTOs per operation** (`CreateXDto`, `UpdateXDto`, `XDto`) | `DTOs/FuelEntry/*` | The API never exposes entities. Create/update payloads can differ from responses. |
| **`AsNoTracking()` on read-only queries** | `VehicleService.GetAllVehiclesAsync`, `FuelEntryService`, `VehicleOwnershipService` | EF skips change-tracking for data you won't modify. Faster, less memory. A commit was dedicated to this (`refactor(data): AsNoTracking + array returns on read paths`). |
| **Secure by default: global fallback auth policy** | `Program.cs`: `FallbackPolicy = RequireAuthenticatedUser()` | Every endpoint requires login unless explicitly `[AllowAnonymous]`. Forgetting `[Authorize]` can't create a hole. |
| **Per-user ownership checks returning 404** | `Services/Security/VehicleOwnershipService.cs` | Stops users reading each other's data by guessing IDs (IDOR). 404 doesn't even reveal the ID exists. |
| **Rate limiting on login and expensive endpoints** | `AddRateLimiter` policies `"login"` and `"ai"` | Slows brute-force attacks and protects paid APIs. |
| **No user enumeration** | `AuthController.ResendConfirmation` always returns the same message | Attackers can't probe which emails are registered. |
| **Secrets outside source control** | `dotnet user-secrets` for `Jwt:Key`, API keys; empty values in `appsettings.json` | A leaked repo doesn't leak credentials. |
| **Options pattern for config** | `AiLimitsOptions` bound with `Configure<AiLimitsOptions>(...)` | Typed, validated config instead of magic strings. |
| **Pure, static domain calculator** | `ComponentHealthCalculator.Compute()` returns a `record` | Business math lives in one place with no dependencies, which makes it trivially unit-testable. Your gym equivalent: 1RM, volume, PR detection. |
| **Abstraction over infrastructure** | `IFileStorage` → `LocalFileStorage` ("swap for Azure Blob, no caller changes") | Swap storage, email or AI providers without touching business code. |
| **Typed `HttpClient` with retry on 429/503** | `AddHttpClient<IGeminiService, GeminiService>` | Correct socket handling + resilience for external calls. |
| **Explicit delete behaviors** | `AppDbContext.OnModelCreating`: `Cascade` vs `NoAction` chosen per relation | Prevents accidental mass deletion and SQL Server multiple-cascade-path errors. |
| **`decimal` for money** | Cost fields, `precision(18,2)` | `double` can't represent money exactly. |
| **Security headers + global exception handler** | `X-Content-Type-Options`, `X-Frame-Options`, `GlobalExceptionHandler` | Cheap protections; no stack traces leak to clients. |
| **Conventional commits** | `feat(auth): …`, `fix(ai): …`, `refactor(data): …` | History is readable and searchable. |
| **README with stack, architecture, schema, setup and API table** | `README.md` | A new person can run the project in minutes. |
| **Seed data for demos** | `Data/DataSeeder.cs` | You can demo and manually test immediately after setup. |

### 2.2 Don't repeat these: AutoCare's inconsistencies and mistakes

Read these as lessons, not criticism: they're the normal result of a project growing feature by feature. The fix is
to **decide the convention on day one** (Part 6) and enforce it.

| # | What happened in AutoCare | Why it hurts | Do this instead |
|---|---|---|---|
| 1 | **Two data-access styles.** `GeneralExpense` and `UserDrivingProfile` use repositories; the other 7 services use `DbContext` directly. | A reader can't predict where queries live. | Pick **one**. Recommended: services use `DbContext` directly (Part 4.3). |
| 2 | **Two mapping styles.** AutoMapper in most services, a hand-written `MapToDto` in `GeneralExpenseService`. `CreateMap<CreateVehicleDto, Vehicle>` is declared twice in `MappingProfile`. | Mapping bugs hide in the inconsistency. | Pick **one**. Recommended: explicit mapping (extension methods or LINQ `Select` projection). AutoMapper moved to a commercial license from v15, and explicit mapping is easier for a beginner to debug. |
| 3 | **Two ways to read the user id**: `_userManager.GetUserId(User)` in `VehicleController`, `User.GetUserId()` elsewhere. | Same thing, two dependencies. | One helper: a `CurrentUserId` property on a shared base controller. |
| 4 | **Ownership check is a separate query**, then the service loads by id without the user filter. Each action repeats `userId` + `Owns…` + `NotFound` (~3 lines × ~40 actions). | Two round-trips per request, duplicated boilerplate, and if someone forgets the check, the service happily returns another user's data. | **Pass `userId` into the service and filter in the same query**: `Where(w => w.Id == id && w.UserId == userId)`. Not found and not yours both become `null` → 404. The security rule lives in the data query, so it can't be forgotten. |
| 5 | **Inconsistent status codes.** Create returns `Ok(...)` in `VehicleController` but `CreatedAtAction(...)` in `GeneralExpenseController`. Ownership failure on create returns `Forbid()` (403), elsewhere `NotFound()` (404). | Frontend has to special-case each endpoint. | One table of status codes (Part 6.7) used everywhere. |
| 6 | **Enums as strings in DTOs + `Enum.Parse` on user input** (`UpdateVehicleByIdAsync`, `MappingProfile`). A typo like `"Disel"` throws `ArgumentException` → **500**. | Bad input should be a **400** with a clear message, not a server crash. | Use the enum type in the DTO and register `JsonStringEnumConverter`. Model binding rejects invalid values with 400 automatically. |
| 7 | **Several `SaveChangesAsync` calls in one operation** (`FuelEntryService.CreateFuelEntryAsync`, `MaintenanceRecordService.CreateMaintenanceRecordAsync`). | If the second save fails, data is half-written. | **One use case = one `SaveChangesAsync`** (it's already a transaction). |
| 8 | **A GET that writes**: `GetVehicleByIdAsync` computes `AverageKmPerYear` and saves. | Reads with side effects surprise people, break caching and cause concurrency bugs. | GETs never write. Compute on write, or compute on read without saving. |
| 9 | **Fire-and-forget `Task.Run`** for background AI updates. | Work is lost on shutdown, exceptions are easy to swallow, can't be monitored or limited. | Use a `BackgroundService` + `Channel<T>` queue (or Hangfire/Quartz for scheduled jobs). |
| 10 | **In-memory singletons for quotas** (`AiUsageLimiter` uses a `ConcurrentDictionary`). | Counters reset on every restart and break when you run 2 instances. | Fine for a prototype, but **record it in DECISIONS.md as a known limitation**. Persist in DB/Redis when it matters. |
| 11 | **Controller talks to `DbContext` directly** (`AiController` injects `AppDbContext`). `Ping` returns `ex.Message` to the client. | Bypasses the service layer and leaks internal error details. | Controllers only depend on services. Errors go through the global handler. |
| 12 | **Global exception handler returns a custom JSON shape** while `AddProblemDetails()` is registered. Other errors return `{ error }`, `{ message }`. | Three error formats. | Use **RFC 7807 ProblemDetails** for every error response. |
| 13 | **No `CancellationToken`** in the core CRUD services (0 occurrences in `Services/GenralModelService`). | Cancelled requests keep running DB queries. | Accept `CancellationToken ct` in every async method and pass it to EF. |
| 14 | **Duplicated math**: `ComponentStateCalculator.DeriveState` recomputes the same percentages as `ComponentHealthCalculator.Compute`. | "Single source of truth" in name only. Change one and they drift. | One function computes; others consume its result. |
| 15 | **Typos frozen into names**: `GenralModelService/`, `AppBdContext.cs` (class is `AppDbContext`), `GeneralExpanseEnum.cs`, `vehilcecomponent`. | Hard to search, looks careless, and renaming later touches many files. | Spell-check names at creation. File name = type name. |
| 16 | **Mixed namespace styles** (block `namespace X { }` and file-scoped `namespace X;`), unused `using`s (`Microsoft.SqlServer.Server`), redundant field copies of primary-constructor parameters. | Noise. | `.editorconfig` + `dotnet format` in CI (Part 9). |
| 17 | **Placeholder test** `UnitTest1.Test1()` is empty; `Assert.True(true)`. Tests use **EF InMemory**, which ignores foreign keys and SQL behavior. | False confidence. | Delete placeholders. Use SQLite in-memory or Testcontainers for data tests (Part 7). |
| 18 | **README drifted from code**: README says `/api/liquidentries`, controller is `FuelEntryController` → `/api/FuelEntry`. Folder list says `Services/Interfaces/`, real path is `Services/GenralModelService/Interfaces/`. | Docs that lie are worse than no docs. | Updating docs is part of Definition of Done. Prefer generated docs (OpenAPI) for endpoint lists. |
| 19 | **Production concerns hard-wired**: a policy named `DevPolicy` used in all environments; migrations auto-run on startup everywhere; `ForwardedHeaders` with known proxies cleared, so any client can spoof `X-Forwarded-For` and bypass IP-based login limiting; real-looking emails in `appsettings.json` `UserOverrides`. | Works on a laptop, risky in production. | Environment-specific config; migrations as a deploy step; restrict trusted proxies; keep personal data out of committed config. |
| 20 | **Entity configuration all in `OnModelCreating`** in one file. | Grows into hundreds of lines. | One `IEntityTypeConfiguration<T>` class per entity (Part 6.5). |
| 21 | **`// dive into`, `// Add pagination`, `// should be a way to shorten this`** comments left in code. | These are tasks hiding where no one tracks them. | Open a GitHub issue; reference it if needed. |
| 22 | **Unbounded list endpoints** (timeline, all records). | Fine with 10 rows, slow with 10,000. | Paginate every list endpoint from the start. |

---

## Part 3: Phase 0: discovery and challenging the idea

**Agent:** do this *before any code*. Turn the answers into `docs/PRODUCT.md` (one page) and the first
entries of `docs/DECISIONS.md`.

### 3.1 The fork in the road: which gym app?

"Gym app" means two very different products. Get an explicit answer first:

| | **A. Personal training tracker** | **B. Gym management system** |
|---|---|---|
| User | Someone who lifts | A gym owner, its staff and members |
| Core job | Log workouts, see progress | Memberships, class bookings, check-ins, payments |
| Data owner | Each user owns their own data | The gym (tenant) owns data about many people |
| Hard parts | Data modeling of sets/reps, progress stats, UX of fast logging | Roles & permissions, multi-tenancy, booking concurrency, payments, legal |
| Complexity for a beginner | **Moderate: recommended first project** | High |

**Recommendation:** start with **A**. Its structure maps directly onto AutoCare (user owns vehicles → user
owns workouts), so the same ownership patterns apply. If the developer wants B, challenge it: "Who is
the first real gym that will use it? Do you know how they manage memberships today?" With no real
customer, build A.

### 3.2 Questions to ask (with suggested defaults)

**Product**
1. Who is the very first user? (Default: *the developer themself*, so they can test daily.)
2. What must that user be able to do in week one? (Default: *log a workout: exercises, sets, reps, weight*.)
3. What does "progress" mean to them: heavier lifts, more volume, body weight, consistency streaks?
4. Mobile in the gym or desktop at home? (Affects UI: logging between sets needs big buttons and few taps.)
5. Offline in a basement gym with no signal? (If yes, that's a big decision: note it, defer it.)
6. Is there a predefined exercise catalog, or does the user type exercises? (Default: seeded catalog + custom exercises.)
7. kg, lb, or both? (Default: store kg, display the user's preference.)
8. Social features (sharing, friends, leaderboards)? (Default: **not in MVP**.)

**Technical**
9. Frontend: separate SPA (React/Vue + this API, like AutoCare) or **Blazor** (C# only)?
   - Developer knows only C# → **Blazor Web App** keeps one language.
   - Developer knows some JS, or wants to reuse AutoCare's split → **ASP.NET Core Web API + React (Vite)**.
   - The backend guidance in this document applies either way.
10. Database: SQL Server (same as AutoCare, so reviews are easier) or PostgreSQL (free and easy to host)? Either is fine; pick one and record it.
11. Where will it be hosted? (Azure App Service, a VPS, Fly.io…) This decides config/secrets handling.
12. Auth: email+password with Identity + JWT (proven in AutoCare), or external login (Google)? (Default: same as AutoCare.)

**Constraints**
13. How many hours per week? Deadline? (This decides scope. Be honest.)
14. Is this for learning, a portfolio, or real users? (Real users → GDPR, backups and uptime matter from day one.)

### 3.3 Challenge questions: use them throughout

- "What's the simplest version of this that would still be useful?"
- "What happens when this data is wrong / missing / duplicated?"
- "If we add this, what do we have to maintain forever?"
- "How will we know this feature works: what's the test?"
- "Who can see this data? Who can change it?"
- "What if the user has 5 years of workouts: does this still work?"
- "Is this a *must* for the MVP, or a *nice-to-have*?" (Keep a `docs/BACKLOG.md` for nice-to-haves so ideas aren't lost, just deferred.)
- **Pre-mortem:** "It's six months from now and the project failed. Why?" Then mitigate the top 3 answers.

### 3.4 Output of Phase 0

- `docs/PRODUCT.md`: problem, first user, MVP features (≤ 5), explicitly *out of scope* list.
- `docs/DECISIONS.md`: product type, frontend, DB, hosting, auth.
- `docs/BACKLOG.md`: everything deferred.
- A rough entity sketch (Part 5) agreed with the developer.

---

## Part 4: Architecture and project structure

### 4.1 Recommended architecture: a modular monolith, organized by feature

One deployable app. Inside, code is grouped **by feature** (Workouts, Exercises, Progress…) instead of
**by technical type** (all controllers in one folder, all services in another, like AutoCare).

Why change from AutoCare's layout? In AutoCare, adding a field to fuel entries touches
`Controllers/`, `Services/GenralModelService/`, `Services/GenralModelService/Interfaces/`, `DTOs/FuelEntry/`,
`Models/Entities/`, `Mappings/`: six folders. Grouping by feature puts everything for a feature in one folder,
so you change one place and can delete a feature cleanly.

The **layering rules stay the same as AutoCare** (Controller → Service → DbContext). Only the folders change.

### 4.2 Solution layout

```
GymApp/
├── .editorconfig                     # code style, enforced by dotnet format
├── .github/workflows/ci.yml          # build + test + format check on every push/PR
├── Directory.Build.props             # shared: net10.0, Nullable, TreatWarningsAsErrors, analyzers
├── Directory.Packages.props          # central package versions
├── GymApp.sln
├── README.md
├── docs/
│   ├── BLUEPRINT.md                  # this file
│   ├── PRODUCT.md
│   ├── DECISIONS.md
│   └── BACKLOG.md
├── src/
│   └── GymApp.Api/
│       ├── Program.cs                # short: calls extension methods, no 200-line setup
│       ├── appsettings.json          # no secrets, no personal data
│       ├── appsettings.Development.json
│       ├── Common/                   # shared building blocks, nothing feature-specific
│       │   ├── ApiControllerBase.cs  # CurrentUserId, shared helpers
│       │   ├── Errors/               # GlobalExceptionHandler, domain exceptions → ProblemDetails
│       │   ├── Paging/               # PagedRequest, PagedResult<T>
│       │   └── Extensions/           # ClaimsPrincipalExtensions, ServiceCollection extensions
│       ├── Infrastructure/
│       │   ├── Data/
│       │   │   ├── AppDbContext.cs
│       │   │   ├── Configurations/   # one IEntityTypeConfiguration<T> per entity
│       │   │   ├── Migrations/
│       │   │   └── Seed/             # exercise catalog, demo user (Development only)
│       │   ├── Auth/                 # JwtTokenService, JwtOptions
│       │   └── Email/
│       └── Features/
│           ├── Auth/
│           │   ├── AuthController.cs
│           │   └── AuthDtos.cs
│           ├── Exercises/
│           │   ├── Exercise.cs               # entity
│           │   ├── ExerciseDtos.cs           # CreateExerciseRequest, ExerciseResponse, …
│           │   ├── ExerciseMappings.cs       # ToResponse() extension / projection
│           │   ├── IExerciseService.cs
│           │   ├── ExerciseService.cs
│           │   └── ExercisesController.cs
│           ├── Workouts/
│           │   ├── WorkoutSession.cs
│           │   ├── SessionExercise.cs
│           │   ├── SetEntry.cs
│           │   ├── WorkoutDtos.cs
│           │   ├── WorkoutMappings.cs
│           │   ├── IWorkoutService.cs
│           │   ├── WorkoutService.cs
│           │   └── WorkoutsController.cs
│           ├── Templates/
│           ├── BodyMetrics/
│           └── Progress/
│               ├── StrengthCalculator.cs     # pure static math (like ComponentHealthCalculator)
│               ├── ProgressService.cs
│               └── ProgressController.cs
└── tests/
    ├── GymApp.UnitTests/             # pure logic: calculators, mappings, validators
    └── GymApp.IntegrationTests/      # real HTTP + real database via WebApplicationFactory
```

**Rules for this layout**
- A feature folder may use `Common/` and `Infrastructure/`. Features should **not** reach into each
  other's services; if `Progress` needs workout data, it queries `AppDbContext` itself or calls a public
  method on `IWorkoutService`.
- `Common/` holds only things used by 2+ features. Don't put things there speculatively.
- When a file grows past ~300 lines, or a service has more than ~7 public methods, split it.
- **When to split into multiple projects** (`GymApp.Domain`, `GymApp.Infrastructure`…): only when you have a
  concrete reason (e.g. a second app, like a background worker, needs the domain). Record the decision.
  Starting with 4–5 projects ("Clean Architecture") is common advice, but for one beginner developer it
  mostly adds ceremony.

### 4.3 Decisions already made for you (and why)

| Decision | Choice | Reason |
|---|---|---|
| Runtime | **.NET 10 (LTS)** | Same as AutoCare; long-term support. |
| API style | **Controllers** (`[ApiController]`) | Same as AutoCare, familiar, good for learning. Minimal APIs are fine too; pick one style and don't mix. |
| Data access | **EF Core, `DbContext` used directly in services**. No generic repository. | `DbContext` is already a unit of work and `DbSet<T>` already a repository. AutoCare's 2 repositories added a layer for 2 of 9 entities and nothing else. Add a specific query class only if a query is reused in 3+ places. |
| Mapping | **Explicit** (extension methods + LINQ `Select` projections) | No magic, compile-time errors, and projection queries only fetch needed columns. |
| Validation | **DataAnnotations on request DTOs** + business rules in services. Upgrade to FluentValidation only if rules get complex. | `[ApiController]` returns 400 automatically on invalid DTOs. |
| Errors | **ProblemDetails everywhere** via `GlobalExceptionHandler` | One error format for the frontend. |
| Auth | **Identity + JWT**, global fallback policy, `[AllowAnonymous]` only on auth endpoints | Proven in AutoCare. |
| Mediator libraries (MediatR etc.) | **No** | Indirection a beginner doesn't need. Plain services are enough. |
| Background work | **`BackgroundService` + `Channel<T>`** | Avoids AutoCare's `Task.Run` fire-and-forget issue. |
| Tests | **xUnit + FluentAssertions (or Shouldly) + `WebApplicationFactory` + SQLite in-memory or Testcontainers** | Real database behavior; see Part 7. Check the current license of each assertion library before adopting it. |

---

## Part 5: Domain model guidance for a gym app

### 5.1 Starting model for a personal training tracker (option A)

```
User (IdentityUser)
 ├── UserSettings (1:1)            preferred unit (kg/lb), time zone
 ├── Exercise (1:N, custom only)   global catalog has UserId = null
 ├── WorkoutTemplate (1:N)         "Push Day", reusable plan
 │    └── TemplateExercise (1:N)   order, target sets, target reps
 ├── WorkoutSession (1:N)          an actual workout that happened
 │    └── SessionExercise (1:N)    order, ExerciseId, notes
 │         └── SetEntry (1:N)      set number, reps, weight (kg), RPE?, IsWarmup
 └── BodyMeasurement (1:N)         date, body weight (kg), optional body fat %
```

**Agent:** walk through this with the developer and **ask before adding anything else**. Every extra entity
is a migration, a service, endpoints and tests.

### 5.2 Key modeling decisions to discuss

| Question | Recommended answer | Why |
|---|---|---|
| Does a session *reference* its template or *copy* from it? | **Copy.** `WorkoutSession.TemplateId` is optional and informational only; exercises/sets are copied into the session. | If the user edits "Push Day" next month, last month's history must not change. |
| Can an exercise used in history be deleted? | **No hard delete. Archive** it (`IsArchived = true`), hide it from pickers. | History must stay intact. AutoCare hit this exact problem: deleting a component required manually deleting linked records because of `NoAction` FKs. |
| Store personal records (PRs) in a table? | **No, compute them** from `SetEntry` with a query. Add a cache table only if it's measurably slow. | Stored derived data goes stale. AutoCare stores AI results on the entity and needs an `AiGeneratedAt` staleness guard. Avoid that complexity until needed. |
| Weight unit? | **Store `decimal WeightKg`** always. Convert at the API edge using the user's preference. | One canonical unit means correct math and comparisons. |
| Workout date/time? | Store `StartedAtUtc` / `FinishedAtUtc` as `DateTimeOffset` (UTC) **and** the user's time zone in settings. | "Which day did I train?" depends on the user's local time. |
| Set order and exercise order? | Explicit `int Order` / `SetNumber` columns. | Database rows have no natural order. |
| Bodyweight exercises (pull-ups)? | `WeightKg` is the *added* weight, nullable or 0; exercise has a `IsBodyweight` flag. | Decide early; it affects volume math. |
| Cardio (time/distance instead of reps)? | **Out of MVP** unless the first user needs it. If needed: separate nullable `DurationSeconds` / `DistanceMeters` on `SetEntry`, plus an `ExerciseKind` enum. | Keeps the first version simple. |

### 5.3 Pure domain logic: the gym's version of `ComponentHealthCalculator`

Put all fitness math in **static, dependency-free classes** with unit tests:

```csharp
namespace GymApp.Api.Features.Progress;

/// <summary>Single source of truth for strength math. No I/O, no DbContext: unit-test everything here.</summary>
public static class StrengthCalculator
{
    /// <summary>Estimated one-rep max using the Epley formula. Returns null when not meaningful.</summary>
    public static decimal? EstimateOneRepMax(decimal weightKg, int reps)
    {
        if (weightKg <= 0 || reps <= 0) return null;
        if (reps == 1) return weightKg;
        if (reps > 12) return null; // formula is unreliable at high reps
        return Math.Round(weightKg * (1 + reps / 30m), 1);
    }

    /// <summary>Training volume = Σ (weight × reps) over working sets only.</summary>
    public static decimal Volume(IEnumerable<(decimal WeightKg, int Reps, bool IsWarmup)> sets) =>
        sets.Where(s => !s.IsWarmup).Sum(s => s.WeightKg * s.Reps);
}
```

Use one function per concept, and have every other place call it. Don't recompute the same thing in two
places (AutoCare lesson #14).

### 5.4 If the developer insists on option B (gym management)

Extra entities: `Gym` (tenant), `Member`, `Staff`/`Trainer`, `MembershipPlan`, `Membership`, `GymClass`,
`ClassSession`, `Booking`, `CheckIn`, `Payment`. Raise these challenges **before** building:

- **Roles & multi-tenancy:** every query must filter by `GymId` *and* role. Consider EF Core global query
  filters for `GymId`, and write integration tests proving cross-gym access returns 404.
- **Booking concurrency:** two people booking the last spot at the same time. Use optimistic concurrency
  (`[Timestamp] byte[] RowVersion` / Postgres `xmin`) or a DB constraint; test it.
- **Payments:** never store card data. Use a provider (Stripe) and webhooks. This is a project in itself.
- **Legal/GDPR:** members' personal and health data, consent, data export and deletion.

### 5.5 Sensitive data

Body weight, measurements and health notes are **personal health data** (special category under GDPR).
Even for a hobby app: collect only what's needed, scope everything by user, support account deletion
(cascade the user's data), and never log these values.

---

## Part 6: Coding rules (with examples)

### 6.1 Project-wide settings (`Directory.Build.props`)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

Warnings-as-errors from day one is easy. Turning it on later, with 300 warnings, never happens.

### 6.2 Naming and style

- **File-scoped namespaces** (`namespace GymApp.Api.Features.Workouts;`), one public type per file
  (small DTO groups for one feature may share `XxxDtos.cs`).
- **File name = type name.** `AppDbContext` lives in `AppDbContext.cs`.
- PascalCase for types/methods/properties, camelCase for locals/parameters, `_camelCase` only if you
  declare private fields. With primary constructors, **use the parameter directly**; don't copy it into a
  field just to rename it (AutoCare does `private readonly IMapper _mapper = mapper;` everywhere).
- Async methods end with `Async`. Every async method takes a `CancellationToken ct` as its last parameter.
- Names describe intent: `GetRecentSessionsAsync`, not `GetData`. Collections are plural.
- Entity primary key: just `Id` (simpler than `WorkoutSessionId`). Foreign keys: `ExerciseId`. Decide once.
- Request DTOs: `CreateWorkoutRequest`, `UpdateWorkoutRequest`. Response DTOs: `WorkoutResponse`,
  `WorkoutSummaryResponse`. Use `record` types for DTOs.
- **Comments explain *why*, not *what*.** Good AutoCare example:
  `// Always generic — don't reveal whether the email exists or its confirmation state.`

### 6.3 Thin controllers

Controllers do HTTP only: bind input, call one service method, translate the result to a status code.
**No business logic, no `DbContext`, no try/catch.**

```csharp
namespace GymApp.Api.Common;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// The authenticated user's id. The global fallback policy guarantees authentication, so a missing
    /// claim is a server misconfiguration, not a client error.
    /// </summary>
    protected string CurrentUserId =>
        User.GetUserId() ?? throw new InvalidOperationException("Authenticated user has no id claim.");
}
```

```csharp
namespace GymApp.Api.Features.Workouts;

[Route("api/workouts")]
public sealed class WorkoutsController(IWorkoutService workouts) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkoutSummaryResponse>>> List(
        [FromQuery] PagedRequest paging, CancellationToken ct) =>
        Ok(await workouts.ListAsync(CurrentUserId, paging, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutResponse>> Get(int id, CancellationToken ct) =>
        await workouts.GetAsync(CurrentUserId, id, ct) is { } workout ? Ok(workout) : NotFound();

    [HttpPost]
    public async Task<ActionResult<WorkoutResponse>> Create(CreateWorkoutRequest request, CancellationToken ct)
    {
        var created = await workouts.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await workouts.DeleteAsync(CurrentUserId, id, ct) ? NoContent() : NotFound();
}
```

Compare with AutoCare's `VehicleController`: there, every action repeats the userId lookup, the ownership
check and the null check. Here the **service** enforces ownership, so the controller can't forget it.

### 6.4 Services: ownership in the query, reads untracked, one save per use case

```csharp
namespace GymApp.Api.Features.Workouts;

public sealed class WorkoutService(AppDbContext db, TimeProvider clock) : IWorkoutService
{
    // READ: AsNoTracking + project straight to the DTO + filter by owner in the same query.
    public async Task<WorkoutResponse?> GetAsync(string userId, int id, CancellationToken ct) =>
        await db.WorkoutSessions
            .AsNoTracking()
            .Where(w => w.Id == id && w.UserId == userId)   // ownership is part of the query
            .Select(WorkoutMappings.ToResponse)              // Expression<Func<WorkoutSession, WorkoutResponse>>
            .SingleOrDefaultAsync(ct);

    // LIST: always paginated, always ordered (stable paging needs a deterministic order).
    public async Task<PagedResult<WorkoutSummaryResponse>> ListAsync(
        string userId, PagedRequest paging, CancellationToken ct)
    {
        var query = db.WorkoutSessions.AsNoTracking().Where(w => w.UserId == userId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(w => w.StartedAtUtc).ThenByDescending(w => w.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(WorkoutMappings.ToSummary)
            .ToListAsync(ct);

        return new PagedResult<WorkoutSummaryResponse>(items, total, paging.Page, paging.PageSize);
    }

    // WRITE: tracked entities, all changes, ONE SaveChangesAsync = one transaction.
    public async Task<WorkoutResponse> CreateAsync(string userId, CreateWorkoutRequest request, CancellationToken ct)
    {
        var exerciseIds = request.Exercises.Select(e => e.ExerciseId).Distinct().ToList();
        var usable = await db.Exercises.AsNoTracking()
            .CountAsync(e => exerciseIds.Contains(e.Id) && !e.IsArchived
                             && (e.UserId == null || e.UserId == userId), ct);
        if (usable != exerciseIds.Count)
            throw new DomainValidationException("One or more exercises do not exist.");

        var session = request.ToEntity(userId, clock.GetUtcNow());
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return session.ToResponse();
    }
}
```

**The rules behind the example**

1. **Reads:** `AsNoTracking()` **and** project with `Select` to the DTO. Projection fetches only the
   needed columns and makes `AsNoTracking` implicit. Use `Include` only when you'll modify the graph.
2. **Writes:** load tracked, modify, **one** `SaveChangesAsync`. Need several aggregates in one transaction?
   Still one `SaveChangesAsync`. Use explicit transactions only for multiple saves or raw SQL.
3. **Ownership** is a `Where(... && x.UserId == userId)` in the same query, never a separate pre-check.
   For child entities, filter through the parent: `s.Session.UserId == userId`.
4. **Inject `TimeProvider`** instead of calling `DateTime.UtcNow` directly, so time is testable.
5. **GETs never write.**
6. **Validate cross-entity rules in the service** (does the exercise exist and belong to this user?).
   Shape rules (required, ranges) go on the DTO.
7. **Never `Enum.Parse` on user input.** DTOs use enum types; `JsonStringEnumConverter` handles strings.

### 6.5 EF Core configuration

```csharp
namespace GymApp.Api.Infrastructure.Data.Configurations;

public sealed class SetEntryConfiguration : IEntityTypeConfiguration<SetEntry>
{
    public void Configure(EntityTypeBuilder<SetEntry> b)
    {
        b.Property(s => s.WeightKg).HasPrecision(6, 2);
        b.HasIndex(s => new { s.SessionExerciseId, s.SetNumber }).IsUnique();

        b.HasOne(s => s.SessionExercise)
         .WithMany(e => e.Sets)
         .HasForeignKey(s => s.SessionExerciseId)
         .OnDelete(DeleteBehavior.Cascade);       // deleting a workout deletes its sets
    }
}

// AppDbContext.OnModelCreating:
//   base.OnModelCreating(modelBuilder);
//   modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

- One configuration class per entity. `OnModelCreating` stays 2 lines.
- **Decide delete behavior deliberately** for every relationship and write a comment with the reason.
  User → their data: Cascade. Exercise → history: **Restrict** (archive instead).
- **Index** every foreign key you filter by and every column you sort lists by (`UserId`, `StartedAtUtc`).
- `decimal` everywhere for weights and money, with explicit precision.
- **Migrations:** descriptive names (`AddBodyMeasurements`, not `Tuning` or `FixCascadeVehicle`). Review the
  generated migration before committing. Never edit a migration that's already been applied anywhere shared.
- **Apply migrations on startup only in Development.** In production, run them as a deploy step
  (`dotnet ef migrations bundle` or a script).
- Seed the exercise catalog with `HasData` or a seeder that runs in all environments. Seed demo users only in Development.

### 6.6 DTOs and validation

```csharp
namespace GymApp.Api.Features.Workouts;

public sealed record CreateWorkoutRequest(
    [property: Required, StringLength(100)] string Name,
    [property: Required] DateTimeOffset StartedAt,
    [property: MinLength(1), MaxLength(30)] IReadOnlyList<CreateSessionExerciseRequest> Exercises);

public sealed record CreateSetRequest(
    [property: Range(1, 1000)] int Reps,
    [property: Range(0, 1000)] decimal WeightKg,
    bool IsWarmup = false);

public sealed record WorkoutResponse(
    int Id, string Name, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt,
    IReadOnlyList<SessionExerciseResponse> Exercises);
```

- Every request DTO has validation attributes. Every string has a max length (prevents huge payloads and
  matches DB column sizes).
- Always set limits on collection sizes.
- Response DTOs contain only what the client needs, never `UserId`, password hashes or internal flags.
- For PATCH: AutoCare's "nullable means don't change" works but can't express "set this to null". Prefer
  **PUT with the full object** for small resources; use PATCH only where partial updates are truly needed.

### 6.7 HTTP status codes: one table, used everywhere

| Situation | Status | How |
|---|---|---|
| Read OK | 200 | `Ok(dto)` |
| Created | 201 + `Location` header | `CreatedAtAction(nameof(Get), new { id }, dto)` |
| Updated | 200 with the updated DTO | `Ok(dto)` |
| Deleted | 204 | `NoContent()` |
| Invalid input (shape) | 400 ProblemDetails | automatic via `[ApiController]` |
| Business rule violated | 400 or 409 ProblemDetails | throw `DomainValidationException` / `ConflictException` → global handler |
| Not authenticated | 401 | automatic via auth middleware |
| Not found **or not owned** | **404** | service returns `null` / `false` |
| Too many requests | 429 | rate limiter |
| Unexpected error | 500 ProblemDetails, generic message, logged with details | global handler |

Never return 403 for "this belongs to another user"; it confirms the resource exists. Use 403 only for
role-based restrictions on resources the user can know exist (e.g. a non-admin calling an admin endpoint).

### 6.8 Errors and logging

```csharp
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            DomainValidationException => (StatusCodes.Status400BadRequest, ex.Message),
            ConflictException         => (StatusCodes.Status409Conflict, ex.Message),
            _                         => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (status >= 500)
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new()
        {
            HttpContext = ctx,
            ProblemDetails = { Status = status, Title = title },
            Exception = ex,
        });
    }
}
```

- Only domain exceptions carry user-facing messages. Everything else is a generic 500.
- **Structured logging** with placeholders: `logger.LogInformation("Workout {WorkoutId} created", id)`, never
  string interpolation in log messages.
- Never log passwords, tokens, health values or full request bodies.
- Don't catch exceptions just to log and rethrow. Catch only where you can actually handle it.

### 6.9 Configuration and secrets

- Bind every config section to an **options class** (`JwtOptions`, `EmailOptions`) with
  `AddOptions<T>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart()`. The app then fails
  fast at startup when config is missing, not on the first login (AutoCare's `JwtTokenService` reads
  `_config["Jwt:Key"]` at request time).
- Secrets go in `dotnet user-secrets` locally and in environment variables or a key vault in production.
  `appsettings.json` holds only safe defaults.
- No personal data (real emails, names) in committed config files.
- Separate CORS policies per environment; production allows only the real frontend origin.

### 6.10 `Program.cs` stays short

Group registrations into extension methods, one per area:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistence(builder.Configuration)
    .AddAuth(builder.Configuration)
    .AddApiDefaults()            // controllers, JSON enum converter, ProblemDetails, exception handler
    .AddRateLimiting()
    .AddFeatures();              // IWorkoutService, IExerciseService, …

var app = builder.Build();
app.UseApiPipeline();            // ordered middleware in one place
app.MapControllers();

if (app.Environment.IsDevelopment())
    await app.MigrateAndSeedAsync();

app.Run();

public partial class Program;   // lets integration tests use WebApplicationFactory<Program>
```

**Middleware order matters:** exception handler → HTTPS/security headers → CORS → authentication →
authorization → rate limiter → endpoints. Write it once and comment why.

### 6.11 Background work

If something must run after the request (recalculate stats, send email):

```csharp
public sealed class StatsRecalculationQueue
{
    private readonly Channel<string> _channel = Channel.CreateBounded<string>(100);
    public ValueTask EnqueueAsync(string userId, CancellationToken ct) => _channel.Writer.WriteAsync(userId, ct);
    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

public sealed class StatsRecalculationWorker(
    StatsRecalculationQueue queue, IServiceScopeFactory scopes, ILogger<StatsRecalculationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var userId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();   // DbContext is scoped: new scope per job
                var stats = scope.ServiceProvider.GetRequiredService<IProgressService>();
                await stats.RecalculateAsync(userId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Stats recalculation failed for user {UserId}", userId);
            }
        }
    }
}
```

But first, **ask whether it needs to be background at all**. Computing stats on read is often fast enough.

---

## Part 7: Testing strategy

### 7.1 What to test, in priority order

1. **Authorization / ownership**: *user B cannot read, update or delete user A's workout* (expect 404).
   These are the most important tests in the app. Write them for every resource.
2. **Pure domain logic**: `StrengthCalculator`, PR detection, unit conversion. Fast unit tests with many cases.
3. **Service behavior with a real database**: creating a workout saves sets in order; archiving an
   exercise hides it from lists but keeps history.
4. **API contract**: status codes (201 + Location, 204, 400 on invalid input, 404) and response shape.

### 7.2 Tools and setup

- **Unit tests** (`GymApp.UnitTests`): xUnit + `[Theory]`/`[InlineData]` for calculators.
- **Integration tests** (`GymApp.IntegrationTests`): `WebApplicationFactory<Program>` with a real relational
  database: **SQLite in-memory** (fast, simple) or **Testcontainers** with the production engine (most
  realistic). **Don't use EF Core InMemory** (AutoCare does): it ignores foreign keys, unique indexes and SQL
  translation, so tests pass while production fails.
- A test helper that registers a user, logs in and returns an authenticated `HttpClient`.
- Each test sets up its own data. No shared mutable state between tests. No test depends on another's order.

```csharp
public sealed class WorkoutOwnershipTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Get_workout_of_another_user_returns_404()
    {
        var alice = await factory.CreateAuthenticatedClientAsync("alice@test.local");
        var bob   = await factory.CreateAuthenticatedClientAsync("bob@test.local");

        var created = await alice.PostAsJsonAsync("/api/workouts", TestData.SimpleWorkout());
        var workout = await created.Content.ReadFromJsonAsync<WorkoutResponse>();

        var response = await bob.GetAsync($"/api/workouts/{workout!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

```csharp
public sealed class StrengthCalculatorTests
{
    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 5, 116.7)]
    [InlineData(60, 10, 80)]
    public void EstimateOneRepMax_uses_epley(decimal weight, int reps, decimal expected) =>
        StrengthCalculator.EstimateOneRepMax(weight, reps).Should().Be(expected);

    [Theory]
    [InlineData(0, 5)]
    [InlineData(100, 0)]
    [InlineData(100, 20)]
    public void EstimateOneRepMax_returns_null_when_not_meaningful(decimal weight, int reps) =>
        StrengthCalculator.EstimateOneRepMax(weight, reps).Should().BeNull();
}
```

### 7.3 Rules

- Test names describe behavior: `Method_condition_expected` or a plain sentence.
- One logical assertion per test (multiple `Should()` calls on the same outcome are fine).
- No `Assert.True(true)`, no empty tests, no commented-out tests.
- A bug fix starts with a failing test that reproduces the bug.
- Don't chase a coverage percentage. Cover the four priorities above.

---

## Part 8: Security checklist

Run through this list for every feature. **Agent:** don't mark a feature done until each item is addressed.

- [ ] Endpoint requires authentication (global fallback policy; `[AllowAnonymous]` only on register/login/confirm).
- [ ] Every query on user data filters by `UserId` (directly or through the parent).
- [ ] Integration test proves cross-user access returns 404.
- [ ] Request DTO has validation: required fields, string lengths, numeric ranges, collection sizes.
- [ ] No entity returned from a controller; response DTO contains no internal fields.
- [ ] No secrets, keys or personal data in code, config files or logs.
- [ ] Error responses are ProblemDetails; no exception messages or stack traces leak.
- [ ] Login and other expensive endpoints are rate-limited.
- [ ] File uploads (if any): size limit, content-type allowlist, random stored name, stored outside
      `wwwroot` or served with care (see AutoCare's `ImageUploadValidator` + `LocalFileStorage`).
- [ ] Passwords only through Identity (never hashed by hand); account lockout enabled after failed attempts.
- [ ] CORS allows only known frontend origins in production.
- [ ] Trusted proxies configured explicitly if behind a reverse proxy (AutoCare lesson #19).
- [ ] Dependencies scanned (`dotnet list package --vulnerable` in CI).
- [ ] Account deletion removes the user's data (GDPR right to erasure).

---

## Part 9: Workflow, Definition of Done, maintenance

### 9.1 Git workflow

- `main` is always deployable. Work on short-lived branches: `feat/workout-logging`, `fix/pr-calculation`.
- **Conventional commits** (as AutoCare does): `feat(workouts): log sets with reps and weight`,
  `fix(progress): ignore warm-up sets in volume`, `refactor(data): move configs to IEntityTypeConfiguration`,
  `test(...)`, `docs(...)`, `chore(...)`.
- Small commits that each build and pass tests. One concern per commit; don't mix refactors with features.
- Pull request for every change, even when working alone. Read your own diff before merging.
- CI on every push/PR (`.github/workflows/ci.yml`):

```yaml
name: ci
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet restore
      - run: dotnet format --verify-no-changes
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release
      - run: dotnet list package --vulnerable --include-transitive
```

(Check the action versions are current when you create the file.)

### 9.2 Definition of Done: every feature, every time

- [ ] Product questions for the feature were asked and answered; decision logged if non-trivial.
- [ ] Follows the folder structure and existing patterns (no new "second way" of doing something).
- [ ] Entity configuration in its own class; migration has a descriptive name and was reviewed.
- [ ] Reads use `AsNoTracking()` + projection; writes use one `SaveChangesAsync`.
- [ ] Every async method takes and passes a `CancellationToken`.
- [ ] List endpoints are paginated and ordered.
- [ ] Status codes follow the table in Part 6.7.
- [ ] Security checklist (Part 8) done.
- [ ] Unit tests for pure logic; integration tests for the endpoint including the ownership test.
- [ ] `dotnet build` has zero warnings; `dotnet format` is clean; all tests pass.
- [ ] README / API docs updated (or confirmed still accurate).
- [ ] No TODO comments, dead code or placeholder tests; open follow-ups as issues.
- [ ] The developer can explain what the code does and why.

### 9.3 Keeping it maintainable over time

- **`docs/DECISIONS.md`**: short entries (date, context, options, decision, consequences). Example:
  *"2026-11-20: No generic repository. DbContext is already a unit of work; a wrapper would duplicate it.
  Revisit if we need to swap the ORM."*
- **Refactor in dedicated commits.** When you find a better pattern, apply it everywhere or note it in an
  issue. Don't leave the codebase with two patterns (the root cause of most AutoCare inconsistencies).
- **Boy-scout rule:** leave each file you touch slightly cleaner, but in a separate commit if it's not
  related to the feature.
- **Dependencies:** update monthly (Dependabot/Renovate). Read release notes for major versions. Check
  licenses: several popular .NET libraries changed to commercial licenses in 2025.
- **Docs:** README stays the single entry point (what, stack, setup, architecture, link to OpenAPI). Endpoint
  lists come from OpenAPI/Swagger, not hand-written tables that drift.
- **Monitor** basic health: a `/health` endpoint (`AddHealthChecks().AddDbContextCheck<AppDbContext>()`),
  structured logs, and once deployed, error alerts.
- **Backups:** if real users exist, automated DB backups and a tested restore.

---

## Part 10: Roadmap: phases and milestones

Each phase ends with something working, tested and committed. **Don't start a phase until the previous
one meets its "Done when".**

### Phase 0: Discovery (no code)
- Part 3 questions → `PRODUCT.md`, `DECISIONS.md`, `BACKLOG.md`, entity sketch.
- **Done when:** the developer can describe the MVP in 3 sentences and list what's out of scope.

### Phase 1: Walking skeleton
- Solution + folders (Part 4.2), `Directory.Build.props`, `.editorconfig`, CI pipeline.
- `AppDbContext`, first migration, Identity + JWT auth (register, login, confirm email optional), global
  fallback policy, ProblemDetails handler, security headers, rate limit on login, Swagger with bearer auth,
  `/health`.
- Integration test infrastructure (`ApiFactory`, authenticated client helper).
- **Done when:** CI is green; a test registers, logs in and calls an authenticated endpoint; an
  unauthenticated call returns 401.

### Phase 2: First vertical slice: Exercises
- Seeded global catalog + user custom exercises (CRUD, archive instead of delete).
- **This slice becomes the template for all others**: review it extra carefully. The agent should explain
  every file; the developer should be able to rebuild it from memory.
- **Done when:** Definition of Done is met; ownership tests pass (user can't see another user's custom exercise).

### Phase 3: Workout logging (the core)
- Workout sessions with exercises and sets: create, view, list (paginated), edit, delete.
- Focus on the UX of fast logging (few taps, sensible defaults like "copy last set").
- **Done when:** the developer logs real workouts with it for a week. Collect what was annoying → backlog.

### Phase 4: Templates and progress
- Workout templates; "start workout from template" (copy, Part 5.2).
- `StrengthCalculator` + progress endpoints: PRs per exercise, estimated 1RM over time, weekly volume.
- Body measurements.
- **Done when:** a user can see if they're getting stronger, and every calculation has unit tests.

### Phase 5: Polish and extras (only from the backlog, one at a time)
- Ideas: unit preference (kg/lb), streaks, data export (CSV), charts, rest timer, AI workout summary
  (reuse AutoCare's patterns: provider behind an interface, typed `HttpClient`, daily quota, prompts in
  dedicated builder classes), social features.
- Each extra: run Part 3.3's challenge questions first.

### Phase 6: Deploy
- Production config (secrets, CORS, trusted proxies), migrations as a deploy step, backups, HTTPS,
  logging/alerts.
- **Done when:** a fresh deploy from `main` works by following the README alone.

---

## Appendix A: CLAUDE.md template for the new repo

Put this file at the root of the new repository so every AI session starts with the right context:

```markdown
# GymApp: instructions for AI agents

Read `docs/BLUEPRINT.md` (Part 1 especially) before doing anything. Then read `docs/DECISIONS.md`
and `docs/PRODUCT.md`.

## Role
You are a senior .NET mentor pair-programming with a developer who is learning clean code.
Ask before building, challenge scope, work in thin vertical slices, explain new concepts briefly,
and have the developer write parts of the code themselves.

## Non-negotiables
- Feature folders under `src/GymApp.Api/Features/<Feature>/`; follow existing patterns exactly.
- Controllers are thin and inherit `ApiControllerBase`; no DbContext or business logic in controllers.
- Services take `userId` and filter ownership inside the query; not-found and not-owned both return 404.
- Reads: `AsNoTracking()` + `Select` projection. Writes: one `SaveChangesAsync` per use case.
- Every async method takes a `CancellationToken`. Every list endpoint is paginated.
- DTOs are records with validation attributes; enums are typed (JsonStringEnumConverter), never `Enum.Parse` on input.
- Errors are ProblemDetails via `GlobalExceptionHandler`. Never return exception messages.
- One `IEntityTypeConfiguration<T>` per entity. Descriptive migration names.
- No secrets or personal data in committed files.
- Tests: unit tests for pure logic; integration tests (WebApplicationFactory + real relational DB) for
  every endpoint, including a cross-user 404 test. No EF InMemory provider.
- Zero build warnings; `dotnet format --verify-no-changes` passes.
- Conventional commits. Record non-trivial decisions in `docs/DECISIONS.md`.

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Format: `dotnet format`
- Add migration: `dotnet ef migrations add <DescriptiveName> -p src/GymApp.Api`
- Run: `dotnet run --project src/GymApp.Api`
```

---

## Appendix B: Glossary

| Term | Plain explanation |
|---|---|
| **Entity** | A C# class that maps to a database table (e.g. `WorkoutSession`). Internal: never sent to the client. |
| **DTO** (Data Transfer Object) | A class/record that defines what the API receives or returns. Decouples the API from the database. |
| **DbContext** | EF Core's session with the database. Tracks loaded entities and saves changes in one transaction. |
| **Change tracking / `AsNoTracking()`** | EF remembers every loaded entity to detect changes. For read-only queries that's wasted work, so `AsNoTracking()` turns it off. |
| **Projection** | Using `.Select(x => new Dto(...))` in a query so the database returns only the needed columns. |
| **Migration** | A versioned C# file describing a database schema change. Applied in order to evolve the DB. |
| **Dependency Injection (DI)** | The framework creates your services and passes them into constructors, so classes don't build their own dependencies. Makes code testable. |
| **Scoped / Singleton / Transient** | DI lifetimes: one instance per request / one for the app's lifetime / a new one every time. `DbContext` is scoped, so never inject it into a singleton. |
| **Middleware** | Code that runs on every HTTP request in a pipeline (auth, error handling, CORS). Order matters. |
| **JWT** | A signed token the client sends with each request to prove who it is. |
| **IDOR** | Insecure Direct Object Reference: accessing someone else's data by changing an ID in the URL. Prevented by ownership filters. |
| **ProblemDetails** | Standard JSON error format (RFC 7807): `type`, `title`, `status`, `detail`. |
| **Vertical slice** | One feature built through all layers (DB → API → tests) before moving on. |
| **Modular monolith** | One deployable app with internal boundaries between features. |
| **Idempotent** | Doing it twice has the same effect as doing it once (PUT, DELETE should be). |
| **Optimistic concurrency** | Detecting that someone else changed a row since you read it (row version), instead of locking. |
| **1RM** | One-rep max: the heaviest weight you can lift once. Estimated from sub-maximal sets with formulas like Epley. |
| **Volume** | Total work: Σ weight × reps over working sets. |
| **RPE** | Rate of Perceived Exertion (1–10): how hard a set felt. |

---

*Derived from a review of the AutoCare codebase (`Persay23/VehicleMaintenance`). When this blueprint and the
new project's reality disagree, update the blueprint and log the change in `DECISIONS.md`.*
