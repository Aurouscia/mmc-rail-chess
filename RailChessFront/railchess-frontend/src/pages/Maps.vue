<script setup lang="ts">
import { computed, nextTick, onMounted, ref, useTemplateRef } from 'vue';
import { injectApi, injectPop } from '../provides';
import { Api } from '../utils/api';
import { RailChessMapIndexResult, RailChessMapIndexResultItem } from '../models/map';
import Loading from '../components/Loading.vue';
import SideBar from '../components/SideBar.vue';
import Notice from '../components/Notice.vue';
import copy from 'copy-to-clipboard';
import { useRoute, useRouter } from 'vue-router';

const pop = injectPop()
const router = useRouter()
const route = useRoute()
const search = ref<string>();
const orderBy = ref<'score'|undefined>()
const scoreMin = ref<number>(0)
const scoreMax = ref<number>(0)
const scoreMinOptions = [1, 200, 500, 1000, 2000]
const scoreMaxOptions = [200, 500, 1000, 2000]
const pageIdx = ref<number>(0)
const pageSize = ref<number>(18)
async function clearSearch(){
    search.value = undefined
    scoreMin.value = 0
    scoreMax.value = 0
    pageIdx.value = 0;
    await load()
}
const isSearching = computed<boolean>(()=> !!search.value || !!scoreMin.value || !!scoreMax.value)

const data = ref<RailChessMapIndexResult>();
async function load(){
    const skip = pageIdx.value * pageSize.value;
    const take = pageSize.value;
    const res = await api.map.index(search.value, orderBy.value, scoreMin.value, scoreMax.value, skip, take)
    if(res){
        data.value = res;
    }
}

const authorSearchPrefix = "作者："
const mineSearch = "作者：我自己"
const idSearchPrefix = "ID："
async function searchAuthorName(authorName:string) {
    search.value = `${authorSearchPrefix}${authorName}`
    pageIdx.value = 0;
    await load()
}
async function searchMine() {
    search.value = mineSearch;
    pageIdx.value = 0;
    await load()
}
async function searchId(id:number) {
    search.value = `${idSearchPrefix}${id}`
    pageIdx.value = 0
    await load()
}

async function switchPage(to:'prev'|'next') {
    let switched = false
    if(to=='prev' && pageCanPrev.value){
        pageIdx.value--;
        switched = true;
    }else if(to=='next' && pageCanNext.value){
        pageIdx.value++;
        switched = true;
    }
    if(switched){
        await load();
        await nextTick();
        const mainOuter = document.querySelector('.mainOuter')
        if(mainOuter)
            mainOuter.scrollTo(0,0);
        const mapTableOuter = document.querySelector('#mapTableOuter')
        if(mapTableOuter)
            mapTableOuter.scrollTo(0,0);
    }
}
async function handleBlur() {
    pageIdx.value = 0;
    await load();
}
const totalPageCount = computed<number>(()=>{
    if(!data.value) return 0; 
    return Math.ceil(data.value.TotalCount/pageSize.value);
})
const pageCanPrev = computed<boolean>(()=>pageIdx.value>0)
const pageCanNext = computed<boolean>(()=>pageIdx.value<totalPageCount.value-1)

const sidebar = useTemplateRef('sidebar')
const editing = ref<RailChessMapIndexResultItem>();
const file = useTemplateRef('file')
const fileSelected = ref<File|undefined>();
function create(){
    editing.value = {
        Id: 0,
        Title: "",
        Author: "",
        Date:"",
        FileName:undefined,
        LineCount:0,
        StationCount:0,
        ExcStationCount:0,
        TotalDirections:0
    }
    fileSelected.value = undefined;
    if(file.value)
        file.value.value = ""
    sidebar.value?.extend();
}
function edit(id:number){
    const target = data.value?.Items.find(x=>x.Id==id);
    if(!target){return;}
    editing.value = target;
    fileSelected.value = undefined;
    if(file.value)
        file.value.value = "";
    sidebar.value?.extend();
}
function fileChange(){
    if(!file.value || !file.value.files){
        return;
    }
    fileSelected.value = file.value.files[0]
}
async function confirm(){
    if(!editing.value){return;}
    const res = await api.map.createOrEdit(editing.value.Id, editing.value.Title, fileSelected.value)
    if(res){
        clearSearch();
        orderBy.value = undefined;
        await load();
        sidebar.value?.fold();
    }
}
function bgFilePath(fileName:string){
    return import.meta.env.VITE_BASEURL+"/maps/"+fileName;
}

function toTopo(id:number){
    let url = router.resolve({name:'topo',params:{id}}).href;
    window.open(url, "_blank")
}

const ipt = useTemplateRef('ipt')
async function clickIpt() {
    if(window.confirm("将会覆盖已有数据，请核对名称")){
        ipt.value?.showPicker();
    }
}
async function iptChange(){
    if(ipt.value && ipt.value.files){
        if(ipt.value.files.length>0 && editing.value){
            const res = await api.map.importTopo(editing.value.Id, ipt.value.files[0])
            if(res){
                await load();
                sidebar.value?.fold();
            }
        }
    }
}

async function deleteMap(id:number) {
    if(window.confirm("确认删除该棋盘")){
        const resp = await api.map.delete(id);
        if(resp){
            await load();
            sidebar.value?.fold();
        }
    }
}

function copyMapName(name:string){
    copy(name);
    pop.value.show('已复制棋盘名称', 'success')
}

const baseUrl = import.meta.env.VITE_BASEURL;
var api:Api;
onMounted(async()=>{
    api = injectApi()
    let id = Number(route.query.id)
    if(id > 0)           
        await searchId(id)
    else
        await load();
})
</script>

<template>
<div class="maps">
    <h1 class="h1WithBtns">
        棋盘列表
        <div>
            <button @click="router.push({name:'aarcConverter'})" class="minor">转换器</button>
            <button @click="create" class="confirm">新建棋盘</button>
        </div>
    </h1>
    <div class="aboveTable">
        <div>
            <input v-model="search" style="width: 150px;" placeholder="搜索棋盘或作者" @blur="handleBlur"/>
            <div>
                <select v-model="scoreMin" @change="handleBlur">
                    <option :value="0">分下限</option>
                    <option v-for="s in scoreMinOptions" :value="s">{{ s }}</option>
                </select>
                <select v-model="scoreMax" @change="handleBlur">
                    <option :value="0">分上限</option>
                    <option v-for="s in scoreMaxOptions" :value="s">{{ s }}</option>
                </select>
            </div>
            <button v-if="isSearching" @click="clearSearch" class="lite">清空筛选</button>
        </div>
        <div>
            <div>
                <button @click="searchMine" class="minor mine-filter">我创建的</button>
            </div>
            <div>
                <select v-model="orderBy" @change="handleBlur">
                    <option :value="undefined">最新</option>
                    <option :value="'score'">分数</option>
                </select>
            </div>
        </div>
    </div>
    <div id="mapTableOuter" style="overflow-x: auto;">
    <table class="list" v-if="data"><tbody>
        <tr>
            <th class="titleTh" style="min-width: 200px;">名称</th>
            <th style="min-width: 130px;">作者</th>
            <th style="width: 100px;"></th>
        </tr>
        <tr v-for="m in data.Items">
            <td>
                <div class="title">
                    <div class="titleLeft">
                        <div class="titleText" @click="copyMapName(m.Title)">
                            {{ m.Title }}
                        </div>
                        <div class="mapInfo">
                            {{ m.LineCount }}线&nbsp;
                            {{ m.StationCount }}站&nbsp;
                            {{ m.ExcStationCount }}换乘站&nbsp;
                            {{ m.TotalDirections }}分
                        </div>
                    </div>
                    <span class="date">{{ m.Date }}</span>
                </div>
            </td>
            <td>
                <span class="authorName" @click="searchAuthorName(m.Author)">
                    {{ m.Author }}
                </span>
            </td>
            <td>
                <div class="ops">
                    <button class="minor" @click="edit(m.Id)">信息</button>
                    <button class="minor" @click="toTopo(m.Id)">编辑</button>
                </div>
            </td>
        </tr>
        <tr>
            <td v-if="totalPageCount">
                <div class="pager">
                    <button class="lite" @click="switchPage('prev')" :class="{canClick:pageCanPrev}">上页</button>
                    <span>第 {{ pageIdx+1 }}/{{ totalPageCount }} 页</span>
                    <button class="lite" @click="switchPage('next')" :class="{canClick:pageCanNext}">下页</button>
                </div>
            </td>
            <td v-else>
                <div class="pager">
                    <span>{{ isSearching ? '搜索结果为空':'暂无数据' }}</span>
                </div>
            </td>
            <td colspan="2" style="color: #666">
                共 {{ data.TotalCount }} 条数据
            </td>
        </tr>
    </tbody></table>
    <Loading v-else>
    </Loading>
    </div>
</div>
<SideBar ref="sidebar">
    <h1>{{ editing && editing.Id>0 ? editing.Title : '新建棋盘' }}</h1>
    <div class="previewDiv" v-if="editing?.FileName">
        <img class="preview" :src="bgFilePath(editing.FileName)"/>
    </div>
    <table v-if="editing"><tbody>
        <tr>
            <td>名称</td>
            <td><input v-model="editing.Title"></td>
        </tr>
        <tr>
            <td>背景<br/>图片</td>
            <td>
                <button @click="file?.showPicker" class="minor">点击选择</button>
                <div v-if="fileSelected">已选择文件</div>
                <input ref="file" type="file" @change="fileChange"/>
            </td>
        </tr>
        <tr class="noneBackground">
            <td colspan="2"><button @click="confirm">确认</button></td>
        </tr>
    </tbody></table>
    <Loading v-else></Loading>
    <Notice v-if="editing && editing.Id>0" :type="'warn'">
        注意：替换背景图片时请确保新图片和旧图片中车站的相对位置一致
    </Notice>
    <Notice v-if="editing && editing.Id==0" :type="'warn'">
        注意：如果地图后续会扩大，提前预留好位置，确保替换背景图片时新图片和旧图片中车站的相对位置一致
    </Notice>
    <Notice :type="'warn'">
        注意：使用复杂的svg格式图片可能导致客户端视角缩放卡顿，建议使用png/webp格式图片
    </Notice>
    <div class="iptOuter">
        <button v-if="editing && editing.Id>0" class="minor" @click="clickIpt">导入数据</button>
        <input type="file" ref="ipt" @change="iptChange" accept=".json"/>
    </div>
    <div class="iptOuter">
        <a v-if="editing && editing.Id>0" :href="`${baseUrl}/api/Map/ExportTopo?id=${editing.Id}`">
            <button class="minor">导出数据</button>
        </a>
    </div>
    <div class="iptOuter">
        <button v-if="editing && editing.Id>0" class="danger" @click="deleteMap(editing.Id)">删除</button>
    </div>
</SideBar>
</template>

<style scoped>
.pager{
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 6px;
}
.pager .canClick{
    color: cornflowerblue
}

.iptOuter{
    margin-top: 20px;
    display: flex;
    justify-content: center;
}

.previewDiv{
    display: flex;
    justify-content: center;
}
.preview{
    width: 200px;
    min-height: 100px;
    max-height: 300px;
    object-fit: contain;
}
input[type=file]{
    display: none;
}


.ops{
    display: flex;
    justify-content: space-around;
    align-items: center;
}
.date{
    font-size: 14px;
    color:#777
}
.authorName{
    display: block;
    cursor: pointer;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}
.authorName:hover{
    text-decoration: underline;
}
.mapInfo{
    font-size: 14px;
    color:#777;
    font-weight: normal;
}
.titleText{
    cursor: pointer;
}
.titleText:hover{
    text-decoration: underline;
}
.titleLeft{
    text-align: left;
    font-size: 18px;
    font-weight: 600;
}
.title{
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    align-items: center;
}
.titleTh{
    width:50%;
}
.aboveTable{
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.aboveTable>div{
    display: flex;
    flex-wrap: wrap;
    justify-content: flex-start;
    align-items: stretch;
}
.aboveTable>div:last-child{
    justify-content: flex-end;
}

.mine-filter{
    font-size: unset;
    border: 1px solid #ccc;
    color: black;
    margin: 5px;
    padding: 3px;
    height: 28px;
    line-height: 22px;
}
</style>