# Product: Fitness Logger

## Problem

People who want to train regularly lack one free tool that combines a training plan, an exercise
library, a workout log, progress tracking and health indicators (body weight, BMI). The popular apps
each cover only part of it (Nike Training Club: plans and library; MyFitnessPal: diet; Strava: GPS cardio),
and all of them are mobile apps that must be installed. This app runs in any browser on a phone, tablet
or computer.

Source: diploma konspekt and thesis chapters 1–2 (WSIiZ Rzeszów, 2026).

## First user

**Ostap**, the developer: trains at the gym and logs real workouts from week 4. If the app is not good
enough for his own training, it is not done.

## MVP (thesis requirements WF01–WF10)

| ID | Feature | Priority (thesis) |
|---|---|---|
| WF01 | Registration with e-mail and password | High |
| WF02 | Login/logout; account locked for 15 min after 3 failed attempts | High |
| WF03 | Profile: age, sex, body weight, height, training goal | High |
| WF04 | Exercise library: technique, muscle groups, difficulty; search and filters | High |
| WF05 | Own training plans: exercises from the library with sets and reps | High |
| WF06 | Logging workout sessions: date, duration, exercises, sets, reps, weight | High |
| WF07 | Progress: charts of training history and health indicators | High |
| WF08 | Fitness goals and their progress | Medium |
| WF09 | Reminders for planned sessions | Medium |
| WF10 | Automatic BMI from profile data | Low |

Non-functional requirements WNF01–WNF07 (performance, security, availability, WCAG 2.1, phones,
scalability, data export) are in thesis section 2.5 and are covered in [PLAN.md](PLAN.md).

## Users and roles

- **Gość** (guest): can register and log in.
- **Użytkownik** (user): owns their own profile, plans, sessions, measurements and goals. Nobody else
  can see them.
- **Administrator**: manages the global exercise library.

## Out of scope

Diet and calorie tracking, GPS tracking, social features (friends, sharing, rankings), native mobile
apps, wearable integrations, AI coaching, payments, multiple gyms or trainers. Ideas go to
[BACKLOG.md](BACKLOG.md); several are named as future work in the thesis summary.

## Constraints

- Deadline: feature freeze on 3 Jan 2027, presentation from the week of 11 Jan 2027.
- One developer plus an AI agent; Ostap reviews and merges every pull request.
- Free hosting only (see [DECISIONS.md](DECISIONS.md)).
- Language: English everywhere (UI, code, docs); see D021. The thesis text stays in Polish.
