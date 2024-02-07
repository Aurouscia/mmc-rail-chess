import * as signalR from '@microsoft/signalr'
import { StepSelection, SyncData } from '../models/play';

export type SyncCall = (data:SyncData)=>void
const syncCallMethodName = "sync";
const selectMethodName = "Select"

export class SignalRClient{
    conn:signalR.HubConnection
    syncCall:SyncCall
    constructor(syncCall:SyncCall){
        const baseUrl = import.meta.env.VITE_BASEURL;
        const url = baseUrl+"/Play";
        this.conn = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .configureLogging(signalR.LogLevel.Information)
            .withAutomaticReconnect()
            .build();
        this.syncCall = syncCall;

        this.conn.on(syncCallMethodName,syncCall);
    }
    select(s:StepSelection){
        this.conn.invoke(selectMethodName, s);
    }
}