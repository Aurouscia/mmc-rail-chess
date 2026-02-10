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
        <div v-for="p, idx in playerScoreboard" class="playerItem" :class="{playerOut: p.out}">
            <span class="rank">{{ idx+1 }}.</span>
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
            gap: 10px;
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
                min-width: 24px;
                text-align: center;
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