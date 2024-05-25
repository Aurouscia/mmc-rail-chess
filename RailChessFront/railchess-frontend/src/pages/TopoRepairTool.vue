<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { StaParsed, posBase } from '../models/map'

const props = defineProps<{
    reRender:()=>void,
    stations:StaParsed[]
}>()
function execute(){
    props.stations.forEach(s=>{
        pointWiseOperation(s)
    });
    props.reRender()
}
function pointWiseOperation(s:StaParsed){
    if(selected.value == selectType.whole){
        s.X += rightVec.value;
        s.Y += downVec.value;
    }
    else if(selected.value == selectType.top){
        const yr = 1 - s.Y / posBase;
        s.Y += yr * downVec.value 
    }
    else if(selected.value == selectType.bottom){
        const yr = s.Y / posBase;
        s.Y += yr * downVec.value 
    }    
    else if(selected.value == selectType.left){
        const xr = 1 - s.X / posBase;
        s.X += xr * rightVec.value 
    }
    else if(selected.value == selectType.right){
        const xr = s.X / posBase;
        s.X += xr * rightVec.value 
    }
}

const original:StaParsed[] = []
onMounted(()=>{
    props.stations.forEach(s=>{
        original.push({Id:s.Id, X:s.X, Y:s.Y})
    })
})
function cancel(){
    for(let i = 0; i < original.length; i++){
        props.stations[i].X = original[i].X;
        props.stations[i].Y = original[i].Y;
    }
    props.reRender();
    emits('done', false)
}
function ok(){
    emits('done', true)
}
const emits = defineEmits<{
    (e:'done', changed:boolean):void
}>()


enum selectType{
    whole, left, right, top, bottom
}
enum moveDirType{
    left, right, up, down
}
const selected = ref<selectType>(selectType.whole)
const moveDir = ref<moveDirType>(moveDirType.left)
const incre = ref<number>(10)
const increActual = computed(()=>incre.value * 5)
const rightVec = computed<number>(()=>{
    if(moveDir.value == moveDirType.right){
        return increActual.value;
    }else if(moveDir.value == moveDirType.left){
        return -increActual.value;
    }
    return 0;
})
const downVec = computed<number>(()=>{
    if(moveDir.value == moveDirType.down){
        return increActual.value;
    }else if(moveDir.value == moveDirType.up){
        return -increActual.value;
    }
    return 0;
})
function canHorizontalMove(selected:selectType) {
    return selected == selectType.left || selected == selectType.right
}
function canVerticalMove(selected:selectType){
    return selected == selectType.top || selected == selectType.bottom
}
const showUpDown = ref<boolean>(true)
const showLeftRight = ref<boolean>(true)
watch(selected, (newVal, oldVal)=>{
    if(newVal == selectType.whole){
        showUpDown.value = true;
        showLeftRight.value = true;
        return;
    }
    if(canHorizontalMove(newVal)){
        if(!canHorizontalMove(oldVal))
            moveDir.value = moveDirType.left
        showUpDown.value = false;
        showLeftRight.value = true;
    }else if(canVerticalMove(newVal)){
        if(!canVerticalMove(oldVal))
            moveDir.value = moveDirType.up
        showLeftRight.value = false;
        showUpDown.value = true;
    }
})
</script>

<template>
<div class="repairTool">
    <br/>
    目标: <select v-model="selected">
        <option :value="selectType.whole">整体</option>
        <option :value="selectType.left">左边缘</option>
        <option :value="selectType.right">右边缘</option>
        <option :value="selectType.top">顶边缘</option>
        <option :value="selectType.bottom">底边缘</option>
    </select>
    方向: <select v-model="moveDir">
        <option v-if="showLeftRight" :value="moveDirType.left">向左</option>
        <option v-if="showLeftRight" :value="moveDirType.right">向右</option>
        <option v-if="showUpDown" :value="moveDirType.up">向上</option>
        <option v-if="showUpDown" :value="moveDirType.down">向下</option>
    </select><br/>
    步长: <input v-model="incre" min="1" max="20" type="number"/>
    <button @click="execute">执行一步</button><br/>
    <button @click="ok" class="ok">完成</button><button @click="cancel" class="cancel">取消</button>
</div>
</template>

<style>
.repairTool{
    text-align: center;
    vertical-align: middle;
}
</style>