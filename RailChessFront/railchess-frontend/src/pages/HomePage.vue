<script setup lang="ts">
import { useFeVersionChecker } from '../utils/feVersionCheck';

const intro = import.meta.env.VITE_INTRO
const contact = import.meta.env.VITE_CONTACT
const nonProfitNotice = import.meta.env.VITE_NONPROFIT_NOTICE
const { checkAndPop } = useFeVersionChecker()
checkAndPop()
</script>

<template>
<h1>欢迎来到轨交棋</h1>
    <div>
        <img src="/railchessLogo.svg" class="logo"/>
        <p v-if="intro">
            {{ intro }}
        </p>
        <p v-if="nonProfitNotice" style="color: cornflowerblue;">
            {{ nonProfitNotice }}
        </p>
        <div v-if="contact" class="contact">
            {{ contact }}
        </div>
        <router-link :to="{name:'aarcConverter'}" class="converter-entry">
            AARC &rarr; 轨交棋 转换器
        </router-link>
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
.converter-entry{
    display: block;
    width: calc(100% - 40px);
    max-width: 360px;
    height: 30px;
    line-height: 30px;
    border-radius: 10px;
    margin: 10px auto 16px auto;
    color: white;
    background: linear-gradient(to right, #31bcb5, #8F98FA);
    text-align: center;
    text-decoration: none;
    transition: 0.3s;
}
.converter-entry:hover{
    transform: scale(1.03);
    box-shadow: 0px 4px 6px 0px rgba(0, 0, 0, 0.6);
}
</style>