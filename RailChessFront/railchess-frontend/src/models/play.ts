import { RailChessGame } from "./game"

export interface InitData{
    BgFileName:string,
    TopoData:string,
    GameInfo:RailChessGame
}

export interface OcpStatus{
    playerId:number,
    stas:number[]
}
export interface SyncData{
    playerStatus:Player[],
    randNumber:number,
    ocps:OcpStatus[],
    newOcps:OcpStatus,
    selections:StepSelection[]
}

export interface StepSelection{
    dest:number
    path:number[]
}

export interface Player{
    id:number,
    name:string,
    score:number,
    stuckTimes:number,
    atSta:number,
    avtFileName:string
}

export interface TextMsg{
    content:string
    sender:string
    textMsgType:"Plain"|"Important"|"Err"
}