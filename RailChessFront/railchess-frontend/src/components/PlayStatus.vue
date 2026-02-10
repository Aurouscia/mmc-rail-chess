<script setup lang="ts">
import { computed } from 'vue';
import { GameInitData, Player, SyncData } from '../models/play';
import { avtSrc } from '../utils/fileSrc';

const props = defineProps<{
    init: GameInitData,
    sync: SyncData
}>()

const playerScoreboard = computed<Player[]>(()=>{
    const res = [...props.sync.playerStatus]
    res.sort((a, b) => b.score - a.score)
    return res
})

const totalScored = computed(()=>{
    return playerScoreboard.value.reduce((sum, p) => sum + p.score, 0)
})

const rankLocked = computed<boolean[]>(()=>{
    const list = playerScoreboard.value
    if (list.length === 0) return []
    
    // 计算剩余分数
    const remainingScore = props.init.TotalScore - totalScored.value
    
    // 计算相邻玩家之间能否互换（能否超过对方）
    // canSwap[i] 表示玩家 i 和玩家 i+1 能否互换排名（即 i+1 能否超过 i）
    const canSwap: boolean[] = []
    for (let i = 0; i < list.length - 1; i++) {
        // 后一名玩家(i+1)能否超过前一名玩家(i)
        canSwap[i] = list[i+1].score + remainingScore >= list[i].score
    }
    
    // 计算每个玩家的排名是否锁定
    // 玩家 i 的排名锁定 = 不能上升（不能与前一名互换）且 不能下降（不能与后一名互换）
    const locked: boolean[] = []
    for (let i = 0; i < list.length; i++) {
        const canRise = i > 0 && canSwap[i-1]      // 能与前一名互换（上升）
        const canFall = i < list.length - 1 && canSwap[i]  // 能与后一名互换（下降）
        locked[i] = !canRise && !canFall
    }
    
    return locked
})
</script>

<template>
<div class="playStatus">
    <div class="gameInfo">
        <div class="infoItem">
            <b>棋盘：</b>{{ init.MapName }}
        </div>
        <div v-if="init.GameInfo.GameName" class="infoItem">
            <b>对局：</b>{{ init.GameInfo.GameName }}
        </div>
        <div class="infoItem">
            <b>分数：</b>{{ totalScored }}/{{ init.TotalScore }}
        </div>
    </div>
    <div class="playerList">
        <div v-for="p, idx in playerScoreboard" class="playerItem"
            :class="{playerOut: p.out}">
            <span v-if="rankLocked[idx]" class="rank">
                [{{ idx+1 }}]
            </span>
            <span v-else class="rank">
                {{ idx+1 }}
            </span>
            <span class="score">{{ p.score }}</span>
            <img class="avatar" :src="avtSrc(p.avtFileName)"/>
            <span class="playerName">{{ p.name }}</span>
        </div>
    </div>
</div>
</template>

<style scoped lang="scss">
.playStatus {
    padding: 10px;
    background-color: #f5f5f5;
    border-radius: 8px;

    .gameInfo {
        margin-bottom: 16px;
        border-bottom: 1px solid #ddd;
        .infoItem {
            margin: 10px 0px;
            color: #666;
            b {
                color: #333;
            }
        }
    }

    .playerList {
        display: flex;
        flex-direction: column;
        gap: 12px;

        .playerItem {
            display: flex;
            align-items: center;
            gap: 4px;
            padding: 4px;
            background-color: #fff;
            border-radius: 6px;
            transition: background-color 0.2s ease;

            .rank {
                font-weight: bold;
                color: #666;
                min-width: 26px;
                text-align: center;
                font-family: monospace;
            }

            .score {
                font-weight: bold;
                color: cornflowerblue;
                min-width: 32px;
                text-align: right;
                font-family: monospace;
            }

            .avatar {
                width: 28px;
                height: 28px;
                border-radius: 50%;
                object-fit: cover;
                border: 1px solid #e0e0e0;
            }

            .playerName {
                flex: 1;
                color: #333;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
                min-width: 0;
            }

            &.playerOut {
                * {
                    opacity: 0.5;
                }
            }
        }
    }
}
</style>