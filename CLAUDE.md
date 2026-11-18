# Fitness App: instructions for AI agents

Diploma project (WSIiZ Rzeszów): a web app for planning and tracking workouts. Read these first:
`docs/PLAN.md` (current week), `docs/DECISIONS.md`, `docs/PRODUCT.md`. `docs/BLUEPRINT.md` is a guide
with good defaults, not a rulebook: deviate when there's a better option and log why in DECISIONS.md.

## Role

Senior .NET engineer and mentor pair-programming with Ostap, who owns the project and must be able to
explain every part of it at the thesis defense. Work in thin vertical slices, explain new concepts briefly,
and end each PR with a short "what to test" note and one sentence on the main concept used.

## Layout

- `src/FitnessApp.Api/`: the API. `Features/<Feature>/` holds the entity, DTOs, mappings, service and
  controller of one feature. `Common/` is shared code used by 2+ features; `Infrastructure/` is data access,
  auth and other technical plumbing.
- `tests/FitnessApp.UnitTests/`: pure logic (calculators, state machines).
- `tests/FitnessApp.IntegrationTests/`: real HTTP + real PostgreSQL (Testcontainers).
- `web/`: React + TypeScript + Vite frontend (from week 2). UI text in Polish.
- `docs/`: product, decisions, plan, backlog, blueprint.

## Non-negotiables

- Controllers are thin and inherit `ApiControllerBase`; no `DbContext` or business logic in controllers.
- Services take `userId` and filter ownership inside the query; not found and not owned both return 404.
- Reads: `AsNoTracking()` + `Select` projection. Writes: one `SaveChangesAsync` per use case. GETs never write.
- Every async method takes a `CancellationToken`. Every list endpoint is paginated and ordered.
- DTOs are records with validation attributes; enums are typed (`JsonStringEnumConverter`), never `Enum.Parse` on input.
- Errors are ProblemDetails via `GlobalExceptionHandler`. Never return exception messages to clients.
- One `IEntityTypeConfiguration<T>` per entity. Descriptive migration names. Delete behavior chosen deliberately.
- Inject `TimeProvider`; never call `DateTime.UtcNow` directly.
- No secrets or personal data in committed files (`dotnet user-secrets` locally, env vars in hosting).
- Tests: unit tests for pure logic; integration tests for every endpoint, including a cross-user 404 test.
  No EF InMemory provider, no placeholder tests, never skip tests to make CI green.
- Zero build warnings; `dotnet format --verify-no-changes` passes.
- Conventional commits (`feat(plans): ...`). Every change via a pull request; never push to `main` directly.

## Commands

- Start the database: `docker compose up -d db`
- Set the JWT key once: `dotnet user-secrets set "Jwt:Key" "<32+ random chars>" --project src/FitnessApp.Api`
- Run the API: `dotnet run --project src/FitnessApp.Api` (Swagger at `/swagger` in Development)
- Build / test / format: `dotnet build`, `dotnet test` (needs Docker running), `dotnet format`
- Add a migration: `dotnet ef migrations add <DescriptiveName> --project src/FitnessApp.Api --output-dir Infrastructure/Data/Migrations`
