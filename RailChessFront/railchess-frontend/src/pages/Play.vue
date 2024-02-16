<script setup lang="ts">
import { CSSProperties, onMounted, ref, watch } from 'vue';
import { injectApi, injectHideTopbar, injectHttp, injectUserInfo } from '../provides';
import { Player, SyncData, TextMsg } from '../models/play';
import SideBar from '../components/SideBar.vue';
import { RailChessTopo,posBase } from '../models/map';
import { Api } from '../utils/api';
import { RailChessGame } from '../models/game';
import { avtSrc,bgSrc } from '../utils/fileSrc';
import { Scaler } from '../models/scale';
import { SignalRClient } from '../utils/signalRClient';
import TextMsgDisplay from '../components/TextMsgDisplay.vue';

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
        const existing = playerRenderedList.value.find(x=>x.p.id==p.id);
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
                if(i==0 && me==existing.p.id && gameStarted.value){
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
        const id = s[0];
        const x = s[1]/posBase*aw;
        const y = s[2]/posBase*ah;
        var side = 30;
        var backgroundImage:string|undefined = undefined;
        var zIndex = 1;

        var atByPlayer = playerList.value.find(x=>x.atSta==id);
        if(atByPlayer){
            side = 60;
            backgroundImage = `url("${avtSrc(atByPlayer.avtFileName)}")`;
            zIndex += 1;
        }
        const style:CSSProperties = {
            left:x-side/2+'px',
            top:y-side/2+'px',
            width:side-4+'px',
            height:side-4+'px',
            borderWidth:'2px',
            backgroundImage,
            zIndex
        };
        const existing = staRenderedList.value.find(x=>x.id==id);
        if(existing){
            existing.x = x;
            existing.y = y;
            existing.side = side;
            existing.style = style;
        }else{
            staRenderedList.value.push({
                id,x,y,
                side,
                style
            })
        }
    }
}

const bgFileName = ref<string>();
const topoData = ref<RailChessTopo>();
const gameInfo = ref<RailChessGame>();
const meJoined = ref<boolean>(false);
const gameStarted = ref<boolean>(false);
const meHost = ref<boolean>(false);
function sync(data:SyncData){
    console.log("收到同步数据指令",data)
    playerList.value = data.playerStatus;
    meJoined.value = playerList.value.some(x=>x.id==me);
    meHost.value = gameInfo.value?.HostUserId == me;
    gameStarted.value = data.gameStarted;
    if(playerList.value.length==0){
        playerRenderedList.value = [];
    }else{
        renderPlayerList();
    }
    renderStaList();
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

const sending = ref<string>("");
function send(){
    if(sending.value && sending.value.trim()){
        sgrc.sendTextMsg(sending.value);
        sending.value = "";
    }
}

const sidebar = ref<InstanceType<typeof SideBar>>();
const msgs = ref<TextMsg[]>([]);
const frame = ref<HTMLDivElement>();
const arena = ref<HTMLDivElement>();
const bgOpacity = ref<number>(1);


var api:Api;
var me:number;
var jwtToken:string|null;
var sgrc:SignalRClient
onMounted(async()=>{
    injectHideTopbar()();
    api = injectApi();
    var http = injectHttp();
    jwtToken = http.jwtToken;
    const textMsgCall = (m:TextMsg)=>{msgs.value.push(m)}
    sgrc = new SignalRClient(gameId,jwtToken||"", sync, textMsgCall);

    const mInfo = await injectUserInfo().getIdentityInfo();
    me = mInfo.Id;
    
    bgOpacity.value = parseFloat(localStorage.getItem(opacityStoreKey)||"1")||1;

    await init();
    await sgrc.connect();
    await sgrc.enter();

    if(frame.value && arena.value){
        scaler = new Scaler(frame.value,arena.value,renderStaList);
    }

    window.addEventListener("keypress",(ev)=>{
        if(ev.key=="Enter"){
            send();
        }
    })
})

var scaler:Scaler|undefined
const scaleBar = ref<number>(0);
watch(scaleBar,(newVal,oldVal)=>{
    if(newVal==0){
        scaler?.reset();return;
    }
    if(newVal>oldVal){
        scaler?.scale(1.1);
    }else if(newVal<oldVal){
        scaler?.scale(1/1.1);
    }
})

const opacityStoreKey = "bgOpacity";
watch(bgOpacity,(newVal)=>{
    localStorage.setItem(opacityStoreKey,String(newVal));
})
</script>

<template>
<div class="topbar">
    <div class="playerList">
        <div v-for="p in playerRenderedList" class="player" :key="p.p.id" :style="p.style">
            <img :src="avtSrc(p.p.avtFileName)"/>
            <div>
                <div :style="p.nameStyle" class="playerName" :class="{meName:me==p.p.id}">{{ p.p.name }}</div>
                <div class="playerData">{{ p.p.score }}分 <span v-show="p.p.stuckTimes">卡{{ p.p.stuckTimes }}次</span></div>
            </div>
        </div>
    </div>
</div>
<div class="frame" ref="frame">
    <div class="arena" ref="arena">
        <img :src="bgSrc(bgFileName||'')" :style="{opacity:bgOpacity}"/>
        <div v-for="s in staRenderedList" :style="s.style" class="station"></div>
    </div>
</div>
<button class="confirm menuEntry" @click="sidebar?.extend">菜单</button>
<div class="scaleBtn">
    <input v-model="scaleBar" type="range" min="0" max="1" step="0.05"/>
</div>
<SideBar ref="sidebar">
    <TextMsgDisplay :msgs="msgs"></TextMsgDisplay>
    <div>
        <input v-model="sending" placeholder="说点什么"/>
        <button @click="send">发送</button>
    </div>
    <div class="sidebarBtns">
        <div style="text-align: center;">
            背景不透明度
            <input type="range" v-model="bgOpacity" min="0" max="1" step="0.1" style="margin: 0px;">
        </div>
        <button v-show="!gameStarted && !meJoined" @click="sgrc.join">加入本棋局</button>
        <button v-show="meJoined" class="minor">已在棋局中</button>
        <button v-show="meHost && !gameStarted && meJoined" @click="sgrc.gameStart">下令开始棋局</button>
        <button v-show="gameStarted" class="minor">本棋局已开始</button>
        <button v-show="meHost" class="cancel" @click="sgrc.gameReset">下令重置房间</button>
    </div>
</SideBar>
</template>

<style scoped>
.sidebarBtns{
    width: 200px;
    margin: 0px auto 10px auto;
    display: flex;
    flex-direction: column;
    gap:5px;
}
.scaleBtn input[type="range"] {
  writing-mode:vertical-lr;
  height: 180px;
}
.scaleBtn{
    position: fixed;
    right: 15px;
    bottom: 15px;
    width: 40px;
    height: 200px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    gap:0px;
}
.station{
    position: absolute;
    border:2px solid black;
    background-color: #ccc;
    background-position: center;
    background-size: contain;
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
.meName{
    text-decoration: underline;
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