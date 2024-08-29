<script setup lang="ts">
import { useFeVersionChecker } from '../utils/feVersionCheck';

const { checkAndPop } = useFeVersionChecker()
checkAndPop()
</script>

<template>
<h1>欢迎来到轨交棋</h1>
    <div>
        <p>
            轨交棋是一款基于地铁线路图的棋类游戏，玩家需要在地铁图上沿着线路移动，
            尽可能多的"占领"车站，并堵截其他玩家的行动，最终获得最高分数。
        </p>
        <p>
            本软件为轨交棋的第二代线上游戏实现，相比第一代有更友好的UI和动画，也更便于部署和维护。
        </p>
    </div>

    <div class="gitInfo">
        <iframe src="/gitInfo.html"></iframe>
    </div>

    <div>
        <p>感谢mmc(SlinkierApple13)及其同学创造了本游戏玩法，感谢滨蜀、mtp、四氨合铜离子等人的支持。</p>
    </div>
</template>

<style scoped>
iframe{
    width: 100%;
    max-width: 600px;
    height: calc(100vh - 250px);
    border: none;
}
.gitInfo{
    display: flex;
    align-items: center;
    justify-content: center;
    margin: 20px 0px 20px 0px;
}
h1{
    text-align: center;
}
div{
    text-indent: 2em;
}
p{
    color:gray;
}
</style>