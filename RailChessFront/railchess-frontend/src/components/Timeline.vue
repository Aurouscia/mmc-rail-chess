<script setup lang="ts">
import { nextTick, onMounted, ref, useTemplateRef } from 'vue';
import { GameTimeline } from '../models/game';
import { injectApi, injectPop } from '../provides';
import { avtSrc } from '../utils/fileSrc';
import { usePlayOptionsStore } from '../utils/stores/playOptionsStore';
import { clamp, throttle } from 'lodash-es';
import { displayForRandNum } from '../utils/randNumDisplay';

const props = defineProps<{
    gameId:number
}>()
const emit = defineEmits<{
    (e:'viewTime', eid?:number):void
}>()
const playOptions = usePlayOptionsStore()
const THROTTLE_MS = 500

const data = ref<GameTimeline>();
const api = injectApi()
const pop = injectPop()
const selectedIdx = ref(-1)
async function load(){
    data.value = await api.game.loadTimeline(props.gameId)
    if(data.value){
        selectedIdx.value = data.value.Items.length-1;
        await nextTick()
        autoScroll()
    }
}
function capColor(capCount:number){
    if(capCount < 0){
        //卡住记作-1
        return 'red'
    }
    if(capCount === 0){
        return '#aaa'
    }
    if(capCount <= 9){
        return '#666'
    }
    return 'green'
}
function seekLeft(){
    if(autoSeeking.value)
        return
    if(data.value && selectedIdx.value>-1){
        selectedIdx.value -= 1
        selectedItem(true)
    }
}
function seekRight(auto?:'auto'){
    if(autoSeeking.value && !auto)
        return false
    if(data.value && selectedIdx.value < data.value.Items.length - 1){
        selectedIdx.value += 1
        selectedItem(true)
        return true
    }
    return false
}

const autoSeeking = ref(false)
let autoSeekTimer = 0
function toggleAuto(){
    if(!autoSeeking.value){
        autoSeeking.value = true
        const itv = clamp(playOptions.autoSeekInterval, 1000, 10000)
        autoSeekTimer = window.setInterval(()=>{
            const success = seekRight('auto')
            if(!success){
                autoSeeking.value = false
                window.clearTimeout(autoSeekTimer)
            }
        }, itv)
    } else {
        autoSeeking.value = false
        window.clearTimeout(autoSeekTimer)
    }
}

function seekSame(dir:'left'|'right'){
    if(autoSeeking.value)
        return
    const sidx = selectedIdx.value
    const item = data.value?.Items[sidx]
    if(!data.value || !item){
        return;
    }
    const uid = item.UId;
    for(let i=1;;i++){
        const idx = dir=='left' ? sidx-i : sidx+i;
        if(idx<0){
            pop.value.show('该玩家无更早行动', 'failed')
            return;
        }
        if(idx>data.value.Items.length-1){
            pop.value.show('该玩家无后续行动', 'failed')
            return;
        }
        const itemAtIdx = data.value.Items[idx]
        if(itemAtIdx && itemAtIdx.UId == uid){
            selectedIdx.value = idx
            selectedItem(true);
            return
        }
    }
}
function seekEnd(dir:'left'|'right'){
    if(autoSeeking.value || !data.value)
        return
    if(dir == 'left'){
        selectedIdx.value = -1
        selectedItem(true)
    }
    else{
        selectedIdx.value = data.value.Items.length - 1
        selectedItem(true)
    }
}

function selectedItem(needAutoScroll?:boolean){
    if(!data.value?.Items)
        return
    const len = data.value.Items.length
    const idx = selectedIdx.value
    let nextEventId:number
    if(idx==-1){
        nextEventId = data.value.Items[0].EId
    }
    else{
        if(idx < len-1)
            nextEventId = data.value.Items[idx+1].EId
        else
            nextEventId = 0
    }
    emit('viewTime', nextEventId)
    if(needAutoScroll){
        autoScroll()
    }
}
function autoScroll() {
    let thisEid = 0
    if(selectedIdx.value >= 0)
        thisEid = data.value?.Items[selectedIdx.value]?.EId ?? 0
    else if(selectedIdx.value == -1)
        thisEid = -1
    if (thisEid) {
        const element = document.getElementById(itemElementId(thisEid))
        if (element && timelineDiv.value) {
            timelineDiv.value.scrollTo({
                left: element.offsetLeft - 100,
                behavior: 'smooth'
            })
        }
    }
}
function itemElementId(eid:number){
    return `te_${eid}`
}

function createBtnThrottle(func:()=>void){
    return throttle(func, THROTTLE_MS, { leading: true, trailing: true })
}
const thSeekEndLeft = createBtnThrottle(()=>seekEnd('left'))
const thSeekSameLeft = createBtnThrottle(()=>seekSame('left'))
const thSeekLeft = createBtnThrottle(seekLeft)
const thToggleAuto = createBtnThrottle(toggleAuto)
const thSeekRight = createBtnThrottle(()=>seekRight())
const thSeekSameRight = createBtnThrottle(()=>seekSame('right'))
const thSeekEndRight = createBtnThrottle(()=>seekEnd('right'))

const timelineDiv = useTemplateRef('timelineDiv')
onMounted(async()=>{
    await load()
    if(data.value?.Warning){
        pop.value.show(data.value.Warning, 'warning')
    }
})
</script>

<template>
    <div class="seek" :class="{autoSeeking}">
        <button @click="thSeekEndLeft"><=</button>
        <button @click="thSeekSameLeft"><-</button>
        <button @click="thSeekLeft"><</button>
        <button @click="thToggleAuto" class="autoSeekBtn">></button>
        <button @click="thSeekRight">></button>
        <button @click="thSeekSameRight">-></button>
        <button @click="thSeekEndRight">=></button>
    </div>
    <div class="timeline" v-if="data" ref="timelineDiv">
        <div :key="-1" @click="selectedIdx=-1; selectedItem()"
            :class="{selected: selectedIdx===-1}" :id="itemElementId(-1)">
            <img :src="avtSrc('')"/>
            <div class="cap" style="background-color: cornflowerblue;">开局</div>
            <div class="rand">>>></div>
        </div>
        <div v-for="i,idx in data.Items" :key="i.EId" @click="selectedIdx=idx; selectedItem()"
            :class="{selected: idx===selectedIdx}" :id="itemElementId(i.EId)">
            <img :src="avtSrc(data.Avts[i.UId])"/>
            <div class="cap" :style="{backgroundColor:capColor(i.Cap)}">{{ i.Cap < 0 ? '卡' : '+'+i.Cap }}</div>
            <!--卡住记作-1-->
            <div class="rand">{{ displayForRandNum(i.Rand) }}</div>
        </div>
    </div>
</template>

<style scoped>
*{
    user-select: none;
}
.seek{
    position: fixed;
    right: 10px;
    bottom: 90px;
    padding: 5px;
    border-radius: 10px;
    background-color: white;
    box-shadow: 0px 0px 5px 0px black;
}
.seek button{
    width: 28px;
    text-align: center;
    font-family: monospace;
}
.seek .autoSeekBtn{
    background-color: #ccc;
}

.autoSeeking button{
    background-color: #ccc;
}
.autoSeeking .autoSeekBtn{
    background-color: green;
    animation: blinkGreen 700ms infinite alternate;
}
@keyframes blinkGreen {
    0%, 49% { background-color: rgb(0, 187, 0); }
    50%, 100% { background-color: green; }
}

.timeline{
    height: 80px;
    position: fixed;
    bottom: 0px;
    left: 0px;
    right: 0px;
    overflow-x: scroll;
    overflow-y: hidden;
    display: flex;
    gap: 5px;
    background-color: white;
    box-shadow: 0px 0px 10px 0px black;
}
.cap,.rand{
    padding: 1px;
    border-radius: 2px;
    min-width: 10px;
    text-align: center;
    white-space: nowrap;
}
.rand{
    background-color: #eee;
    color: #666
}
.timeline>div{
    padding: 0px 2px 0px 2px;
    width: 36px;
    height: 64px;
    display: flex;
    flex-direction: column;
    justify-content: space-around;
    align-items: center;
    font-size: 12px;
    color:white;
    cursor: pointer;
}
.timeline>div:hover{
    background-color: #ddd;
}
.timeline>div>img{
    width: 24px;
    height: 24px;
    border-radius: 1000px;
}
.selected{
    background-color: #bbb;
}
</style>