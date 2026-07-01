<script setup lang="ts">
import { reactive, ref, useTemplateRef, watch } from 'vue'
import { injectApi } from '../provides'
import { CompetitionParticipant } from '../models/competition'
import Search from './Search.vue'

const props = defineProps<{
    modelValue?: string
}>()

const emit = defineEmits<{
    (e: 'update:modelValue', value: string | undefined): void
}>()

const api = injectApi()
const searchRef = useTemplateRef('search')

const participants = ref<CompetitionParticipant[]>([])
const userNames = reactive<Record<number, string>>({})

function parse(value?: string) {
    if (!value) {
        participants.value = []
        return
    }
    try {
        const parsed = JSON.parse(value) as CompetitionParticipant[]
        if (Array.isArray(parsed)) {
            participants.value = parsed
            loadUserNames()
            return
        }
    } catch {
        // ignore
    }
    participants.value = []
}

async function loadUserNames() {
    const ids = participants.value
        .map(p => p.UserId)
        .filter(id => !userNames[id])
    if (ids.length === 0) return
    const infos = await api.identites.user.getUserInfoByIds(ids)
    for (const u of infos) {
        userNames[u.Id] = u.Name
    }
}

function emitValue() {
    const arr = participants.value
    emit('update:modelValue', arr.length > 0 ? JSON.stringify(arr) : undefined)
}

function add(userId: number, name: string) {
    if (!userId) {
        return
    }
    if (participants.value.some(p => p.UserId === userId)) {
        return
    }
    userNames[userId] = name
    participants.value.push({
        UserId: userId,
        Number: undefined
    })
    emitValue()
    searchRef.value?.clear()
}

function remove(index: number) {
    participants.value.splice(index, 1)
    emitValue()
}

function onNumberChanged(index: number, value: string) {
    participants.value[index].Number = value.trim() || undefined
    emitValue()
}

watch(() => props.modelValue, parse, { immediate: true })
</script>

<template>
    <div class="participantEditor">
        <div class="addRow">
            <Search
                ref="search"
                :source="api.identites.user.quickSearch"
                :allow-free-input="false"
                :placeholder="'搜索用户并确认添加'"
                @done="(_name, id) => add(id, _name)"
            />
        </div>
        <div class="list">
            <div v-for="(p, idx) in participants" :key="p.UserId" class="item">
                <span class="userName">{{ userNames[p.UserId] || 'UID ' + p.UserId }}</span>
                <input
                    :value="p.Number"
                    @input="(e) => onNumberChanged(idx, (e.target as HTMLInputElement).value)"
                    placeholder="参赛编号"
                    class="numberInput"
                />
                <button class="lite danger" @click="remove(idx)">×</button>
            </div>
            <div v-if="participants.length === 0" class="empty">暂无参赛选手</div>
        </div>
    </div>
</template>

<style scoped>
.participantEditor {
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.addRow {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 6px;
    flex-wrap: wrap;
}
.numberInput {
    width: 90px;
    padding: 3px 5px;
    border: 1px solid #aaa;
    border-radius: 4px;
}
.list {
    display: flex;
    flex-direction: column;
    gap: 4px;
    max-height: 200px;
    overflow-y: auto;
}
.item {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 4px 6px;
    border: 1px solid #ddd;
    border-radius: 4px;
    background: #fafafa;
}
.userName {
    font-size: 14px;
    color: #333;
    flex: 1;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}
.empty {
    font-size: 13px;
    color: #999;
    padding: 8px 0;
}
.danger {
    color: #d9534f;
}
</style>
