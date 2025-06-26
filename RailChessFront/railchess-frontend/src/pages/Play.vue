<script setup lang="ts">
import { computed, CSSProperties, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { injectApi, injectHideTopbar, injectHttp, injectPop, injectUserInfo } from '../provides';
import { OcpStatus, Player, SyncData, TextMsg } from '../models/play';
import SideBar from '../components/SideBar.vue';
import { RailChessTopo,Sta,posBase } from '../models/map';
import { Api } from '../utils/api';
import { GameTimeline, RailChessGame } from '../models/game';
import { avtSrc,bgSrc } from '../utils/fileSrc';
import { Scaler } from '../models/scale';
import { SignalRClient } from '../utils/signalRClient';
import TextMsgDisplay from '../components/TextMsgDisplay.vue';
import { useAnimator, AnimNode } from '../utils/pathAnim';
import _, { truncate } from 'lodash'
import { boxTypes } from '../components/Pop.vue';
import Timeline from '../components/Timeline.vue';

const props = defineProps<{
    id:string,
    playback?:string
}>();
const gameId = parseInt(props.id);

const playerList = ref<Player[]>([]);

const playerRenderedWidth = 167;
interface PlayerRendered{
    p:Player,
    idx:number,
    style:CSSProperties,
    nameStyle:CSSProperties
}
const playerRenderedList = ref<PlayerRendered[]>([]);
const animTimers:number[] = []
function renderPlayerList(){
    const playerListSplitted:Player[] = []
    playerListSplitted.push(...playerList.value.filter(p=>!p.out))
    playerListSplitted.push(...playerList.value.filter(p=>p.out))
    animTimers.forEach(t=>{
        clearTimeout(t)
    })
    animTimers.length = 0;
    for(var i=0;i<playerListSplitted.length;i++){
        const p = playerListSplitted[i];
        const left = playerRenderedWidth*i;
        const existing = playerRenderedList.value.find(x=>x.p.id==p.id);
        if(existing){
            existing.p = p;
            if(i > existing.idx){
                existing.style.opacity = 0;
                existing.style.zIndex = 0;
                const moveTimer = setTimeout(()=>{
                    existing.style.left = left+"px"
                    const emergeTimer = setTimeout(()=>{
                        existing.style.opacity = undefined;
                        existing.style.zIndex = undefined;
                    },100)
                    animTimers.push(emergeTimer)
                },500)
                animTimers.push(moveTimer)
            }else{
                existing.style.opacity = undefined;
                existing.style.zIndex = undefined;
                existing.style.left = left+"px";
                if(i==0 && me==existing.p.id && gameStarted.value){
                    textAttention(existing.nameStyle);
                }
            }
            existing.idx = i;
        }else{
            playerRenderedList.value.push({
                p,
                idx: playerRenderedList.value.length,
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
type CSSWithSId = CSSProperties & {sId:number}
type StaRendered = Record<number, CSSWithSId>
const staRenderedList = ref<StaRendered>({});
function renderStaList(){
    if(!topoData.value || !arena.value){return;}
    const aw = arena.value.clientWidth;
    const ah = arena.value.clientHeight;
    autoStaSize();
    const size = staRenderedWidth*staSize.value;
    for(var i=0;i<topoData.value.Stations.length;i++){
        const s:Sta = topoData.value.Stations[i];
        const id = s[0];
        const x = s[1]/posBase*aw;
        const y = s[2]/posBase*ah;
        var side = size;
        var backgroundImage:string|undefined = undefined;
        var zIndex = undefined;
        var shadow = undefined;

        var atByPlayer = playerList.value.find(x=>x.atSta==id);
        var occupiedByPlayer = ocpStatus.value.find(x=>x.stas.includes(id));
        var isNewOcp = newOcps.value?.stas.includes(id);
        if(atByPlayer){
            side = size * 2;
            backgroundImage = `url("${avtSrc(atByPlayer.avtFileName)}")`;
            zIndex = 21;
            if(atByPlayer.id === newOcps.value?.playerId){
                shadow = "0px 0px 5px 5px black"
            } 
        }
        else if(occupiedByPlayer){
            const player = playerList.value.find(x=>x.id==occupiedByPlayer?.playerId);
            if(player){
                side = size *1.3;
                backgroundImage = `url("${avtSrc(player.avtFileName)}")`;
                zIndex = 13;
            }
        }
        if(isNewOcp){
            zIndex = 14;
            shadow = "0px 0px 5px 5px orange"
        }
        const style:CSSWithSId = {
            sId: id,
            left:x-side/2+'px',
            top:y-side/2+'px',
            width:side-4+'px',
            height:side-4+'px',
            backgroundImage,
            boxShadow:shadow,
            zIndex
        };
        staRenderedList.value[id] = style;
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
    if(props.playback){
        return;
    }
    if(!clickableStations.value.includes(id)){
        selectedDist.value = undefined;
        return;
    }
    selectedDist.value = id
    renderPathAnims();
}
async function select(){
    if(props.playback){
        return;
    }
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
let changeRandNumTimer = 0;
function changeRandNum(to:number){
    clearTimeout(changeRandNumTimer)
    if(!randNumStyle.value){
        randNumStyle.value = {};
    }
    randNumStyle.value.opacity = 0;
    changeRandNumTimer = setTimeout(()=>{
        randNum.value = to;
        randNumStyle.value.opacity = 1;
    },500)
}
//AB型随机数：三或四位数，个位十位、百位千位分别是A、B两个十位数，A和A+B都是可选项
const randNumIsAB = computed<boolean>(()=>{
    return randNum.value > 100
})
const randNumDisplay = computed<string>(()=>{
    if(randNumIsAB.value){
        const A = randNum.value % 100
        const B = Math.floor(randNum.value / 100)
        return `${A}/${A+B}`;
    }else{
        return randNum.value.toString();
    }
})

const sec = ref(0)
const secDisplay = computed<string|undefined>(()=>{
    if(sec.value < 0)
        return `超时${-sec.value}秒`
    else if(sec.value < 100)
        return `剩余${sec.value}秒`
})
const secDanger = computed<boolean>(()=>{
    return sec.value <= 10
})
window.setInterval(()=>{
    sec.value -= 1
}, 1000)

const bgFileName = ref<string>();
const topoData = ref<RailChessTopo>();
const gameInfo = ref<RailChessGame>();
const meJoined = ref<boolean>(false);
const meOut = ref<boolean>(false);
const gameStarted = ref<boolean>(false);
const meHost = ref<boolean>(false);
const currentSelections = ref<number[][]|undefined>([])
const clickableStations = ref<number[]>([])
const ocpStatus = ref<OcpStatus[]>([])
const newOcps = ref<OcpStatus|undefined>();
const randNum = ref<number>(0);
const ended = ref<boolean>(false);
async function sync(data:SyncData){
    console.log("收到同步数据指令",data)
    //当前currentSelections未更新，还是上个玩家的可选选项
    //当前playerList未更新，第一个还是上个玩家
    const lastPlayer = playerList.value[0];
    //找到上个玩家现在的新位置，并设置为动画目标（展示上个玩家走的路线）
    if(lastPlayer && lastPlayer.id !== me){
        const newPos = data.playerStatus.find(x=>x.id==lastPlayer?.id)?.atSta;
        selectedDist.value = newPos;
        renderPathAnims();
    }
    await nextTick()

    playerList.value = data.playerStatus;
    const meInList = playerList.value.find(x=>x.id==me);
    meJoined.value = !!meInList;
    meOut.value = meInList?.out||false;
    ended.value = playerList.value.length>0 && playerList.value.every(x=>x.out);
    meHost.value = gameInfo.value?.HostUserId == me;
    gameStarted.value = data.gameStarted;
    currentSelections.value = data.selections;
    ocpStatus.value = data.ocps;
    newOcps.value = data.newOcps;
    changeRandNum(data.randNumber);
    if(currentSelections.value){
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
    sec.value = data.leftSecsBeforeCanKick
}
async function init(){
    const resp = await api.game.init(gameId);
    if(resp){
        bgFileName.value = resp.BgFileName;
        topoData.value = JSON.parse(resp.TopoData);
        gameInfo.value = resp.GameInfo;
    }
    await nextTick();
    renderStaList()
    if(bg.value){
        bg.value.addEventListener('load', ()=>{
            renderStaList();
            clearTimeout(loadingBgTimer);
        })
    }
}
const bg = ref<HTMLImageElement>()
let loadingBgTimer = 0;

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
const bgOpacity = ref<number>(0.4);
const staSizeRatio = ref<number>(0.8);
const staSize = ref<number>(0.8)
function autoStaSize(){
    if(!frame.value || !arena.value){return;}
    const wr = arena.value.clientWidth/frame.value.clientWidth;
    const hr = arena.value.clientHeight/frame.value.clientHeight;
    const smallerR = Math.min(wr, hr)
    const biggerR = Math.max(wr, hr)
    staSize.value = smallerR*staSizeRatio.value*0.3
    if(biggerR<=0.9){
        staSize.value = 1
    }
    if(staSize.value < 0.4)
        staSize.value = 0.4
}

var api:Api;
var me:number;
var jwtToken:string|null;
var sgrc:SignalRClient
const moveLocked = ref(false)
const timeline = ref<GameTimeline>()
const msgDisp = ref<InstanceType<typeof TextMsgDisplay>>()
onMounted(async()=>{
    injectHideTopbar()();
    api = injectApi();
    var http = injectHttp();
    const pop = injectPop();
    jwtToken = http.jwtToken;
    
    loadingBgTimer = setTimeout(()=>{
        pop.value.show("地图加载中请稍后", "warning")
    }, 1500)
    
    const mInfo = await injectUserInfo().getIdentityInfo();
    me = mInfo.Id;
    const textMsgCall = (m:TextMsg)=>{
        msgs.value.push(m);
        if(msgs.value.length>50){
            msgs.value.shift()
        }
        msgDisp.value?.moveDown()
        var t:boxTypes;
        if(m.type==0){
            t = "info"
        }else if(m.type==1){
            t = "warning"
        }else{
            t = "failed"
        }
        if(m.sender!=mInfo.Name)
            pop.value.show(truncate(m.sender+":"+m.content, {length:30}), t)
    }
    sgrc = new SignalRClient(gameId,jwtToken||"", sync, textMsgCall);

    bgOpacity.value = parseFloat(localStorage.getItem(opacityStoreKey)||"0.4")||0.4;
    if(bgOpacity.value<0)
        bgOpacity.value = 0
    if(bgOpacity.value>1)
        bgOpacity.value = 1
    staSizeRatio.value = parseFloat(localStorage.getItem(staSizeRatioStoreKey)||"1")||1;
    if(staSizeRatio.value<0.3)
        staSizeRatio.value = 0.3
    if(staSizeRatio.value>1)
        staSizeRatio.value = 1

    await init();
    await sgrc.connect();
    await sgrc.enter();

    if(frame.value && arena.value){
        scaler = new Scaler(frame.value,arena.value,renderStaList, moveLocked, bg);
    }

    window.addEventListener("keypress",(ev)=>{
        if(ev.key=="Enter"){
            send();
        }
    })
    if(props.playback){
        timeline.value = await api.game.loadTimeline(gameId)
    }
})
onUnmounted(()=>{
    sgrc.conn.stop();
})

var scaler:Scaler|undefined;
const scalerPosRight = ref<number>(-100);
var scalerFolded:boolean = true;
function toggleScaler(){
    scalerFolded = !scalerFolded;
    if(scalerFolded){
        scalerPosRight.value = -100;
    }else{
        scalerPosRight.value = 15;
    }
}

const opacityStoreKey = "bgOpacity";
watch(bgOpacity,(newVal)=>{
    localStorage.setItem(opacityStoreKey,String(newVal));
})
const staSizeRatioStoreKey = "staSizeRatio";
watch(staSizeRatio,(newVal)=>{
    localStorage.setItem(staSizeRatioStoreKey,String(newVal));
    renderStaList();
})
const randNumText = computed<string>(()=>{
    if(props.playback){
        return '▲回放中'
    }
    if(playerList.value[0]?.id===me){
        return '▲轮到你了'
    }else{
        return '▲等待玩家'
    }
})
const tooManySelections = computed<boolean>(()=>{
    if(currentSelections.value && currentSelections.value.length >= 8){
        return true;
    }
    return false;
})
const nowMe = computed<boolean>(()=>{
    return playerList.value[0]?.id==me
})

watch(props,()=>{
    sgrc.conn.stop();
    window.location.reload();
})
</script>

<template>
<div class="topbar">
    <div class="playerList">
        <div v-for="p in playerRenderedList" class="player" :class="{outPlayer:p.p.out}" :key="p.p.id" :style="p.style">
            <img :src="avtSrc(p.p.avtFileName)"/>
            <div>
                <div :style="p.nameStyle" class="playerName" :class="{meName:me==p.p.id}">{{ p.p.name }}</div>
                <div class="playerData">{{ p.p.score }}分 <span v-show="p.p.stuckTimes">卡{{ p.p.stuckTimes }}次</span></div>
            </div>
        </div>
    </div>
</div>    
<div class="randNumOuter">
    <div v-show="gameStarted && !ended" class="randNum" :class="{randNumIsAB}" :style="randNumStyle">
        {{ randNumDisplay }}
    </div>
    <div v-show="gameStarted && !ended" class="status" :class="{secDanger}">
        {{ randNumText }}
        <div v-if="secDisplay" class="leftSecs">{{ secDisplay }}</div>
    </div>
    <div v-show="!gameStarted && !ended" class="status">等待房主开始中</div>
    <div v-show="ended" class="status">本对局已经结束</div>
</div>
<div class="frame" ref="frame" :class="{playbackFrame:playback, tooManySelections}">
    <div class="arena" ref="arena">
        <img v-if="bgFileName" ref="bg" :src="bgSrc(bgFileName||'')" :style="{opacity:bgOpacity}"/>
        <div v-for="s in staRenderedList" :style="s" 
            :class="{clickable:clickableStations.includes(s.sId)&&nowMe&&!playback, selected:selectedDist==s.sId&&nowMe&&!playback}" 
            :key="s.sId" class="station" @click="clickStation(s.sId)"></div>
        <div v-if="animatorRendered" :style="animatorRendered.style" class="pathAnim">{{ animatorRendered.step }}</div>
    </div>
</div>
<button class="confirm menuEntry" @click="sidebar?.extend">菜单</button>
<div class="scaleBtn" :style="{right:scalerPosRight+'px'}">
    <button class="scaleFold off" @click="toggleScaler">缩放</button>
    <button @click="scaler?.autoMutiple(5)">500%</button>
    <button @click="scaler?.autoMutiple(3)">300%</button>
    <button @click="scaler?.autoMutiple(2)">200%</button>
    <button @click="scaler?.autoMutiple(1)">占满</button>
    <button @click="scaler?.autoMutiple(1,true)">总览</button>
</div>
<button v-show="selectedDist && !ended && !playback" class="decideBtn" @click="select">确认选择</button>
<button v-show="gameStarted && !ended && !playback && playerList[0]?.id==me && !currentSelections?.length" class="cancel decideBtn" @click="select">无路可走</button>
<SideBar ref="sidebar">
    <TextMsgDisplay :msgs="msgs" ref="msgDisp"></TextMsgDisplay>
    <div>
        <input v-model="sending" placeholder="说点什么"/>
        <button @click="send">发送</button>
    </div>
    <div class="sidebarBtns">
        <button v-show="!gameStarted && !meJoined" @click="sgrc.join">加入本棋局</button>
        <button v-show="meJoined && !meOut" class="cancel" @click="sgrc.out">退出本棋局</button>
        <button v-show="meOut" class="minor">已退出本棋局</button>
        <button v-show="meHost && !gameStarted && meJoined" @click="sgrc.gameStart">下令开始棋局</button>
        <button v-show="gameStarted" class="minor">本棋局已开始</button>
        <button v-show="meHost && !ended && !gameStarted" class="cancel" @click="sgrc.gameReset">下令重置房间</button>
        <button v-show="gameStarted && !ended" class="cancel" @click="sgrc.kickAfk">移除挂机玩家</button>
        <button class="off" @click="$router.push('/')">返回主菜单</button>
        <div class="sideBarSlideOuter">
            背景不透明度：{{ bgOpacity }}
            <input type="range" v-model="bgOpacity" min="0" max="1" step="0.1">
        </div>
        <div class="sideBarSlideOuter">
            站点标记尺寸倍率：{{ staSizeRatio }}
            <input type="range" v-model="staSizeRatio" min="0.3" max="1.0" step="0.1">
            <div style="font-size: 12px;">站点有最小尺寸限制，视角近时调整才看得到效果</div>
        </div>
    </div>
</SideBar>
<Timeline v-if="playback" :game-id="gameId" @view-time="t=>sgrc.syncMe(t)"></Timeline>
</template>

<style scoped>
canvas{
    position: absolute;
}
@keyframes secDanger {
    0%{color:white}
    49%{color:white}
    50%{color:red}
}
.status{
    color:white;
    white-space: nowrap;
}
.status.secDanger{
    animation: secDanger 1s infinite;
}
.status .leftSecs{
    font-size: 12px;
}
.randNumOuter{
    position: fixed;
    top:70px;
    left:0px;
    width: 190px;
    height: 50px;
    background-color: cornflowerblue;
    border: 2px solid white;
    border-radius: 0px 25px 25px 0px;
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap:10px;
    box-sizing: border-box;
    padding: 0px 12px 0px 20px;
    overflow: hidden;
}
.randNum{
    text-align: center;
    font-size: 35px;
    color:white;
    transition: 500ms;
    font-family: 'system-ui sans-serif';
    white-space: nowrap;
}
.randNumIsAB{
    font-size: 22px;
}
.sideBarSlideOuter input{
    margin: 0px;
}
.sideBarSlideOuter{
    text-align: center;
    border-top: 1px solid #ccc;
    padding-top: 5px;
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
    box-shadow: 0px 0px 10px 0px black;
}
.scaleFold{
    position: absolute;
    bottom:0px;
    left:-50px;
}
.scaleBtn{
    position: fixed;
    right: 15px;
    bottom: 15px;
    width: 100px;
    display: flex;
    flex-direction: column;
    overflow: visible;
    gap:0px;
    transition: 0.5s;
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
    box-shadow:0px 0px 10px 10px cadetblue;
    
    animation-duration: 1s;
    animation-name: stationSpark;
    animation-iteration-count: infinite;
    animation-direction: alternate;
    cursor: pointer;
    z-index: 10;
}
.tooManySelections .station.clickable{
    animation: none !important;
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
.playbackFrame{
    bottom: 80px;
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
    width: fit-content;
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
.outPlayer{
    opacity: 0.4;;
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
    z-index: 100;
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
img{
    user-select: none;
}
</style>