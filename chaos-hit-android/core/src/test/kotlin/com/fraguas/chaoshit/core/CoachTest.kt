package com.fraguas.chaoshit.core

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs
import kotlin.test.assertNotEquals
import kotlin.test.assertTrue

class CoachTest {
    @Test fun sameSeedSamePlan() {
        val a = Coach.plan(SessionType.UPPER_A, emptyList(), 42)
        val b = Coach.plan(SessionType.UPPER_A, emptyList(), 42)
        assertEquals(a.exercises.map { it.exercise.id }, b.exercises.map { it.exercise.id })
    }

    @Test fun seedsProduceVariety() {
        val combos = (1L..200L).map { seed -> Coach.plan(SessionType.UPPER_B, emptyList(), seed).exercises.map { it.exercise.id } }.toSet()
        assertTrue(combos.size > 20, "only ${combos.size} distinct plans")
    }

    @Test fun avoidsLastSessionPicks() {
        repeat(50) { seed ->
            val first = Coach.plan(SessionType.LOWER, emptyList(), seed.toLong())
            val log = SessionLog(SessionType.LOWER, 0, first.exercises.map { SetLog(it.exercise.id, 50.0, 10) })
            val next = Coach.plan(SessionType.LOWER, listOf(log), seed + 1000L)
            first.exercises.zip(next.exercises).forEach { (p, n) -> assertNotEquals(p.exercise.id, n.exercise.id) }
        }
    }

    @Test fun preExhaustPairsFlagged() {
        val plan = Coach.plan(SessionType.UPPER_B, emptyList(), 7)
        assertEquals(listOf(false, true, false, true, false), plan.exercises.map { it.supersetWithPrevious })
    }

    @Test fun overloadRules() {
        assertEquals(105.0, Coach.suggestLoad(SetLog("x", 100.0, 10), 6..10))
        assertEquals(100.0, Coach.suggestLoad(SetLog("x", 100.0, 8), 6..10))
        assertEquals(95.0, Coach.suggestLoad(SetLog("x", 100.0, 4), 6..10))
    }

    @Test fun recoveryEnforcesRestAndStall() {
        val s = { day: Long, reps: Int -> SessionLog(SessionType.UPPER_A, day, listOf(SetLog("bb_row", 100.0, reps))) }
        assertIs<Coach.Recovery.Ready>(Coach.recovery(emptyList(), 0))
        assertIs<Coach.Recovery.TooSoon>(Coach.recovery(listOf(s(0, 8)), 2))
        assertIs<Coach.Recovery.Ready>(Coach.recovery(listOf(s(0, 8)), 4))
        val stalled = listOf(s(0, 8), s(5, 8), s(10, 7))
        assertIs<Coach.Recovery.TooSoon>(Coach.recovery(stalled, 15)) // needs 4 + 2 days
        assertIs<Coach.Recovery.Stalled>(Coach.recovery(stalled, 16))
    }

    @Test fun codecRoundTrip() {
        val logs = listOf(SessionLog(SessionType.LOWER, 20000, listOf(SetLog("squat", 102.5, 12), SetLog("rdl", 80.0, 9))))
        assertEquals(logs, LogCodec.decode(LogCodec.encode(logs)))
    }
}
