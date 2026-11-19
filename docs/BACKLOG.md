# Backlog

Ideas that are deferred, not rejected. Each one gets the challenge questions from
[BLUEPRINT.md](BLUEPRINT.md) Part 3.3 before it is built.

## After the MVP

- Web Push with a service worker, so reminders arrive with the tab closed (stretch goal in week 6).
- Server-side token revocation and refresh tokens (logout is client-side for now, see D006).
- E-mail confirmation and password reset (needs an e-mail provider).
- kg / lb display preference.
- Exercise search that understands Polish word endings ("ławka" also finding "na ławce"). Today search
  ignores case and accents but matches text literally; PostgreSQL has no built-in Polish stemmer.
- Token in an httpOnly cookie instead of localStorage (see D019).
- Dark mode.
- Offline logging for gyms without signal.

## Future work named in the thesis (chapter 8)

- Diet and calorie tracking module.
- GPS tracking of outdoor activities.
- Integration with fitness bands and sports watches.
- Social features: rivalry and sharing between users.
- Recommendations: automatic choice of a plan for the user's goal.

## Annoyances from real use

Filled in from Ostap's notes, starting in week 4.
