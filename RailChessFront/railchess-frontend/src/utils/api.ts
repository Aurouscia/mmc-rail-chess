import { GameActiveResult, GameTimeline, RailChessGame } from "../models/game";
import { RailChessMapIndexResult, RailChessTopo, TopoEditorLoadResult } from "../models/map";
import { GameInitData } from "../models/play";
import { QuickSearchResult } from "../models/quickSearch";
import { User, UserRankingListItem } from "../models/user";
import { HttpClient } from "./httpClient";
import { IdentityInfo } from "./userInfo";

export class Api{
    private httpClient: HttpClient;
    constructor(httpClient:HttpClient){
        this.httpClient = httpClient;
    }
    identites = {
        authen:{
            login: async(reqObj:{userName:string,password:string})=>{
                var res = await this.httpClient.request(
                    "/api/Auth/Login",
                    "postForm",
                    reqObj,
                    "已成功登录");
                if(res.success){
                    return res.data["token"] as string;
                }
            },
            identityTest: async()=>{
                var res = await this.httpClient.request(
                    "/api/Auth/IdentityTest",
                    "get")
                if(res.success){
                    return res.data as IdentityInfo
                }
            },
        },
        user:{
            add: async(reqObj:{userName:string,password:string})=>{
                var res = await this.httpClient.request(
                    "/api/User/Add",
                    "postForm",
                    reqObj,
                    "已成功注册")
                return res.success;
            },
            edit: async()=>{
                var res = await this.httpClient.request(
                    "/api/User/Edit",
                    "get")
                if(res.success){
                    return res.data as User
                }
            },
            editExe: async(user:User)=>{
                var res = await this.httpClient.request(
                    "/api/User/EditExe",
                    "postRaw",
                    user,
                    "修改成功");
                return res.success
            },
            setAvatar: async(avatar:File)=>{
                var res = await this.httpClient.request(
                    "/api/User/SetAvatar",
                    "postForm",
                    {avatar},
                    "上传成功");
                return res.success
            },
            rankingList: async()=>{
                var res = await this.httpClient.request(
                    "/api/User/RankingList",
                    "get")
                if(res.success){
                    return res.data as UserRankingListItem[]
                }
            },
            quickSearch: async(s:string)=>{
                const resp = await this.httpClient.request(
                    "/api/User/QuickSearch",
                    "get",
                    {s}
                );
                if(resp.success){
                    return resp.data as QuickSearchResult
                }
            }
        }
    }
    map = {
        index: async (search?: string, orderBy?:'score', scoreMin?:number, scoreMax?:number, skip?:number, take?:number) => {
            const resp = await this.httpClient.request(
                "/api/Map/Index",
                "get",
                { search, orderBy, scoreMin, scoreMax, skip, take }
            );
            if (resp.success) {
                return resp.data as RailChessMapIndexResult
            }
        },
        createOrEdit: async(id:number, title:string, file?:File)=>{
            const resp = await this.httpClient.request(
                "/api/Map/CreateOrEdit",
                "postForm",
                {id,title,file},
                "保存成功"
            );
            return resp.success
        },
        loadTopo: async (id:number)=>{
            const resp = await this.httpClient.request(
                "/api/Map/LoadTopo",
                "get",
                {id},
            );
            if(resp.success){
                return resp.data as TopoEditorLoadResult
            }
        },
        saveTopo: async (id:number, data:RailChessTopo)=>{
            const resp = await this.httpClient.request(
                "/api/Map/SaveTopo",
                "postForm",
                {id,data:JSON.stringify(data)},
                "已保存"
            )
            return resp.success
        },
        importTopo: async (id:number, file:File)=>{
            const resp = await this.httpClient.request(
                "/api/Map/ImportTopo",
                "postForm",
                {id,file},
                "成功导入"
            )
            return resp.success
        },
        delete: async(id:number)=>{
            const resp = await this.httpClient.request(
                "/api/Map/Delete",
                "get",
                {id},
                "成功删除"
            )
            return resp.success
        },
        quickSearch: async(s:string)=>{
            const resp = await this.httpClient.request(
                "/api/Map/QuickSearch",
                "get",
                {s}
            );
            if(resp.success){
                return resp.data as QuickSearchResult
            }
        }
    }
    game = {
        active:async()=>{
            const resp = await this.httpClient.request(
                "/api/Game/Active",
                "get"
            )
            if(resp.success){
                return resp.data as GameActiveResult
            }
        },
        create:async(game:RailChessGame)=>{
            const resp = await this.httpClient.request(
                "/api/Game/Create",
                "postRaw",
                game,
                "创建成功"
            )
            return resp.success;
        },
        init:async(id:number)=>{
            const resp = await this.httpClient.request(
                "/api/Game/Init",
                "get",
                {id}
            );
            if(resp.success){
                return resp.data as GameInitData
            }
        },
        delete:async(id:number)=>{
            if(!window.confirm("确认删除")){return;}
            const resp = await this.httpClient.request(
                "/api/Game/Delete",
                "get",
                {id},
                "删除成功"
            )
            return resp.success
        },
        loadTimeline:async(id:number)=>{
            const resp = await this.httpClient.request(
                "/api/Game/LoadTimeline",
                "get",
                {id}
            )
            if(resp.success)
                return resp.data as GameTimeline
        }
    }
    gameResult = {
        ofUser:async(userId:number)=>{
            const resp = await this.httpClient.request(
                "/api/GameResult/OfUser",
                "get",
                {userId}
            )
            if(resp.success){
                return resp.data as GameResultListResponse
            }
        },
        ofGame:async(gameId:number)=>{
            const resp = await this.httpClient.request(
                "/api/GameResult/OfGame",
                "get",
                {gameId}
            )
            if(resp.success){
                return resp.data as GameResultListResponse
            }
        }
    }
    aarcConvert = {
        uploadSave: async(f:File)=>{
            const resp = await this.httpClient.request(
                "/api/AarcConvert/UploadSave",
                "postForm",
                {save: f}
            )
            return resp
        },
        createTask: async(md5:string, configJson:string)=>{
            const resp = await this.httpClient.request(
                "/api/AarcConvert/CreateTask",
                "postForm",
                {md5, configJson}
            )
            return resp
        },
        getSvcUrl: async()=>{
            const resp = await this.httpClient.request(
                "/api/AarcConvert/GetSvcUrl",
                "get"
            )
            if(resp.success)
                return resp.data.svcUrl as string|null|undefined
        }
    }
}