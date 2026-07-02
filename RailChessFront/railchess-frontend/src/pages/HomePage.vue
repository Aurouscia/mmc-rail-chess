<script setup lang="ts">
import { ref } from 'vue'
import copy from 'copy-to-clipboard'

const intro = import.meta.env.VITE_INTRO
const contact = import.meta.env.VITE_CONTACT
const nonProfitNotice = import.meta.env.VITE_NONPROFIT_NOTICE

interface WidgetConfig {
    id: string
    src: string
    title: string
}

const widgets: WidgetConfig[] = [
    { id: 'active', src: '/api/embed/active?count=5&theme=light', title: '当前进行中的对局' },
    { id: 'recent', src: '/api/embed/recent?count=5&theme=light', title: '最新完成对局' }
]

const copiedId = ref<string | null>(null)

function iframeHtml(src: string): string {
    const absSrc = new URL(src, window.location.origin).href
    return `<iframe src="${absSrc}" width="320" height="420" frameborder="0"></iframe>`
}

function copyHtml(id: string, src: string) {
    const html = iframeHtml(src)
    if (copy(html)) {
        copiedId.value = id
        setTimeout(() => {
            if (copiedId.value === id) {
                copiedId.value = null
            }
        }, 2000)
    }
}
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
    <img style="width: 80vw; max-width: 400px; margin: auto; display: block;" src="/mor26poster.jpg"/>
    <div class="widgets">
        <div v-for="w in widgets" :key="w.id" class="widget-wrapper">
            <iframe :src="w.src" class="widget" :title="w.title"></iframe>
            <button class="copy-btn" @click="copyHtml(w.id, w.src)">
                {{ copiedId === w.id ? '复制成功' : '复制widget代码' }}
            </button>
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
.widgets{
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 16px;
    margin: 20px 0;
}
.widget-wrapper{
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
}
.widget{
    width: 100%;
    max-width: 320px;
    height: 420px;
    border: 1px solid #eee;
    border-radius: 8px;
    background: #fff;
    box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    overflow: visible;
}
.copy-btn{
    padding: 6px 14px;
    font-size: 13px;
    color: #666;
    background: #f5f5f5;
    border: 1px solid #ddd;
    border-radius: 6px;
    cursor: pointer;
    transition: 0.2s;
}
.copy-btn:hover{
    background: #eee;
    color: #333;
}
.gitInfo iframe{
    width: 100%;
    max-width: 600px;
    height: 500px;
    border: none;
}
.gitInfo{
    display: flex;
    align-items: center;
    justify-content: center;
}
.converter-entry {
    display: block;
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