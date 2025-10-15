package com.example.inscripcionapp.data.network

// Modelo de datos para un grupo, ajustado para coincidir con la respuesta de la API.
data class Grupo(
    val id: Int,
    val grupo: String, // Corregido: de 'nombre' a 'grupo'
    val docente: String,
    val cupo: Int,
    val horario: String // Corregido: ahora es un String simple
)
