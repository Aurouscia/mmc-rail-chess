import NotFound from './pages/NotFound.vue'
import HomePage from './pages/HomePage.vue'
import Login from './pages/Login.vue'
import Maps from './pages/Maps.vue'
import Topo from './pages/Topo.vue'
import Games from './pages/Games.vue'
import Play from './pages/Play.vue'
import GameResults from './pages/GameResults.vue'
import GameResultsOfGame from './pages/GameResultsOfGame.vue'
import RankingList from './pages/RankingList.vue'
import Guide from './pages/Guide.vue'
import { RouteLocation } from 'vue-router'
import AarcConverter from './pages/AarcConverter.vue'

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
        path: '/playback/:id',
        component: Play,
        props: (route:RouteLocation) => ({ id: route.params.id, playback: 'playback' }),
        name:'playback'
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
    },{
        path: '/ranking',
        component: RankingList,
        name: 'ranking'
    },{
        path: '/guide',
        component: Guide,
        name: 'guide'
    },{
        path: '/aarcConverter',
        component: AarcConverter,
        name: 'aarcConverter'
    }
]