import { defineStore } from "pinia";
import { persistKey } from "./persistKey";
import { ref, watch } from "vue";

function applyVacuumStaOpacity(){
    const val = usePlayOptionsStore().vacuumStaOpacity
    const root = document.documentElement
    root.style.setProperty('--vacuum-sta-opacity', val.toString())
}
function fixStringNumbers(){
    const store = usePlayOptionsStore()
    if(typeof store.bgOpacity == 'string')
        store.bgOpacity = parseFloat(store.bgOpacity)
    if(typeof store.staSizeRatio == 'string')
        store.staSizeRatio = parseFloat(store.staSizeRatio)
    if(typeof store.vacuumStaOpacity == 'string')
        store.vacuumStaOpacity = parseFloat(store.vacuumStaOpacity)
}

const storeName = 'playOptionsStore'
export const usePlayOptionsStore = defineStore(storeName, ()=>{
    const bgOpacity = ref<number>(0.4)
    const staSizeRatio = ref<number>(0.8)
    const vacuumStaOpacity = ref<number>(1)
    const connDisplayMode = ref<'none'|'anim'>('none')
    
    watch(() => vacuumStaOpacity.value, () => {
        applyVacuumStaOpacity()
    })

    return {
        bgOpacity,
        staSizeRatio,
        vacuumStaOpacity,
        connDisplayMode
    }
}, {
    persist:{
        key: persistKey(storeName),
        afterHydrate: ()=>{
            fixStringNumbers()
            applyVacuumStaOpacity()
        }
    }
})