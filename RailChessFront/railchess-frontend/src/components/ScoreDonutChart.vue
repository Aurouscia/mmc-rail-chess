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
    const backgroundColors = [
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
    
    return {
        labels,
        datasets: [{
            data,
            backgroundColor: backgroundColors.slice(0, data.length),
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
                padding: 10,
                font: {
                    size: 11
                }
            }
        },
        tooltip: {
            callbacks: {
                label: (context: any) => {
                    const label = context.label || ''
                    const value = context.parsed || 0
                    const percentage = ((value / props.totalScore) * 100).toFixed(1)
                    return `${label}: ${value}分 (${percentage}%)`
                }
            }
        },
        title: {
            display: true,
            text: '分数分布',
            font: {
                size: 14
            },
            padding: {
                top: 10,
                bottom: 10
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
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}
</style>
