import { createRouter, createWebHistory } from 'vue-router';
import Login from '../views/Login.vue';
import Dashboard from '../views/Dashboard.vue';
import MateriasList from '../components/MateriasList.vue'; // Importamos el nuevo componente

const routes = [
  { path: '/', redirect: '/dashboard' },
  { path: '/login', component: Login, name: 'Login' },
  { path: '/dashboard', component: Dashboard, name: 'Dashboard', meta: { requiresAuth: true } },
  // Nueva ruta para ver las materias
  { path: '/materias', component: MateriasList, name: 'Materias', meta: { requiresAuth: true } },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

// Guardia de navegación para proteger rutas
router.beforeEach((to, from, next) => {
  const loggedIn = localStorage.getItem('user-token');

  if (to.matched.some(record => record.meta.requiresAuth) && !loggedIn) {
    next('/login');
  } else {
    next();
  }
});

export default router;