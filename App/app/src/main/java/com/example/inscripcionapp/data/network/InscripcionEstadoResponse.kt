package com.example.inscripcionapp.data.network

data class MateriaInscrita(
    val codigo: String,
    val nombre: String,
    val grupo: String,
    val estado: String
)

data class InscripcionEstadoResponse(
    val id: Int,
    val fecha: String, // La fecha viene como String (e.g., "2024-05-22T15:30:00Z")
    val estado: String,
    val materias: List<MateriaInscrita>
)
