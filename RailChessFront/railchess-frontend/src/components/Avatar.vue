<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';


const props = defineProps<{
    fileName?:string
}>();
onMounted(()=>{
    setSrc()
})

const src = ref<string>();
function setSrc(){
    if(!props.fileName){
        src.value = undefined;
        return;
    }
    const baseUrl = import.meta.env.VITE_BASEURL;
    src.value = baseUrl+"/avts/"+props.fileName;
}
watch(props,()=>{
    setSrc();
})
</script>

<template>
    <div class="avatar">
        <img class="img" v-if="src" :src="src"/>
        <div class="img" v-else>
            请设头像
        </div>
    </div>
</template>

<style scoped>
    .avatar, .img{
        position: absolute;
        top:0px;bottom: 0px;left:0px;right: 0px;
    }
    .img{
        width: 64px;
        height: 64px;
        border-radius: 1000px;
        border:#ccc solid 2px;
        background-color: white;
    }
    img.img{
        object-fit: contain;
    }
    div.img{
        text-align: center;
        font-size: 15px;
        line-height: 64px;
        color: #aaa
    }
</style>