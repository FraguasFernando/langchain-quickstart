package com.fraguas.chaoshit.core

import kotlin.math.round

/** Mentzer Heavy Duty rules applied to plan generation and recovery. */
object Coach {
    /** Heavy Duty II: 4–7 days between workouts; more as strength rises. */
    const val MIN_REST_DAYS = 4
    const val CADENCE = "4s up · 2s squeeze · 4s down"
    const val LOAD_STEP = 0.05

    sealed interface Recovery {
        data object Ready : Recovery
        data class TooSoon(val daysSince: Long, val daysLeft: Long) : Recovery
        /** Last two sessions showed no progress — Mentzer reads this as under-recovery. */
        data class Stalled(val extraRestDays: Int) : Recovery
    }

    fun plan(type: SessionType, history: List<SessionLog>, seed: Long): Plan {
        val rng = ChaosRng(seed)
        val lastSame = history.filter { it.type == type }.maxByOrNull { it.epochDay }
        val usedLastTime = lastSame?.sets?.map { it.exerciseId }?.toSet().orEmpty()

        val picks = Catalog.sessions.getValue(type).map { slot ->
            // Variation, not repetition: avoid last session's pick when a fresh option exists.
            val fresh = slot.pool.filter { it.id !in usedLastTime }.ifEmpty { slot.pool }
            val exercise = fresh[rng.nextIndex(fresh.size)]
            val last = lastSet(exercise.id, history)
            PlannedExercise(
                slotId = slot.id,
                slotLabel = slot.label,
                exercise = exercise,
                supersetWithPrevious = slot.preExhaustWith != null,
                lastSet = last,
                suggestedLoadKg = last?.let { suggestLoad(it, exercise.repRange) },
            )
        }
        return Plan(type, seed, picks)
    }

    fun lastSet(exerciseId: String, history: List<SessionLog>): SetLog? =
        history.sortedByDescending { it.epochDay }
            .firstNotNullOfOrNull { s -> s.sets.firstOrNull { it.exerciseId == exerciseId } }

    /** Hit top of range → add load. Inside range → same load, beat the reps. Below → drop load. */
    fun suggestLoad(last: SetLog, range: IntRange): Double = when {
        last.reps >= range.last -> roundToPlate(last.loadKg * (1 + LOAD_STEP))
        last.reps < range.first -> roundToPlate(last.loadKg * (1 - LOAD_STEP))
        else -> last.loadKg
    }

    private fun roundToPlate(kg: Double) = round(kg / 1.25) * 1.25

    fun recovery(history: List<SessionLog>, todayEpochDay: Long): Recovery {
        val sorted = history.sortedBy { it.epochDay }
        val last = sorted.lastOrNull() ?: return Recovery.Ready
        val since = todayEpochDay - last.epochDay
        val needed = MIN_REST_DAYS + stalledSessions(sorted).coerceAtMost(3)
        return when {
            since < needed -> Recovery.TooSoon(since, needed - since)
            stalledSessions(sorted) >= 2 -> Recovery.Stalled(stalledSessions(sorted))
            else -> Recovery.Ready
        }
    }

    /** Consecutive most-recent sessions where no repeated exercise improved load or reps. */
    private fun stalledSessions(sorted: List<SessionLog>): Int {
        var count = 0
        for (i in sorted.indices.reversed()) {
            val session = sorted[i]
            val before = sorted.subList(0, i)
            val comparisons = session.sets.mapNotNull { set -> lastSet(set.exerciseId, before)?.let { set to it } }
            if (comparisons.isEmpty()) break
            val improved = comparisons.any { (now, prev) ->
                now.loadKg > prev.loadKg || (now.loadKg == prev.loadKg && now.reps > prev.reps)
            }
            if (improved) break
            count++
        }
        return count
    }
}
