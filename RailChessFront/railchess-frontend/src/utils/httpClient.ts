import axios, { Axios } from 'axios'
import {AxiosError} from 'axios'
import { useJwtTokenStore } from './stores/jwtTokenStore'
import { storeToRefs } from 'pinia'
import { Ref } from 'vue'

export type ApiResponse = {
    success: boolean
    data: any
    errmsg: string
}
export type RequestType = "get"|"postForm"|"postRaw";

export type HttpCallBack = (result:"ok"|"warn"|"err",msg:string)=>void
export interface ApiRequestHeader{
    Authorization:string|undefined
}

//<obsolete>
const storageKey = "fcloudAuthToken"
//</obsolete>
const defaultFailResp:ApiResponse = {data:undefined,success:false,errmsg:"失败"}

export class HttpClient{
    httpCallBack:HttpCallBack
    ax:Axios
    jwtToken:Ref<string|undefined>
    constructor(httpCallBack:HttpCallBack){
        const refs = storeToRefs(useJwtTokenStore())
        this.jwtToken = refs.jwtToken
        const jwtTokenLegacy = localStorage.getItem(storageKey);
        if(jwtTokenLegacy){
            this.jwtToken.value = jwtTokenLegacy
        }
        localStorage.removeItem(storageKey)
        this.httpCallBack = httpCallBack;
        this.ax = axios.create({
            baseURL: import.meta.env.VITE_BASEURL,
            validateStatus: (n)=>n < 500
        });
    }
    setToken(token:string){
        this.jwtToken.value = token;
    }
    clearToken(){
        this.jwtToken.value = undefined;
    }
    private headers(){
        return {
            Authorization: `Bearer ${this.jwtToken.value}`
        }
    }
    private showErrToUser(err:AxiosError){
        console.log(err);
        if(err.response?.status==401){
            this.httpCallBack("err","请登录");
            return;
        }
        this.httpCallBack("err","请检查网络连接");
    }
    async request(resource:string,type:RequestType,data?:any,successMsg?:string): Promise<ApiResponse>{
        console.log(`开始发送[${type}]=>[${resource}]`,data)
        var res;
        try{
            if(type=='get'){
                res = await this.ax.get(
                    resource,
                    {
                        params:data,
                        headers:this.headers(),
                    }
                );
            }else if(type=='postRaw'){
                res = await this.ax.post(
                    resource,
                    data,
                    {
                        headers:this.headers()
                    }
                );
            }else if(type=='postForm'){
                res = await this.ax.postForm(resource,
                    data,
                    {
                        headers:this.headers()
                    }
                );
            }
        }
        catch(ex){
            const err = ex as AxiosError;
            console.log(`[${type}]${resource}失败`,err)
            this.showErrToUser(err);
        }
        if(res){
            const resp = res.data as ApiResponse;
            if(resp.success){
                console.log(`[${type}]${resource}成功`,resp.data)
                if(successMsg){
                    this.httpCallBack('ok',successMsg)
                }
            }
            if(!resp.success){
                console.log(`[${type}]${resource}失败`,resp.errmsg||res.statusText);
                if(res.status==401){
                    this.httpCallBack('err','请登录');
                }
                else if(res.status==429){
                    this.httpCallBack('warn',resp.errmsg||'操作过于频繁\n请稍后再试')
                }
                else{
                    this.httpCallBack('err',resp.errmsg||'未知错误\n请联系管理员')
                }
            }
            return resp;
        }else{
            return defaultFailResp;
        }
    }
}