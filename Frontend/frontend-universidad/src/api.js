import axios from 'axios';

// Creamos una instancia de Axios con la URL base de tu backend.
// El puerto 5001 es el que definiste en tu docker-compose.yml para 'servicioa'.
const apiClient = axios.create({
    baseURL: 'http://localhost:5001/api',
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