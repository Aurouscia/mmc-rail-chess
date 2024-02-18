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
    selections?:number[][],
    gameStarted:boolean
}

export interface Player{
    id:number,
    name:string,
    score:number,
    stuckTimes:number,
    atSta:number,
    avtFileName:string
}

export type TextMsgType = 0|1|2
export interface TextMsg{
    content:string
    sender:string
    time:string,
    type:TextMsgType
}