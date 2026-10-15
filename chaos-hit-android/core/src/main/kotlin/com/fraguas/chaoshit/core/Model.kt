package com.fraguas.chaoshit.core

enum class SessionType(val label: String) {
    UPPER_A("Upper A"),
    UPPER_B("Upper B"),
    LOWER("Lower"),
}

enum class Kind { ISOLATION, COMPOUND }

data class Exercise(
    val id: String,
    val name: String,
    val kind: Kind,
    /** Mentzer rep target for the single working set. */
    val repRange: IntRange,
)

/**
 * One position in a session. [pool] holds interchangeable variations of the same movement pattern;
 * the chaos picker chooses one. A slot with [preExhaustWith] set is performed immediately after
 * that slot with zero rest (Mentzer pre-exhaust).
 */
data class Slot(
    val id: String,
    val label: String,
    val pool: List<Exercise>,
    val preExhaustWith: String? = null,
)

data class PlannedExercise(
    val slotId: String,
    val slotLabel: String,
    val exercise: Exercise,
    val supersetWithPrevious: Boolean,
    val lastSet: SetLog?,
    val suggestedLoadKg: Double?,
)

data class Plan(
    val type: SessionType,
    val seed: Long,
    val exercises: List<PlannedExercise>,
)

data class SetLog(val exerciseId: String, val loadKg: Double, val reps: Int)

data class SessionLog(
    val type: SessionType,
    val epochDay: Long,
    val sets: List<SetLog>,
)
