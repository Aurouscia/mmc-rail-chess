<script setup lang="ts">
import { inject, onMounted, ref } from 'vue';
import { avtSrc } from '../utils/fileSrc';
import { UserRankingListItem } from '../models/user';
import { Api } from '../utils/api';
import { useRouter } from 'vue-router';

const data = ref<UserRankingListItem[]>();
async function load(){
    data.value = await api.identites.user.rankingList();
}

const router = useRouter();
function jumpToPlayerLog(playerId:number){
    router.push(`/results/ofPlayer/${playerId}`);
}

let api:Api;
onMounted(async()=>{
    api = inject('api') as Api;
    await load();
})
</script>

<template>
<h1>战绩</h1>
<!--<div class="note">注：双人局分别记[100, 0]，四人局分别记[100, 66, 33, 0]，以此类推</div>-->
<div>
    <table><tbody>
        <tr>
            <th class="avtTd"></th>
            <th>用户</th>
            <th>近30天局数</th>
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
                {{ u.Plays }}
            </td>
            <!--<td>
                {{ u.AvgRank/100 || '——' }}
            </td>-->
        </tr>
    </tbody></table>
</div>
</template>

<style scoped>
.note{
    text-align: center;
    margin-bottom: 10px;
    color: #666
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
