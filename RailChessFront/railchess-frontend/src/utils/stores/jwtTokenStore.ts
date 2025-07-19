import { defineStore } from 'pinia'
import { persistKey } from './persistKey'
import { ref } from 'vue'

const storeName = 'jwtTokenStore'
export const useJwtTokenStore = defineStore(storeName, ()=>{
    const jwtToken = ref<string>()
    return {
        jwtToken
    }
}, {
    persist: {
        key: persistKey(storeName)
    }
})