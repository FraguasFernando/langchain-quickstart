# Chaos HIT

Android app (Kotlin + Jetpack Compose). Pick Upper A, Upper B or Lower; app rolls the exercises.

- **Chaos**: logistic map (r = 3.99) picks one variation per movement slot. Avoids last session's picks.
- **Mentzer HIT**: ~5 working sets/session, one set to failure, 4-2-4 cadence, pre-exhaust pairs
  (isolation → compound, no rest), min 4 rest days, +5% load when top of rep range hit,
  extra rest when progress stalls.
- **Batman (Affleck/Norton) bias**: heavy compounds, traps, thick back, delts.

| Session | Slots |
|---|---|
| Upper A | chest iso → chest press, row, traps, triceps |
| Upper B | pullover → vertical pull, lateral → overhead press, biceps |
| Lower | leg ext → squat pattern, hinge, calves, abs |

## Build
Open in Android Studio, run `app`. Logic tests: `./gradlew :core:test`.
