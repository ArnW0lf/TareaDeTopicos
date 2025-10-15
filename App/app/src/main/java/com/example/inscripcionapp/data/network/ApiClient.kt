package com.example.inscripcionapp.data.network

import android.app.Application
import com.example.inscripcionapp.data.UserPreferencesRepository
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

// --------------------------------------------------------------------------------
// ⚠️ IMPORTANTE: CAMBIA ESTA IP
// Usa la IP de tu computadora en la red local.
// Para encontrarla, en Windows abre cmd y escribe: ipconfig
// Busca la dirección IPv4 de tu adaptador Wi-Fi o Ethernet.
// --------------------------------------------------------------------------------
private const val BASE_URL = "http://192.168.0.6:5001/" // <-- CAMBIA ESTO

interface ApiService {
    @POST("api/estudiantes/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    @GET("api/inscripciones/materias-disponibles/{registro}")
    suspend fun getMateriasDisponibles(@Path("registro") registro: String): Response<List<MateriaDisponible>>

    @GET("api/inscripciones/grupos/{materiaCodigo}")
    suspend fun getGruposPorMateria(@Path("materiaCodigo") materiaCodigo: String): Response<GruposResponse>

    @POST("api/inscripciones/async")
    suspend fun confirmarInscripcion(@Body request: InscripcionRequest): Response<Unit> // La respuesta es vacía (202 Accepted)

    @GET("api/inscripciones/estado-inscripcion/{registro}")
    suspend fun getEstadoInscripcion(@Path("registro") registro: String): Response<List<InscripcionEstadoResponse>>
}

object ApiClient {
    // Este objeto necesita ser inicializado desde la app con el contexto
    private var userPreferencesRepository: UserPreferencesRepository? = null

    fun initialize(application: Application) {
        userPreferencesRepository = UserPreferencesRepository(application)
    }

    private val logging = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }

    // Interceptor para añadir el token a las cabeceras
    private val authInterceptor = Interceptor { chain ->
        val token = runBlocking { // Usamos runBlocking porque el interceptor no es una corrutina
            userPreferencesRepository?.authToken?.first()
        }
        val requestBuilder = chain.request().newBuilder()
        if (token != null) {
            requestBuilder.addHeader("Authorization", "Bearer $token")
        }
        chain.proceed(requestBuilder.build())
    }

    private val httpClient = OkHttpClient.Builder()
        .addInterceptor(logging)
        .addInterceptor(authInterceptor)
        .build()

    val instance: ApiService by lazy {
        if (userPreferencesRepository == null) {
            throw IllegalStateException("ApiClient must be initialized by calling ApiClient.initialize(context) in your Application class.")
        }
        val retrofit = Retrofit.Builder()
            .baseUrl(BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .client(httpClient)
            .build()
        retrofit.create(ApiService::class.java)
    }
}
