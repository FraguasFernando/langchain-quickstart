package com.fraguas.chaoshit.core

/**
 * Session templates. Volume follows Mentzer (≈5 working sets per session); exercise bias follows
 * the Affleck/Norton Batman build: heavy compounds, thick upper back, traps and delts for frame.
 */
object Catalog {
    private val UPPER = 6..10
    private val LOWER = 8..15

    private fun iso(id: String, name: String, reps: IntRange) = Exercise(id, name, Kind.ISOLATION, reps)
    private fun cmp(id: String, name: String, reps: IntRange) = Exercise(id, name, Kind.COMPOUND, reps)

    val sessions: Map<SessionType, List<Slot>> = mapOf(
        SessionType.UPPER_A to listOf(
            Slot("chest_iso", "Chest pre-exhaust", listOf(
                iso("pec_deck", "Pec deck", UPPER),
                iso("db_fly", "Dumbbell fly", UPPER),
                iso("cable_cross", "Cable crossover", UPPER),
            )),
            Slot("chest_cmp", "Chest compound", listOf(
                cmp("incline_bench", "Incline barbell press", UPPER),
                cmp("incline_db", "Incline dumbbell press", UPPER),
                cmp("dips", "Weighted dips", UPPER),
            ), preExhaustWith = "chest_iso"),
            Slot("row", "Horizontal pull", listOf(
                cmp("bb_row", "Barbell row", UPPER),
                cmp("tbar_row", "T-bar row", UPPER),
                cmp("seal_row", "Chest-supported row", UPPER),
            )),
            Slot("traps", "Traps (Bat-frame)", listOf(
                iso("bb_shrug", "Barbell shrug", UPPER),
                iso("db_shrug", "Dumbbell shrug", UPPER),
                cmp("rack_pull", "Rack pull", UPPER),
            )),
            Slot("triceps", "Triceps", listOf(
                iso("pushdown", "Cable pushdown", UPPER),
                iso("skull", "Skull crusher", UPPER),
            )),
        ),
        SessionType.UPPER_B to listOf(
            Slot("lat_iso", "Lat pre-exhaust", listOf(
                iso("nautilus_pullover", "Machine pullover", UPPER),
                iso("db_pullover", "Dumbbell pullover", UPPER),
                iso("straight_arm", "Straight-arm pulldown", UPPER),
            )),
            Slot("lat_cmp", "Vertical pull", listOf(
                cmp("pulldown", "Close-grip pulldown", UPPER),
                cmp("chin", "Weighted chin-up", UPPER),
                cmp("pullup", "Weighted pull-up", UPPER),
            ), preExhaustWith = "lat_iso"),
            Slot("delt_iso", "Delt pre-exhaust", listOf(
                iso("db_lateral", "Dumbbell lateral raise", UPPER),
                iso("cable_lateral", "Cable lateral raise", UPPER),
                iso("machine_lateral", "Machine lateral raise", UPPER),
            )),
            Slot("delt_cmp", "Overhead press", listOf(
                cmp("ohp", "Standing barbell press", UPPER),
                cmp("db_ohp", "Seated dumbbell press", UPPER),
                cmp("machine_ohp", "Machine shoulder press", UPPER),
            ), preExhaustWith = "delt_iso"),
            Slot("biceps", "Biceps", listOf(
                iso("bb_curl", "Barbell curl", UPPER),
                iso("incline_curl", "Incline dumbbell curl", UPPER),
                iso("hammer", "Hammer curl", UPPER),
            )),
        ),
        SessionType.LOWER to listOf(
            Slot("quad_iso", "Quad pre-exhaust", listOf(
                iso("leg_ext", "Leg extension", LOWER),
                iso("sissy", "Sissy squat", LOWER),
            )),
            Slot("quad_cmp", "Squat pattern", listOf(
                cmp("leg_press", "Leg press", LOWER),
                cmp("squat", "Back squat", LOWER),
                cmp("hack", "Hack squat", LOWER),
            ), preExhaustWith = "quad_iso"),
            Slot("hinge", "Posterior chain", listOf(
                cmp("deadlift", "Deadlift", 5..8),
                cmp("rdl", "Romanian deadlift", LOWER),
                iso("leg_curl", "Leg curl", LOWER),
            )),
            Slot("calf", "Calves", listOf(
                iso("standing_calf", "Standing calf raise", LOWER),
                iso("seated_calf", "Seated calf raise", LOWER),
            )),
            Slot("abs", "Abs", listOf(
                iso("hanging_raise", "Hanging leg raise", LOWER),
                iso("cable_crunch", "Cable crunch", LOWER),
                iso("ab_wheel", "Ab wheel rollout", LOWER),
            )),
        ),
    )
}
