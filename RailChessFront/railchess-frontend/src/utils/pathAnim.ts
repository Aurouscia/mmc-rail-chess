import { CSSProperties, nextTick, ref } from "vue"
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

export interface AnimConn{
    a: number, 
    b: number,
    text: string
}
export interface AnimConnsParam{
    conns: AnimConn[],
    getPos: (sta:number)=>AnimPos|undefined
    styleBase:CSSProperties
}
export interface AnimConnItem{
    a: number,
    b: number,
    text: string,
    style:CSSProperties
}

export function useConnectionAnimator(){
    const stepMs: number = 1000;
    let runningTimer: number = 0;
    let animatorRendered = ref<AnimConnItem[]>([]);
    function setConnections(param:AnimConnsParam){
        stopConnectionsAnim()
        if(param.conns.length === 0)
            return
        animatorRendered.value = param.conns.map<AnimConnItem>(x=>{
            return {
                a: x.a,
                b: x.b,
                text: x.text,
                style: {...param.styleBase}
            }
        })
        const updateCall = async()=>{
            for(const item of animatorRendered.value){
                item.style["transition-duration"] = '0ms'
                const posA = param.getPos(item.a)
                if(!posA)
                    continue
                let { top, left } = posA
                item.style.left = left
                item.style.top = top
                await nextTick()
                const posB = param.getPos(item.b)
                if(!posB)
                    continue
                item.style["transition-duration"] = stepMs + 'ms';
                ({ top, left } = posB)
                item.style.left = left
                item.style.top = top
            }
        }
        updateCall();
        runningTimer = window.setInterval(updateCall, stepMs)
    }
    function stopConnectionsAnim(){
        window.clearInterval(runningTimer);
        animatorRendered.value = [];
    }
    return{
        connectionAnimatorRendered: animatorRendered,
        setConnections,
        stopConnectionsAnim
    }
}