<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { usePlayOptionsStore } from '../utils/stores/playOptionsStore';

defineProps<{
    playback?:boolean|string
}>()

const { bgOpacity, staSizeRatio, vacuumStaOpacity, autoSeekInterval, connDisplayMode, syncMeIntervalSec } = storeToRefs(usePlayOptionsStore())
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
    <div v-if="!playback" class="sideBarOption">
        <b>兜底同步间隔：{{ syncMeIntervalSec }} 秒</b>
        <input type="range" v-model.number="syncMeIntervalSec" min="3" max="15" step="1">
        <p style="color: gray; margin: 10px; font-size: 15px;">
            由于移动端性能问题或网络波动，传来的更新可能丢失，这里是一个兜底机制，（理论上）进度条走到头时会强制更新一次，你可以按需调整这个速度。如果进度条多次走到头了依然有丢更新的问题，请向Au反馈。
        </p>
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