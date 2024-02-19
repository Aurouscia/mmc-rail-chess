<script setup lang="ts">

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
<div class="gitee">
    <img src="https://gitee.com/au114514/mmc-rail-chess/widgets/widget_card.svg?colors=4183c4,ffffff,ffffff,e3e9ed,666666,9b9b9b"/>
</div>
<div>
    <p>感谢mmc及其同学创造了本游戏玩法，感谢mtp、四氨合铜离子、等人的支持。</p>
</div>
</template>

<style scoped>
.gitee{
    margin: 30px 0px 30px 0px;
    display: flex;
    justify-content: center;
    align-items: center;
}
img{
    max-width: 100%;
    object-fit: contain;
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