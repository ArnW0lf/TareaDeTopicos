package com.example.inscripcionapp.ui.dashboard

import android.app.Application
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.example.inscripcionapp.data.UserPreferencesRepository
import com.example.inscripcionapp.data.network.ApiClient
import com.example.inscripcionapp.data.network.MateriaDisponible
import com.example.inscripcionapp.data.network.Grupo
import com.example.inscripcionapp.data.network.InscripcionEstadoResponse
import com.example.inscripcionapp.data.network.InscripcionRequest
import com.example.inscripcionapp.data.network.MateriaInscripcion
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class DashboardUiState(
    val materias: List<MateriaDisponible> = emptyList(),
    val isLoading: Boolean = true,
    val error: String? = null,
    // Estado para el modal de grupos
    val materiaSeleccionadaParaVerGrupos: MateriaDisponible? = null,
    val gruposDisponibles: List<Grupo> = emptyList(),
    val isLoadingGrupos: Boolean = false,
    val errorGrupos: String? = null,
    // Estado para el "carrito de compras"
    val seleccion: List<GrupoSeleccionado> = emptyList(),
    val seleccionError: String? = null, // Para mostrar errores como choques de horario
    // Estado para la confirmación y el historial
    val isConfirming: Boolean = false,
    val inscripciones: List<InscripcionEstadoResponse> = emptyList(),
    val inscripcionesError: String? = null
)

class DashboardViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(DashboardUiState())
    val uiState = _uiState.asStateFlow()

    private val userPreferencesRepository = UserPreferencesRepository(application)

    private var pollingJob: Job? = null

    init {
        loadMateriasDisponibles()
        loadEstadoInscripciones()
    }

    private fun loadMateriasDisponibles() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }
            try {
                // Obtenemos el registro guardado
                val registro = userPreferencesRepository.userRegistro.first()
                if (registro != null) {
                    val response = ApiClient.instance.getMateriasDisponibles(registro)
                    if (response.isSuccessful) {
                        _uiState.update { it.copy(materias = response.body() ?: emptyList(), isLoading = false) }
                    } else {
                        _uiState.update { it.copy(error = "Error al cargar materias.", isLoading = false) }
                    }
                }
            } catch (e: Exception) {
                Log.e("DashboardViewModel", "Error de red", e)
                _uiState.update { it.copy(error = "Error de conexión.", isLoading = false) }
            }
        }
    }

    fun onVerGruposClicked(materia: MateriaDisponible) {
        _uiState.update { it.copy(materiaSeleccionadaParaVerGrupos = materia, isLoadingGrupos = true, errorGrupos = null) }
        viewModelScope.launch {
            try {
                val response = ApiClient.instance.getGruposPorMateria(materia.codigo)
                if (response.isSuccessful) {
                    _uiState.update {
                        // Extraemos la lista de la propiedad "grupos" del objeto de respuesta
                        it.copy(gruposDisponibles = response.body()?.grupos ?: emptyList(), isLoadingGrupos = false)
                    }
                } else {
                    _uiState.update { it.copy(errorGrupos = "Error al cargar grupos.", isLoadingGrupos = false) }
                }
            } catch (e: Exception) {
                Log.e("DashboardViewModel", "Error de red al cargar grupos", e)
                _uiState.update { it.copy(errorGrupos = "Error de conexión.", isLoadingGrupos = false) }
            }
        }
    }

    fun onDismissVerGrupos() {
        _uiState.update { it.copy(materiaSeleccionadaParaVerGrupos = null, gruposDisponibles = emptyList()) }
    }

    fun onGrupoSeleccionado(grupo: Grupo) {
        val currentState = _uiState.value
        val materia = currentState.materiaSeleccionadaParaVerGrupos ?: return

        // 1. Validar si la materia ya está en la selección
        if (currentState.seleccion.any { it.materiaCodigo == materia.codigo }) {
            _uiState.update { it.copy(
                seleccionError = "Ya has seleccionado esta materia.",
                materiaSeleccionadaParaVerGrupos = null, // Cierra el diálogo
                gruposDisponibles = emptyList()
            ) }
            return
        }

        // 2. Validar cupo
        if (grupo.cupo <= 0) {
            _uiState.update { it.copy(
                seleccionError = "El grupo ${grupo.grupo} no tiene cupos.",
                materiaSeleccionadaParaVerGrupos = null, // Cierra el diálogo
                gruposDisponibles = emptyList()
            ) }
            return
        }

        // 3. Validar choque de horario
        val choque = hayChoqueHorario(grupo, currentState.seleccion)
        if (choque != null) {
            _uiState.update { it.copy(
                seleccionError = choque,
                materiaSeleccionadaParaVerGrupos = null, // Cierra el diálogo
                gruposDisponibles = emptyList()
            ) }
            return
        }

        // Si todas las validaciones pasan, se añade a la selección
        val nuevaSeleccion = GrupoSeleccionado(
            materiaCodigo = materia.codigo,
            materiaNombre = materia.nombre,
            grupo = grupo
        )

        _uiState.update {
            it.copy(
                seleccion = it.seleccion + nuevaSeleccion,
                materiaSeleccionadaParaVerGrupos = null, // Cierra el diálogo
                gruposDisponibles = emptyList()
            )
        }
    }

    fun onQuitarDeSeleccion(item: GrupoSeleccionado) {
        _uiState.update { it.copy(seleccion = it.seleccion - item) }
    }

    fun clearSeleccionError() {
        _uiState.update { it.copy(seleccionError = null) }
    }

    private fun parseHorario(horarioStr: String): Triple<String, Int, Int>? {
        val parts = horarioStr.split(" ")
        if (parts.size < 4) return null
        return try {
            val dia = parts[0]
            val horaInicio = parts[1].replace(":", "").toInt()
            val horaFin = parts[3].replace(":", "").toInt()
            Triple(dia, horaInicio, horaFin)
        } catch (e: NumberFormatException) {
            null
        }
    }

    private fun hayChoqueHorario(nuevoGrupo: Grupo, seleccionActual: List<GrupoSeleccionado>): String? {
        val horarioNuevo = parseHorario(nuevoGrupo.horario) ?: return null

        for (item in seleccionActual) {
            val horarioExistente = parseHorario(item.grupo.horario) ?: continue
            if (horarioNuevo.first == horarioExistente.first) { // Mismo día
                // Hay choque si (InicioA < FinB) y (InicioB < FinA)
                if (horarioNuevo.second < horarioExistente.third && horarioExistente.second < horarioNuevo.third) {
                    return "Choque de horario con ${item.materiaNombre} (G: ${item.grupo.grupo})."
                }
            }
        }
        return null
    }

    fun onConfirmarInscripcion() {
        viewModelScope.launch {
            _uiState.update { it.copy(isConfirming = true) }
            try {
                val registro = userPreferencesRepository.userRegistro.first()
                if (registro == null) {
                    _uiState.update { it.copy(isConfirming = false, seleccionError = "No se pudo obtener el registro.") }
                    return@launch
                }

                val payload = InscripcionRequest(
                    registro = registro,
                    periodoId = 1, // Asumimos ID de período fijo como en la web
                    materias = _uiState.value.seleccion.map {
                        MateriaInscripcion(it.materiaCodigo, it.grupo.grupo)
                    },
                    // ✨ Añadimos la clave de idempotencia para que coincida con el backend
                    idempotencyKey = "insc-android-$registro-${System.currentTimeMillis()}"
                )

                val response = ApiClient.instance.confirmarInscripcion(payload)
                if (response.isSuccessful) {
                    // Limpiamos el carrito y refrescamos el estado
                    _uiState.update { it.copy(seleccion = emptyList()) }
                    loadEstadoInscripciones()
                } else {
                    _uiState.update { it.copy(seleccionError = "Error al confirmar la inscripción.") }
                }

            } catch (e: Exception) {
                Log.e("DashboardViewModel", "Error al confirmar inscripción", e)
                _uiState.update { it.copy(seleccionError = "Error de conexión al confirmar.") }
            } finally {
                _uiState.update { it.copy(isConfirming = false) }
            }
        }
    }

    fun onCancelarInscripcion(inscripcionId: Int) {
        viewModelScope.launch {
            _uiState.update { it.copy(isConfirming = true) } // Reutilizamos el estado de carga
            try {
                val response = ApiClient.instance.cancelarInscripcion(inscripcionId)
                if (response.isSuccessful) {
                    // Refrescar la lista de inscripciones y materias disponibles
                    loadEstadoInscripciones()
                    loadMateriasDisponibles()
                } else {
                    _uiState.update { it.copy(seleccionError = "Error al cancelar la inscripción.") }
                }
            } catch (e: Exception) {
                Log.e("DashboardViewModel", "Error al cancelar inscripción", e)
                _uiState.update { it.copy(seleccionError = "Error de conexión al cancelar.") }
            } finally {
                _uiState.update { it.copy(isConfirming = false) }
            }
        }
    }

    private fun loadEstadoInscripciones() {
        viewModelScope.launch {
            val registro = userPreferencesRepository.userRegistro.first() ?: return@launch
            try {
                val response = ApiClient.instance.getEstadoInscripcion(registro)
                if (response.isSuccessful) {
                    val inscripciones = response.body() ?: emptyList()
                    _uiState.update { it.copy(inscripciones = inscripciones) }

                    // Iniciar o detener el sondeo según el estado
                    if (inscripciones.any { it.estado.equals("PENDIENTE", ignoreCase = true) }) {
                        startPolling()
                    } else {
                        stopPolling()
                    }
                }
            } catch (e: Exception) {
                Log.w("DashboardViewModel", "No se pudo refrescar el estado de inscripciones.", e)
            }
        }
    }

    private fun startPolling() {
        if (pollingJob?.isActive == true) return // Ya está en ejecución
        pollingJob = viewModelScope.launch {
            while (true) {
                delay(5000) // Espera 5 segundos
                loadEstadoInscripciones()
            }
        }
    }

    private fun stopPolling() {
        pollingJob?.cancel()
        pollingJob = null
    }

    override fun onCleared() {
        super.onCleared()
        stopPolling() // Asegurarse de detener el sondeo al destruir el ViewModel
    }
}
