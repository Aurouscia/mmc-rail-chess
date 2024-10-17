<script setup lang="ts">
import { onMounted, ref } from 'vue';
import SideBar from '../components/SideBar.vue';
import { GameActiveResult, RailChessGame } from '../models/game';
import { Api } from '../utils/api';
import { injectApi, injectUserInfo } from '../provides';
import Loading from '../components/Loading.vue';
import Search from '../components/Search.vue';
import { router } from '../main';
import { useFeVersionChecker } from '../utils/feVersionCheck';

const { checkAndPop } = useFeVersionChecker()
checkAndPop()

const active = ref<GameActiveResult>();
async function loadActive(){
    const resp = await api.game.active();
    if(resp){
        active.value = resp;
    }
}

const creating = ref<RailChessGame>();
const sidebar = ref<InstanceType<typeof SideBar>>();
async function create(){
    if(creating.value){
        const resp = await api.game.create(creating.value);
        if(resp){
            sidebar.value?.fold();
            await loadActive();
        }
    }
}
function enter(id:number){
    router.push({name:'play',params:{id}});
}
async function deleteGame(id:number){
    await api.game.delete(id);
    await loadActive();
}
var api:Api
const me = ref<number>(0);
onMounted(async ()=>{
    api = injectApi();
    me.value = (await injectUserInfo().getIdentityInfo()).Id;
    await loadActive();
    creating.value = {
        UseMapId: 0,
        RandAlg: 0,
        RandMin: 1,
        RandMax: 12,
        StucksToLose: 5,
        AllowReverseAtTerminal:true,
        AllowTransfer:1
    }
})
</script>

<template>
<h1>正在进行</h1>
<button class="confirm" @click="sidebar?.extend">新建棋局</button>
<table v-if="active" class="list">
    <tr><th class="mapCol">棋盘</th><th></th> <th>房主</th></tr>
    <tr v-for="g in active.Items">
        <td>
            <div class="mapName">
                {{ g.MapName }}
            </div>
            <div v-if="g.StartedMins>=0" class="gameStatus">已开始{{ g.StartedMins }}分钟</div>
            <div v-else class="gameStatus">正在等人</div>
        </td>
        <td>
            <button @click="enter(g.Data.Id||0)">进入</button>
            <button v-if="g.Data.HostUserId==me && g.StartedMins===-1" @click="deleteGame(g.Data.Id||0)" class="cancel">删除</button>
        </td>
        <td>
            {{ g.HostUserName }}
        </td>
    </tr>
    <tr><td colspan="3">已结束的棋局会被封存，请在战绩页面使用回放功能</td></tr>
</table>
<Loading v-else></Loading>
<SideBar ref="sidebar">
    <h1>创建棋局</h1>
    <table v-if="creating">
        <tr>
            <td>随机数</td>
            <td>
                下界 <input v-model="creating.RandMin" type="number" min="0" max="13"/><br/>
                上界 <input v-model="creating.RandMax" type="number" min="3" max="16"/>
            </td>
        </tr>
        <tr>
            <td>出局所需<br/>卡住次数</td>
            <td>
                <input v-model="creating.StucksToLose" type="number">
            </td>
        </tr>
        <tr>
            <td>可终点折返</td>
            <td>暂不支持设定</td>
            <!-- <td><input type="checkbox" v-model="creating.AllowReverseAtTerminal"/></td> -->
        </tr>
        <tr>
            <td>可中途换乘</td>
            <td><input type="number" v-model="creating.AllowTransfer" min="0" max="2"></td>
        </tr>
        <tr>
            <td>允许跨过<br/>其他玩家<br/>占领车站</td>
            <td>暂不支持设定</td>
        </tr>
        <tr>
            <td>每回合瞬移</td>
            <td>暂不支持设定</td>
        </tr>
        <tr>
            <td colspan="2">
                <Search :source="api.map.quickSearch" :allow-free-input="false" 
                    @done="(_x,id)=>{if(creating){creating.UseMapId=id;create()}}" :placeholder="'使用棋盘名称'"></Search>
            </td>
        </tr>
    </table>
</SideBar>
</template>

<style scoped>
.mapName{
    font-weight: bold;
    cursor: pointer;
}
.gameStatus{
    color:#666;
    font-size: 12px;
}
input[type=number]{
    width: 80px;
}
.mapCol{
    width: 50%;
}
table.list{
    table-layout: fixed;
    width: 100%;
}
</style>