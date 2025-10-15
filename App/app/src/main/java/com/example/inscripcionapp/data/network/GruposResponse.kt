package com.example.inscripcionapp.data.network

// Esta clase representa la estructura completa del JSON que devuelve el endpoint de grupos.
// Ahora incluye todos los campos que el backend envía.
data class GruposResponse(
    // Los nombres de las variables deben coincidir con las claves del JSON.
    // Por defecto, ASP.NET convierte PascalCase (Materia) a camelCase (materia).
    val materia: String,
    val codigo: String,
    // El nombre de la variable debe coincidir exactamente con la clave en el JSON ("grupos").
    val grupos: List<Grupo>
)
