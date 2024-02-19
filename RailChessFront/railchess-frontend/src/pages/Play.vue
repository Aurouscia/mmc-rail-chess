<script setup lang="ts">
import { CSSProperties, onMounted, ref, watch } from 'vue';
import { injectApi, injectHideTopbar, injectHttp, injectPop, injectUserInfo } from '../provides';
import { OcpStatus, Player, SyncData, TextMsg } from '../models/play';
import SideBar from '../components/SideBar.vue';
import { RailChessTopo,posBase } from '../models/map';
import { Api } from '../utils/api';
import { RailChessGame } from '../models/game';
import { avtSrc,bgSrc } from '../utils/fileSrc';
import { Scaler } from '../models/scale';
import { SignalRClient } from '../utils/signalRClient';
import TextMsgDisplay from '../components/TextMsgDisplay.vue';
import { useAnimator, AnimNode } from '../utils/pathAnim';
import _, { truncate } from 'lodash'
import { boxTypes } from '../components/Pop.vue';
import { sleep } from '../utils/sleep';

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

const staRenderedWidth = 30;
interface StaRendered{
    id:number,
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
        var side = staRenderedWidth;
        var backgroundImage:string|undefined = undefined;
        var zIndex = undefined;
        var shadow = undefined;

        var atByPlayer = playerList.value.find(x=>x.atSta==id);
        var occupiedByPlayer = ocpStatus.value.find(x=>x.stas.includes(id));
        var isNewOcp = newOcps.value?.stas.includes(id);
        if(atByPlayer){
            side = staRenderedWidth * 2;
            backgroundImage = `url("${avtSrc(atByPlayer.avtFileName)}")`;
            zIndex = 21;
        }
        else if(occupiedByPlayer){
            const player = playerList.value.find(x=>x.id==occupiedByPlayer?.playerId);
            if(player){
                side = staRenderedWidth *1.3;
                backgroundImage = `url("${avtSrc(player.avtFileName)}")`;
                zIndex = 13;
                if(isNewOcp){
                    zIndex = 14;
                    shadow = "0px 0px 5px 5px orange"
                }
            }
        }
        const style:CSSProperties = {
            left:x-side/2+'px',
            top:y-side/2+'px',
            width:side-4+'px',
            height:side-4+'px',
            backgroundImage,
            boxShadow:shadow,
            zIndex
        };
        const existing = staRenderedList.value.find(x=>x.id==id);
        if(existing){
            existing.style = style;
        }else{
            staRenderedList.value.push({
                id,
                style
            })
        }
    }
}

const pathAnimRenderedWidth = 26;
const { animatorRendered, setPaths } = useAnimator();
const selectedDist = ref<number|undefined>();
function renderPathAnims() {
    if (!topoData.value || !arena.value) { return; }
    if(!currentSelections.value || selectedDist.value===undefined){return;}
    const path = currentSelections.value.find(s=>s[s.length-1] == selectedDist.value);
    if(!path){return;}
    var backgroundImage: string | undefined = undefined;
    var side = pathAnimRenderedWidth;
    var nodes: AnimNode[] = [];
    const getPos = (sta:number)=>{
            const s = topoData.value!.Stations.find(x=>x[0]==sta);
            if(!s)return undefined
            const x = s[1] / posBase * arena.value!.clientWidth;
            const y = s[2] / posBase * arena.value!.clientHeight;
            const left = x - side / 2 + 'px';
            const top = y - side / 2 + 'px';
            return {left, top}
    }
    path.forEach(sta => {
        nodes.push({getPos,sta});
    });
    const styleBase: CSSProperties = {
        width: side - 4 + 'px',
        height: side - 4 + 'px',
        borderWidth: '2px',
        backgroundImage,
    };
    const animPath = {
        dist:path[path.length-1],
        styleBase,
        nodes: nodes
    }
    setPaths(animPath);
}
function clickStation(id:number){
    if(!clickableStations.value.includes(id)){
        selectedDist.value = undefined;
        return;
    }
    selectedDist.value = id
    renderPathAnims();
}
async function select(){
    if(!currentSelections.value || currentSelections.value.length==0){
        await sgrc.select([]);
        return;
    }
    const path = currentSelections.value.find(x=>x.length>1 && x[x.length-1]==selectedDist.value);
    if(path){
        await sgrc.select(path);
        setPaths();
    }
}

const randNumStyle = ref<CSSProperties>({});
function changeRandNum(to:number){
    if(!randNumStyle.value){
        randNumStyle.value = {};
    }
    randNumStyle.value.opacity = 0;
    setTimeout(()=>{
        randNum.value = to;
        randNumStyle.value.opacity = 1;
    },500)
}

const bgFileName = ref<string>();
const topoData = ref<RailChessTopo>();
const gameInfo = ref<RailChessGame>();
const meJoined = ref<boolean>(false);
const gameStarted = ref<boolean>(false);
const meHost = ref<boolean>(false);
const currentSelections = ref<number[][]|undefined>([])
const clickableStations = ref<number[]>([])
const ocpStatus = ref<OcpStatus[]>([])
const newOcps = ref<OcpStatus|undefined>();
const randNum = ref<number>(0);
async function sync(data:SyncData){
    console.log("收到同步数据指令",data)
    const lastPlayer = playerList.value[0];
    playerList.value = data.playerStatus;
    const newPos = playerList.value.find(x=>x.id==lastPlayer?.id)?.atSta;
    selectedDist.value = newPos;
    renderPathAnims();
    await sleep(100);

    meJoined.value = playerList.value.some(x=>x.id==me);
    meHost.value = gameInfo.value?.HostUserId == me;
    gameStarted.value = data.gameStarted;
    currentSelections.value = data.selections;
    ocpStatus.value = data.ocps;
    newOcps.value = data.newOcps;
    changeRandNum(data.randNumber);
    if(currentSelections.value){
        renderPathAnims();
        clickableStations.value = currentSelections.value.map(x=>x[x.length-1]);
        console.log("更新可点击车站", clickableStations.value)
    }else{
        animatorRendered.value = undefined
        clickableStations.value = []
    }
    if(playerList.value.length==0){
        playerRenderedList.value = [];
        animatorRendered.value = undefined
    }else{
        renderPlayerList();
    }
    renderStaList();
    selectedDist.value = undefined;
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
const bgOpacity = ref<number>(0.5);


var api:Api;
var me:number;
var jwtToken:string|null;
var sgrc:SignalRClient
onMounted(async()=>{
    injectHideTopbar()();
    api = injectApi();
    var http = injectHttp();
    const pop = injectPop();
    jwtToken = http.jwtToken;
    
    const mInfo = await injectUserInfo().getIdentityInfo();
    me = mInfo.Id;
    const textMsgCall = (m:TextMsg)=>{
        msgs.value.push(m);
        var t:boxTypes;
        if(m.type==0){
            t = "info"
        }else if(m.type==1){
            t = "warning"
        }else{
            t = "failed"
        }
        if(m.sender!=mInfo.Name)
            pop.value.show(truncate(m.sender+"："+m.content, {length:30}), t)
    }
    sgrc = new SignalRClient(gameId,jwtToken||"", sync, textMsgCall);

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
<div class="randNumOuter">
    <div v-show="gameStarted" class="randNum" :style="randNumStyle">{{ randNum }}</div>
    <div v-show="gameStarted" class="status">{{playerList[0]?.id!=me ? '▲等待玩家': '▲轮到你了'}}</div>
    <div v-show="!gameStarted" class="status">等待房主开始中</div>
</div>
<div class="frame" ref="frame">
    <div class="arena" ref="arena">
        <img :src="bgSrc(bgFileName||'')" :style="{opacity:bgOpacity}"/>
        <div v-for="s in staRenderedList" :style="s.style" 
            :class="{clickable:clickableStations.includes(s.id)&&playerList[0]?.id==me, selected:selectedDist==s.id}" 
            :key="s.id" class="station" @click="clickStation(s.id)">{{ s.id }}</div>
        <div v-if="animatorRendered" :style="animatorRendered.style" class="pathAnim">{{ animatorRendered.step }}</div>
    </div>
</div>
<button class="confirm menuEntry" @click="sidebar?.extend">菜单</button>
<div class="scaleBtn">
    <input v-model="scaleBar" type="range" min="0" max="1" step="0.05"/>
</div>
<button v-show="selectedDist" class="decideBtn" @click="select">确认选择</button>
<button v-show="gameStarted && playerList[0]?.id==me && !currentSelections?.length" class="cancel decideBtn" @click="select">无路可走</button>
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
.status{
    color:white;
    white-space: nowrap;
}
.randNumOuter{
    position: fixed;
    top:70px;
    left:8px;
    width: 160px;
    height: 50px;
    background-color: cornflowerblue;
    border: 2px solid white;
    border-radius: 1000px;
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: left;
    gap:10px;
    box-sizing: border-box;
    padding: 0px 10px 0px 20px;
    overflow: hidden;
}
.randNum{
    text-align: center;
    font-size: 35px;
    color:white;
    transition: 500ms;
}
.sidebarBtns{
    width: 200px;
    margin: 0px auto 10px auto;
    display: flex;
    flex-direction: column;
    gap:5px;
}
.decideBtn{
    position: fixed;
    left:15px;
    bottom: 30px;
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
.pathAnim{
    position: absolute;
    border:2px solid black;
    background-color: black;
    background-position: center;
    background-size: contain;
    border-radius: 1000px;
    z-index: 50;
    left: -100px;
    color:white;
    line-height: 26px;
    font-size: 20px;
    text-align: center;
}
.station.clickable{
    border-color: rgb(71, 171, 174);
    background-color: rgb(51, 118, 120);
    
    animation-duration: 1s;
    animation-name: stationSpark;
    animation-iteration-count: infinite;
    animation-direction: alternate;
    cursor: pointer;
    z-index: 10;
}
.station.selected{
    border-color: rgb(0, 201, 0);
    background-color: green;
    box-shadow: 0px 0px 15px 10px green;
    animation: none;
    z-index: 20 !important;
}
@keyframes stationSpark {
  from {box-shadow:0px 0px 5px 5px cadetblue}
  to {box-shadow:0px 0px 10px 10px cadetblue}
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
    background-color: black;
    color:white;
    border-radius: 5px;
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