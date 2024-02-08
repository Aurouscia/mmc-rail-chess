import * as signalR from '@microsoft/signalr'
import { SyncData } from '../models/play';

export type SyncCall = (data:SyncData)=>void
const syncCallMethodName = "sync";
//const selectMethodName = "Select"

interface RequestModelBase{
    GameId:number
}
interface JoinRequest extends RequestModelBase{
}

export class SignalRClient{
    conn:signalR.HubConnection
    syncCall:SyncCall
    constructor(jwtToken:string, syncCall:SyncCall){
        const baseUrl = import.meta.env.VITE_BASEURL;
        const url = baseUrl+"/Play";
        this.conn = new signalR.HubConnectionBuilder()
            .withUrl(url,{ accessTokenFactory: () => jwtToken })
            .configureLogging(signalR.LogLevel.Information)
            .withAutomaticReconnect()
            .build();
        this.syncCall = syncCall;
        this.conn.on(syncCallMethodName,syncCall);

        this.conn.start();
    }
    join(gameid:number){
        const r:JoinRequest = {
            GameId: gameid
        }
        this.conn.invoke("Join",r);
    }
}