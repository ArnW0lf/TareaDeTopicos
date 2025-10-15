<!-- src/views/LoginView.vue -->
<template>
  <div class="container mt-5">
    <div class="row justify-content-center">
      <div class="col-md-6 col-lg-4">
        <div class="card shadow-sm">
          <div class="card-body p-4">
            <h3 class="card-title text-center mb-4">Portal de Inscripciones</h3>
            
            <form @submit.prevent="handleLogin">
              <div class="mb-3">
                <label for="registro" class="form-label">Registro de Estudiante</label>
                <input type="text" class="form-control" id="registro" v-model="registro" required placeholder="Ej: 20251234">
              </div>
              <div class="mb-3">
                <label for="password" class="form-label">Contraseña</label>
                <input type="password" class="form-control" id="password" v-model="password" required placeholder="Tu contraseña">
              </div>
              
              <div v-if="error" class="alert alert-danger mt-3 py-2">
                {{ error }}
              </div>

              <div class="d-grid mt-4">
                <button type="submit" class="btn btn-primary" :disabled="loading">
                  <span v-if="loading" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                  <span v-else>Ingresar</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import apiClient from '../services/api';

const registro = ref('');
const password = ref('');
const error = ref(null);
const loading = ref(false);
const router = useRouter();

const handleLogin = async () => {
  loading.value = true;
  error.value = null;
  try {
    const response = await apiClient.post('/estudiantes/login', {
      Registro: registro.value,
      password: password.value
    });
    localStorage.setItem('user-token', response.data.token);
    localStorage.setItem('user-name', response.data.nombre);
    router.push('/dashboard');
  } catch (err) {
    error.value = 'Registro o contraseña incorrectos.';
    console.error(err);
  } finally {
    loading.value = false;
  }
};
</script>