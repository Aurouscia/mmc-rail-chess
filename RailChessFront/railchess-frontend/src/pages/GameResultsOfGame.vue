<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { Api } from '../utils/api';
import { injectApi } from '../provides';
import Loading from '../components/Loading.vue';
import { useRouter } from 'vue-router';

const props = defineProps<{
    gameId:string
}>();
const idNum = parseInt(props.gameId||"0");

const data = ref<GameResultListResponse>();
async function load(){
    let gid = idNum;
    const res = await api.gameResult.ofGame(gid);
    if(res)
        data.value = res;
}

const router = useRouter();
function jumpToPlayerLog(playerId:number){
    router.push(`/results/ofPlayer/${playerId}`);
}

let api:Api;
onMounted(async()=>{
    api = injectApi();
    await load();
})
</script>

<template>
    <div v-if="data">
        <div v-if="data.Logs.length>0">
            <h1>{{ data.Logs[0].StartTime }} <br/> {{ data.Logs[0].MapName }} 的记录</h1>
            <button @click="router.push('/playback/'+gameId)">查看棋局回放</button>
            <table>
                <tr>
                    <th>排名</th>
                    <th>玩家</th>
                </tr>
                <tr v-for="log in data.Logs" @click="jumpToPlayerLog(log.UserId)">
                    <td>
                        {{ log.Rank }}
                    </td>
                    <td>
                        {{ log.UserName }}
                    </td>
                </tr>
            </table>
        </div>
        <h1 v-else>
            暂无对弈记录
        </h1>
    </div>
    <Loading v-else></Loading>
</template>

<style scoped>
tr{
    cursor: pointer;
}
tr:hover td{
    background-color: #ccc;
}
table{
    table-layout: fixed;
    width: 100%;
}
.time{
    font-size: small;
}
</style>