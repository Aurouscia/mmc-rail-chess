export class Scaler{
    private frame: HTMLDivElement;
    private arena: HTMLDivElement;
    private callBack:()=>void;
    constructor(frame:HTMLDivElement,arena:HTMLDivElement,callBack:()=>void){
        this.frame = frame;
        this.arena = arena;
        this.callBack = callBack;
        frame.addEventListener("click",(e)=>{
            const x = e.clientX - this.frame.offsetLeft;
            const y = e.clientY - this.frame.offsetTop;
            const fw = this.frame.clientWidth;
            const fh = this.frame.clientHeight;
            const ax = x/fw;
            const ay = y/fh;
            console.log(ax,ay);
            if(e.shiftKey){
                this.scale(1.2,{x:ax,y:ay});
            }else if(e.ctrlKey){
                this.scale(0.8,{x:ax,y:ay});
            }
        })
        
        // frame.addEventListener("touchmove",this.touchHandler)
        // frame.addEventListener("touchend",()=>{this.dist=0})
    }
    // dist=0;
    // touchHandler(e:TouchEvent){
    //     if (e.touches.length != 2) { return; }
    //     const { cx, cy, dist: distNow } = this.touchInfo(e);
    //     //console.log(cx,cy,distNow)
    //     if (!this.dist) {
    //         this.dist = distNow;
    //     } else if (distNow - this.dist > 50) {
    //         const fw = this.frame.clientWidth;
    //         const fh = this.frame.clientHeight;
    //         const ax = cx / fw;
    //         const ay = cy / fh;
    //         this.scale(1.2, { x: ax, y: ay })
    //         this.callBack();
    //         this.dist = distNow;
    //     }
    // }
    // private touchInfo(e:TouchEvent):{cx:number,cy:number,dist:number}{
    //     const x0 = e.touches[0].clientX - this.frame.offsetLeft;
    //     const y0 = e.touches[0].clientY - this.frame.offsetTop;
    //     const x1 = e.touches[1].clientX - this.frame.offsetLeft;
    //     const y1 = e.touches[1].clientY - this.frame.offsetTop;
    //     const cx = (x0+x1)/2;
    //     const cy = (y0+y1)/2;
    //     const dist = Math.sqrt((x0-x1)**2 + (y0-y1)**2);
    //     return{cx,cy,dist}
    // }
    scale(ratio:number,anchor?:{x:number,y:number}){
        const ww = window.innerWidth;
        const w = this.arena.clientWidth;
        const h = this.arena.clientHeight
        var x:number;
        var y:number;
        if(anchor){
            x = anchor.x;
            y = anchor.y;
        }else{
            x = 0.5;
            y = 0.5;
        }
        if(w<ww && ratio<1){
            return;
        }
        if(w>ww*10 && ratio>1){
            return;
        }
        this.arena.style.width = w*ratio+'px';
        const wGrowth = w*(ratio-1);
        const hGrowth = h*(ratio-1);
        this.frame.scrollLeft += wGrowth*x
        this.frame.scrollTop += hGrowth*y
        this.callBack();
    }
}