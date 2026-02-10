<script setup lang="ts">
import { computed, nextTick, onMounted, useTemplateRef } from 'vue';
import { TextMsg, TextMsgType } from '../models/play';
import { purify } from '../utils/purify';

const props = defineProps<{
    msgs:TextMsg[]
}>();

const msgsPurified = computed(()=>{
    return props.msgs.map(x=>({
        ...x,
        content: purify(x.content)
    }))
})

function msgTypeClassName(t:TextMsgType){
    if(t==0)return "plain";
    if(t==1)return "important";
    if(t==2)return "err";
}

const msgs = useTemplateRef('msgs')
async function moveDown(){
    if(msgs.value){
        const atBottom = msgs.value.clientHeight + msgs.value.scrollTop - msgs.value.scrollHeight > -30
        await nextTick();
        if(atBottom){
            msgs.value.scrollTop = 10000000000;
        }
    }
}
onMounted(async()=>{
    await nextTick();
    if(msgs.value)
        msgs.value.scrollTop = 10000000000;
})
defineExpose({moveDown})
</script>

<template>
<div class="msgs" ref="msgs">
    <div v-for="m in msgsPurified" :key="m.time" class="msg" >
        <div class="meta">
            <div class="sender">{{ m.sender }}</div>
            <div class="time">{{ m.time }}</div>
        </div>
        <div :class="msgTypeClassName(m.type)" v-html="m.content">
        </div>
    </div>
</div>
</template>

<style scoped lang="scss">
.err{
    color:white;
    background-color: red;
}
.important{
    color:orange;
}
.plain{
    color:#666
}
.sender{
    font-weight: bold;
}
.time{
    color:#999;
}
.meta{
    display: flex;
    justify-content: space-between;
}
.msg{
    width: calc(100% - 20px);
    margin: 5px auto;
    padding: 5px;
    background-color: white;
    border-radius: 5px;
    &:deep(b){
        text-decoration: underline;
    }
}
.msgs{
    height: calc(100vh - 380px);
    width: calc(100% - 4px);
    overflow-x: hidden;
    overflow-y: scroll;
    border: 2px solid black;
    background-color: #aaa;
    border-radius: 6px;
}
</style>