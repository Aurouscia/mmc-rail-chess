import { appVersionCheck } from '@aurouscia/vite-app-version/check'
import { injectPop } from '../provides';
import viteAppVersionConfig from '../../appVersionOptions.json'

export function useFeVersionChecker(){
    const pop = injectPop();
    function checkAndPop(){
        appVersionCheck(viteAppVersionConfig).then(res=>{
            if(!res){
                pop.value?.show("客户端已更新，建议刷新浏览器获取最新版", "warning")
            }
        })
    }
    return { checkAndPop }
}