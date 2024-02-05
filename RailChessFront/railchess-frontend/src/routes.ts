import NotFound from './pages/NotFound.vue'
import HomePage from './pages/HomePage.vue'
import Login from './pages/Login.vue'
import Maps from './pages/Maps.vue'


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
    },{
        path: '/maps',
        component: Maps
    }
]