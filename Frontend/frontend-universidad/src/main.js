// src/main.js
import './assets/main.css'
import 'bootstrap/dist/css/bootstrap.min.css' // <-- Añade esta línea

import { createApp } from 'vue'
import App from './App.vue'
import router from './router'

const app = createApp(App)

app.use(router)

app.mount('#app')
