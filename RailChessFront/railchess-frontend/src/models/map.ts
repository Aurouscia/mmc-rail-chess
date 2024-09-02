export interface RailChessMapIndexResult{
    Items:RailChessMapIndexResultItem[]
}

export interface RailChessMapIndexResultItem{
    Id:number
    Title:string
    Author:string
    Date:string
    FileName?:string,
    LineCount:number,
    StationCount:number,
    ExcStationCount:number,
    TotalDirections:number
}

export interface TopoEditorLoadResult{
    TopoData?:string,
    FileName:string,
    WarnMsg?:string
}

export const posBase = 10000
export interface RailChessTopo{
    Stations:Sta[]
    Lines:Line[]
}
export type Sta = [number,number,number]
export interface Line{
    Id:number,
    Stas:number[]
}


export interface StaParsed{
    Id:number,
    X:number,
    Y:number
}
export function toStaParsed(s:Sta):StaParsed{
    return{
        Id:s[0],
        X:s[1],
        Y:s[2]
    }
}
export function toSta(s:StaParsed):Sta{
    return [s.Id,Math.round(s.X),Math.round(s.Y)];
}