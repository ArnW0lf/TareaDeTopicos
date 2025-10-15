<template>
  <div>
    <nav v-if="isLoggedIn" class="navbar navbar-expand-lg navbar-dark bg-dark">
      <div class="container-fluid">
        <a class="navbar-brand" href="#">Universidad Autonoma Gabriel Rene Moreno</a>
        <div class="collapse navbar-collapse">
          <ul class="navbar-nav me-auto mb-2 mb-lg-0">
            <li class="nav-item">
              <router-link to="/dashboard" class="nav-link" active-class="active">Inscripción</router-link>
            </li>
            <li class="nav-item">
              <router-link to="/materias" class="nav-link" active-class="active">Materias</router-link>
            </li>
          </ul>
          <span class="navbar-text me-3">
            Bienvenido, {{ userName }}
          </span>
          <button class="btn btn-outline-light" @click="logout">Cerrar Sesión</button>
        </div>
      </div>
    </nav>
 
    <main>
      <router-view />
    </main>
  </div>
</template>
 
<script setup>
import { ref, computed, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
 
const router = useRouter();
const route = useRoute();
 
// Estado reactivo para saber si el usuario está logueado
const isLoggedIn = ref(!!localStorage.getItem('user-token'));
const userName = ref(localStorage.getItem('user-name') || '');
 
// Observa los cambios de ruta para actualizar el estado de login
watch(() => route.path, () => {
  isLoggedIn.value = !!localStorage.getItem('user-token');
  userName.value = localStorage.getItem('user-name') || '';
});
 
const logout = () => {
  localStorage.removeItem('user-token');
  localStorage.removeItem('user-name');
  localStorage.removeItem('user-registro');
  isLoggedIn.value = false;
  router.push('/login');
};
</script>
 
<style>
/* Puedes añadir estilos globales aquí si lo necesitas */
body {
  background-color: #f8f9fa;
}
</style>
