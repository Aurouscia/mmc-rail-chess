<script setup lang="ts">
import { computed, CSSProperties, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { injectApi, injectHideTopbar, injectPop, injectUserInfo } from '../provides';
import { OcpStatus, Player, SyncData, TextMsg } from '../models/play';
import SideBar from '../components/SideBar.vue';
import { RailChessTopo,Sta,posBase } from '../models/map';
import { Api } from '../utils/api';
import { GameTimeline, RailChessGame } from '../models/game';
import { avtSrc,bgSrc } from '../utils/fileSrc';
import { Scaler } from '../models/scale';
import { SignalRClient } from '../utils/signalRClient';
import TextMsgDisplay from '../components/TextMsgDisplay.vue';
import { useAnimator, AnimNode, useConnectionAnimator, AnimConn } from '../utils/pathAnim';
import _, { truncate } from 'lodash'
import { boxTypes } from '../components/Pop.vue';
import Timeline from '../components/Timeline.vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { usePlayOptionsStore } from '../utils/stores/playOptionsStore';
import { useJwtTokenStore } from '../utils/stores/jwtTokenStore';

const router = useRouter()
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
function renderPlayerList(reversed:boolean){
    const playerListSplitted:Player[] = []
    playerListSplitted.push(...playerList.value.filter(p=>!p.out))
    const outPlayers = [...playerList.value.filter(p=>p.out)]
    outPlayers.sort((p1,p2)=>p2.score-p1.score)
    playerListSplitted.push(...outPlayers)
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
            let still = i === existing.idx
            let goRight = i > existing.idx
            if(reversed && !still)
                goRight = !goRight
            if(goRight){
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
type StaRendered = {sId:number, classes:string[], styles:CSSProperties}
type StaRenderedRecord = Record<number, StaRendered>
const staRenderedRecord = ref<StaRenderedRecord>({});
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
        const classes = [];

        var atByPlayer = playerList.value.find(x=>x.atSta==id);
        var occupiedByPlayer = ocpStatus.value.find(x=>x.stas.includes(id));
        var isNewOcp = newOcps.value?.stas.includes(id);
        if(atByPlayer){
            side = size * 2;
            backgroundImage = `url("${avtSrc(atByPlayer.avtFileName)}")`;
            zIndex = 21;
            if(atByPlayer.id === newOcps.value?.playerId){
                classes.push('atByPlayer')
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
        else{
            //未被占领的空站
            classes.push('vacuum')
        }
        // isNewOcp不能套在上面的if-else里，因为不管玩家是否在那个位置，都可能是新占的
        if(isNewOcp){
            zIndex = 14;
            classes.push('isNewOcp')
        }
        const style:StaRendered = {
            sId: id,
            classes,
            styles:{
                left:x-side/2+'px',
                top:y-side/2+'px',
                width:side-4+'px',
                height:side-4+'px',
                backgroundImage,
                zIndex
            }
        };
        staRenderedRecord.value[id] = style;
    }
}

const pathAnimRenderedWidth = 26;
const connAnimRenderedWidth = 18;
const { animatorRendered, setPaths, stopPathAnim } = useAnimator();
const { connectionAnimatorRendered, setConnections, stopConnectionsAnim } = useConnectionAnimator()
const selectedDist = ref<number|undefined>();
function animPosGet(sta:number, type:'path'|'conn'){
    const side = type === 'path' ? pathAnimRenderedWidth : connAnimRenderedWidth
    const s = topoData.value!.Stations.find(x=>x[0]==sta);
    if(!s || !arena.value)return undefined
    const xRatio = s[1] / posBase
    const yRatio = s[2] / posBase
    const sideXRatioHalf = (side/2) / arena.value.clientWidth;
    const sideYRatioHalf = (side/2) / arena.value.clientHeight;
    const left = (xRatio - sideXRatioHalf) * 100 + '%';
    const top = (yRatio - sideYRatioHalf) * 100 + '%';
    return {left, top}
}
function animStyleBase(type:'path'|'conn'):CSSProperties{
    const side = type === 'path' ? pathAnimRenderedWidth : connAnimRenderedWidth
    return {
        width: side - 4 + 'px',
        height: side - 4 + 'px',
        borderWidth: '2px',
        backgroundImage: undefined,
    }
}
function renderPathAnims() {
    if (!topoData.value || !arena.value) { return; }
    if(!currentSelections.value || selectedDist.value===undefined){return;}
    const path = currentSelections.value.find(s=>s[s.length-1] == selectedDist.value);
    if(!path){return;}
    var nodes: AnimNode[] = [];
    path.forEach(sta => {
        nodes.push({getPos:s=>animPosGet(s,'path'),sta});
    });
    const animPath = {
        dist: path[path.length-1],
        styleBase: animStyleBase('path'),
        nodes: nodes
    }
    setPaths(animPath);
}
function renderConnectionAnims(id:number){
    const topo = topoData.value
    if(!topo)
        return
    let incre = 1
    const conns:AnimConn[] = []
    const addTwin = (neibId:number, text:string)=>{
        conns.push({
            a: id,
            b: neibId,
            text})
        conns.push({
            b: id,
            a: neibId,
            text})
    }
    for(const line of topo.Lines){
        if(line.Stas.length <= 1)
            continue
        const text = String.fromCharCode(64 + incre) //初版中用来标注线路名称的，现在已弃用
        for(let idx=0; idx<line.Stas.length; idx++){
            if(id != line.Stas[idx])
                continue
            if(idx>0){
                const neib = line.Stas[idx-1]
                addTwin(neib, text)
            }
            if(idx<line.Stas.length-1){
                const neib = line.Stas[idx+1]
                addTwin(neib, text)
            }
        }
        incre++
    }
    setConnections({
        styleBase: animStyleBase('conn'),
        conns,
        getPos: s=>animPosGet(s, 'conn')
    })
}
function clickStation(id:number){
    if(connDisplayMode.value==='anim')
        window.setTimeout(()=>renderConnectionAnims(id), 1)
    if(props.playback || !nowMe){
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
    stopConnectionsAnim()
    if(props.playback || !nowMe){
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
let frameStatus = ''
function getArenaStatus(){
    //用于判断视角是否移动过
    const f = frame.value
    if(!f)
        return
    return `${f.scrollLeft}|${f.scrollTop}`
}
function frameDownHandlerForAnim(){
    frameStatus = getArenaStatus() ?? ''
}
function frameUpHandlerForAnim(){
    //仅在原地点击时停止播放，移动后松开的不算停止指令
    const statusNow = getArenaStatus()
    if(frameStatus == statusNow){
        stopConnectionsAnim()
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
    if(sec.value < 0){
        if(sec.value < -99){
            const mins = Math.ceil(-sec.value/60)
            return `超时${mins}分钟`
        }
        return `超时${-sec.value}秒`
    }
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
let myLastSyncTimeMs = 0
let lastSyncTFilterId = 0
async function sync(data:SyncData|null){
    console.log("收到同步数据指令",data)
    if(!data){
        console.log("无需同步数据")
        return
    }
    myLastSyncTimeMs = Date.now()
    resetPollTimer()
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
    await nextTick()
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
        let reversed = false
        if(props.playback){
            reversed = lastSyncTFilterId > data.tFilterId
        }
        renderPlayerList(reversed);
        lastSyncTFilterId = data.tFilterId
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
const sidebarOptions = ref<InstanceType<typeof SideBar>>();
const msgs = ref<TextMsg[]>([]);
const frame = ref<HTMLDivElement>();
const arena = ref<HTMLDivElement>();
const { bgOpacity, staSizeRatio, vacuumStaOpacity, connDisplayMode } = storeToRefs(usePlayOptionsStore())
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
watch(staSizeRatio, ()=>{
    renderStaList()
})

async function visibilityChangedHandler(){
    if(!props.playback && document.visibilityState==='visible'){
        await sgrc.syncMe()
    }
}
//保险措施，有时因为客户端休眠等原因没收到sync指令
const pollIntv = 15 * 1000
let pollTimer = 0
async function poll(){
    if(!props.playback){
        await sgrc.syncMeIfNecessary(myLastSyncTimeMs)
    }
}
function resetPollTimer(){
    disposePollTimer()
    pollTimer = window.setInterval(poll, pollIntv)
}
function disposePollTimer(){
    window.clearInterval(pollTimer)
}

var api:Api;
var me:number;
var sgrc:SignalRClient
const moveLocked = ref(false)
const timeline = ref<GameTimeline>()
const msgDisp = ref<InstanceType<typeof TextMsgDisplay>>()
onMounted(async()=>{
    injectHideTopbar()();
    api = injectApi();
    const pop = injectPop();
    
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
            pop.value.show(truncate(m.sender+"："+m.content, {length:30}), t)
    }
    const jwtToken = useJwtTokenStore().jwtToken
    sgrc = new SignalRClient(gameId,jwtToken||"", sync, textMsgCall);

    //旧版兼容性
    localStorage.removeItem('staSize')
    localStorage.removeItem('staSizeAuto')
    const bgO = localStorage.getItem(opacityStoreKey)
    if(bgO){
        const bgONum = parseFloat(bgO);
        if(!isNaN(bgONum)){
            bgOpacity.value = bgONum
        }
    }
    localStorage.removeItem(opacityStoreKey)
    const sr = localStorage.getItem(staSizeRatioStoreKey)
    if(sr){
        const srNum = parseFloat(sr);
        if(!isNaN(srNum)){
            staSizeRatio.value = srNum
        }
    }
    localStorage.removeItem(staSizeRatioStoreKey)

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
    document.addEventListener("visibilitychange", visibilityChangedHandler)
    if(props.playback){
        timeline.value = await api.game.loadTimeline(gameId)
    }
    resetPollTimer()
})
onBeforeUnmount(()=>{
    stopPathAnim()
    disposePollTimer()
    document.removeEventListener("visibilitychange", visibilityChangedHandler)
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

//<obsolete>
const opacityStoreKey = "bgOpacity";
const staSizeRatioStoreKey = "staSizeRatio";
//</obsolete>

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
<div class="frame" ref="frame" :class="{playbackFrame:playback, tooManySelections, nowMe:nowMe, nowNotMe:!nowMe}"
    @mousedown="frameDownHandlerForAnim" @touchstart="frameDownHandlerForAnim"
    @mouseup="frameUpHandlerForAnim" @touchend="frameUpHandlerForAnim">
    <div class="arena" ref="arena">
        <img v-if="bgFileName" ref="bg" :src="bgSrc(bgFileName||'')" :style="{opacity:bgOpacity}"/>
        <div v-for="s in staRenderedRecord" :style="s.styles" 
            :class="[{
                clickable:clickableStations.includes(s.sId) && !playback,
                selected:selectedDist==s.sId && !playback,  
            }, ...s.classes]" 
            :key="s.sId" class="station" @click="clickStation(s.sId)"></div>
        <div v-if="animatorRendered" :style="animatorRendered.style" class="pathAnim">{{ animatorRendered.step }}</div>
        <div v-for="i in connectionAnimatorRendered" :style="i.style" class="pathAnim connAnim">{{ i.num }}</div>
    </div>
</div>
<div class="menuEntries">
    <button class="minor" @click="sidebarOptions?.extend">设置</button>
    <button class="confirm" @click="sidebar?.extend">菜单</button>
</div>
<div class="scaleBtn" :style="{right:scalerPosRight+'px'}">
    <button class="scaleFold off" @click="toggleScaler">缩放</button>
    <button @click="scaler?.autoMutiple(5)">500%</button>
    <button @click="scaler?.autoMutiple(3)">300%</button>
    <button @click="scaler?.autoMutiple(2)">200%</button>
    <button @click="scaler?.autoMutiple(1)">占满</button>
    <button @click="scaler?.autoMutiple(1,true)">总览</button>
</div>
<!--选中站id可能为0，所以不能直接作为布尔值用-->
<button v-show="selectedDist!==undefined && gameStarted && nowMe && !ended && !playback" class="decideBtn" @click="select">确认选择</button>
<button v-show="!currentSelections?.length && gameStarted && nowMe && !ended && !playback" class="cancel decideBtn" @click="select">无路可走</button>
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
        <button v-show="meHost && !gameStarted" @click="sgrc.gameStart">下令开始棋局</button>
        <button v-show="gameStarted && !ended" class="minor">本棋局已开始</button>
        <button v-show="ended" class="minor">本棋局已结束</button>
        <button v-show="meHost && !ended && !gameStarted" class="cancel" @click="sgrc.gameReset">下令重置房间</button>
        <button v-show="gameStarted && !ended" class="cancel" @click="sgrc.kickAfk">移除挂机玩家</button>
        <button v-show="ended && !playback" @click="router.replace('/playback/'+props.id)">查看本局回放</button>
        <button class="off" @click="router.push('/')">返回主菜单</button>
    </div>
</SideBar>
<SideBar ref="sidebarOptions">
    <div class="sideBarOption">
        <b>背景不透明度：{{ bgOpacity?.toFixed(2) }}</b>
        <input type="range" v-model.number="bgOpacity" min="0" max="1" step="0.05">
    </div>
    <div class="sideBarOption">
        <b>站点标记尺寸倍率：{{ staSizeRatio?.toFixed(2) }}</b>
        <input type="range" v-model.number="staSizeRatio" min="0.3" max="1.0" step="0.05">
        <div style="font-size: 12px;">站点有最小尺寸限制<br/>视角近时调整才看得到效果</div>
    </div>
    <div class="sideBarOption">
        <b>未占站点不透明度：{{ vacuumStaOpacity?.toFixed(2) }}</b>
        <input type="range" v-model.number="vacuumStaOpacity" min="0.1" max="1.0" step="0.05">
    </div>
    <div class="sideBarOption">
        <b>查看车站连接关系</b>
        <select v-model="connDisplayMode">
            <option :value="'none'">关闭</option>
            <option :value="'anim'">动画</option>
        </select>
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
    font-family: '宋体','serif';
    white-space: nowrap;
    flex-grow: 1;
}
.randNumIsAB{
    font-size: 22px;
}
.sideBarOption input{
    margin: 0px;
}
.sideBarOption{
    text-align: center;
    border-bottom: 1px solid #ccc;
    padding: 10px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
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
    font-size: 20px;
    display: flex;
    justify-content: center;
    align-items: center;
    font-family: '微软雅黑','sans-serif';
}
.connAnim{
    z-index: -1;
    background-color: #fff;
    color: #666;
    transition-timing-function: linear;
    font-size: 14px;
}
@keyframes stationSpark {
  from {box-shadow:0px 0px 5px 5px cadetblue}
  to {box-shadow:0px 0px 10px 10px cadetblue}
}
@keyframes stationSparkPink {
  from {box-shadow:0px 0px 5px 5px palevioletred}
  to {box-shadow:0px 0px 10px 10px palevioletred}
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
.nowNotMe .station.clickable{
    border-color: rgb(212, 92, 132);
    background-color: rgb(175, 61, 99);
    box-shadow:0px 0px 10px 10px palevioletred;
    animation-name: stationSparkPink
}
.tooManySelections .station.clickable{
    animation: none !important;
}
.nowMe .station.selected{
    border-color: rgb(0, 201, 0);
    background-color: green;
    box-shadow: 0px 0px 15px 10px green;
    animation: none;
    z-index: 20 !important;
}
.station.atByPlayer{
    box-shadow: 0px 0px 5px 5px black;
}
.station.isNewOcp{
    box-shadow: 0px 0px 5px 5px orange;
}
.station.vacuum{
    opacity: var(--vacuum-sta-opacity, 1);
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
    z-index: -10;
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
.menuEntries{
    position: fixed;
    right:7px;
    top:68px;
    display: flex;
    gap: 1px;
    background-color: white;
    border-radius: 8px;
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