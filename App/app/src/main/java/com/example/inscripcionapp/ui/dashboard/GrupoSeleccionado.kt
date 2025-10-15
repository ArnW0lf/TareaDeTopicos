package com.example.inscripcionapp.ui.dashboard

import com.example.inscripcionapp.data.network.Grupo

data class GrupoSeleccionado(
    val materiaCodigo: String,
    val materiaNombre: String,
    val grupo: Grupo
)
