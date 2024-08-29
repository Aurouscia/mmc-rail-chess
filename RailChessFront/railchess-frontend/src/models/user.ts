export interface User{
    Id:number,
    Name?:string,
    Pwd:string,
    AvatarName?:string,
    Elo:number
}
export interface UserRankingListItem{
    UId:number,
    UName:string,
    UAvt:string,
    Plays:number,
    AvgRank:number
}