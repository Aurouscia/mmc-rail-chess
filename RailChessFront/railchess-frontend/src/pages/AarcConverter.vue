<script setup lang="ts">
import { computed, onMounted, ref, useTemplateRef, watch } from 'vue';
import { useRoute } from 'vue-router';
import { injectApi } from '../provides';
import AarcConverterOptions from './AarcConverterOptions.vue';

const api = injectApi()
const route = useRoute()

interface Config {
    max_length: number;
    merge_consecutive_duplicates: boolean;
    link_modes: {
        ThickLine: 'Group' | 'Connect' | 'None';
        ThinLine: 'Group' | 'Connect' | 'None';
        DottedLine1: 'Group' | 'Connect' | 'None';
        DottedLine2: 'Group' | 'Connect' | 'None';
        Group: 'Group' | 'Connect' | 'None';
    };
    friend_lines: string;
    merged_lines: string;
    max_rc_steps: number;
    optimize_segmentation: boolean;
    optimize_iterations: number;
    segmented_lines: string;
}

const config = ref<Config>({
    max_length: 128,
    merge_consecutive_duplicates: true,
    link_modes: {
        ThickLine: 'Connect',
        ThinLine: 'Connect',
        DottedLine1: 'None',
        DottedLine2: 'None',
        Group: 'Group',
    },
    friend_lines: '',
    merged_lines: '',
    max_rc_steps: 16,
    optimize_segmentation: false,
    optimize_iterations: 5,
    segmented_lines: '',
})
const showOptions = ref<boolean>(false)

const errmsg = ref<string>()
const md5 = ref<string>()
const sourceDomain = ref<string>()
const sourceId = ref<number>()
const taskCreating = ref<boolean>(false)
const taskKey = ref<string>()
const resultGetting = ref<boolean>(false)
const result = ref<{
    status?: 'pending'|'processing'|'completed'|'failed'|'timeout',
    result?: object
}>()

// 配置变化时重置任务状态
watch(config, () => {
    taskKey.value = undefined
    result.value = undefined
}, { deep: true })

const fileInput = useTemplateRef('fileInput')
async function onFileChange(e: Event) {
    errmsg.value = undefined
    md5.value = undefined
    taskKey.value = undefined
    result.value = undefined
    const tar = e.target
    if (! (tar instanceof HTMLInputElement))
        return
    const file = tar.files?.item(0)
    if(!file)
        return
    if(!file.name.endsWith(".json")){
        errmsg.value = "请选择一个json文件"
        return
    }
    const resp = await api.aarcConvert.uploadSave(file)
    const resMd5 = resp.data?.md5
    if(resp.success && resMd5 && typeof resMd5 === "string" && resMd5.length === 32){
        md5.value = resMd5
        errmsg.value = undefined
    }
    else{
        errmsg.value = resp.errmsg || "未知错误"
    }
}
function clearFileInput(){
    if(fileInput.value){
        fileInput.value.value = ""
        fileInput.value.dispatchEvent(new Event("change"))
    }
    showOptions.value = false
}
async function createTask(){
    if(!md5.value)
        return
    if(taskCreating.value)
        return
    taskKey.value = undefined
    result.value = undefined
    taskCreating.value = true
    errmsg.value = undefined
    const resp = await api.aarcConvert.createTask(md5.value, JSON.stringify(config.value), sourceDomain.value, sourceId.value)
    const key = resp.data?.key
    if(resp.success && key && typeof key === "string"){
        errmsg.value = undefined
        taskKey.value = key
    }
    else{
        errmsg.value = resp.errmsg || "未知错误"
    }
    taskCreating.value = false
}
async function getResult() {
    if(!svcUrl.value)
        return
    if(resultGetting.value)
        return
    if(!taskKey.value)
        return
    result.value = undefined
    resultGetting.value = true
    errmsg.value = undefined
    const getResultUrl = `${svcUrl.value}/get`
    const body = JSON.stringify({
        key: taskKey.value
    })
    try{
        const res = await fetch(getResultUrl, {method: 'POST', body})
        result.value = await res.json()
        if(!result.value?.status){
            errmsg.value = "从接口获取结果失败"
        }
    }
    catch(e){
        console.error(e)
        errmsg.value = "从接口获取结果失败"
    }
    resultGetting.value = false
}

const resultDownloadable = computed(()=>{
    return result.value?.status === 'completed' && result.value?.result
})
const activeObjUrl = ref<string>()
function downloadResult(){
    if(activeObjUrl.value){
        URL.revokeObjectURL(activeObjUrl.value)
        activeObjUrl.value = undefined
    }
    if(!result.value?.result)
        return
    const blob = new Blob([JSON.stringify(result.value.result, null, 2)], {type: 'application/json'})
    activeObjUrl.value = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = activeObjUrl.value
    a.download = getDownloadName()
    a.click()
}
const replaceTarget1 = '.aarc.json'
const replaceTarget2 = '.json'
const newSuffix = '.rlcs.json'
function getDownloadName(){
    if(fileInput.value){
        const file = fileInput.value.files?.item(0)
        if(file?.name){
            let n = file.name
            if(n.endsWith(replaceTarget1))
                return n.replace(replaceTarget1, newSuffix)
            else
                return n.replace(replaceTarget2, newSuffix)
        }
    }
    return "railchess.json"
}

const svcUrl = ref<string>()
onMounted(async()=>{
    const resp = await api.aarcConvert.getSvcUrl()
    if(resp && typeof resp == 'string'){
        svcUrl.value = resp
    }
    if(!svcUrl.value){
        errmsg.value = "未配置转换器服务地址"
        return
    }
    
    // 处理 query 参数
    const queryMd5 = route.query.md5 as string | undefined
    const querySourceDomain = route.query.sourceDomain ? decodeURIComponent(route.query.sourceDomain as string) : undefined
    const querySourceId = route.query.sourceId ? parseInt(route.query.sourceId as string) : undefined
    
    // 如果有 md5，认为文件已上传
    if(queryMd5 && queryMd5.length === 32){
        md5.value = queryMd5
    }
    
    // 如果有 sourceDomain 和 sourceId，获取配置并保存到 ref
    if(querySourceDomain && querySourceId !== undefined && !isNaN(querySourceId)){
        sourceDomain.value = querySourceDomain
        sourceId.value = querySourceId
        const configResp = await api.aarcConvert.getConfig(querySourceDomain, querySourceId)
        if(configResp.success && configResp.data){
            config.value = configResp.data as Config
        }
    }
})
</script>

<template>
    <h1>“AARC &rarr; 轨交棋”转换器</h1>
    <table><tbody>
        <tr v-if="!sourceDomain">
            <td colspan="2" class="explain-big">
                请先前往AARC 我的存档-设置-导出工程文件 
            </td>
        </tr>
        <tr>
            <td colspan="2" class="explain-small">
                转换器项目地址：https://github.com/SlinkierApple13/railchess-aarc-rc-converter<br/>
                当前使用的服务：{{ svcUrl }}
            </td>
        </tr>
        <tr v-if="errmsg">
            <td>错误信息</td>
            <td class="status-bad">{{errmsg}}</td>
        </tr>
        <tr v-if="svcUrl && !sourceDomain">
            <td>AARC存档json文件</td>
            <td>
                <input type="file" ref="fileInput" @change="onFileChange" accept=".json" :disabled="taskCreating || resultGetting"/>
                <button v-if="md5" class="lite red" @click="clearFileInput">×</button>
            </td>
        </tr>
        <tr v-if="md5">
            <td>存档 md5</td>
            <td class="some-code">{{md5}}</td>
        </tr>
        <tr v-if="md5">
            <td colspan="2">
                <button @click="showOptions = !showOptions" class="lite blue">{{ showOptions ? '隐藏高级选项' : '显示高级选项' }}</button>
            </td>
        </tr>
        <tr v-if="md5 && !sourceDomain" v-show="showOptions">
            <td colspan="2">
                <div class="jump-recommend">建议从 AARC 存档管理页跳转过来<br/>（导出工程文件按钮旁边）<br/>每个存档的配置将自动记忆</div>
            </td>
        </tr>
        <tr v-if="md5" v-show="showOptions">
            <td colspan="2">
                <AarcConverterOptions v-model="config" />
            </td>
        </tr>
        <tr v-if="md5">
            <td colspan="2">
                <button v-if="!taskCreating" @click="createTask">创建任务</button>
                <button v-else disabled>创建中...</button>
            </td>
        </tr>
        <tr v-if="taskKey">
            <td>任务 key</td>
            <td class="some-code">{{taskKey}}</td>
        </tr>
        <tr v-if="taskKey">
            <td colspan="2">
                <button v-if="!resultGetting" @click="getResult">获取结果</button>
                <button v-else disabled>获取中...</button>
            </td>
        </tr>
        <tr v-if="result">
            <td>结果</td>
            <td>
                <div>
                    状态：
                    <span v-if="result.status === 'pending'" class="status-wait">
                        等待中
                    </span>
                    <span v-else-if="result.status === 'processing'" class="status-wait">
                        处理中
                    </span>
                    <span v-else-if="result.status === 'completed'" class="status-good">
                        完成
                    </span>
                    <span v-else-if="result.status === 'failed'" class="status-bad">
                        失败
                    </span>
                    <span v-else-if="result.status === 'timeout'" class="status-bad">
                        超时
                    </span>
                    <span v-else class="status-bad">未知</span>
                </div>
                <div v-if="resultDownloadable" style="margin-top: 10px;">
                    <button @click="downloadResult">下载结果</button>
                    <div v-if="activeObjUrl">
                        <a :href="activeObjUrl" target="_blank" :download="getDownloadName()" class="fallback-anchor">
                            如果没有开始下载，请点击这里
                        </a>
                    </div>
                </div>
            </td>
        </tr>
        <tr v-if="result">
            <td colspan="2" class="explain-big">
                转换完成后，请创建棋盘，找到棋盘列表右侧的“信息-导入数据”，选择在这里下载的json文件
            </td>
        </tr>
    </tbody></table>
</template>

<style scoped>
table{
    margin: auto;
    margin-bottom: 200px;
}
.explain-big{
    color: #333;
    font-size: 14px;
    text-align: left;
}
.explain-small{
    color: #888;
    font-size: 12px;
    text-align: left;
}
.status-good{
    color: green;
}
.status-bad{
    color: red;
}
.status-wait{
    color: #888;
}
button.lite{
    text-decoration: none;
    font-weight: bold;
}
button.lite.blue{
    color: cornflowerblue;
}
button.lite.red{
    color: palevioletred
}
.some-code{
    font-size: 12px;
    font-family: monospace;
    word-break: break-all;
}
.fallback-anchor{
    color: #aaa;
    font-size: 12px;
}
.jump-recommend{
    font-size: 14px;
    color: palevioletred;
}
</style>