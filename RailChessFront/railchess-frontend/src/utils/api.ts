import { RailChessMapIndexResult, RailChessTopo, TopoEditorLoadResult } from "../models/map";
import { User } from "../models/user";
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
            }
        }
    }
    map = {
        index: async (search: string) => {
            const resp = await this.httpClient.request(
                "/api/Map/Index",
                "get",
                { search }
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
        }
    }
}