import axios from 'axios';

// Vite expone las variables de entorno en el objeto `import.meta.env`.
// Leemos la URL base desde el archivo .env correspondiente (development o production).
const baseURL = import.meta.env.VITE_API_BASE_URL;

// Es una buena práctica verificar que la variable de entorno esté definida.
if (!baseURL) {
  console.error("Error: La variable de entorno VITE_API_BASE_URL no está definida. Revisa tus archivos .env");
}

const apiClient = axios.create({
  baseURL: baseURL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor para añadir el token JWT a todas las peticiones
apiClient.interceptors.request.use(config => {
  const token = localStorage.getItem('user-token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
}, error => {
  return Promise.reject(error);
});

export default apiClient;