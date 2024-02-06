<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { injectApi } from '../provides';
import { Api } from '../utils/api';
import { RailChessMapIndexResult, RailChessMapIndexResultItem } from '../models/map';
import Loading from '../components/Loading.vue';
import SideBar from '../components/SideBar.vue';
import Notice from '../components/Notice.vue';
import { router } from '../main';

const search = ref<string>();
const data = ref<RailChessMapIndexResult>();
async function load(){
    const res = await api.map.index(search.value||"")
    if(res){
        data.value = res;
    }
}

const sidebar = ref<InstanceType<typeof SideBar>>();
const editing = ref<RailChessMapIndexResultItem>();
const file = ref<InstanceType<typeof HTMLInputElement>>();
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
        ExcStationCount:0
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
        await load();
        sidebar.value?.fold();
    }
}
function bgFilePath(fileName:string){
    return import.meta.env.VITE_BASEURL+"/maps/"+fileName;
}

function toTopo(id:number){
    router.push({name:'topo',params:{id}});
}

const ipt = ref<HTMLInputElement>();
async function clickIpt() {
    if(window.confirm("将会覆盖已有数据，确认操作")){
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

var api:Api;
onMounted(async()=>{
    api = injectApi()
    await load();
})
</script>

<template>
<div class="maps">
    <h1>棋盘列表</h1>
    <input v-model="search" style="width: 150px;" placeholder="搜索棋盘或作者" @blur="load"/>
    <button @click="create" class="confirm">新建</button>
    <button class="gray" @click="search='我上传的';load()">我的</button>
    <table class="list" v-if="data">
        <tr>
            <th class="titleTh">名称</th>
            <th>作者</th>
            <th></th>
        </tr>
        <tr v-for="m in data.Items">
            <td>
                <div class="title">
                    <div class="titleLeft">
                        <div>{{ m.Title }}</div>
                        <div class="mapInfo">
                            {{ m.LineCount }}线 
                            {{ m.StationCount }}站 
                            {{ m.ExcStationCount }}换乘站
                        </div>
                    </div>
                    <span class="date">{{ m.Date }}</span>
                </div>
            </td>
            <td>
                <span class="authorName" @click="search = m.Author; load();">{{ m.Author }}</span>
            </td>
            <td>
                <div class="ops">
                    <button class="minor" @click="edit(m.Id)">信息</button>
                    <button class="minor" @click="toTopo(m.Id)">编辑</button>
                </div>
            </td>
        </tr>
        <tr v-if="data.Items.length==20">
            <td style="color:#999;">仅显示前20个结果</td><td></td><td></td>
        </tr>
    </table>
    <Loading v-else>
    </Loading>
</div>
<SideBar ref="sidebar">
    <h1>{{ editing && editing.Id>0 ? editing.Title : '新建棋盘' }}</h1>
    <div class="previewDiv" v-if="editing?.FileName">
        <img class="preview" :src="bgFilePath(editing.FileName)"/>
    </div>
    <table v-if="editing">
        <tr>
            <td>名称</td>
            <td><input v-model="editing.Title"></td>
        </tr>
        <tr>
            <td>文件</td>
            <td>
                <button @click="file?.showPicker" class="minor">点击选择</button>
                <div v-if="fileSelected">已选择文件</div>
                <input ref="file" type="file" @change="fileChange"/>
            </td>
        </tr>
        <tr class="noneBackground">
            <td></td>
            <td><button @click="confirm">确认</button></td>
        </tr>
    </table>
    <Loading v-else></Loading>
    <Notice v-if="editing && editing.Id>0" :type="'warn'">
        注意：替换背景图片时请确保新图片和旧图片中车站的相对位置一致
    </Notice>
    <Notice v-if="editing && editing.Id==0" :type="'warn'">
        注意：如果地图后续会扩大，提前预留好位置，确保替换背景图片时新图片和旧图片中车站的相对位置一致
    </Notice>
    <div class="iptOuter">
        <button v-if="editing && editing.Id>0" class="minor" @click="clickIpt">导入数据</button>
        <input type="file" ref="ipt" @change="iptChange" accept=".json"/>
    </div>
    <div class="iptOuter">
        <button v-if="editing && editing.Id>0" class="danger" @click="deleteMap(editing.Id)">删除</button>
    </div>
</SideBar>
</template>

<style scoped>
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
    cursor: pointer;
}
.authorName:hover{
    text-decoration: underline;
}
.mapInfo{
    font-size: 14px;
    color:#777;
    font-weight: normal;
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
table.list{
    table-layout: fixed;
    width: 100%;
}
</style>