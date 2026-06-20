import { appVersionCheck } from '@aurouscia/vite-app-version/check'
import { Ref } from 'vue';
import Pop from '../components/Pop.vue';
import viteAppVersionConfig from '../../appVersionOptions.json'

export function createFeVersionChecker(pop: Ref<InstanceType<typeof Pop>|null>){
    function checkAndPop(){
        appVersionCheck(viteAppVersionConfig).then(res=>{
            if(!res){
                pop.value?.show("有版本更新\n现为您刷新页面", "warning")
                setTimeout(()=>{
                    window.location.reload()
                }, 1000)
            }
        })
    }
    return { checkAndPop }
}
