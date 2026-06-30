<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
    progress: number
}>()
const emit = defineEmits<{
    click: []
}>()

const safeProgress = computed(()=>{
    const p = Number(props.progress)
    if(!Number.isFinite(p)){
        return 0
    }
    return Math.max(0, Math.min(1, p))
})
</script>

<template>
    <div class="poll-progress" @click="emit('click')">
        <div class="poll-progress-bar" :style="{ height: `${safeProgress * 100}%` }"></div>
    </div>
</template>

<style scoped>
.poll-progress{
    width: 15px;
    height: 40px;
    background-color: white;
    border: 1px solid #ccc;
    border-radius: 4px;
    position: relative;
    box-sizing: border-box;
    overflow: hidden;
    cursor: pointer;
}
.poll-progress-bar{
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: black;
}
</style>
