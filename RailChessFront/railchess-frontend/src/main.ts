import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import { routes } from './routes'

const router = createRouter({
history: createWebHashHistory(),routes})
export{router}
createApp(App).use(router).mount('#app')
