import { Ref } from "vue";

export class Scaler{
    private frame: HTMLDivElement;
    private arena: HTMLDivElement;
    private callBack:()=>void;
    private moveLocked:Ref<boolean>
    private mouseDown:boolean = false;
    private heightWidthRatioBase:Ref<HTMLElement|undefined|null>
    constructor(frame:HTMLDivElement,arena:HTMLDivElement,callBack:()=>void,moveLocked:Ref<boolean>,heightWidthRatioBase:Ref<HTMLElement|undefined|null>){
        this.frame = frame;
        this.arena = arena;
        this.callBack = callBack;
        this.moveLocked = moveLocked
        this.heightWidthRatioBase = heightWidthRatioBase
        frame.addEventListener("click",(e)=>{
            const x = e.clientX
            const y = e.clientY
            const fw = this.frame.clientWidth;
            const fh = this.frame.clientHeight;
            const ax = x/fw;
            const ay = y/fh;
            if(e.altKey){
                this.scale(1.2,{x:ax,y:ay});
            }else if(e.ctrlKey){
                this.scale(0.8,{x:ax,y:ay});
            }
        })

        frame.addEventListener('mousedown',e=>{e.preventDefault();this.mouseDown = true})
        frame.addEventListener("touchmove",this.moveHandlerBinded)
        frame.addEventListener("mousemove",this.moveHandlerBinded)
        frame.addEventListener("touchend",()=>{
            this.touchDist=-1; this.touchCx=-1; this.touchCy=-1
        })
        frame.addEventListener("mouseup",()=>{
            this.touchDist=-1; this.touchCx=-1; this.touchCy=-1; this.mouseDown = false
        })
        frame.addEventListener("wheel", this.wheelHandlerBinded)
    }

    touchDist=-1;
    touchCx=-1;
    touchCy=-1;
    lastTouchResponse = 0;
    touchTimeThrs = 20;

    private moveHandlerBinded = this.moveHandler.bind(this)
    private moveHandler(e:TouchEvent|MouseEvent){
        e.preventDefault()
        const time = +new Date();
        if(time - this.lastTouchResponse < this.touchTimeThrs)
            return;
        this.lastTouchResponse = time;
        let info:TouchInfoRes
        if('touches' in e)
            info = this.touchInfo(e as TouchEvent);
        else{
            if(!this.mouseDown)
                return;
            info = {
                cx: e.clientX,
                cy: e.clientY
            }
        }
        if(!info || (this.moveLocked.value && !info.dist))
            return;
        const {cx, cy} = info;
        if(!info.dist){
            if(this.touchCx >= 0 && this.touchCy >= 0){
                this.move(cx - this.touchCx, cy - this.touchCy)
            }
            this.touchCx = cx;
            this.touchCy = cy;
        }
        else{
            const distNow = info.dist;
            if (this.touchDist >= 0 && this.touchCx >= 0 && this.touchCy >= 0){
                const ratio = distNow/this.touchDist
                const fw = this.frame.clientWidth;
                const fh = this.frame.clientHeight;
                const ax = cx / fw;
                const ay = cy / fh;
                this.scale(ratio, { x: ax, y: ay })
                this.move(cx - this.touchCx, cy - this.touchCy)
            }
            this.touchDist = distNow;
            this.touchCx = cx;
            this.touchCy = cy;
        }
    }
    private touchInfo(e:TouchEvent):TouchInfoRes{
        if(e.touches.length<1)
            return;
        const x0 = e.touches[0].clientX;
        const y0 = e.touches[0].clientY;
        if(e.touches.length==1){
            return{
                cx:x0,
                cy:y0
            }
        }
        if(e.touches.length==2){
            const x1 = e.touches[1].clientX;
            const y1 = e.touches[1].clientY;
            const cx = (x0+x1)/2;
            const cy = (y0+y1)/2;
            const dist = Math.sqrt((x0-x1)**2 + (y0-y1)**2);
            return{cx,cy,dist}
        }
    }

    
    private wheelHandlerBinded = this.wheelHandler.bind(this)
    private wheelHandler(e:WheelEvent){
        const anchor = this.getAnchorFromEvent(e)
        e.preventDefault()
        const ratio = 1-e.deltaY*0.001;
        this.scale(ratio,anchor)
    }


    private getAnchorFromEvent(e:{clientX:number,clientY:number}){
        const fx = e.clientX;
        const fy = e.clientY;
        const fw = this.frame.clientWidth;
        const fh = this.frame.clientHeight;
        const x = fx/fw;
        const y = fy/fh;
        return{x,y}
    }


    scale(ratio:number,anchor?:{x:number,y:number}){
        const ww = this.frame.clientWidth;
        const hh = this.frame.clientHeight;
        const w = this.arena.clientWidth;
        const h = this.arena.clientHeight;
        let hwr;
        if(!this.heightWidthRatioBase.value){
            hwr = h/w
        }else{
            hwr = this.heightWidthRatioBase.value.clientHeight/this.heightWidthRatioBase.value.clientWidth;
        }
        var x:number;
        var y:number;
        if(anchor){
            x = anchor.x;
            y = anchor.y;
        }else{
            x = (this.frame.scrollLeft+ww/2)/w;
            y = (this.frame.scrollTop+hh/2)/h;
        }
        if(w<ww*0.8 && ratio<1){
            return;
        }
        if(w>ww*10 && ratio>1){
            return;
        }
        const arx = this.frame.scrollLeft + ww*x;
        const ary = this.frame.scrollTop + hh*y;
        const gx = arx/w
        const gy = ary/h
        const newW = w*ratio-1;
        this.arena.style.width = newW+'px';
        this.arena.style.height = newW*hwr+'px';
        const wGrowth = w*(ratio-1);
        const hGrowth = h*(ratio-1);
        this.frame.scrollLeft += wGrowth*gx
        this.frame.scrollTop += hGrowth*gy
        this.callBack();
    }
    move(increX:number,increY:number){
        this.frame.scrollLeft -= increX
        this.frame.scrollTop -= increY
    }
    widthReset(mutiple?:number){
        mutiple = mutiple || 1;
        const ww = this.frame.clientWidth;
        const w = this.arena.clientWidth;
        const ratio = w/ww;
        this.scale(1/ratio*mutiple);
    }
    heightReset(mutiple?:number){
        mutiple = mutiple || 1;
        const hh = this.frame.clientHeight;
        const h = this.arena.clientHeight;
        const ratio = h/hh;
        this.scale(1/ratio*mutiple);
    }
    autoMutiple(mutiple?:number, flag?:boolean){
        const frameWHRatio = this.frame.clientWidth / this.frame.clientHeight;
        const arenaWHRatio = this.arena.clientWidth / this.arena.clientHeight;
        var arenaWider = arenaWHRatio > frameWHRatio;
        if(flag){
            arenaWider = !arenaWider;
        }
        if(arenaWider){
            this.heightReset(mutiple);
        }else{
            this.widthReset(mutiple);
        }
    }
}

type TouchInfoRes = {cx:number,cy:number,dist?:number}|undefined