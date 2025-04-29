export interface RailChessGame{
    Id?:number
    HostUserId?:number
    UseMapId:number
    Started?:boolean
    StartTime?:string
    Ended?:boolean
    DurationMins?:number
    Steps?:number

    RandAlg:RandAlgType
    RandMin:number
    RandMax:number
    StucksToLose:number
    AllowReverseAtTerminal:boolean
    AllowTransfer:number
    AiPlayer: AiPlayerType
    SpawnRule: SpawnRuleType
}
export enum RandAlgType
{
    Uniform = 0,
    Gaussian10 = 10,
    Gaussian15 = 11,
    Gaussian20 = 12,
    Gaussian25 = 13,
    TriangleMiddle = 20,
    TriangleLeft = 21,
    TriangleRight = 22,
    AlwaysNegativeOne = 90
}
export enum AiPlayerType
{
    None = 0,
    Simple = 1,
    Medium = 2
}
export enum SpawnRuleType
{
    Terminal = 0,
    TwinExchange = 1
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
    EId: number
}