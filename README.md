# Fitness Logger

Webowa aplikacja fitness wspierająca planowanie i monitorowanie aktywności fizycznej
(praca dyplomowa, WSIiZ Rzeszów).

A web app for planning and tracking workouts: exercise library, training plans, workout logging,
progress charts, body weight and BMI, goals and reminders. It runs in any browser, with nothing to install.

**Status:** week 2 of 8. Accounts, profile with BMI and the exercise library work in the API and the web app. See [docs/PLAN.md](docs/PLAN.md).

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
    └── Exercises/             # Exercise, ExerciseService, user + admin controllers
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
| GET | `/health` | anonymous | `Healthy` when the API can reach the database |

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc7807) JSON with messages meant for the user.

## Working on the code

- Every change goes through a pull request; CI must be green before merging.
- Add a migration: `dotnet tool restore`, then
  `dotnet ef migrations add <DescriptiveName> --project src/FitnessApp.Api --output-dir Infrastructure/Data/Migrations`
- Format: `dotnet format`
- Rules for contributors and AI agents: [CLAUDE.md](CLAUDE.md).
