package com.example.inscripcionapp.ui.login

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.example.inscripcionapp.ui.theme.InscripcionappTheme

@Composable
fun LoginScreen(
    uiState: LoginUiState,
    onRegistroChange: (String) -> Unit,
    onContrasenaChange: (String) -> Unit,
    onLoginClick: () -> Unit
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "Iniciar Sesión",
            style = MaterialTheme.typography.headlineLarge,
            modifier = Modifier.padding(bottom = 32.dp)
        )

        // Campo de texto para el Registro
        OutlinedTextField(
            value = uiState.registro,
            onValueChange = onRegistroChange,
            label = { Text("Registro") },
            leadingIcon = { Icon(Icons.Default.Person, contentDescription = "Registro Icon") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number)
        )

        Spacer(modifier = Modifier.height(16.dp))

        // Campo de texto para la Contraseña
        OutlinedTextField(
            value = uiState.contrasena,
            onValueChange = onContrasenaChange,
            label = { Text("Contraseña") },
            leadingIcon = { Icon(Icons.Default.Lock, contentDescription = "Password Icon") },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true,
            visualTransformation = PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password)
        )

        // Muestra el mensaje de error si existe
        if (uiState.error != null) {
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = uiState.error,
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodySmall
            )
        }

        Spacer(modifier = Modifier.height(24.dp))

        // Botón de Login
        Button(
            onClick = onLoginClick,
            modifier = Modifier.fillMaxWidth(),
            enabled = !uiState.isLoading
        ) {
            if (uiState.isLoading) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = MaterialTheme.colorScheme.onPrimary
                )
            } else {
                Text("Ingresar")
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
fun LoginScreenPreview() {
    val previewState = LoginUiState()
    InscripcionappTheme {
        LoginScreen(uiState = previewState, onRegistroChange = {}, onContrasenaChange = {}, onLoginClick = {})
    }
}

@Preview(showBackground = true)
@Composable
fun LoginScreenLoadingPreview() {
    val previewState = LoginUiState(isLoading = true)
    InscripcionappTheme {
        LoginScreen(uiState = previewState, onRegistroChange = {}, onContrasenaChange = {}, onLoginClick = {})
    }
}

@Preview(showBackground = true)
@Composable
fun LoginScreenErrorPreview() {
    val previewState = LoginUiState(error = "Usuario o contraseña incorrectos")
    InscripcionappTheme {
        LoginScreen(uiState = previewState, onRegistroChange = {}, onContrasenaChange = {}, onLoginClick = {})
    }
}