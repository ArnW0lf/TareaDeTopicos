<template>
  <div class="row justify-content-center">
    <div class="col-sm-8 col-md-6 col-lg-4">
      <div class="card shadow-lg">
        <div class="card-header text-center">
          <i class="bi bi-book-half fs-1 text-primary"></i>
          <h3>Iniciar Sesión</h3>
          <p class="text-muted">Portal del Estudiante</p>
        </div>
        <div class="card-body">
          <form @submit.prevent="handleLogin">
            <div class="mb-3">
              <label for="registro" class="form-label">Registro</label>
              <input type="text" class="form-control" id="registro" v-model="registro" required>
            </div>
            <div class="mb-3">
              <label for="password" class="form-label">Contraseña</label>
              <input type="password" class="form-control" id="password" v-model="password" required>
            </div>
            <div v-if="error" class="alert alert-danger">
              {{ error }}
            </div>
            <div class="d-grid">
              <button type="submit" class="btn btn-primary" :disabled="loading">
                <span v-if="loading" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                {{ loading ? 'Ingresando...' : 'Ingresar' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import apiClient from '../api';
import { jwtDecode } from 'jwt-decode';

const registro = ref('');
const password = ref('');
const loading = ref(false);
const error = ref(null);
const router = useRouter();

const handleLogin = async () => {
  loading.value = true;
  error.value = null;
  try {
    const response = await apiClient.post('/estudiantes/login', {
      Registro: registro.value,
      password: password.value,
    });
    const token = response.data.token;
    localStorage.setItem('user-token', token);
    const decoded = jwtDecode(token);
    localStorage.setItem('user-name', decoded.nombre || decoded.sub); // Guarda el nombre
    localStorage.setItem('user-registro', decoded.sub); // Guarda el registro del estudiante
    router.push('/dashboard');
  } catch (err) {
    error.value = 'Registro o contraseña incorrectos.';
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
}
</style>