<template>
  <div class="container-fluid mt-4 px-0">
    <div class="card">
      <div class="card-header">
        <h2>Materias Disponibles</h2>
        <p class="mb-0">Esta es la lista de todas las materias ofrecidas por la universidad.</p>
      </div>
      <div class="card-body">
        <div v-if="loading" class="text-center">
          <div class="spinner-border" role="status">
            <span class="visually-hidden">Cargando...</span>
          </div>
        </div>

        <div v-if="error" class="alert alert-danger">
          {{ error }}
        </div>

        <div v-if="materias.length" class="table-responsive">
          <table class="table table-striped table-hover">
            <thead class="table-dark">
              <tr>
                <th>Código</th>
                <th>Nombre</th>
                <th>Créditos</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="materia in materias" :key="materia.id">
                <td>{{ materia.codigo }}</td>
                <td>{{ materia.nombre }}</td>
                <td>{{ materia.creditos }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import apiClient from '../api';

const materias = ref([]);
const loading = ref(true);
const error = ref(null);

onMounted(async () => {
  try {
    // Usamos el endpoint que ya tienes para obtener las materias
    const response = await apiClient.get('/materias?pageSize=100'); // Pedimos hasta 100 para empezar
    materias.value = response.data.items;
  } catch (err) {
    error.value = 'Error al cargar las materias. Por favor, intenta más tarde.';
    console.error(err);
  } finally {
    loading.value = false;
  }
});
</script>