<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref } from 'vue';

const rightDefault = -200;
const height = 70;
export type boxTypes = "success"|"failed"|"warning"|"info"
export type popDelegate = (msg:string,type:boxTypes)=>void;

interface msgBox {
    right: number,
    msg: string,
    created:number,
    type:boxTypes
}
const boxes = ref<msgBox[]>([])

function show(msg: string,type: boxTypes) {
    const newBox:msgBox = {
        right: rightDefault,
        msg: msg,
        created:Number(new Date()),
        type
    };
    boxes.value.push(newBox)
}
function refresh() {
    if(boxes.value.length==0){
        return;
    }
    const now = Number(new Date());
    boxes.value.filter(x=>{
        return now - x.created < 2000 && now-x.created>100
    }).forEach((b)=>{
        b.right = 0
    });
    boxes.value.filter(x=>{
        return now - x.created >= 2000
    }).forEach((b)=>{
        b.right = rightDefault
    });
    boxes.value = boxes.value.filter(x=>{
        return now-x.created <= 2500
    });
}
var interval:number; 
onMounted(()=>{
    interval = setInterval(refresh,20)
})
onBeforeUnmount(()=>{
    clearInterval(interval);
})

defineExpose({ show }) 

</script>

<template>
    <div v-for="box,index in boxes" :key="box.created" class="box" :style="{ 
        right: box.right + 'px',
        width: (-rightDefault) + 'px',
        top: height*index + 100 + 'px'
         }" :class="box.type">
        <div v-html="box.msg">
        </div>
    </div>
</template>

<style scoped>
.box {
    position: fixed;
    top: 100px;
    right:-200px;
    height: 60px;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: 0.5s;
    margin: 0px;
    color:white;
    z-index: 1000000;
}
.box div{
    max-width: 180px;
    color:white;
    word-break: break-all;;
}
.success{
    background-color: #339933;
}
.failed{
    background-color: #cc2222;
}
.info{
    background-color: #cccccc;
}
.warning{
    background-color: orange;
    color:black;
}
</style>