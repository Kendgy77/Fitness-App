# Fitness App

Webowa aplikacja fitness wspierająca planowanie i monitorowanie aktywności fizycznej
(praca dyplomowa, WSIiZ Rzeszów).

A web app for planning and tracking workouts: exercise library, training plans, workout logging,
progress charts, body weight and BMI, goals and reminders. It runs in any browser, with nothing to install.

**Status:** week 1 of 8. Accounts work (register, login, lockout). See [docs/PLAN.md](docs/PLAN.md).

## Stack

| Layer | Technology |
|---|---|
| API | .NET 10, ASP.NET Core Web API (controllers), EF Core 10 |
| Database | PostgreSQL 16 |
| Auth | ASP.NET Core Identity + JWT bearer tokens |
| Frontend | React + TypeScript + Vite, Tailwind CSS, Chart.js (from week 2) |
| Tests | xUnit v3, Shouldly, Testcontainers (real PostgreSQL in Docker) |
| CI | GitHub Actions: format check, build, tests, vulnerable package scan |

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
    └── Auth/                  # AppUser, DTOs, AuthService, AuthController
tests/
├── FitnessApp.UnitTests/          # pure logic, no database
└── FitnessApp.IntegrationTests/   # real HTTP calls against a real PostgreSQL
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

## Tests

```bash
dotnet test
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
| GET | `/health` | anonymous | `Healthy` when the API can reach the database |

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc7807) JSON with Polish messages.

## Working on the code

- Every change goes through a pull request; CI must be green before merging.
- Add a migration: `dotnet tool restore`, then
  `dotnet ef migrations add <DescriptiveName> --project src/FitnessApp.Api --output-dir Infrastructure/Data/Migrations`
- Format: `dotnet format`
- Rules for contributors and AI agents: [CLAUDE.md](CLAUDE.md).
