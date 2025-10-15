package com.example.inscripcionapp.ui.login

import android.app.Application
import android.util.Log
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.example.inscripcionapp.data.UserPreferencesRepository
import com.example.inscripcionapp.data.network.ApiClient
import com.example.inscripcionapp.data.network.LoginRequest
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

// Data class para representar todo el estado de la pantalla de Login
data class LoginUiState(
    val registro: String = "",
    val contrasena: String = "",
    val isLoading: Boolean = false,
    val error: String? = null,
    val loginSuccess: Boolean = false
)

class LoginViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(LoginUiState())
    val uiState = _uiState.asStateFlow()

    // Instancia del repositorio para guardar el token
    private val userPreferencesRepository = UserPreferencesRepository(application)

    fun onRegistroChange(registro: String) {
        _uiState.update { it.copy(registro = registro) }
    }

    fun onContrasenaChange(contrasena: String) {
        _uiState.update { it.copy(contrasena = contrasena) }
    }

    fun login() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) } // Inicia la carga

            val registro = _uiState.value.registro
            val contrasena = _uiState.value.contrasena

            try {
                val request = LoginRequest(
                    Registro = registro,
                    password = contrasena
                )
                val response = ApiClient.instance.login(request)

                if (response.isSuccessful && response.body()?.token != null) {
                    val token = response.body()!!.token
                    Log.d("LoginViewModel", "Login exitoso. Token: $token")
                    userPreferencesRepository.saveUserCredentials(token, registro)
                    _uiState.update { it.copy(isLoading = false, loginSuccess = true) }
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Registro o contraseña incorrectos.") }
                }
            } catch (e: Exception) {
                Log.e("LoginViewModel", "Error de red", e)
                _uiState.update { it.copy(isLoading = false, error = "Error de conexión. Verifica la IP y que el backend esté funcionando.") }
            }
        }
    }
}