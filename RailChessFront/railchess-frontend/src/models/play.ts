import { RailChessGame } from "./game"

export interface InitData{
    BgFileName:string,
    TopoData:string,
    GameInfo:RailChessGame
}

export interface OcpStatus{
    PlayerId:number,
    Stas:number[]
}
export interface SyncData{
    PlayerStatus:Player[],
    RandNumber:number,
    Ocps:OcpStatus[],
    NewOcps:OcpStatus,
    Selections:StepSelection[]
}

export interface StepSelection{
    Dest:number
    Path:number[]
}

export interface Player{
    Id:number,
    Name:string,
    Score:number,
    StuckTimes:number,
    AtSta:number,
    AvtFileName:string
}