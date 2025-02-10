<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { Api } from '../utils/api';
import { injectApi, injectUserInfo } from '../provides';
import { IdentityInfo } from '../utils/userInfo';
import Loading from '../components/Loading.vue';
import { useRouter } from 'vue-router';

const props = defineProps<{
    userId:string|undefined
}>();
const idNum = parseInt(props.userId||"0");

const data = ref<GameResultListResponse>();
async function load(){
    let uid = idNum;
    if(!uid){
        uid = info.Id;
    }
    const res = await api.gameResult.ofUser(uid);
    if(res)
        data.value = res;
}

const router = useRouter();
function jumpToGameLog(gameId:number){
    router.push(`/results/ofGame/${gameId}`);
}

let api:Api;
let info:IdentityInfo;
onMounted(async()=>{
    api = injectApi();
    info = await injectUserInfo().getIdentityInfo();
    await load();
})
</script>

<template>
    <div v-if="data">
        <div v-if="data.Logs.length>0">
            <h1>{{ data.Logs[0].UserName }} 的记录</h1>
            <table><tbody>
                <tr>
                    <th>时间</th>
                    <th>棋盘</th>
                    <th>排名</th>
                    <th></th>
                </tr>
                <tr v-for="log in data.Logs">
                    <td class="time">
                        {{ log.StartTime }}
                    </td>
                    <td>
                        {{ log.MapName }}
                    </td>
                    <td>
                        {{ log.Rank }}/{{ log.PlayerCount }}
                    </td>
                    <td>
                        <button class="minor" @click="jumpToGameLog(log.GameId)">详情</button>
                        <button class="minor" @click="router.push('/playback/'+log.GameId)">回放</button>
                    </td>
                </tr>
            </tbody></table>
        </div>
        <h1 v-else>
            暂无对弈记录
        </h1>
    </div>
    <Loading v-else></Loading>
</template>

<style scoped>
table{
    table-layout: fixed;
    width: 100%;
}
.time{
    font-size: small;
}
</style>