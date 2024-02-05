<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { injectApi } from '../provides';
import { Api } from '../utils/api';
import { RailChessMapIndexResult, RailChessMapIndexResultItem } from '../models/map';
import Loading from '../components/Loading.vue';
import SideBar from '../components/SideBar.vue';

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
        Date:""
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
                    {{ m.Title }}
                    <span class="date">{{ m.Date }}</span>
                </div>
            </td>
            <td>
                <span class="authorName" @click="search = m.Author; load();">{{ m.Author }}</span>
            </td>
            <td>
                <button class="minor" @click="edit(m.Id)">编辑</button>
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
    <h1>{{ editing?.Title ? editing.Title : '新建棋盘' }}</h1>
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
        <tr>
            <td></td>
            <td><button @click="confirm">确认</button></td>
        </tr>
    </table>
    <Loading v-else></Loading>
</SideBar>
</template>

<style scoped>
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
.date{
    font-size: 14px;
    color:#999
}
.authorName{
    cursor: pointer;
}
.authorName:hover{
    text-decoration: underline;
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