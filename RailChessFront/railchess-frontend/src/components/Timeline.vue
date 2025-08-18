<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue';
import { GameTimeline } from '../models/game';
import { injectApi, injectPop } from '../provides';
import { avtSrc } from '../utils/fileSrc';

const props = defineProps<{
    gameId:number
}>()
const emit = defineEmits<{
    (e:'viewTime', eid?:number):void
}>()

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
    if(capCount <= 9){
        return 'black'
    }
    return 'green'
}
function seekLeft(){
    if(data.value && selectedIdx.value>0){
        selectedIdx.value -= 1
        selectedItem(true)
    }
}
function seekRight(){
    if(data.value && selectedIdx.value<data.value.Items.length-1){
        selectedIdx.value += 1
        selectedItem(true)
    }
}
function seekSame(dir:'left'|'right'){
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
function selectedItem(needAutoScroll?:boolean){
    if(!data.value?.Items)
        return
    const len = data.value.Items.length
    const idx = selectedIdx.value
    let nextEventId:number
    if(idx < len-1)
        nextEventId = data.value.Items[idx+1].EId
    else
        nextEventId = 0
    emit('viewTime', nextEventId)
    if(needAutoScroll){
        autoScroll()
    }
}
function autoScroll() {
    const thisEid = data.value?.Items[selectedIdx.value]?.EId
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

const timelineDiv = ref<HTMLDivElement>()
onMounted(async()=>{
    await load()
    if(data.value?.Warning){
        pop.value.show(data.value.Warning, 'warning')
    }
})
</script>

<template>
    <div class="seek">
        <button @click="seekSame('left')"><<</button>
        <button @click="seekLeft"><</button>
        <button @click="seekRight">></button>
        <button @click="seekSame('right')">>></button>
    </div>
    <div class="timeline" v-if="data" ref="timelineDiv">
        <div v-for="i,idx in data.Items" :key="i.EId" @click="selectedIdx=idx; selectedItem()"
            :class="{selected: idx===selectedIdx}" :id="itemElementId(i.EId)">
            <img :src="avtSrc(data.Avts[i.UId])"/>
            <div class="cap" :style="{backgroundColor:capColor(i.Cap)}">{{ i.Cap < 0 ? '卡' : '+'+i.Cap }}</div>
            <!--卡住记作-1-->
            <div class="rand">{{ i.Rand }}</div>
        </div>
    </div>
</template>

<style scoped>
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