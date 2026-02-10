<script setup lang="ts">
import { computed } from 'vue';
import { Doughnut } from 'vue-chartjs';
import { Chart as ChartJS, ArcElement, Tooltip, Legend, Title } from 'chart.js';
import { Player } from '../models/play';

ChartJS.register(ArcElement, Tooltip, Legend, Title);

const props = defineProps<{
    totalScore: number,
    players: Player[]
}>()

// 计算图表数据
const chartData = computed(() => {
    const labels = props.players.map(p => p.name)
    const data = props.players.map(p => p.score)
    const baseColors = [
        '#5470c6',
        '#91cc75',
        '#fac858',
        '#ee6666',
        '#73c0de',
        '#3ba272',
        '#fc8452',
        '#9a60b4',
        '#ea7ccc',
    ]
    
    // 如果还有剩余分数，添加一个"未分配"区块
    const totalScored = data.reduce((sum, score) => sum + score, 0)
    const remainingScore = props.totalScore - totalScored
    
    if (remainingScore > 0) {
        labels.push('未分配')
        data.push(remainingScore)
    }
    
    // 循环使用颜色，确保玩家数量超出颜色列表时仍有颜色
    const backgroundColors = data.map((_, index) => 
        baseColors[index % baseColors.length]
    )
    
    return {
        labels,
        datasets: [{
            data,
            backgroundColor: backgroundColors,
            borderWidth: 2,
            borderColor: '#fff',
            hoverOffset: 4
        }]
    }
})

const chartOptions = {
    responsive: true,
    maintainAspectRatio: true,
    cutout: '60%',
    plugins: {
        legend: {
            position: 'bottom' as const,
            labels: {
                boxWidth: 12,
                padding: 4,
                font: {
                    size: 11
                }
            }
        },
        tooltip: {
            callbacks: {
                label: (context: any) => {
                    const value = context.parsed || 0
                    const percentage = ((value / props.totalScore) * 100).toFixed(1)
                    return ` ${value}分 (${percentage}%)`
                }
            }
        },
        title: {
            display: true,
            text: '分数分布',
            font: {
                size: 12
            },
            padding: {
                top: 4,
                bottom: 4
            }
        }
    }
}
</script>

<template>
<div class="scoreDonutChart">
    <Doughnut :data="chartData" :options="chartOptions" />
</div>
</template>

<style scoped lang="scss">
.scoreDonutChart {
    width: 100%;
    padding: 12px;
    box-sizing: border-box;
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}
</style>
