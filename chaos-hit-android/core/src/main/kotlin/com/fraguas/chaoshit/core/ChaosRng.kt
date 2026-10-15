package com.fraguas.chaoshit.core

/**
 * Logistic map x' = r·x·(1−x) at r = 3.99, deep in the chaotic regime: deterministic, yet
 * tiny seed differences diverge fast. Same seed → same workout, so a plan can be reproduced.
 */
class ChaosRng(seed: Long) {
    private var x: Double

    init {
        // Map the seed into (0,1), avoiding the fixed points 0, 0.5 and 1.
        val s = ((seed xor (seed ushr 33)) and 0xFFFFFFFL).toDouble() / 0x10000000L.toDouble()
        x = 0.1 + 0.8 * s
        repeat(64) { step() } // burn-in: lose memory of the seed's structure
    }

    private fun step(): Double {
        x = R * x * (1 - x)
        if (x <= 0.0 || x >= 1.0) x = 0.123456789 // guard against float collapse
        return x
    }

    fun nextIndex(bound: Int): Int {
        require(bound > 0)
        return (step() * bound).toInt().coerceIn(0, bound - 1)
    }

    private companion object { const val R = 3.99 }
}
