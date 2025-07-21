<script setup lang="ts">
import { onMounted, ref } from 'vue';
import SideBar from '../components/SideBar.vue';
import { AiPlayerType, GameActiveResult, RailChessGame, RandAlgType, SpawnRuleType } from '../models/game';
import { injectApi, injectPop, injectUserInfo } from '../provides';
import Loading from '../components/Loading.vue';
import Search from '../components/Search.vue';
import { router } from '../main';
import { useFeVersionChecker } from '../utils/feVersionCheck';
import { contact } from '../utils/consts';

const api = injectApi()
const pop = injectPop()
const me = ref<number>(0)
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
        creating.value.AllowUserIdCsv = getAllowUserCsv();
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

const allowUsers = ref<{name:string, id:number}[]>([])
function addAllowUser(name:string, id:number){
    if(allowUsers.value.length>=10){
        pop.value.show('最多10个用户', 'failed')
        return;
    }
    if(!allowUsers.value.some(x=>x.id == id))
        allowUsers.value.push({name, id})
}
function removeAllowUser(id:number){
    const idx = allowUsers.value.findIndex(x=>x.id==id)
    if(idx>=0){
        allowUsers.value.splice(idx, 1)
    }
}
function getAllowUserCsv(){
    return allowUsers.value.map(x=>x.id).join(',')
}

onMounted(async ()=>{
    me.value = (await injectUserInfo().getIdentityInfo()).Id;
    await loadActive();
    creating.value = {
        UseMapId: 0,
        RandAlg: RandAlgType.Uniform,
        RandMin: 1,
        RandMax: 12,
        StucksToLose: 5,
        AllowReverseAtTerminal:false,
        AllowTransfer:1,
        AiPlayer:AiPlayerType.None,
        SpawnRule:SpawnRuleType.Terminal,
        ThinkSecsPerTurn: 60,
        ThinkSecsPerGame: 0,
        AllowUserIdCsv: ''
    }
})
</script>

<template>
<h1>正在进行</h1>
<button class="confirm" @click="sidebar?.extend">新建棋局</button>
<table v-if="active" class="list"><tbody>
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
    <tr><td colspan="3" class="notice">
        注意：本游戏玩家较少<br/>
        请<b>务必</b>在约好其他玩家后，再创建房间，不要创建房间后离开<br/>
        请<b>务必</b>在确认房主在线后，再加入对局，以免开局被房主踢出<br/>
        如果找不到一起玩的人，请考虑：{{ contact }}
    </td></tr>
</tbody></table>
<Loading v-else></Loading>
<SideBar ref="sidebar">
    <h1>创建棋局</h1>
    <table v-if="creating"><tbody>
        <tr>
            <td>随机数</td>
            <td>
                下界 <input v-model="creating.RandMin" type="number" min="1" max="13"/><br/>
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
            <td>开局起始点</td>
            <td>
                <select v-model="creating.SpawnRule">
                    <option :value="SpawnRuleType.Terminal">单线终点站</option>
                    <option :value="SpawnRuleType.TwinExchange">双线换乘站</option>
                </select>
            </td>
        </tr>
        <tr>
            <td>随机算法</td>
            <td>
                <select v-model="creating.RandAlg">
                    <option :value="RandAlgType.Uniform">均匀分布</option>
                    <option :value="RandAlgType.UniformAB">均匀分布 双步数</option>
                    <option :value="RandAlgType.AlwaysNegativeOne">仅-1（自由移动）</option>
                </select>
            </td>
        </tr>
        <tr>
            <td>Ai玩家</td>
            <td>暂不支持设定</td>
        </tr>
        <tr>
            <td>
                玩家回合<br/>时间限制
            </td>
            <td>
                <input v-model="creating.ThinkSecsPerTurn" placeholder="单位为秒" type="number"/>秒
            </td>
        </tr>
        <tr>
            <td>允许加入<br/>玩家名单</td>
            <td>
                <div class="allowUsers">
                    <div v-for="u in allowUsers" class="allowUser">
                        <span>{{ u.name }}</span>
                        <button @click="removeAllowUser(u.id)" class="cancel">x</button>
                    </div>
                </div>
                <Search :source="api.identites.user.quickSearch" :allow-free-input="false"
                    @done="addAllowUser" :compact="true" :placeholder="'玩家名称'"></Search>
                <div class="note">留空：任何人可加入</div>
                <div class="note">无需设置房主自己</div>
            </td>
        </tr>
        <tr>
            <td>对局名称</td>
            <td>
                <input v-model="creating.GameName" placeholder="非必填"/>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <Search :source="api.map.quickSearch" :allow-free-input="false" 
                    @done="(_x,id)=>{if(creating){creating.UseMapId=id;create()}}" :placeholder="'使用棋盘名称'"></Search>
                <div class="note">选择棋盘后，点击右侧确认，即可创建房间</div>
            </td>
        </tr>
    </tbody></table>
</SideBar>
</template>

<style scoped>
.note{
    font-size: 12px;
    color: #666;
}
.allowUsers{
    margin-bottom: 3px;
}
.allowUser{
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 5px;
    padding: 3px;
    border-bottom: 1px solid #aaa;
}
.allowUser>span{
    max-width: 110px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.mapName{
    font-weight: bold;
    cursor: pointer;
}
.gameStatus{
    color:#666;
    font-size: 12px;
}
input{
    width: 140px;   
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
.notice{
    font-size: 14px;
    color: #666
}
</style>