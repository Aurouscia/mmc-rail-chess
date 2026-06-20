<script setup lang="ts">
import { inject, onMounted, ref, watch, computed, onBeforeUnmount } from 'vue';
import { avtSrc } from '../utils/fileSrc';
import { UserRankingListItem } from '../models/user';
import { Api } from '../utils/api';
import { useRouter } from 'vue-router';
import { useRankingListStore, RankingOrderBy } from '../utils/stores/rankingListStore';
import { injectPop } from '../provides';
import { debounce } from 'lodash-es';

const data = ref<UserRankingListItem[]>();
const store = useRankingListStore();
const PAGE_SIZE = 30;
const skip = ref(0);
const hasMore = ref(true);
const loadingMore = ref(false);
const searchInput = ref('');
const currentSearch = ref<string|undefined>(undefined);
const orderOptions: { value: RankingOrderBy, label: string }[] = [
    { value: 'last30days', label: '近30天局数' },
    { value: 'last7days', label: '近7天局数' },
    { value: 'history', label: '历史局数' },
    { value: 'recentGame', label: '最近参与时间' },
];
const currentOrderLabel = computed(() => {
    return orderOptions.find(x => x.value === store.orderBy)?.label ?? orderOptions[0].label;
});
async function loadMore(){
    loadingMore.value = true;
    const res = await api.identites.user.rankingList(store.orderBy, skip.value, PAGE_SIZE, currentSearch.value);
    loadingMore.value = false;
    if(!res) return;
    if(res.length === 0){
        if(skip.value > 0){
            pop.value?.show("没有更多了", "info");
        }
        hasMore.value = false;
    }
    if(!data.value){
        data.value = res;
    }else{
        data.value.push(...res);
    }
    if(res.length < PAGE_SIZE){
        hasMore.value = false;
    }else{
        skip.value += res.length;
    }
}

const router = useRouter();
function jumpToPlayerLog(playerId:number){
    router.push(`/results/ofPlayer/${playerId}`);
}

const pop = injectPop();
async function resetAndLoad(){
    data.value = undefined;
    skip.value = 0;
    hasMore.value = true;
    await loadMore();
}
function applySearch(){
    const trimmed = searchInput.value.trim();
    currentSearch.value = trimmed.length > 0 ? trimmed : undefined;
    resetAndLoad();
}
const debouncedSearch = debounce(applySearch, 500);
watch(() => store.orderBy, async () => {
    await resetAndLoad();
});

let api:Api;
onMounted(async()=>{
    api = inject('api') as Api;
    await loadMore();
})
onBeforeUnmount(()=>{
    debouncedSearch.cancel();
})
</script>

<template>
<h1>战绩</h1>
<div class="toolbar">
    <div class="order-select">
        <select v-model="store.orderBy">
            <option v-for="opt in orderOptions" :value="opt.value" :key="opt.value">
                {{ opt.label }}
            </option>
        </select>
    </div>
    <div class="search-box">
        <input v-model="searchInput" type="text" placeholder="搜索玩家"
            @input="debouncedSearch"
            @blur="debouncedSearch.flush()"
            @keyup.enter="debouncedSearch.flush()">
    </div>
</div>
<!--<div class="note">注：双人局分别记[100, 0]，四人局分别记[100, 66, 33, 0]，以此类推</div>-->
<div>
    <table><tbody>
        <tr>
            <th class="avtTd"></th>
            <th>用户</th>
            <th>{{ currentOrderLabel }}</th>
            <!--<th>平均胜率</th>-->
        </tr>
        <tr v-for="u in data" @click="jumpToPlayerLog(u.UId)" :key="u.UId">
            <td class="avtTd">
                <img v-if="u.UAvt" :src="avtSrc(u.UAvt)" width="35" height="35" loading="lazy">
            </td>
            <td>
                {{ u.UName }}
            </td>
            <td>
                {{ store.orderBy === 'recentGame' ? u.LastPlayTime : u.Plays }}
            </td>
            <!--<td>
                {{ u.AvgRank/100 || '——' }}
            </td>-->
        </tr>
        <tr v-if="data && data.length === 0">
            <td class="avtTd"></td>
            <td colspan="2" class="empty-tip">
                {{
                    store.orderBy === 'last30days' ? '最近30天暂无对局' :
                    store.orderBy === 'last7days' ? '最近7天暂无对局' :
                    '暂无对局'
                }}
            </td>
        </tr>
    </tbody></table>
    <div class="load-more">
        <button v-if="hasMore" @click="loadMore" :disabled="loadingMore">
            {{ loadingMore ? '加载中...' : '加载更多' }}
        </button>
    </div>
</div>
</template>

<style scoped>
.note{
    text-align: center;
    margin-bottom: 10px;
    color: #666
}
.toolbar{
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    align-items: center;
    margin-bottom: 10px;
}
.order-select{
    display: flex;
    align-items: center;
    gap: 4px;
}
.order-select select{
    margin: 0px;
}
.search-box{
    display: flex;
    align-items: center;
    gap: 4px;
}
.search-box input{
    width: 120px;
    margin: 0px;
}
.empty-tip{
    text-align: center;
    color: #666;
    padding: 16px;
}
.load-more{
    text-align: center;
    margin: 16px 0;
}
.load-more button{
    padding-left: 24px;
    padding-right: 24px;
}
.avtTd{
    background-color: white !important;
    width: 35px !important;
    height: 35px !important;
    padding: 0px;
}
table{
    table-layout: fixed;
    width: 100%;
}
tr:hover td{
    cursor: pointer;
    background-color: #ccc;
}
</style>
