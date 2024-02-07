export function bgSrc(fileName?:string) {
    if(!fileName){return ""}
    return import.meta.env.VITE_BASEURL + "/maps/" +fileName
}
export function avtSrc(fileName:string){
    const baseUrl = import.meta.env.VITE_BASEURL;
    return baseUrl+"/avts/"+fileName;
}
