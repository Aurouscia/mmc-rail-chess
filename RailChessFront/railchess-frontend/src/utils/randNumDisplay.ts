import { computed, Ref } from "vue"
import { RailChessGame, RandAlgType } from "../models/game"

function isAB(num:number){
    return num > 100
}
function displayForAB(num:number){
    const A = num % 100
    const B = Math.floor(num / 100)
    return `${A}/${A+B}`;
}
export function displayForRandNum(num:number, gameInfo?: RailChessGame){
    if(gameInfo && gameInfo.RandAlg === RandAlgType.FreeWithinRange){
        return `${gameInfo.RandMin}-${gameInfo.RandMax}`
    }
    if(isAB(num)){
        return displayForAB(num)
    }
    return num.toString()
}

export function useRandNumDisplay(randNum:Ref<number>, gameInfo?: Ref<RailChessGame | undefined>){
    //AB型随机数：三或四位数，个位十位、百位千位分别是A、B两个十位数，A和A+B都是可选项
    const randNumIsAB = computed<boolean>(()=>{
        return isAB(randNum.value)
    })
    const randNumDisplay = computed<string>(()=>{
        if(gameInfo && gameInfo.value && gameInfo.value.RandAlg === RandAlgType.FreeWithinRange){
            return `${gameInfo.value.RandMin}-${gameInfo.value.RandMax}`
        }
        if(randNumIsAB.value)
            return displayForAB(randNum.value)
        return randNum.value.toString();
    })
    return {
        randNumIsAB, 
        randNumDisplay,
        displayForRandNum
    }
}