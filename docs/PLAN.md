# Plan: 8 weeks, 17 Nov 2026 – 10 Jan 2027

The full plan with dates, risks and a detailed step-by-step playbook lives in the shared doc
"Fitness App – 8-week plan". This file is the in-repo checklist: tick items as PRs merge.

- Feature blocks: weeks 1–6. Feature freeze and `v1.0.0` tag on **3 Jan 2027**. After that only bug fixes.
- Every change goes in as a pull request with green CI. Ostap reviews and merges on weekdays.
- A feature is done when CI is green, it has tests (including "user B can't see user A's data"),
  it works in a phone browser, and Ostap can explain how it works.

## Week 1 · 17–22 Nov · Foundation and login (WF01, WF02, WNF02)

- [x] PR docs/phase-0: BLUEPRINT, PRODUCT, DECISIONS, BACKLOG, PLAN, CLAUDE.md
- [x] PR chore/skeleton: solution, shared build settings, Docker Compose (PostgreSQL 16), Program.cs,
      ProblemDetails, security headers, `/health`, Swagger, CORS, test projects, CI
- [x] PR feat/auth: Identity + JWT; register, login, logout, me; lockout 3 tries / 15 min; rate limiting
- [x] Tests T01–T04 automated; unauthenticated call → 401
- [x] Ostap: ran it locally, tried register/login/lockout in Swagger
- [ ] Ostap can explain what a JWT is and why lockout uses Identity

## Week 2 · 23–29 Nov · Profile, BMI, exercise library, frontend shell (WF03, WF04, WF10)

- [ ] PR feat/profile: UserProfile, BMI calculator; T05, T06, T16, T17
- [ ] PR feat/exercises: ~60 Polish exercises seeded; search, filters, paging; custom exercises; admin; T07, T08
- [ ] PR feat/web-shell: React app, login/register, layout, profile page, exercise library; frontend CI
- [x] UI direction (colours, font, spacing) in DECISIONS.md
- [ ] Ostap: checked the Polish exercise texts; tried it on a phone

## Week 3 · 30 Nov–6 Dec · Training plans (WF05)

- [ ] PR feat/plans-api: TrainingPlan, PlanExercise, PlanStateMachine (Draft → Active → Archived); T09, T10
- [ ] PR feat/plans-web: plan list and editor with exercise picker
- [ ] Three template plans in the seed
- [ ] Ostap: built his real plan in the app

## Week 4 · 7–13 Dec · Workout logging (WF06)

- [ ] PR feat/sessions-api: WorkoutSession, SessionExercise, SetEntry, SessionStateMachine; T11, T12
- [ ] PR feat/sessions-web: log a past workout + live workout mode for the phone
- [ ] Ostap: logged at least 2 real workouts; annoyances → BACKLOG.md

## Week 5 · 14–20 Dec · Progress, measurements, goals, dashboard (WF07, WF08)

- [ ] PR feat/body-measurements
- [ ] PR feat/progress: StrengthCalculator (Epley 1RM, volume, PRs), progress endpoints; T13
- [ ] PR feat/goals; T14
- [ ] PR feat/progress-web: dashboard and progress page with charts
- [ ] Ostap: added weight entries and two real goals; checked the numbers

## Week 6 · 21–27 Dec · Reminders, export, buffer (WF09, WNF07)

- [ ] Slipped items from weeks 1–5 cleared first
- [ ] PR feat/reminders-api: Notification, ReminderWorker; T15
- [ ] PR feat/reminders-web: schedule a workout, notification bell, browser notifications
- [ ] PR feat/export: JSON and CSV export; account deletion
- [ ] Ostap: scheduled a workout with a reminder; opened the CSV in Excel

## Week 7 · 28 Dec–3 Jan · Deploy, test, freeze (WNF01–WNF06)

- [ ] Hosting agreed with Ostap; accounts created by Ostap
- [ ] PR chore/deploy: Dockerfile, production config, migrations as a deploy step, docs/DEPLOY.md
- [ ] PR test/e2e: Playwright flows
- [ ] docs/TESTS.md: result for every thesis test case T01–T24
- [ ] Quality pass: Lighthouse, axe, keyboard-only, phones, load test
- [ ] Security pass: blueprint Part 8, vulnerable packages, no secrets in history
- [ ] `v1.0.0` tagged on 3 Jan
- [ ] Ostap: updated thesis 4.1–4.2, class diagram, ERD, chapter 7

## Week 8 · 4–10 Jan · Demo, docs, presentation

- [ ] Demo account with 8 weeks of realistic data
- [ ] Final README with screenshots, live links and architecture diagram; ERD image
- [ ] Slide deck (Polish, 10–12 slides) and 5-minute demo script
- [ ] 20 likely defense questions with answers
- [ ] Ostap: backup demo video recorded; presentation rehearsed twice
