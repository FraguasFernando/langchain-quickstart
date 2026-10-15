package com.fraguas.chaoshit

import android.content.Context
import com.fraguas.chaoshit.core.LogCodec
import com.fraguas.chaoshit.core.SessionLog

class LogStore(context: Context) {
    private val prefs = context.getSharedPreferences("chaos_hit", Context.MODE_PRIVATE)

    fun load(): List<SessionLog> = LogCodec.decode(prefs.getString(KEY, "").orEmpty())

    fun append(log: SessionLog) {
        prefs.edit().putString(KEY, LogCodec.encode(load() + log)).apply()
    }

    private companion object { const val KEY = "sessions" }
}
