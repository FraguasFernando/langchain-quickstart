package com.fraguas.chaoshit.core

/** Plain-text storage: one session per line, `TYPE;epochDay;id:kg:reps,id:kg:reps`. */
object LogCodec {
    fun encode(logs: List<SessionLog>): String = logs.joinToString("\n") { s ->
        "${s.type.name};${s.epochDay};" + s.sets.joinToString(",") { "${it.exerciseId}:${it.loadKg}:${it.reps}" }
    }

    fun decode(text: String): List<SessionLog> = text.lineSequence().filter { it.isNotBlank() }.mapNotNull { line ->
        val parts = line.split(";")
        if (parts.size != 3) return@mapNotNull null
        val type = SessionType.entries.firstOrNull { it.name == parts[0] } ?: return@mapNotNull null
        val day = parts[1].toLongOrNull() ?: return@mapNotNull null
        val sets = parts[2].split(",").filter { it.isNotBlank() }.mapNotNull { s ->
            val f = s.split(":")
            if (f.size != 3) null else SetLog(f[0], f[1].toDoubleOrNull() ?: return@mapNotNull null, f[2].toIntOrNull() ?: return@mapNotNull null)
        }
        SessionLog(type, day, sets)
    }.toList()
}
