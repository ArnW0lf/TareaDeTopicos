package com.example.inscripcionapp.ui.dashboard

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.ExpandLess
import androidx.compose.material.icons.filled.ExpandMore
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import com.example.inscripcionapp.data.network.MateriaDisponible
import com.example.inscripcionapp.data.network.Grupo
import androidx.compose.material.icons.Icons

@Composable
fun DashboardScreen(
    uiState: DashboardUiState,
    snackbarHostState: SnackbarHostState,
    onVerGruposClicked: (MateriaDisponible) -> Unit,
    onDismissVerGrupos: () -> Unit,
    onGrupoSeleccionado: (Grupo) -> Unit,
    onQuitarDeSeleccion: (GrupoSeleccionado) -> Unit,
    onConfirmarInscripcion: () -> Unit,
    onClearSeleccionError: () -> Unit,
    onCancelarInscripcion: (Int) -> Unit // ✨ Añadimos el nuevo callback
) {
    // Muestra un Snackbar cuando hay un error en la selección (choque de horario, etc.)
    LaunchedEffect(uiState.seleccionError) {
        if (uiState.seleccionError != null) {
            snackbarHostState.showSnackbar(
                message = uiState.seleccionError,
                duration = SnackbarDuration.Short
            )
            onClearSeleccionError()
        }
    }

    if (uiState.isLoading) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
    } else if (uiState.error != null) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text(text = uiState.error, color = MaterialTheme.colorScheme.error)
        }
    } else {
        // Usamos una sola LazyColumn para toda la pantalla para un scroll unificado.
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = PaddingValues(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            // --- Sección "Mi Selección" ---
            item {
                Card(modifier = Modifier.fillMaxWidth()) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text("Mi Selección", style = MaterialTheme.typography.headlineSmall)
                        Spacer(modifier = Modifier.height(16.dp))
    
                        if (uiState.seleccion.isEmpty()) {
                            Text("Aún no has seleccionado materias.", style = MaterialTheme.typography.bodyMedium)
                        } else {
                            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                                uiState.seleccion.forEach { item ->
                                    SeleccionItem(item = item, onQuitar = { onQuitarDeSeleccion(item) })
                                }
                            }
                        }
    
                        Spacer(modifier = Modifier.height(24.dp))
    
                        Button(
                            onClick = onConfirmarInscripcion,
                            modifier = Modifier.fillMaxWidth(),
                            enabled = uiState.seleccion.isNotEmpty() && !uiState.isConfirming
                        ) {
                            if (uiState.isConfirming) {
                                CircularProgressIndicator(modifier = Modifier.size(24.dp), color = MaterialTheme.colorScheme.onPrimary)
                            } else {
                                Text("Confirmar Inscripción")
                            }
                        }
                    }
                }
            }

            // --- Sección "Materias Disponibles" ---
            item {
                Text(
                    "Materias Disponibles",
                    style = MaterialTheme.typography.headlineSmall,
                    modifier = Modifier.padding(top = 8.dp) // Espacio extra arriba
                )
            }
            items(uiState.materias) { materia ->
                MateriaItem(
                    materia = materia,
                    onVerGruposClicked = { onVerGruposClicked(materia) }
                )
            }

            // --- Sección "Mis Inscripciones" ---
            if (uiState.inscripciones.isNotEmpty()) {
                item {
                    Text(
                        "Mis Inscripciones",
                        style = MaterialTheme.typography.headlineSmall,
                        modifier = Modifier.padding(top = 8.dp)
                    )
                }
                items(uiState.inscripciones) { inscripcion ->
                    InscripcionItem(
                        inscripcion = inscripcion,
                        onCancelar = { onCancelarInscripcion(inscripcion.id) } // ✨ Pasamos la función
                    )
                }
            } else if (!uiState.isLoading) {
                item {
                    Card(modifier = Modifier.fillMaxWidth()) {
                        Text("No tienes inscripciones registradas.", modifier = Modifier.padding(16.dp))
                    }
                }
            }
        }
    }
    // --- Modal para ver grupos ---
    if (uiState.materiaSeleccionadaParaVerGrupos != null) {
        GruposDialog(
            uiState = uiState,
            onDismiss = onDismissVerGrupos,
            onGrupoSeleccionado = onGrupoSeleccionado
        )
    }
}

@Composable
fun MateriaItem(materia: MateriaDisponible, onVerGruposClicked: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(text = materia.nombre, fontWeight = FontWeight.Bold)
                Text(
                    text = "${materia.codigo} - ${materia.creditos} créditos - Semestre ${materia.semestre}",
                    style = MaterialTheme.typography.bodySmall
                )
            }
            Spacer(modifier = Modifier.width(16.dp))
            Button(onClick = onVerGruposClicked) {
                Text("Ver Grupos")
            }
        }
    }
}

@Composable
fun InscripcionItem(
    inscripcion: com.example.inscripcionapp.data.network.InscripcionEstadoResponse,
    onCancelar: () -> Unit
) {
    var isExpanded by remember { mutableStateOf(false) }

    Card(modifier = Modifier.fillMaxWidth()) {
        Column {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Column(modifier = Modifier.weight(1f)) {
                    Text("Inscripción del ${inscripcion.fecha.substringBefore('T')}", style = MaterialTheme.typography.titleMedium)
                    StatusBadge(estado = inscripcion.estado)
                }
                IconButton(onClick = { isExpanded = !isExpanded }) {
                    Icon(
                        imageVector = if (isExpanded) Icons.Default.ExpandLess else Icons.Default.ExpandMore,
                        contentDescription = "Expandir"
                    )
                }
            }

            if (isExpanded) {
                Divider(modifier = Modifier.padding(horizontal = 16.dp))
                Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    inscripcion.materias.forEach { materia ->
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text("${materia.nombre} (G: ${materia.grupo})")
                            StatusBadge(estado = materia.estado)
                        }
                    }
                }

                // ✨ CORRECCIÓN: Mostrar el botón si la inscripción contiene materias ya inscritas.
                // El backend cancela el bloque completo, por lo que solo se necesita un botón.
                val puedeCancelar = inscripcion.materias.any { it.estado.equals("INSCRITO", true) }

                if (puedeCancelar) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(start = 16.dp, end = 16.dp, bottom = 16.dp, top = 8.dp),
                        horizontalArrangement = Arrangement.End
                    ) {
                        Button(onClick = onCancelar, colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)) {
                            Text("Cancelar Inscripción")
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun StatusBadge(estado: String) {
    val (color, textColor) = when (estado.uppercase()) {
        "VALIDO", "COMPLETADA" -> MaterialTheme.colorScheme.primaryContainer to MaterialTheme.colorScheme.onPrimaryContainer
        "ERROR", "CON_ERRORES", "RECHAZADA" -> MaterialTheme.colorScheme.errorContainer to MaterialTheme.colorScheme.onErrorContainer
        "PENDIENTE" -> MaterialTheme.colorScheme.tertiaryContainer to MaterialTheme.colorScheme.onTertiaryContainer
        else -> MaterialTheme.colorScheme.secondaryContainer to MaterialTheme.colorScheme.onSecondaryContainer
    }

    Card(
        shape = RoundedCornerShape(50), // Forma de píldora
        colors = CardDefaults.cardColors(containerColor = color)
    ) {
        Text(
            text = estado,
            color = textColor,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
        )
    }
}

@Composable
fun GruposDialog(
    uiState: DashboardUiState,
    onDismiss: () -> Unit,
    onGrupoSeleccionado: (Grupo) -> Unit
) {
    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .height(500.dp), // Altura fija para el diálogo
            shape = RoundedCornerShape(16.dp),
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = "Grupos para ${uiState.materiaSeleccionadaParaVerGrupos?.nombre}",
                    style = MaterialTheme.typography.titleLarge,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                if (uiState.isLoadingGrupos) {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                } else if (uiState.errorGrupos != null) {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Text(text = uiState.errorGrupos, color = MaterialTheme.colorScheme.error)
                    }
                } else if (uiState.gruposDisponibles.isEmpty()) {
                    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Text("No hay grupos disponibles para esta materia.")
                    }
                } else {
                    LazyColumn(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                        items(uiState.gruposDisponibles) { grupo: Grupo ->
                            GrupoItem(grupo = grupo, onAddClicked = { onGrupoSeleccionado(grupo) })
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun GrupoItem(grupo: Grupo, onAddClicked: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
    ) {
        Column(modifier = Modifier.padding(12.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text("Grupo ${grupo.grupo}", fontWeight = FontWeight.Bold)
                Button(onClick = onAddClicked, enabled = grupo.cupo > 0) {
                    Text("Añadir")
                }
            }
            Spacer(modifier = Modifier.height(4.dp))
            Text("Docente: ${grupo.docente}", style = MaterialTheme.typography.bodyMedium)
            Text("Horario: ${grupo.horario}", style = MaterialTheme.typography.bodyMedium)
            Text("Cupos disponibles: ${grupo.cupo}", style = MaterialTheme.typography.bodyMedium)
        }
    }
}

@Composable
fun SeleccionItem(item: GrupoSeleccionado, onQuitar: () -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(text = item.materiaNombre, fontWeight = FontWeight.SemiBold)
            Text(
                text = "Grupo ${item.grupo.grupo} - ${item.grupo.horario}",
                style = MaterialTheme.typography.bodySmall
            )
        }
        IconButton(onClick = onQuitar) {
            Icon(
                imageVector = Icons.Default.Delete,
                contentDescription = "Quitar materia",
                tint = MaterialTheme.colorScheme.error
            )
        }
    }
}