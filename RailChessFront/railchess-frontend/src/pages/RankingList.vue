<script setup lang="ts">
import { inject, onMounted, ref } from 'vue';
import { avtSrc } from '../utils/fileSrc';
import { User } from '../models/user';
import { Api } from '../utils/api';
import { useRouter } from 'vue-router';

const data = ref<User[]>();
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
<h1>排行榜</h1>
<div>
    <table>
        <tr>
            <th class="avtTd"></th>
            <th>用户</th>
            <th>分数</th>
        </tr>
        <tr v-for="u in data" @click="jumpToPlayerLog(u.Id)" :key="u.Id">
            <td class="avtTd">
                <img v-if="u.AvatarName" :src="avtSrc(u.AvatarName)" width="35" height="35">
            </td>
            <td>
                {{u.Name}}
            </td>
            <td>
                {{u.Elo}}
            </td>
        </tr>
    </table>
</div>
</template>

<style scoped>
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
