import NotFound from './pages/NotFound.vue'
import HomePage from './pages/HomePage.vue'
import Login from './pages/Login.vue'
import Maps from './pages/Maps.vue'
import Topo from './pages/Topo.vue'
import Games from './pages/Games.vue'
import Play from './pages/Play.vue'
import GameResults from './pages/GameResults.vue'
import GameResultsOfGame from './pages/GameResultsOfGame.vue'

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
    },{
        path: '/topo/:id',
        component: Topo,
        props:true,
        name:'topo'
    },{
        path: '/games',
        component: Games
    },{
        path: '/play/:id',
        component: Play,
        props:true,
        name:'play'
    },{
        path: '/results/ofPlayer/:userId?',
        component: GameResults,
        props:true,
        name:'resultsOfPlayer'
    },{
        path: '/results/ofGame/:gameId',
        component: GameResultsOfGame,
        props:true,
        name:'resultsOfGame'
    }
]