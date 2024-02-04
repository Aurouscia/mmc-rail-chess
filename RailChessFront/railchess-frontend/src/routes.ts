import NotFound from './pages/NotFound.vue'
import HomePage from './pages/HomePage.vue'
import Login from './pages/Login.vue'


export const routes = [
    {
        path: '/',
        component:HomePage
    },{
        path: '/:pathMatch(.*)*',
        component: NotFound 
    },{
        path: '/login',
        component: Login
    }
]