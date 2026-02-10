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

const rankLocked = computed<boolean[]>(()=>{
    const list = playerScoreboard.value
    if (list.length === 0) return []
    
    // 计算剩余分数
    const totalScored = list.reduce((sum, p) => sum + p.score, 0)
    const remainingScore = props.init.TotalScore - totalScored
    
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
        <span class="infoItem">
            <b>棋盘：</b>{{ init.MapName }}
        </span>
        <span v-if="init.GameInfo.GameName" class="infoItem">
            <b>对局：</b>{{ init.GameInfo.GameName }}
        </span>
        <span class="infoItem">
            <b>总分：</b>{{ init.TotalScore }}
        </span>
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
    width: 280px;
    padding: 16px;
    background-color: #f5f5f5;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);

    .gameInfo {
        margin-bottom: 16px;
        padding-bottom: 12px;
        border-bottom: 1px solid #ddd;

        .infoItem {
            display: inline-block;
            margin-right: 12px;

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
            gap: 8px;
            padding: 8px;
            background-color: #fff;
            border-radius: 6px;
            transition: background-color 0.2s ease;

            &:hover {
                background-color: #e8f4f8;
            }

            .rank {
                font-weight: bold;
                color: #666;
                min-width: 30px;
                text-align: center;
                font-family: monospace;
            }

            .score {
                font-weight: bold;
                color: #1890ff;
                min-width: 40px;
                text-align: right;
            }

            .avatar {
                width: 40px;
                height: 40px;
                border-radius: 50%;
                object-fit: cover;
                border: 2px solid #e0e0e0;
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
                filter: saturate(0.4) brightness(1.2);
                opacity: 0.7;
            }
        }
    }
}
</style>