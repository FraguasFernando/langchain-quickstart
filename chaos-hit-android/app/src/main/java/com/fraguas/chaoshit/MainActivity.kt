package com.fraguas.chaoshit

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.getValue
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.fraguas.chaoshit.core.Coach
import com.fraguas.chaoshit.core.Plan
import com.fraguas.chaoshit.core.SessionLog
import com.fraguas.chaoshit.core.SessionType
import com.fraguas.chaoshit.core.SetLog
import java.time.LocalDate

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        val store = LogStore(this)
        setContent {
            MaterialTheme(colorScheme = darkColorScheme()) {
                Surface(Modifier.fillMaxSize()) { App(store) }
            }
        }
    }
}

@Composable
private fun App(store: LogStore) {
    var history by remember { mutableStateOf(store.load()) }
    var plan by remember { mutableStateOf<Plan?>(null) }
    val today = LocalDate.now().toEpochDay()

    Column(Modifier.safeDrawingPadding().padding(16.dp)) {
        val current = plan
        if (current == null) {
            Home(history, today) { type -> plan = Coach.plan(type, history, System.nanoTime()) }
        } else {
            BackHandler { plan = null }
            Workout(
                plan = current,
                onReroll = { plan = Coach.plan(current.type, history, System.nanoTime()) },
                onCancel = { plan = null },
                onFinish = { sets ->
                    store.append(SessionLog(current.type, today, sets))
                    history = store.load()
                    plan = null
                },
            )
        }
    }
}

@Composable
private fun ColumnScope.Home(history: List<SessionLog>, today: Long, onStart: (SessionType) -> Unit) {
    Text("Chaos HIT", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
    Text("One set. To failure. Then go home and grow.", style = MaterialTheme.typography.bodyMedium)
    Spacer(Modifier.height(16.dp))

    val (title, detail) = when (val r = Coach.recovery(history, today)) {
        Coach.Recovery.Ready -> "Recovered" to "Train today."
        is Coach.Recovery.TooSoon -> "Still recovering" to
            "Last session ${r.daysSince} day(s) ago. Wait ${r.daysLeft} more day(s) — growth happens at rest."
        is Coach.Recovery.Stalled -> "Stalled ${r.extraRestDays} sessions" to
            "No progress = under-recovered. Rest longer, sleep more, eat more. Don't add volume."
    }
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp)) {
            Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
            Text(detail, style = MaterialTheme.typography.bodyMedium)
        }
    }
    Spacer(Modifier.height(24.dp))
    Text("Choose session", style = MaterialTheme.typography.titleMedium)
    SessionType.entries.forEach { type ->
        Button(onClick = { onStart(type) }, modifier = Modifier.fillMaxWidth().padding(top = 8.dp)) {
            Text(type.label)
        }
    }
    Spacer(Modifier.height(24.dp))
    Text("Sessions logged: ${history.size}", style = MaterialTheme.typography.bodySmall)
}

@Composable
private fun ColumnScope.Workout(
    plan: Plan,
    onReroll: () -> Unit,
    onCancel: () -> Unit,
    onFinish: (List<SetLog>) -> Unit,
) {
    val loads = remember(plan) { mutableStateListOf(*plan.exercises.map { p -> (p.suggestedLoadKg ?: p.lastSet?.loadKg)?.toString() ?: "" }.toTypedArray()) }
    val reps = remember(plan) { mutableStateListOf(*Array(plan.exercises.size) { "" }) }

    Text(plan.type.label, style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
    Text("Warm up, then ONE all-out set each. ${Coach.CADENCE}.", style = MaterialTheme.typography.bodySmall)
    LazyColumn(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(8.dp)) {
        itemsIndexed(plan.exercises) { i, p ->
            Card(Modifier.fillMaxWidth().padding(top = 8.dp)) {
                Column(Modifier.padding(12.dp)) {
                    if (p.supersetWithPrevious) Text("↳ NO REST — straight from previous", style = MaterialTheme.typography.labelSmall)
                    Text(p.slotLabel, style = MaterialTheme.typography.labelMedium)
                    Text(p.exercise.name, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                    Text("Target ${p.exercise.repRange.first}–${p.exercise.repRange.last} reps to failure", style = MaterialTheme.typography.bodySmall)
                    val last = p.lastSet
                    Text(
                        if (last == null) "First time — find a load that fails in range."
                        else "Last: ${last.loadKg} kg × ${last.reps} → today ${p.suggestedLoadKg} kg",
                        style = MaterialTheme.typography.bodySmall,
                    )
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        OutlinedTextField(
                            value = loads[i], onValueChange = { loads[i] = it }, label = { Text("kg") },
                            singleLine = true, modifier = Modifier.weight(1f),
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        )
                        OutlinedTextField(
                            value = reps[i], onValueChange = { reps[i] = it }, label = { Text("reps") },
                            singleLine = true, modifier = Modifier.weight(1f),
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                        )
                    }
                }
            }
        }
    }
    Row(Modifier.fillMaxWidth().padding(top = 8.dp), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
        OutlinedButton(onClick = onCancel, modifier = Modifier.weight(1f)) { Text("Cancel") }
        OutlinedButton(onClick = onReroll, modifier = Modifier.weight(1f)) { Text("Re-roll") }
        Button(
            onClick = {
                val sets = plan.exercises.indices.mapNotNull { i ->
                    val kg = loads[i].replace(',', '.').toDoubleOrNull()
                    val r = reps[i].toIntOrNull()
                    if (kg == null || r == null) null else SetLog(plan.exercises[i].exercise.id, kg, r)
                }
                if (sets.isEmpty()) onCancel() else onFinish(sets)
            },
            modifier = Modifier.weight(1f),
        ) { Text("Finish") }
    }
}
