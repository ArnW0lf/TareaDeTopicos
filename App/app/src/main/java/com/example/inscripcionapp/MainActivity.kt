package com.example.inscripcionapp

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.inscripcionapp.data.UserPreferencesRepository
import com.example.inscripcionapp.data.network.ApiClient
import com.example.inscripcionapp.ui.dashboard.DashboardScreen
import com.example.inscripcionapp.ui.dashboard.DashboardViewModel
import com.example.inscripcionapp.ui.login.LoginScreen
import com.example.inscripcionapp.ui.login.LoginViewModel
import com.example.inscripcionapp.ui.theme.InscripcionappTheme

class MainActivity : ComponentActivity() {
    
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Inicializamos el ApiClient con el repositorio de preferencias
        // Este paso es crucial y debe hacerse antes de que se use ApiClient
        ApiClient.initialize(application)

        setContent { 
            InscripcionappTheme { 
                val snackbarHostState = remember { SnackbarHostState() }
                Scaffold(
                    snackbarHost = { SnackbarHost(hostState = snackbarHostState) }
                ) { innerPadding ->
                    val navController = rememberNavController()
                    val loginViewModel: LoginViewModel = viewModel()
                    val loginUiState by loginViewModel.uiState.collectAsState()
    
                    // Efecto que se ejecuta cuando el estado de loginSuccess cambia a true
                    LaunchedEffect(key1 = loginUiState.loginSuccess) {
                        if (loginUiState.loginSuccess) {
                            navController.navigate("dashboard") {
                                // Limpia la pila de navegación para que el usuario no pueda volver al login
                                popUpTo("login") { inclusive = true }
                            }
                        }
                    }
    
                    NavHost(navController = navController, startDestination = "login") {
                        composable("login") {
                            LoginScreen(
                                uiState = loginUiState,
                                onRegistroChange = loginViewModel::onRegistroChange,
                                onContrasenaChange = loginViewModel::onContrasenaChange,
                                onLoginClick = loginViewModel::login
                            )
                        }
                        composable("dashboard") {
                            val dashboardViewModel: DashboardViewModel = viewModel()
                            val dashboardUiState by dashboardViewModel.uiState.collectAsState()
                            DashboardScreen(
                                uiState = dashboardUiState,
                                snackbarHostState = snackbarHostState,
                                onVerGruposClicked = dashboardViewModel::onVerGruposClicked,
                                onDismissVerGrupos = dashboardViewModel::onDismissVerGrupos,
                                onGrupoSeleccionado = dashboardViewModel::onGrupoSeleccionado,
                                onQuitarDeSeleccion = dashboardViewModel::onQuitarDeSeleccion,
                                onConfirmarInscripcion = dashboardViewModel::onConfirmarInscripcion,
                                onClearSeleccionError = dashboardViewModel::clearSeleccionError,
                                onCancelarInscripcion = dashboardViewModel::onCancelarInscripcion
                            )
                        }
                    }
                }
            }
        }
    }
}