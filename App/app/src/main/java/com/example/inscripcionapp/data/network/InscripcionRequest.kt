package com.example.inscripcionapp.data.network

data class MateriaInscripcion(
    val materiaCodigo: String,
    val grupo: String
)

data class InscripcionRequest(
    val registro: String,
    val periodoId: Int,
    val materias: List<MateriaInscripcion>,
    val idempotencyKey: String
)
