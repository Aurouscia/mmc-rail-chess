<script setup lang="ts">
import { CSSProperties, onMounted, ref } from 'vue';
import { injectHideTopbar, injectUserInfo } from '../provides';
import { Player, SyncData } from '../models/play';
import SideBar from '../components/SideBar.vue';

const playerList = ref<Player[]>([]);
function sync(data:SyncData){
    playerList.value = data.PlayerStatus;
    renderPlayerList();
}
function avtUrl(fileName:string){
    const baseUrl = import.meta.env.VITE_BASEURL;
    return baseUrl+"/avts/"+fileName;
}

const playerRenderedWidth = 167;
interface PlayerRendered{
    p:Player,
    style:CSSProperties,
    nameStyle:CSSProperties,
    order:number
}
const playerRenderedList = ref<PlayerRendered[]>([]);
function renderPlayerList(){
    for(var i=0;i<playerList.value.length;i++){
        const p = playerList.value[i];
        const left = playerRenderedWidth*i;
        const order = i;
        const existing = playerRenderedList.value.find(x=>x.p.Id==p.Id);
        if(existing){
            existing.order = i;
            if(i==playerList.value.length-1){
                existing.style.opacity = 0;
                existing.style.zIndex = 0;
                setTimeout(()=>{
                    existing.style.left = left+"px"
                    setTimeout(()=>{
                        existing.style.opacity = 1;
                    },500)
                },500)
            }else{
                existing.style.opacity = 1;
                existing.style.left = left+"px";
                existing.style.zIndex = 100
                if(i==0 && me==existing.p.Id){
                    attention(existing.nameStyle);
                }
            }
        }else{
            playerRenderedList.value.push({
                p,
                style:{left:left+"px"},
                nameStyle:{},
                order
            })
        }
    }
}
function attention(style:CSSProperties){
    var flag = false;
    var counter = 0;
    const timer = setInterval(()=>{
        if(flag){
            style.backgroundColor = "#ff0000";
            style.color = "white";
        }else{
            style.backgroundColor = undefined;
            style.color = undefined;
        }
        flag = !flag;
        counter++;
        if(counter>6){
            clearInterval(timer);
        }
    },200)
}

const sidebar = ref<InstanceType<typeof SideBar>>();
var me:number;
onMounted(async()=>{
    injectHideTopbar()();
    //const mInfo = await injectUserInfo().getIdentityInfo();
    //me = mInfo.Id;

    playerList.value = [
        {Id:1,Name:"四氨合铜离子",Score:666,StuckTimes:1},
        {Id:2,Name:"Bu",Score:777},
        {Id:3,Name:"Cu",Score:888}
    ]
    me = 1;
    renderPlayerList()
})
function temp(){
    const p = playerList.value.splice(0,1);
    playerList.value.push(...p);
    renderPlayerList();
}
</script>

<template>
<div class="topbar" @click="temp">
    <div class="playerList">
        <div v-for="p in playerRenderedList" class="player" :key="p.p.Id" :style="p.style">
            <img :src="avtUrl(p.p.AvtFileName)"/>
            <div>
                <div :style="p.nameStyle" class="playerName">{{ p.p.Name }}</div>
                <div class="playerData">{{ p.p.Score }}分 <span v-show="p.p.StuckTimes">卡{{ p.p.StuckTimes }}次</span></div>
            </div>
        </div>
    </div>
</div>
<button class="confirm menuEntry" @click="sidebar?.extend">菜单</button>
<SideBar ref="sidebar"></SideBar>
</template>

<style scoped>
.menuEntry{
    position: fixed;
    right:7px;
    top:65px;
    width: 65px;
    border: 2px solid white;
}
.playerData{
    font-size: 14px;
    color:#999
}
.playerName{
    padding: 2px;
    font-weight: bold;
    max-width: 96px;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}
.player img{
    height: 40px;
    width: 40px;
    border-radius: 1000px;
    border:1px solid #ccc;
    flex-shrink: 0;
}
.player{
    width: 146px;
    padding: 5px;
    border: 2px solid black;
    border-radius: 5px;
    position: absolute;
    top:0px;bottom: 0px;
    display: flex;
    gap:5px;
    align-items: center;
    transition: 500ms;
    background-color: white;
    box-shadow: 0px 0px 5px 0px black;
}
.playerList{
    position: relative;
    height: 60px;
    width: calc(100vw - 100px);
    display: flex;
    gap:5px;
}
.topbar{
    position: fixed;
    top:0px;
    left:0px;
    right: 0px;
    height: 70px;
    background-color: #eee;
    display: flex;
    align-items: center;
    padding: 10px;
}
</style>