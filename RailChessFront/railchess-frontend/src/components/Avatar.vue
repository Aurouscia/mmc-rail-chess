<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import SideBar from './SideBar.vue';
import { Api } from '../utils/api';
import { injectApi } from '../provides';
import { getTimeStamp } from '../utils/timeStamp';
import { avtSrc } from '../utils/fileSrc';


const props = defineProps<{
    fileName?:string
}>();
onMounted(()=>{
    setSrc()
})

const sidebar = ref<InstanceType<typeof SideBar>>();
const file = ref<HTMLInputElement>();
async function pick() {
    if(!file.value){
        return false;
    }
    file.value.showPicker();
}
async function pickDone() {
    if(!file.value){
        return false;
    }
    if(file.value.files && file.value.files.length>0){
        const resp = await api.identites.user.setAvatar(file.value.files[0]);
        if(resp){
            file.value.value = "";
            emit('needRefresh')
            sidebar.value?.fold();
        }
    }
}
const src = ref<string>();
function setSrc(){
    if(!props.fileName){
        src.value = undefined;
        return;
    }
    src.value = avtSrc(props.fileName);
}
watch(props,()=>{
    setSrc();
})

const colorList = ref<string[]>(["#000000"]);
const cvs = ref<HTMLCanvasElement>();
const cvsSide = 256;
const startAngle = ref<number>(0);
const xOffset = ref<number>(0);
const yOffset = ref<number>(0);
const text = ref<string>("");
const textColor = ref<string>("#ffffff");
function addColor(){
    colorList.value?.push("#000000");
    render();
}
function removeColor(idx:number){
    colorList.value?.splice(idx,1);
    render();
}
var lastRender = 0;
function tryRender(){
    var time = getTimeStamp();
    if(time-lastRender<0.03){return;}
    lastRender = time;
    render();
}
function render(){
    if(!cvs.value){return}
    var ctx = cvs.value.getContext('2d');
    if(!ctx){return}
    ctx.fillStyle="#ffffff"
    ctx.fillRect(0,0,cvsSide,cvsSide);
    const start = startAngle.value/360*2*Math.PI;
    const angleEach = 2*Math.PI/colorList.value.length;
    var counter = 0;
    colorList.value.forEach(c=>{
        if(!ctx){return}
        ctx.beginPath()
        ctx.moveTo(cvsSide/2,cvsSide/2);
        var s = start+counter*angleEach;
        var e = start+(counter+1)*angleEach;
        ctx.arc(cvsSide/2,cvsSide/2,cvsSide/2,s,e,false)
        ctx.fillStyle = c;
        ctx.fill();
        counter++;
    })
    ctx.fillStyle = textColor.value;
    ctx.font = `${cvsSide*0.8}px msyh`;
    ctx.textAlign = 'center'
    ctx.textBaseline = 'hanging'
    ctx.fillText(text.value, cvsSide/2 + xOffset.value/3, cvsSide*0.1 + yOffset.value/3);
}
function done(){
    if(cvs.value){
        cvs.value.toBlob(async(b)=>{
            if(b){
                var f = new File([b],"avt.png");
                const resp = await api.identites.user.setAvatar(f);
                if(resp){
                    emit('needRefresh');
                    sidebar.value?.fold();
                }
            }
        });
    }
}

var api:Api;
onMounted(()=>{
    api = injectApi();
})
const emit = defineEmits<{
    (e:'needRefresh'):void
}>()
</script>

<template>
    <div class="avatar" @click="sidebar?.extend">
        <img class="img" v-if="src" :src="src"/>
        <div class="img" v-else>
            请设头像
        </div>
    </div>
    <SideBar ref="sidebar">
        <h1>上传头像</h1>
        <button @click="pick">选择本地图片</button>
        <input ref="file" @change="pickDone" type="file">
        <h1 class="makeAvtH1">创建头像</h1>
        <canvas class="makeAvt" ref="cvs" :width="cvsSide" :height="cvsSide"></canvas>
        <div v-if="colorList" class="colorList">
            <div v-for="_c,idx in colorList">
                <input type="color" v-model="colorList[idx]" @input="tryRender" @blur="render"/>
                <button v-show="colorList.length>1" @click="removeColor(idx)" class="cancel">移除</button>
            </div>
            <div>
                <button @click="addColor" v-show="colorList.length<4" class="minor" style="flex-grow: 1;">添加颜色+</button>
            </div>
            <div>背景角度偏移<input v-model="startAngle" type="range" min="0" max="360" @input="tryRender" @blur="render"></div>
            <div>文字横向偏移<input v-model="xOffset" type="range" :min="-cvsSide/2" :max="cvsSide/2" step="1" @input="tryRender" @blur="render"></div>
            <div>文字纵向偏移<input v-model="yOffset" type="range" :min="-cvsSide/2" :max="cvsSide/2" step="1" @input="tryRender" @blur="render"></div>
            <div>
                <input v-model="text" maxlength="1" placeholder="写字" style="width: 50px;" @blur="render"/>
                <input v-model="textColor" type="color" @input="tryRender" @blur="render">
            </div>
            <div>
                <button @click="done" class="ok">设为头像</button>
            </div>
        </div>
    </SideBar>
</template>

<style scoped>
.colorList>div{
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
    background-color: #eee;
}
.colorList{
    display: flex;
    flex-direction: column;
    gap:5px
}
.makeAvt{
    width: 250px;
    height: 250px;
    border-radius: 1000px;
}
.makeAvtH1{
    margin-top: 50px;
}
input{
    width: 150px;
}
input[type=number]{
    width: 50px;
}
input[type=file] {
    display: none;
}
.avatar,.img {
    position: absolute;
    top: 0px;
    bottom: 0px;
    left: 0px;
    right: 0px;
}
.img {
    width: 64px;
    height: 64px;
    border-radius: 1000px;
    border: #ccc solid 2px;
    background-color: white;
}
img.img {
    object-fit: contain;
}
div.img {
    text-align: center;
    font-size: 15px;
    line-height: 64px;
    color: #aaa
}
</style>