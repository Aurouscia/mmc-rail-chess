import * as signalR from '@microsoft/signalr'
import { SyncData, TextMsg } from '../models/play';

export type SyncCall = (data:SyncData)=>void
export type TextMsgCall = (data:TextMsg)=>void
const syncCallMethodName = "sync";
const textMsgMethodName = "textmsg";

interface RequestModelBase{
    GameId:number
}
interface JoinRequest extends RequestModelBase{}
interface EnterRequest extends RequestModelBase{}
interface GameStartRequest extends RequestModelBase{}
interface GameResetRequest extends RequestModelBase{}
interface SendTextMsgRequest extends RequestModelBase{
    Content:string
}

export class SignalRClient{
    gameId:number
    conn:signalR.HubConnection

    constructor(gameId:number, jwtToken:string, syncCall:SyncCall, textMsgCall:TextMsgCall){
        this.gameId = gameId;
        const baseUrl = import.meta.env.VITE_BASEURL;
        const url = baseUrl+"/Play";
        this.conn = new signalR.HubConnectionBuilder()
            .withUrl(url,{ accessTokenFactory: () => jwtToken })
            .configureLogging(signalR.LogLevel.Information)
            .withAutomaticReconnect()
            .build();
        this.conn.on(syncCallMethodName, syncCall);
        this.conn.on(textMsgMethodName, (m)=>{
            console.log("展示信息",m)
            textMsgCall(m)
        });
    }
    async connect(){
        await this.conn.start();
    }
    async join(){
        const r:JoinRequest = {
            GameId: this.gameId
        }
        await this.conn.invoke("Join",r);
    }
    async enter(){
        const r:EnterRequest = {
            GameId: this.gameId
        }
        await this.conn.invoke("Enter",r);
    }
    async sendTextMsg(content:string){
        const r:SendTextMsgRequest = {
            GameId: this.gameId,
            Content: content
        }
        await this.conn.invoke("SendTextMsg",r)
    }
    async gameStart(){
        const r:GameStartRequest = {
            GameId: this.gameId
        }
        await this.conn.invoke("GameStart",r);
    }
    async gameReset(){
        const r:GameResetRequest = {
            GameId: this.gameId
        }
        await this.conn.invoke("GameReset",r);
    }
}