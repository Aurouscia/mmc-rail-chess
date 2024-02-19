export function getTimeStamp(){
    return (new Date()).getTime()/1000
}
export function getTimeStampMs(){
    return (new Date()).getTime()
}
export function getTimeStr(){
    var date = new Date();
    var h = (date.getHours() < 10 ? '0'+date.getHours():date.getHours())+ ':';
    var m = (date.getMinutes() < 10 ? '0'+date.getMinutes():date.getMinutes()) + ':';
    var s = (date.getSeconds() < 10 ? '0'+date.getSeconds():date.getSeconds()) + ':';
    var ms = date.getMilliseconds();
    var msStr;
    if(ms<10){
        msStr = '00' + ms;
    }else if(ms<100){
        msStr = '0' + ms;
    }else{
        msStr = '' + ms;
    }
    return h+m+s+msStr;
}