<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { usePlayOptionsStore } from '../utils/stores/playOptionsStore';

defineProps<{
    playback?:boolean|string
}>()

const { bgOpacity, staSizeRatio, vacuumStaOpacity, autoSeekInterval, connDisplayMode } = storeToRefs(usePlayOptionsStore())
</script>

<template>
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
    <div v-if="playback" class="sideBarOption">
        <b>自动播放间隔(ms)：{{ autoSeekInterval?.toFixed(0) }}</b>
        <input type="range" v-model.number="autoSeekInterval" min="1000" max="10000" step="500">
    </div>
    <div class="sideBarOption">
        <b>查看车站连接关系</b>
        <select v-model="connDisplayMode">
            <option :value="'none'">关闭</option>
            <option :value="'anim'">动画</option>
        </select>
    </div>
</template>

<style scoped>
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
</style>