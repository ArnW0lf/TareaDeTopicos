<template>
  <div class="dashboard-view">
    <div class="container-fluid mt-4">
      <div v-if="error" class="alert alert-danger">{{ error }}</div>
      <div v-if="loading" class="text-center">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Cargando...</span>
        </div>
        <p>Cargando datos...</p>
      </div>

      <div v-else>
        <!-- Sección de Inscripción -->
        <div class="row">
          <div class="col-lg-7">
            <div class="card shadow-sm mb-4">
              <div class="card-header">
                <h3>Materias Disponibles para Inscripción</h3>
              </div>
              <div class="card-body">
                <table class="table table-hover">
                  <thead>
                    <tr>
                      <th>Código</th>
                      <th>Nombre</th>
                      <th>Créditos</th>
                      <th>Semestre</th>
                      <th>Acción</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="materia in materiasDisponibles" :key="materia.codigo">
                      <td>{{ materia.codigo }}</td>
                      <td>{{ materia.nombre }}</td>
                      <td>{{ materia.creditos }}</td>
                      <td>{{ materia.semestre }}</td>
                      <td>
                        <button class="btn btn-sm btn-primary" @click="verGrupos(materia)">
                          Ver Grupos
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <!-- Carrito de Selección -->
          <div class="col-lg-5">
            <div class="card shadow-sm mb-4">
              <div class="card-header">
                <h3>Mi Selección</h3>
              </div>
              <div class="card-body">
                <ul class="list-group">
                  <li v-for="(item, index) in seleccion" :key="item.materiaCodigo" class="list-group-item d-flex justify-content-between align-items-center">
                    <span>{{ item.materiaNombre }} (G: {{ item.grupo.grupo }}) - {{ item.grupo.horario }}</span>
                    <button class="btn btn-sm btn-outline-danger" @click="quitarDeSeleccion(index)">
                      <i class="bi bi-trash"></i>
                    </button>
                  </li>
                  <li v-if="seleccion.length === 0" class="list-group-item text-muted">
                    Aún no has seleccionado materias.
                  </li>
                </ul>
              </div>
              <div class="card-footer text-end">
                <button class="btn btn-success" :disabled="seleccion.length === 0 || enviando" @click="confirmarInscripcion">
                  <span v-if="enviando" class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                  {{ enviando ? 'Procesando...' : 'Confirmar Inscripción' }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Historial de Inscripciones -->
        <div class="card shadow-sm">
          <div class="card-header">
            <h3>Mis Inscripciones</h3>
          </div>
          <div class="card-body">
            <div v-if="inscripciones.length === 0" class="text-muted">No tienes inscripciones registradas.</div>
            <div v-else class="accordion" id="accordionInscripciones">
              <div v-for="inscripcion in inscripciones" :key="inscripcion.id" class="accordion-item">
                <h2 class="accordion-header" :id="'heading' + inscripcion.id">
                  <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" :data-bs-target="'#collapse' + inscripcion.id">
                    Inscripción del {{ new Date(inscripcion.fecha).toLocaleDateString() }} -
                    <span :class="getStatusClass(inscripcion.estado)" class="fw-bold ms-2">{{ inscripcion.estado }}</span>
                  </button>
                </h2>
                <div :id="'collapse' + inscripcion.id" class="accordion-collapse collapse" :data-bs-parent="'#accordionInscripciones'">
                  <div class="accordion-body">
                    <ul class="list-group">
                      <li v-for="materia in inscripcion.materias" :key="materia.codigo + materia.grupo" class="list-group-item d-flex justify-content-between">
                        <div>
                          <strong>{{ materia.nombre }}</strong> ({{ materia.codigo }} - Grupo {{ materia.grupo }})
                        </div>
                        <span :class="getStatusClass(materia.estado)" class="badge rounded-pill align-self-center p-2">{{ materia.estado }}</span>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Modal para ver grupos -->
      <div class="modal fade" id="gruposModal" tabindex="-1">
        <div class="modal-dialog modal-lg">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title">Grupos para {{ materiaSeleccionada?.nombre }}</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
              <table class="table">
                <thead>
                  <tr>
                    <th>Grupo</th>
                    <th>Docente</th>
                    <th>Cupo</th>
                    <th>Horario</th>
                    <th>Acción</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="grupo in gruposPorMateria" :key="grupo.id">
                    <td>{{ grupo.grupo }}</td>
                    <td>{{ grupo.docente }}</td>
                    <td>{{ grupo.cupo }}</td>
                    <td>{{ grupo.horario }}</td>
                    <td>
                      <button class="btn btn-sm btn-success" @click="seleccionarGrupo(grupo)">
                        Seleccionar
                      </button>
                    </td>
                  </tr>
                  <tr v-if="gruposPorMateria.length === 0">
                    <td colspan="5" class="text-center text-muted">No hay grupos disponibles para esta materia.</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import apiClient from '../api';
import { jwtDecode } from 'jwt-decode';
import { Modal } from 'bootstrap';

const loading = ref(true);
const error = ref(null);
const enviando = ref(false);

const materiasDisponibles = ref([]);
const gruposPorMateria = ref([]);
const materiaSeleccionada = ref(null);
const seleccion = ref([]);
const inscripciones = ref([]);

let gruposModal = null;
let pollingInterval = null;

const getRegistroFromToken = () => {
  const token = localStorage.getItem('user-token');
  if (!token) {
    console.error('No se encontró token en localStorage');
    return null;
  }

  try {
    const decoded = jwtDecode(token);
    console.log('Token decodificado:', decoded);
    // Busca el registro en las propiedades comunes del payload del token.
    return decoded.sub || decoded.registro || decoded.Registro;
  } catch (error) {
    console.error('Error decodificando token:', error);
    return null;
  }
};

const fetchMateriasDisponibles = async () => {
  const registro = getRegistroFromToken();
  console.log('Registro obtenido:', registro); // Para debug

  if (!registro) {
    error.value = "No se pudo obtener el registro del estudiante. Por favor, cierra sesión y vuelve a ingresar.";
    loading.value = false; // Detener el spinner de carga
    return;
  }
  try {
    const response = await apiClient.get(`/inscripciones/materias-disponibles/${registro}`);
    materiasDisponibles.value = response.data;
  } catch (err) {
    error.value = "Error al cargar las materias disponibles.";
    console.error('Error fetching materias:', err);
  }
};

const fetchEstadoInscripciones = async () => {
  const registro = getRegistroFromToken();
  if (!registro) return;
  try {
    const response = await apiClient.get(`/inscripciones/estado-inscripcion/${registro}`);
    inscripciones.value = response.data;
    // Si hay alguna inscripción pendiente, activa el sondeo
    if (response.data.some(i => i.estado === 'PENDIENTE')) {
      startPolling();
    } else {
      stopPolling();
    }
  } catch (err) {
    // No mostrar error si solo falla el refresco
    console.error("Error al refrescar estado de inscripciones:", err);
  }
};

const verGrupos = async (materia) => {
  materiaSeleccionada.value = materia;
  try {
    const response = await apiClient.get(`/inscripciones/grupos/${materia.codigo}`);
    gruposPorMateria.value = response.data.grupos;
    gruposModal.show();
  } catch (err) {
    error.value = `Error al cargar los grupos para ${materia.nombre}.`;
  }
};

const parseHorario = (horarioStr) => {
  if (!horarioStr || !horarioStr.includes(' ')) return null;
  const parts = horarioStr.split(' ');
  if (parts.length < 4) return null;

  const dia = parts[0];
  const horaInicio = parseInt(parts[1].replace(':', ''), 10);
  const horaFin = parseInt(parts[3].replace(':', ''), 10);

  return { dia, horaInicio, horaFin };
};

const hayChoqueHorario = (nuevoGrupo) => {
  // MODIFICADO: Se mejora la robustez de la función
  const horarioNuevo = parseHorario(nuevoGrupo.horario);
  if (!horarioNuevo) return false; // No se puede validar si el formato es incorrecto

  for (const item of seleccion.value) {
    const horarioExistente = parseHorario(item.grupo.horario);
    if (!horarioExistente) continue;

    // Si no es el mismo día, no hay choque
    if (horarioNuevo.dia !== horarioExistente.dia) {
      continue;
    }

    // Lógica de solapamiento de rangos de tiempo
    // Hay choque si (InicioA < FinB) y (InicioB < FinA)
    if (horarioNuevo.horaInicio < horarioExistente.horaFin && horarioExistente.horaInicio < horarioNuevo.horaFin) {
      return `Hay un choque de horario con ${item.materiaNombre} (G: ${item.grupo.grupo}).`;
    }
  }
  return null; // No hay choque
};

const seleccionarGrupo = (grupo) => {
  // MODIFICADO: Lógica de validación en tiempo real
  // 1. Validar si la materia ya está en la selección
  if (seleccion.value.some(item => item.materiaCodigo === materiaSeleccionada.value.codigo)) {
    alert('Ya has seleccionado esta materia. Quítala de tu selección si quieres cambiar de grupo.');
    return;
  }

  // 2. NUEVO: Validar cupo antes de añadir
  if (grupo.cupo <= 0) {
    alert(`El grupo ${grupo.grupo} de ${materiaSeleccionada.value.nombre} ya no tiene cupos disponibles.`);
    return;
  }

  // 3. NUEVO: Validar choque de horario antes de añadir
  const choque = hayChoqueHorario(grupo);
  if (choque) {
    alert(choque);
    return;
  }
  // Si todas las validaciones pasan, se añade a la selección
  seleccion.value.push({
    materiaCodigo: materiaSeleccionada.value.codigo,
    materiaNombre: materiaSeleccionada.value.nombre,
    grupo: grupo // Guardamos el objeto completo
  });
  gruposModal.hide();
};

const quitarDeSeleccion = (index) => {
  seleccion.value.splice(index, 1);
};

const confirmarInscripcion = async () => {
  enviando.value = true;
  const registro = getRegistroFromToken();
  const payload = {
    registro: registro,
    periodoId: 1, // Asumimos un ID de período fijo por ahora
    materias: seleccion.value.map(s => ({ materiaCodigo: s.materiaCodigo, grupo: s.grupo.grupo }))
  };

  try {
    await apiClient.post('/inscripciones/async', payload);
    seleccion.value = []; // Limpiar carrito
    await fetchEstadoInscripciones(); // Cargar estado inmediatamente
  } catch (err) {
    error.value = "Ocurrió un error al enviar la inscripción.";
  } finally {
    enviando.value = false;
  }
};

const startPolling = () => {
  if (pollingInterval) return; // Evitar múltiples intervalos
  pollingInterval = setInterval(fetchEstadoInscripciones, 5000); // Refrescar cada 5 segundos
};

const stopPolling = () => {
  if (pollingInterval) {
    clearInterval(pollingInterval);
    pollingInterval = null;
  }
};

const getStatusClass = (estado) => {
  switch (estado?.toUpperCase()) {
    case 'VALIDO':
    case 'COMPLETADA':
      return 'text-bg-success';
    case 'ERROR':
    case 'CON_ERRORES':
      return 'text-bg-danger';
    case 'PENDIENTE':
      return 'text-bg-warning';
    default:
      return 'text-bg-secondary';
  }
};

onMounted(async () => {
  // Logs para depuración
  console.log('Token en localStorage:', localStorage.getItem('user-token'));
  console.log('Registro en localStorage:', localStorage.getItem('user-registro'));
  console.log('Nombre en localStorage:', localStorage.getItem('user-name'));

  gruposModal = new Modal(document.getElementById('gruposModal'));
  await Promise.all([
    fetchMateriasDisponibles(),
    fetchEstadoInscripciones()
  ]);
  loading.value = false;
});

onUnmounted(() => {
  stopPolling();
});
</script>

<style scoped>
.table-hover tbody tr:hover {
  background-color: #f1f1f1;
  cursor: pointer;
}
.badge {
  font-size: 0.8rem;
}
</style>