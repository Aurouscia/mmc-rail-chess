import { defineStore } from "pinia";
import { persistKey } from "./persistKey";
import { ref } from "vue";

const storeName = 'rankingListStore';
export type RankingOrderBy = 'last30days' | 'last7days' | 'history' | 'recentGame';
export const useRankingListStore = defineStore(storeName, () => {
    const orderBy = ref<RankingOrderBy>('last30days');
    return {
        orderBy
    };
}, {
    persist: {
        key: persistKey(storeName)
    }
});
