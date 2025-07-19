import { defineStore } from "pinia";
import { persistKey } from "./persistKey";
import { ref } from "vue";

const storeName = 'playOptionsStore'
export const usePlayOptionsStore = defineStore(storeName, ()=>{
    const bgOpacity = ref<number>(0.4)
    const staSizeRatio = ref<number>(0.8)
    return {
        bgOpacity,
        staSizeRatio
    }
}, {
    persist:{
        key:persistKey(storeName)
    }
})