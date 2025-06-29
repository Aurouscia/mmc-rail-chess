import { CSSProperties, ref } from "vue"
import { sleep } from "./sleep";

export interface AnimPos{
    top:string,
    left:string
}
export interface AnimNode{
    getPos:(sta:number)=>AnimPos|undefined,
    sta:number
}
export interface AnimPath{
    dist:number
    nodes:AnimNode[],
    styleBase:CSSProperties
}
export interface AnimItem{
    dist:number,
    style:CSSProperties,
    step:number
}

export function useAnimator() {
    const stepMs: number = 400;
    var runningTimer: number;
    var animatorRendered = ref<AnimItem | undefined>();

    function setPaths(p?: AnimPath,single?:boolean) {
        window.clearInterval(runningTimer);
        animatorRendered.value = undefined;

        if(!p){return};

        const stepCount = p.nodes.length - 1;
        if (stepCount == 0) return;
        const loopTime = (stepCount + 2) * stepMs;
        const r: CSSProperties = p.styleBase;
        animatorRendered.value = {
            dist: p.dist,
            style: r,
            step:0
        };
        const updateCall = async()=>{
            const runForDist = p.dist;
            for (var i = 0; i <= p.nodes.length - 1; i++) {
                if (!animatorRendered.value || animatorRendered.value.dist!=runForDist) { break; }
                const node = p.nodes[i];
                if (i == 0) {
                    animatorRendered.value.style.transition = '0ms';
                } else {
                    animatorRendered.value.style.transition = stepMs + 'ms';
                }
                const pos = node.getPos(node.sta);
                if (!pos) continue;
                const { top, left } = pos;
                animatorRendered.value.style.left = left;
                animatorRendered.value.style.top = top;
                animatorRendered.value.step = i;
                await sleep(stepMs);
            }
        }
        updateCall();
        if(!single){
            runningTimer = window.setInterval(updateCall, loopTime)
        }
    };
    function stopPathAnim(){
        window.clearInterval(runningTimer)
    }
    return { animatorRendered, setPaths, stopPathAnim }
}
