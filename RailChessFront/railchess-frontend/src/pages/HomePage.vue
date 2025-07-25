<script setup lang="ts">
import { contact } from '../utils/consts';
import { useFeVersionChecker } from '../utils/feVersionCheck';

const { checkAndPop } = useFeVersionChecker()
checkAndPop()
</script>

<template>
<h1>欢迎来到轨交棋</h1>
    <div>
        <img src="/railchessLogo.svg" class="logo"/>
        <p>
            轨交棋是一款基于地铁线路图的棋类游戏，玩家需要在地铁图上沿着线路移动，
            尽可能多的"占领"车站，并堵截其他玩家的行动，最终获得最高分数。
        </p>
        <div class="contact">
            {{ contact }}
        </div>
    </div>

    <div class="gitInfo">
        <iframe src="/gitInfo.html"></iframe>
    </div>

    <div>
        <p>游戏玩法创造者：mmc(SlinkierApple13)及其同学</p>
        <p>代码贡献者：Au</p>
        <p>issue贡献者：滨蜀</p>
        <p>logo设计者：三几几</p>
        <p>活跃玩家：四氨合铜离子、哦、南京精灵的灵芝、Ripple、梅天浦等人</p>
    </div>
</template>

<style scoped>
iframe{
    width: 100%;
    max-width: 600px;
    height: 500px;
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
.logo{
    display: block;
    width: 200px;
    margin: 10px auto 20px auto;
}
div p{
    text-indent: 2em;
    color: gray;
    margin-bottom: 8px;
}
@media screen and (min-width: 1200px) {
    div p{
        text-align: center;
    }
}
</style>