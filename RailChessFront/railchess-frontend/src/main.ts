import { createApp } from 'vue'
import './style.css'
import App from './App.vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import { routes } from './routes'

const router = createRouter({
history: createWebHashHistory(),routes})
router.afterEach((to,from)=>{
    if(to.path == "/" && from.path != "/"){
        location.reload();//要不然主页的那个git展示框就没了
    }
})
export{router}
createApp(App).use(router).mount('#app')
