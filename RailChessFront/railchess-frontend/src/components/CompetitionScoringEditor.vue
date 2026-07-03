<script setup lang="ts">
import { ref, watch } from 'vue'
import { CompetitionMatchScoring } from '../models/competition'
import { useScoringPresetStore, ScoringPreset } from '../utils/stores/scoringPresetStore'

const presetStore = useScoringPresetStore()

const props = defineProps<{
    modelValue?: string
}>()

const emit = defineEmits<{
    (e: 'update:modelValue', value: string | undefined): void
}>()

const scoring = ref<CompetitionMatchScoring>({ Rules: [] })

function parse(value?: string) {
    if (!value) {
        scoring.value = { Rules: [] }
        return
    }
    try {
        const parsed = JSON.parse(value) as CompetitionMatchScoring
        if (parsed && Array.isArray(parsed.Rules)) {
            scoring.value = parsed
            return
        }
    } catch {
        // ignore
    }
    scoring.value = { Rules: [] }
}

function emitValue() {
    const rules = scoring.value.Rules
        .filter(r => r.PlayerCount > 0 && r.Points.length === r.PlayerCount)
        .sort((a, b) => a.PlayerCount - b.PlayerCount)
    if (rules.length === 0) {
        emit('update:modelValue', undefined)
        return
    }
    emit('update:modelValue', JSON.stringify({ Rules: rules }))
}

function addRule() {
    scoring.value.Rules.push({ PlayerCount: 4, Points: [0, 0, 0, 0] })
    emitValue()
}

function removeRule(index: number) {
    scoring.value.Rules.splice(index, 1)
    emitValue()
}

function updatePlayerCount(index: number, value: string) {
    let count = parseInt(value)
    if (isNaN(count)) count = 2
    count = Math.max(2, Math.min(10, count))
    const rule = scoring.value.Rules[index]
    const oldPoints = rule.Points
    if (count > oldPoints.length) {
        rule.Points = [...oldPoints, ...Array(count - oldPoints.length).fill(0)]
    } else {
        rule.Points = oldPoints.slice(0, count)
    }
    rule.PlayerCount = count
    emitValue()
}

function updatePoint(ruleIndex: number, pointIndex: number, value: string) {
    const n = parseInt(value)
    scoring.value.Rules[ruleIndex].Points[pointIndex] = isNaN(n) ? 0 : n
    emitValue()
}

watch(() => props.modelValue, parse, { immediate: true })

function savePreset() {
    const name = window.prompt('请输入预设名称')
    if (!name || !name.trim()) return
    presetStore.add({
        name: name.trim(),
        scoring: JSON.parse(JSON.stringify(scoring.value)) as CompetitionMatchScoring
    })
}

function applyPreset(preset: ScoringPreset) {
    if (scoring.value.Rules.length > 0) {
        if (!window.confirm(`当前已有积分规则，确定应用预设“${preset.name}”吗？`)) {
            return
        }
    }
    parse(JSON.stringify(preset.scoring))
    emitValue()
}
</script>

<template>
    <div class="scoringEditor">
        <div v-if="presetStore.presets.length > 0" class="presets">
            <div
                v-for="preset in presetStore.presets"
                :key="preset.name"
                class="presetItem"
            >
                <button class="lite applyBtn" @click="applyPreset(preset)">
                    使用预设 {{ preset.name }}
                </button>
                <button class="lite removeBtn" @click="presetStore.remove(preset.name)">
                    移除
                </button>
            </div>
        </div>
        <div class="rules">
            <div v-for="(rule, rIdx) in scoring.Rules" :key="rIdx" class="rule">
                <div class="ruleHeader">
                    <label>
                        人数
                        <input
                            type="number"
                            min="2"
                            max="10"
                            :value="rule.PlayerCount"
                            @change="(e) => updatePlayerCount(rIdx, (e.target as HTMLInputElement).value)"
                            class="countInput"
                        />
                    </label>
                    <button class="lite danger" @click="removeRule(rIdx)">删除规则</button>
                </div>
                <div class="points">
                    <div v-for="(_, pIdx) in rule.Points" :key="pIdx" class="point">
                        <span class="rank">第 {{ pIdx + 1 }} 名</span>
                        <input
                            type="number"
                            :value="rule.Points[pIdx]"
                            @input="(e) => updatePoint(rIdx, pIdx, (e.target as HTMLInputElement).value)"
                            class="pointInput"
                        />
                    </div>
                </div>
            </div>
            <div v-if="scoring.Rules.length === 0" class="empty">非积分赛（无积分规则）</div>
        </div>
        <button class="minor" @click="addRule">+添加积分规则</button>
        <button class="lite save-preset" @click="savePreset">
            保存当前设置为预设
        </button>
    </div>
</template>

<style scoped>
.scoringEditor {
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.rules {
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.rule {
    border: 1px solid #ddd;
    border-radius: 6px;
    padding: 8px;
    background: #fafafa;
}
.ruleHeader {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 6px;
}
.countInput {
    width: 60px;
    padding: 2px 4px;
    border: 1px solid #aaa;
    border-radius: 4px;
    margin-left: 4px;
}
.points {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    align-items: center;
}
.point {
    display: flex;
    align-items: center;
    gap: 4px;
    padding: 3px 6px;
    border: 1px solid #e0e0e0;
    border-radius: 4px;
    background: white;
}
.rank {
    font-size: 12px;
    color: #666;
}
.pointInput {
    width: 50px;
    padding: 2px 4px;
    border: 1px solid #aaa;
    border-radius: 4px;
}
.empty {
    font-size: 13px;
    color: #999;
    padding: 4px 0;
}
.danger {
    color: #d9534f;
}
.presets {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.presetItem {
    display: flex;
    gap: 4px;
    align-items: center;
}
.applyBtn {
    flex: 1;
    text-align: left;
    padding-top: 2px;
    padding-bottom: 2px;
    color: cornflowerblue
}
.removeBtn {
    color: #999;
    padding-top: 2px;
    padding-bottom: 2px;
}
.save-preset{
    color: cornflowerblue;
    margin-top: 10px;
}
</style>
