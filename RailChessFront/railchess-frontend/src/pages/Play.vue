<script setup lang="ts">
import { CSSProperties, onMounted, ref } from 'vue';
import { injectApi, injectHideTopbar, injectUserInfo } from '../provides';
import { Player, SyncData } from '../models/play';
import SideBar from '../components/SideBar.vue';
import { RailChessTopo,posBase } from '../models/map';
import { Api } from '../utils/api';
import { RailChessGame } from '../models/game';
import { avtSrc,bgSrc } from '../utils/fileSrc';
import { Scaler } from '../models/scale';

const props = defineProps<{id:string}>();
const gameId = parseInt(props.id);

const playerList = ref<Player[]>([]);

const playerRenderedWidth = 167;
interface PlayerRendered{
    p:Player,
    style:CSSProperties,
    nameStyle:CSSProperties
}
const playerRenderedList = ref<PlayerRendered[]>([]);
function renderPlayerList(){
    for(var i=0;i<playerList.value.length;i++){
        const p = playerList.value[i];
        const left = playerRenderedWidth*i;
        const existing = playerRenderedList.value.find(x=>x.p.Id==p.Id);
        if(existing){
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
                    textAttention(existing.nameStyle);
                }
            }
        }else{
            playerRenderedList.value.push({
                p,
                style:{left:left+"px"},
                nameStyle:{}
            })
        }
    }
}
function textAttention(style:CSSProperties){
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

interface StaRendered{
    id:number,
    x:number,
    y:number,
    side:number,
    style:CSSProperties
}
const staRenderedList = ref<StaRendered[]>([]);
function renderStaList(){
    if(!topoData.value || !arena.value){return;}
    const aw = arena.value.clientWidth;
    const ah = arena.value.clientHeight;
    for(var i=0;i<topoData.value.Stations.length;i++){
        const s = topoData.value.Stations[i];
        const x = s[1]/posBase*aw;
        const y = s[2]/posBase*ah;
        const side = 30;
        const style = {
            left:x-side/2+'px',
            top:y-side/2+'px',
            width:side-4+'px',
            height:side-4+'px',
            borderWidth:'2px'
        };
        const existing = staRenderedList.value.find(x=>x.id==s[0]);
        if(existing){
            existing.x = x;
            existing.y = y;
            existing.side = side;
            existing.style = style;
        }else{
            staRenderedList.value.push({
                id:s[0],
                x,y,
                side,
                style
            })
        }
    }
}

const bgFileName = ref<string>();
const topoData = ref<RailChessTopo>();
const gameInfo = ref<RailChessGame>();
function sync(data:SyncData){
    playerList.value = data.PlayerStatus;
    renderPlayerList();
}
async function init(){
    const resp = await api.game.init(gameId);
    if(resp){
        bgFileName.value = resp.BgFileName;
        topoData.value = JSON.parse(resp.TopoData);
        gameInfo.value = resp.GameInfo;
    }
    setTimeout(()=>{renderStaList()},100)
}

const sidebar = ref<InstanceType<typeof SideBar>>();
const frame = ref<HTMLDivElement>();
const arena = ref<HTMLDivElement>();
var api:Api;
var me:number;
onMounted(async()=>{
    injectHideTopbar()();
    api = injectApi();
    const mInfo = await injectUserInfo().getIdentityInfo();
    me = mInfo.Id;
    init();
    if(frame.value && arena.value){
        scaler = new Scaler(frame.value,arena.value,renderStaList);
    }

    if(1/3==8){
        const a:any={};
        sync(a);
    }
})
var scaler:Scaler|undefined
</script>

<template>
<div class="topbar">
    <div class="playerList">
        <div v-for="p in playerRenderedList" class="player" :key="p.p.Id" :style="p.style">
            <img :src="avtSrc(p.p.AvtFileName)"/>
            <div>
                <div :style="p.nameStyle" class="playerName">{{ p.p.Name }}</div>
                <div class="playerData">{{ p.p.Score }}分 <span v-show="p.p.StuckTimes">卡{{ p.p.StuckTimes }}次</span></div>
            </div>
        </div>
    </div>
</div>
<div class="frame" ref="frame">
    <div class="arena" ref="arena">
        <img :src="bgSrc(bgFileName||'')"/>
        <div v-for="s in staRenderedList" :style="s.style" class="station"></div>
    </div>
</div>
<button class="confirm menuEntry" @click="sidebar?.extend">菜单</button>
<div class="scaleBtn">
    <button class="confirm" @click="scaler?.scale(1.2)">+</button>
    <button class="confirm" @click="scaler?.scale(0.8)">-</button>
</div>
<SideBar ref="sidebar">
    提示：PC端可按住ctrl或shift在图上点击来放大缩小
</SideBar>
</template>

<style scoped>
.scaleBtn button{
    font-size: 30px;
    line-height: 25px;
    height: 38px;
}
.scaleBtn{
    position: fixed;
    right: 15px;
    bottom: 15px;
    width: 40px;
    height: 80px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    gap:0px;
}
.station{
    position: absolute;
    border:2px solid black;
    background-color: #ccc;
    border-radius: 1000px;
}
.arena img{
    position: relative;
    width: 100%;
    margin-bottom: -4px;
}
.arena{
    position: relative;
    width: 90vw;
}
.frame{
    position: fixed;
    top:90px;left:0px;right:0px;bottom: 0px;
    overflow: scroll;
    user-select: none;
}
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