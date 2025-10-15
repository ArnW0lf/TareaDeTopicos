package com.example.inscripcionapp

import android.app.Application
import com.example.inscripcionapp.data.network.ApiClient

class InscripcionApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        // Inicializamos el ApiClient una sola vez cuando la app se crea.
        ApiClient.initialize(this)
    }
}
