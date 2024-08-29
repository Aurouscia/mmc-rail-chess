export interface RailChessGame{
    Id?:number
    HostUserId?:number
    UseMapId:number
    Started?:boolean
    StartTime?:string
    Ended?:boolean
    DurationMins?:number
    Steps?:number

    RandAlg:0|1
    RandMin:number
    RandMax:number
    StucksToLose:number
    AllowReverseAtTerminal:boolean
    AllowTransfer:number
}

export interface GameActiveResult{
    Items:GameActiveResultItem[]
}
export interface GameActiveResultItem{
    Data:RailChessGame
    MapName:string
    HostUserName:string
    StartedMins:number
}

export interface GameTimeline
{
    Items:GameTimelineItem[],
    Avts:Record<number, string>,
    Warning:string
}
export interface GameTimelineItem
{
    UId: number
    Rand: number
    Cap: number
    T: number
}