<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';
import { Line, RailChessTopo, Sta, StaParsed, toSta, toStaParsed } from '../models/map';
import { Api } from '../utils/api';
import { injectApi, injectHideTopbar, injectPop } from '../provides';
import { MouseDragListener } from '../utils/mouseDrag';
import SideBar from '../components/SideBar.vue'
import { router } from '../main';
import { AurStateStore } from '@aurouscia/au-undo-redo'
import { bgSrc } from '../utils/fileSrc';
import { posBase } from '../models/map';
import { clone, pullAt } from 'lodash';
import TopoRepairTool from './TopoRepairTool.vue';

const props = defineProps<{
  id:string
}>()
const bgImg = ref<string>();
const lines = ref<Line[]>([]);
const stations = ref<StaParsed[]>([]),
selectedLineId = ref<number>(-1);

const autoSave = 20
interface TempStore{lines:Line[],stations:StaParsed[]}

const cvsWidth = ref<number>(0);
const cvsHeight = ref<number>(0);

const closeDefKey = 'closeDef'
const closeDefRead = Number(localStorage.getItem(closeDefKey)) || 0.007
const closeDef = ref<number>(closeDefRead)

const cvs = ref<HTMLCanvasElement>();
const bg = ref<HTMLDivElement>();
var ctx:CanvasRenderingContext2D;

const canUndo = ref<boolean>(false);
const canRedo = ref<boolean>(false);
const ss = new AurStateStore<TempStore>(10,(u,r)=>{
  canUndo.value = u;
  canRedo.value = r;
});
function undo(){
  if(!canUndo.value){return}
  const hist = ss.undo();
  if(hist){
    lines.value = hist.lines;
    stations.value = hist.stations;
    Render();
  }
}
function redo(){
  if(!canRedo.value){return}
  const hist = ss.redo();
  if(hist){
    lines.value = hist.lines;
    stations.value = hist.stations;
    Render();
  }
}

function AddLine(){
  var maxId = MaxLineId();
  var newLine:Line={
    Id:maxId+1,
    Stas:[],
  };
  lines.value.push(newLine);
  SelectLine(maxId+1);
}
function MaxLineId(){
  var max=0;
  lines.value.forEach(line => {
    if(line.Id>max){
      max=line.Id;
    }
  });
  return max;
}
function MaxStationId(){
  var max=0;
  stations.value.forEach(sta => {
    if(sta.Id>max){
      max=sta.Id;
    }
  });
  return max;
}
function SelectLine(id:number){
  selectedLineId.value=id;
  Render();
}
function CvsInit(){
  var c=cvs.value
  var i=bg.value
  if(!c||!i){return;}
  c.style.width = i.clientWidth+"px";
  c.style.height = i.clientHeight+"px";
  c.width = i.clientWidth;
  c.height = i.clientHeight;
  cvsWidth.value = i.clientWidth;
  cvsHeight.value = i.clientHeight;
  const context = c.getContext("2d");
  if(context){
    ctx = context
    Render();
  }
}
function Render(){
  localStorage.setItem(closeDefKey, closeDef.value.toString())
      ctx.clearRect(0,0,cvsWidth.value,cvsHeight.value);
      ctx.strokeStyle="#008800";
      ctx.lineWidth=2;
      const linesCopy = clone(lines.value);
      const selectedIdx = linesCopy.findIndex(x=>x.Id==selectedLineId.value);
      if(selectedIdx!=-1){
        const selectedOne = pullAt(linesCopy, selectedIdx);
        linesCopy.push(...selectedOne);
      }
      linesCopy.forEach(line=>{
        if(line.Id==selectedLineId.value){
          ctx.strokeStyle="#ff0000";
          ctx.lineWidth=3;
        }
        ctx.beginPath();
        if(line.Stas.length!=0){
          var cx:number=-1;
          var cy:number=-1;
          while(true){
            const sta = stations.value.find(x=>x.Id==line.Stas[0]);
            if(!sta ){
              if(line.Stas.length>0){
                line.Stas.splice(0,1);
                continue;
              }
              else{
                break;
              }
            }
            cx = sta.X/posBase*cvsWidth.value;
            cy = sta.Y/posBase*cvsHeight.value;
            break;
          }
          if(cx<0||cy<0){return;}

          ctx.moveTo(cx,cy);
          line.Stas.forEach(staId=>{
            var sta = stations.value.find(s=>s.Id==staId);
            if(!sta){return;}
            var cx = sta.X/posBase*cvsWidth.value;
            var cy = sta.Y/posBase*cvsHeight.value;
            ctx.lineTo(cx,cy);
          })
          ctx.stroke();
        }
        ctx.strokeStyle="#008800";
        ctx.lineWidth=2;
      });

      stations.value.forEach(sta=>{
        var cx = sta.X/posBase*cvsWidth.value;
        var cy = sta.Y/posBase*cvsHeight.value;
        ctx.fillStyle = 'black'
        const xClose = closeDef.value * cvsWidth.value
        const yClose = closeDef.value * cvsHeight.value
        const l = cx - xClose
        const u = cy - yClose
        ctx.globalAlpha = 0.3
        ctx.fillRect(l, u, xClose*2, yClose*2)
        ctx.globalAlpha = 1
        ctx.beginPath();
        ctx.arc(cx,cy,3,0,Math.PI*2,true);
        ctx.fillStyle=StationColor(sta.Id);
        ctx.fill();
      });
}
function StationColor(id:number){
      var line = lines.value.find(line=>line.Id==selectedLineId.value);
      if(line==undefined){
        return "#000000";
      }
      var idx = line.Stas.findIndex(s=>s==id);
      if(idx!=-1){
        if(idx==line.Stas.length-1){
          return "#ff00ff";
        }
        return "#ff0000";
      }
      return "#008800";
}
function ClickCvs(e: MouseEvent) {
  var line = lines.value.find(l => l.Id == selectedLineId.value);
  var x = e.offsetX / cvsWidth.value * posBase;
  var y = e.offsetY / cvsHeight.value * posBase;
  var s = stations.value.find(s => CloseTo(s, x, y));
  if (!s && line && !e.ctrlKey) {
    var newSta: StaParsed = {
      Id: MaxStationId() + 1,
      X: x,
      Y: y
    }
    stations.value.push(newSta);
    line.Stas.push(newSta.Id);
    ssSave();
  }
  else if (s && !line) {
    if (e.ctrlKey) {
      DeleteStation(s.Id);
      ssSave();
    }
    else {
      // s.X=x;
      // s.Y=y;
    }
  }
  else if (s && line) {
    if (e.ctrlKey) {
      var internalIdx = line.Stas.findIndex(sta => s && sta == s.Id);
      line.Stas.splice(internalIdx, 1);
      ssSave();
    }
    else {
      var l = line.Stas.length;
      if (line.Stas[l - 1] != s.Id) {
        line.Stas.push(s.Id);
        ssSave()
      } else {
        window.alert("两个站不可过近");
      }
    }
  }
  Render();
  TryAutoSave();
}
function CloseTo(station:StaParsed,x:number,y:number){
  return (Math.abs(station.X-x)<=closeDef.value*posBase && Math.abs(station.Y-y)<=closeDef.value*posBase);
}
function DeleteStation(id:number){
      lines.value.forEach(line=>{
        var tarIdx = -1;
        do{
          tarIdx=line.Stas.findIndex(s=>s==id);
          if(tarIdx!=-1){
            line.Stas.splice(tarIdx,1);
          }
        }while(tarIdx!=-1);
      });
      var tarIdx = stations.value.findIndex(s=>s.Id==id);
      if(tarIdx!=-1){
        stations.value.splice(tarIdx,1);
      }
      Render();
}
function DeleteLine(id:number){
  if(confirm("确定删除选中线路")){
    var tarIdx = lines.value.findIndex(line=>line.Id==id);
    if(tarIdx==-1){
      return;
    }
    lines.value.splice(tarIdx,1);
    Render();
    ssSave();
  }
}
function ReverseLine(id:number){
  var tar = lines.value.find(line=>line.Id==id);
  if(!tar){
    return;
  }
  tar.Stas.reverse();
  Render();
  ssSave();
}
async function Save(noJump?:boolean) {
  var stas: Sta[] = [];
  stations.value.forEach(x => stas.push(toSta(x)));
  var data:RailChessTopo = {
    Stations: stas,
    Lines: lines.value
  }
  const idNum = parseInt(props.id);
  if (idNum && idNum > 0) {
    const resp = await api.map.saveTopo(idNum, data);
    if(resp && !noJump){
      router.push("/maps")
    }
  }
  autoSaveCounter = 0;
}
function UpCvs(e:MouseEvent){
  if(e.button==2){
    SelectLine(-1)
    return;
  }
}

var autoSaveCounter=0;
async function TryAutoSave(){
  if(autoSaveCounter<autoSave){
    autoSaveCounter++;
  }
  else{
    await Save(true);
    autoSaveCounter=0;
  }
}
async function ssSave() {
  ss.push({lines:lines.value,stations:stations.value})
}

const showRepairTool = ref<boolean>(false)
function repairDone(changed:boolean){
  if(changed){
    ssSave()
    Save(true)
  }
  showRepairTool.value = false;
}

var api:Api;
var disposeListeners:()=>void;
var movingSta:number = 0;
const sb = ref<InstanceType<typeof SideBar>>();
const pop = injectPop()
onMounted(async()=>{
  injectHideTopbar()();
  api = injectApi();
  window.addEventListener('resize', windowResizeHandler)
  if(props.id){
    const idNum = parseInt(props.id);
    if(idNum && idNum>0){
      const res = await api.map.loadTopo(idNum);
      if(res && res.TopoData){
        const data = JSON.parse(res.TopoData) as RailChessTopo;
        lines.value = data.Lines || [];
        if(data.Stations){
          data.Stations.forEach(s=>{
            stations.value.push(toStaParsed(s))
          })
        }else{
          stations.value = [];
        }
        ssSave();
      }
      if(res && res.FileName){
        bgImg.value = bgSrc(res.FileName);
      }
      if(res && res.WarnMsg){
        pop.value.show(res.WarnMsg, 'warning')
      }
    }
  }
  else {
    window.alert("请从正确入口进入");
  }
  var listener = new MouseDragListener()
  if(!cvs.value){return;}
  disposeListeners = listener.startListen(cvs.value,
  (x,y)=>{
    if(selectedLineId.value>0){
      return;
    }
    x = x/cvsWidth.value*posBase;
    y = y/cvsHeight.value*posBase;
    if(movingSta==0){
      var s = stations.value.find(s=>CloseTo(s,x,y));
      movingSta = s?.Id || 0;
    }
  },
  (x,y)=>{
    if(movingSta>0){
      x = x/cvsWidth.value*posBase;
      y = y/cvsHeight.value*posBase;
      var s = stations.value.find(s=>s.Id==movingSta);
      if(s){
        s.X = x;s.Y = y;
        Render();
      }
    }
  },(x,y)=>{
    if(movingSta>0){
      x = Math.round(x/cvsWidth.value*posBase);
      y = Math.round(y/cvsHeight.value*posBase);
      var s = stations.value.find(s=>s.Id==movingSta);
      if(s){
        s.X = x;s.Y = y;
        Render();
      }
    }
    movingSta = 0;
    ssSave()
  });
  document.oncontextmenu = function(e){
    e.preventDefault()
  }
})
onUnmounted(()=>{
  disposeListeners();
  window.removeEventListener('resize', windowResizeHandler)
})

let resizeStoppedTimer = 0;
function windowResizeHandler(){
  clearTimeout(resizeStoppedTimer);
  resizeStoppedTimer = setTimeout(()=>{
    CvsInit();
  }, 500)
}
</script>

<template>
  <div class="topbar">
    <div class="lineList">
      <div v-for="line,idx in lines" @click="SelectLine(line.Id)" :class="line.Id == selectedLineId ? 'selected btn' : 'btn'" :key="line.Id">
        [{{ idx }}]{{ line.Stas.length }}站
      </div>
      <div class="btn" @click="AddLine()" style="background-color: olivedrab;color:white">
        加线
      </div>
    </div>
    <div class="btns">
      <div v-if="selectedLineId>=0" class="btn" @click="DeleteLine(selectedLineId)" style="background-color: plum;color:white">删除该线</div>
      <div v-if="selectedLineId>=0" class="btn" @click="ReverseLine(selectedLineId)">头尾反转</div>
      <div class="btn" @click="showRepairTool = true">修复工具</div>
      <div class="btn" @click="sb?.extend">使用说明</div>
      <div class="btn" @click="Save()" style="background-color: olivedrab;color:white">保存</div>
    </div>
  </div>

  <div class="area">
    <img class="bg" ref="bg" :src="bgImg" @load="CvsInit()" />
    <canvas ref="cvs" @click="ClickCvs" @mouseup="UpCvs"></canvas>
  </div>

  <div class="ur">
    <div :class="{locked:!canUndo}" @click="undo">撤销</div>
    <div :class="{locked:!canRedo}" @click="redo">重做</div>
  </div>

  <div v-if="showRepairTool" class="repairTool">
    <TopoRepairTool :re-render="Render" :stations="stations" @done="repairDone"></TopoRepairTool>
  </div>

  <SideBar ref="sb">
    <div class="closeDefEdit">
      <div>判定阈值</div>
      <input v-model="closeDef" type="range" min="0.001" max="0.015" step="0.001" @blur="Render"/>
      <div>{{ closeDef }}</div>
    </div>
    <div class="manual">
      本编辑器仅支持PC端使用，手机端仅供浏览<br/><br/>
      <b>勤保存</b><br/><br/>
      1.创建新线后，从任意一条线的起点站开始，逐个点击车站<b>（仅点击车站）</b>，此图仅展示连接关系，不要求连线和走向完全一致。<br /><br />
      2.左上角的线路ID与图中线路号无需对应。<br /><br />
      3.选中线路的情况下，按住ctrl键，点击车站，使线路从该车站脱离。<br /><br />
      <b>在画布上右键点击可以取消选择线路</b><br/><br/>
      4.取消选中线路，按住ctrl键点击车站，使所有线路从该车站脱离，并删除该车站<br /><br />
      5.取消选中线路，<b>用鼠标按下并拖动车站</b>，可调整其位置。<br /><br />
      6.点击删除线路，并不会删除其车站，如果漏掉了车站可以用此方法重新连接。<br /><br />
      7.换乘站务必画成一个点。
    </div>
  </SideBar>
</template>

<style scoped>
.closeDefEdit{
  display: flex;
  justify-content: center;
  align-items: center;
  gap:3px;
  flex-direction: column;
  padding: 6px;
  background-color: #eee;
}
.repairTool{
  position: fixed;
  top: 0px;
  right: 0px;
  background-color: #ddd;
  height: 160px;
  width: 320px;
  border-radius: 5px;
  border: 2px solid #fff;
  z-index: 1000;
}
.ur div.locked:hover{
  background-color: #000;
}
.ur div.locked{
  cursor: not-allowed;
  background-color: #999;
}
.ur div:hover{
  background-color: green;
}
.ur div{
  width: 45px;
  height: 35px;
  line-height: 35px;
  text-align: center;
  border: 2px solid white;
  background-color:olivedrab;
  color:white;
  cursor: pointer;
  transition: 0.5s;
  overflow: hidden;
  user-select: none;
}
.ur{
  position: fixed;
  left: 5px;
  bottom: 15px;
  width: 95px;
  height: 40px;
  display: flex;
  flex-wrap: nowrap;
  justify-content: space-between;
  align-items: center;
  overflow: hidden;
}

.area{
  top:155px;left:20px;
  width: calc(100vw - 50px);
  height: calc(100vh - 160px);
  position: fixed;
  overflow: auto;
}
.bg{
  opacity: 0.3;
  top:0px;left: 0px;
  width: calc(100% - 5px);
  object-fit: contain;
  user-select: none;
  position:absolute;
}
canvas{
  position:absolute;
}
.lineList{
  position: relative;
  overflow-x: auto;
  display: flex;
  flex-direction: column;
  flex-wrap: wrap;
}
.manual{
  color:#333
}
.topbar{
  position:absolute;
  top:0px;
  left:0px;
  right: 0px;
  height: 150px;
  display: flex;
  flex-direction: row;
  justify-content: space-between;
  gap:5px;
  background-color: #eee;
}
.btns{
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
}
.btn{
  line-height: 25px;
  height:25px;
  width: 70px;
  margin: 0px;
  background-color: #ccc;
  border: 2px solid #fff;
  border-radius: 5px;
  user-select: none;
  text-align: center;
}
.btn:hover{
  cursor: pointer;
  border: 2px solid #aaa;
}
.btn:active{
  cursor: pointer;
  border: 2px solid #000;
}
.selected{
  border:2px solid #000 !important;
}
</style>
