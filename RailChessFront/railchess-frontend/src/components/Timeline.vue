<script setup lang="ts">
import { onMounted, ref } from 'vue';
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
    if(data.value)
        selectedIdx.value = data.value.Items.length-1;
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
        selectedItem()
    }
}
function seekRight(){
    if(data.value && selectedIdx.value<data.value.Items.length-1){
        selectedIdx.value += 1
        selectedItem()
    }
}
function selectedItem(){
    emit('viewTime', data.value?.Items[selectedIdx.value+1]?.EId)
}
onMounted(async()=>{
    await load()
    if(data.value?.Warning){
        pop.value.show(data.value.Warning, 'warning')
    }
})
</script>

<template>
    <div class="seek">
        <button @click="seekLeft"><<</button>
        <button @click="seekRight">>></button>
    </div>
    <div class="timeline" v-if="data">
        <div v-for="i,idx in data.Items" :key="i.UId" @click="selectedIdx=idx; selectedItem()" :class="{selected: idx===selectedIdx}">
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