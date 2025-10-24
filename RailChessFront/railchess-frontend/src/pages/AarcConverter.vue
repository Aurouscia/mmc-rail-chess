<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { injectApi } from '../provides';

const api = injectApi()

const errmsg = ref<string>()
const md5 = ref<string>()
const taskCreating = ref<boolean>(false)
const taskKey = ref<string>()
const resultGetting = ref<boolean>(false)
const result = ref<string>()

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
async function createTask(){
    if(!svcUrl.value)
        return
    if(!md5.value)
        return
    if(taskCreating.value)
        return
    taskKey.value = undefined
    result.value = undefined
    taskCreating.value = true
    errmsg.value = undefined
    const resp = await api.aarcConvert.createTask(md5.value, "{}")
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
    const res = await fetch(getResultUrl, {method: 'POST', body})
    const resText = await res.text()
    result.value = resText
    try{
        const resJson = JSON.parse(resText)
        result.value = JSON.stringify(resJson, null, 2)
        console.log(result.value)
    }catch{

    }
    resultGetting.value = false
}

const svcUrl = ref<string>()
onMounted(async()=>{
    const resp = await api.aarcConvert.getSvcUrl()
    if(resp && typeof resp == 'string'){
        svcUrl.value = resp
    }
    if(!svcUrl.value){
        errmsg.value = "未配置转换器服务地址"
    }
})
</script>

<template>
    <h1>“AARC &rarr; 轨交棋”转换器（试验版）</h1>
    <table><tbody>
        <tr>
            <td colspan="2" class="explain">
                转换器项目地址：https://github.com/SlinkierApple13/railchess-aarc-rc-converter<br/>
                当前使用的服务：{{ svcUrl }}
            </td>
        </tr>
        <tr v-if="errmsg">
            <td>错误信息</td>
            <td style="color: red;">{{errmsg}}</td>
        </tr>
        <tr v-if="svcUrl">
            <td>AARC存档json文件</td>
            <td>
                <input type="file" @change="onFileChange" accept=".json" :disabled="taskCreating"/>
            </td>
        </tr>
        <tr v-if="md5">
            <td>存档 md5</td>
            <td>{{md5}}</td>
        </tr>
        <tr v-if="md5">
            <td colspan="2">
                <button v-if="!taskCreating" @click="createTask">创建任务</button>
                <button v-else disabled>创建中...</button>
            </td>
        </tr>
        <tr v-if="taskKey">
            <td>任务 key</td>
            <td>{{taskKey}}</td>
        </tr>
        <tr v-if="taskKey">
            <td colspan="2">
                <button v-if="!resultGetting" @click="getResult">获取结果</button>
                <button v-else disabled>获取中...</button>
            </td>
        </tr>
        <tr v-if="result">
            <td>结果</td>
            <td style="text-align: left;">
                <code style="white-space: pre-wrap;">
                    {{result}}
                </code>
            </td>
        </tr>
    </tbody></table>
</template>

<style scoped>
table{
    margin: auto;
}
.explain{
    color: #888;
    font-size: 12px;
    text-align: left;
}
</style>