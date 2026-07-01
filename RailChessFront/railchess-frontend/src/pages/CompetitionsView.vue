<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { injectApi } from '../provides';
import { Competition } from '../models/competition';
import Loading from '../components/Loading.vue';

const api = injectApi()

const pageSize = 6
const loadTake = pageSize + 1
const items = ref<Competition[]>([])
const total = ref<number>(0)
const loading = ref<boolean>(false)
const hasMore = ref<boolean>(true)

async function loadMore() {
    if (loading.value || !hasMore.value) return
    loading.value = true
    const resp = await api.competition.list(items.value.length, loadTake)
    if (resp) {
        const received = resp.items
        hasMore.value = received.length > pageSize
        const toShow = hasMore.value ? received.slice(0, pageSize) : received
        items.value.push(...toShow)
        total.value = resp.total
    }
    loading.value = false
}

function widgetSrc(id: number): string {
    return `${import.meta.env.VITE_BASEURL}/api/Embed/Widget?competitionId=${id}&theme=light`
}

function participantsSrc(id: number): string {
    return `${import.meta.env.VITE_BASEURL}/api/Embed/Participants?competitionId=${id}&theme=light`
}

onMounted(() => {
    loadMore()
})
</script>

<template>
<div class="viewCompetitions">
    <h1 class="h1WithBtns">
        比赛
        <div>
            <RouterLink to="/competitions/manage">
                <button class="minor">管理比赛</button>
            </RouterLink>
        </div>
    </h1>
    <div class="widgets">
        <div v-for="c in items" :key="c.Id" class="competitionBox">
            <iframe :src="widgetSrc(c.Id)" class="widget" :title="c.Title || '比赛 #' + c.Id"></iframe>
            <iframe :src="participantsSrc(c.Id)" class="widget" :title="(c.Title || '比赛 #' + c.Id) + ' 积分榜'"></iframe>
        </div>
    </div>
    <div v-if="!loading && items.length === 0" class="empty">当前暂无比赛</div>
    <div v-if="loading" class="loadingWrap">
        <Loading></Loading>
    </div>
    <div v-if="hasMore && items.length > 0" class="loadMoreWrap">
        <button @click="loadMore" :disabled="loading" class="confirm">加载更多</button>
    </div>
</div>
</template>

<style scoped>
.viewCompetitions{
    padding: 20px;
}
.h1WithBtns{
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
}
.widgets{
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 20px;
    margin: 20px 0;
}
.competitionBox{
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 16px;
    padding: 16px;
    border-radius: 12px;
    background: #fff;
    box-shadow: 0 2px 12px rgba(0,0,0,0.10);
}
.widget{
    width: 320px;
    height: 420px;
    border: 1px solid #eee;
    border-radius: 8px;
    background: #fff;
    overflow: visible;
}
.loadingWrap{
    display: flex;
    justify-content: center;
    margin: 20px 0;
}
.loadMoreWrap{
    display: flex;
    justify-content: center;
    margin: 20px 0;
}
.empty{
    text-align: center;
    font-size: 24px;
    color: #aaa;
    margin-top: 50px;
}
</style>
