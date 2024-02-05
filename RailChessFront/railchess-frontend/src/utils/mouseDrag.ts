import { getTimeStamp } from "./timeStamp";

export class MouseDragListener{
    started:boolean = false
    startTime:number = 0
    updateTime:number = 0
    startX:number = 0
    startY:number = 0
    updateInterval:number
    constructor(updateInterval:number = 0.1){
        this.updateInterval = updateInterval;
    }

    private start(ex:number,ey:number,finish:(x:number,y:number)=>void){
        this.startX = ex;
        this.startY = ey;
        this.started = true;
        this.updateTime = getTimeStamp();
        this.startTime = getTimeStamp();
        finish(ex,ey);
    }
    private end(ex:number,ey:number,finish:(x:number,y:number)=>void){
        if(this.started){
            const x = ex;
            const y = ey;
            if(getTimeStamp()-this.startTime>0.2){
                console.log("调用finish")
                finish(x,y);
            }
            this.started = false
        }
    }
    private move(ex:number,ey:number,update:(x:number,y:number)=>void){
        if(this.started){
            const x = ex;
            const y = ey;
            const now = getTimeStamp();
            if(now - this.updateTime > this.updateInterval){
                update(x,y);
                this.updateTime = now;
            }
        }
    }

    startListen(ele:HTMLElement, 
        start:(x:number,y:number)=>void,
        update:(x:number, y:number)=>void,
        finish:(x:number, y:number)=>void
        ):()=>void
    {
        const mousedown = (e:MouseEvent)=>{
            this.start(e.offsetX,e.offsetY,start);
            e.preventDefault();
        }
        const mouseup = (e:MouseEvent)=>{
            this.end(e.offsetX,e.offsetY,finish)
            e.preventDefault();
        }
        const mousemove = (e:MouseEvent)=>{
            this.move(e.offsetX,e.offsetY,update)
            e.preventDefault();
        }
        ele.addEventListener("mousedown",mousedown);
        ele.addEventListener("mouseup",mouseup);
        ele.addEventListener("mousemove",mousemove);

        return ()=>{
            ele.removeEventListener("mousedown",mousedown);
            ele.removeEventListener("mouseup",mouseup);
            ele.removeEventListener("mousemove",mousemove);
        }
    }
}