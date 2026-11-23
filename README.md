# Fitness Logger

Webowa aplikacja fitness wspierająca planowanie i monitorowanie aktywności fizycznej
(praca dyplomowa, WSIiZ Rzeszów).

A web app for planning and tracking workouts: exercise library, training plans, workout logging,
progress charts, body weight and BMI, goals and reminders. It runs in any browser, with nothing to install.

**Status:** week 3 of 8. Accounts, profile with BMI, the exercise library and training plans.

## Stack

| Layer | Technology |
|---|---|
| API | .NET 10, ASP.NET Core Web API (controllers), EF Core 10 |
| Database | PostgreSQL 16 |
| Auth | ASP.NET Core Identity + JWT bearer tokens |
| Frontend | React 19 + TypeScript + Vite, Tailwind CSS 4, React Router, TanStack Query, Axios |
| Tests | xUnit v3, Shouldly, Testcontainers (real PostgreSQL in Docker); Vitest + Testing Library |
| CI | GitHub Actions: backend (format, build, migrations check, tests, package scan) and frontend (lint, typecheck, tests, build, audit) |

Why these choices, and where the code differs from the thesis: [docs/DECISIONS.md](docs/DECISIONS.md).

## Architecture

One API project organized by feature. Everything about a feature lives in one folder:

```
src/FitnessApp.Api/
├── Program.cs                 # ~20 lines: registers areas, builds the pipeline
├── Common/                    # shared by several features: base controller, errors, extensions
├── Infrastructure/
│   ├── Auth/                  # JWT, Identity setup, rate limiting
│   └── Data/                  # AppDbContext, entity configurations, migrations, seeding
└── Features/
    ├── Auth/                  # AppUser, DTOs, AuthService, AuthController
    ├── Profile/               # UserProfile, BmiCalculator, AgeCalculator, ProfileService
    ├── Exercises/             # Exercise, ExerciseService, user + admin controllers
    └── Plans/                 # TrainingPlan, PlanExercise, PlanStateMachine, PlanService
tests/
├── FitnessApp.UnitTests/          # pure logic, no database
└── FitnessApp.IntegrationTests/   # real HTTP calls against a real PostgreSQL
web/src/
├── api/                       # HTTP client, one function per endpoint, DTO types, error mapping
├── auth/                      # login state (token in localStorage), protected routes
├── components/                # layout and shared UI (buttons, fields, BMI card)
├── lib/                       # display labels, query keys, React Query setup
└── pages/                     # one file per screen, with its tests next to it
```

A request goes **Controller → Service → AppDbContext → PostgreSQL**. Controllers only handle HTTP;
services hold the rules; every query on user data filters by the logged-in user.

## Running locally

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Docker Desktop](https://www.docker.com/products/docker-desktop/), Git.

```bash
# 1. Start PostgreSQL (first time downloads the image)
docker compose up -d db

# 2. Set the JWT signing key once (any random string of 32+ characters; stays on your machine)
dotnet user-secrets set "Jwt:Key" "<32+ random characters>" --project src/FitnessApp.Api

# 3. Optional: a local admin account, created on startup in Development
dotnet user-secrets set "SeedAdmin:Email" "admin@fitnessapp.local" --project src/FitnessApp.Api
dotnet user-secrets set "SeedAdmin:Password" "<a password with upper, lower case and a digit>" --project src/FitnessApp.Api

# 4. Run the API: applies migrations, then opens Swagger at http://localhost:5080/swagger
dotnet run --project src/FitnessApp.Api --launch-profile http
```

In Swagger: call `POST /api/auth/register`, copy `accessToken` from the response, click **Authorize**,
paste it, then call `GET /api/auth/me`.

### The web app

Requirements: [Node.js 26](https://nodejs.org/). With the API running (step 4):

```bash
cd web
npm install
npm run dev
```

Open http://localhost:5173. In development the web app sends `/api` calls to the API on port 5080
(Vite proxy), so no CORS setup is needed.

The app is installable on a phone (a PWA: `public/manifest.webmanifest`, `public/sw.js`). The service worker
runs only in a production build, so to try it locally use `npm run build` and then `npm run preview`
(http://localhost:4173). Phones offer to install only over HTTPS, so on a phone this works on the live site.

## Tests

```bash
dotnet test
```

```bash
cd web && npm test     # frontend: Vitest + Testing Library, no API needed
```

Integration tests start their own PostgreSQL container, so Docker must be running. They don't touch your
local database.

## API

The full, always-current list is in Swagger (`/swagger` when running in Development). So far:

| Method | Path | Auth | What it does |
|---|---|---|---|
| POST | `/api/auth/register` | anonymous | Create an account; returns a token (user is logged in) |
| POST | `/api/auth/login` | anonymous | Log in; 3 wrong passwords lock the account for 15 min (423) |
| POST | `/api/auth/logout` | user | Logout (the client discards its token) |
| GET | `/api/auth/me` | user | The logged-in user |
| GET | `/api/profile` | user | Own profile with age and BMI (empty until first saved) |
| PUT | `/api/profile` | user | Save own profile: birth date, sex, height, weight, goal, time zone |
| GET | `/api/exercises` | user | Library: global + own custom; `search`, `category`, `difficulty`, `equipment`, `onlyCustom`, paging |
| GET | `/api/exercises/{id}` | user | One exercise (also archived ones) |
| POST | `/api/exercises` | user | Add a custom exercise (only this user sees it) |
| PUT | `/api/exercises/{id}` | user | Edit own custom exercise (global ones: 403) |
| DELETE | `/api/exercises/{id}` | user | Archive own custom exercise |
| POST, PUT, DELETE | `/api/admin/exercises[/{id}]` | admin | Manage the global catalog |
| GET | `/api/plans` | user | Own plans, newest change first; `status` filter, paging |
| GET | `/api/plans/templates` | user | Ready-made template plans |
| GET | `/api/plans/{id}` | user | One plan with its exercises (own or template) |
| POST | `/api/plans` | user | Create a draft plan with exercises |
| PUT | `/api/plans/{id}` | user | Save a plan (name, description, exercises in order) |
| POST | `/api/plans/{id}/status` | user | Activate, archive or restore |
| POST | `/api/plans/{id}/copy` | user | Copy a template or own plan into a new draft |
| DELETE | `/api/plans/{id}` | user | Delete a draft |
| GET | `/api/sessions` | user | Workout history, newest first; `from`, `to`, `exerciseId`, `status`, paging |
| GET | `/api/sessions/{id}` | user | One workout with exercises and sets |
| GET | `/api/sessions/active` | user | The workout in progress or paused (204 if none) |
| GET | `/api/sessions/start-options` | user | Active plans and templates to start from, most recently used first |
| POST | `/api/sessions` | user | Save a workout entered afterwards (completed) |
| PUT | `/api/sessions/{id}` | user | Correct a finished workout (details, exercises, sets) |
| POST | `/api/sessions/start` | user | Start a live workout now, empty or from a plan (one at a time) |
| POST | `/api/sessions/{id}/start\|pause\|resume\|finish` | user | Change a workout's status |
| POST, DELETE | `/api/sessions/{id}/exercises[/{entryId}]` | user | Add or remove an exercise during a workout |
| POST, PUT, DELETE | `/api/sessions/{id}/exercises/{entryId}/sets[/{setId}]` | user | Log, correct or delete a set during a workout |
| DELETE | `/api/sessions/{id}` | user | Delete a workout |
| GET | `/api/exercises/suggestions` | user | The user's most-used (90 days) and recent exercises |
| GET | `/api/exercises/{id}/history` | user | Every finished workout with this exercise and its sets |
| GET | `/api/measurements` | user | Body measurements, newest day first, each with its BMI; `from`, `to`, paging |
| GET, PUT, DELETE | `/api/measurements/{id}` | user | One day's measurements |
| POST | `/api/measurements` | user | Save a day's measurements (one entry per day); a newer weight updates the profile |
| GET | `/api/progress/summary` | user | Dashboard numbers: workouts this week/month, streak, volume, weight, BMI |
| GET | `/api/progress/volume` | user | Volume, workouts and sets per week; `weeks` (1–52) |
| GET | `/api/progress/exercises` | user | Exercise ranking for a `period`: workouts, volume, best 1RM, 1RM change |
| GET | `/api/progress/exercises/{id}` | user | One exercise in detail: 1RM and top set per workout, rep ranges |
| GET | `/api/progress/records` | user | Personal records: heaviest set, best estimated 1RM, best workout volume |
| GET | `/api/progress/muscle-groups` | user | Working sets and volume per week by muscle group |
| GET | `/health` | anonymous | `Healthy` when the API can reach the database |

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc7807) JSON with messages meant for the user.

## Working on the code

- Every change goes through a pull request; CI must be green before merging.
- Add a migration: `dotnet tool restore`, then
  `dotnet ef migrations add <DescriptiveName> --project src/FitnessApp.Api --output-dir Infrastructure/Data/Migrations`
- Format: `dotnet format`
- Rules for contributors and AI agents: [CLAUDE.md](CLAUDE.md).
