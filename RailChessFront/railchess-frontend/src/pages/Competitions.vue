<script setup lang="ts">
import { computed, onMounted, ref, useTemplateRef } from 'vue';
import { injectApi, injectPop, injectUserInfo } from '../provides';
import { Competition, CompetitionDetail, CompetitionMatch, CompetitionStatus, CompetitionStatusText } from '../models/competition';
import Loading from '../components/Loading.vue';
import SideBar from '../components/SideBar.vue';
import Search from '../components/Search.vue';

const api = injectApi()
const pop = injectPop()
const me = ref<number>(0)

const data = ref<Competition[]>([])
const total = ref<number>(0)
const loading = ref<boolean>(false)
const pageIdx = ref<number>(0)
const pageSize = ref<number>(10)

const sidebar = useTemplateRef('sidebar')
const editing = ref<Competition | undefined>()
const editingMatches = ref<CompetitionDetail | undefined>()
const orderedMatches = ref<CompetitionMatch[]>([])
const initialMatchIds = ref<number[]>([])

async function load() {
    loading.value = true
    const skip = pageIdx.value * pageSize.value
    const resp = await api.competition.list(skip, pageSize.value)
    if (resp) {
        data.value = resp.items
        total.value = resp.total
    }
    loading.value = false
}

function create() {
    editing.value = {
        Id: 0,
        Title: '',
        Description: '',
        HostUserId: me.value,
        StartTime: dateToTimestamp(new Date()),
        EndTime: dateToTimestamp(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)),
        Status: CompetitionStatus.Planned
    }
    editingMatches.value = undefined
    orderedMatches.value = []
    initialMatchIds.value = []
    sidebar.value?.extend()
}

function isOwner(c: Competition): boolean {
    return me.value > 0 && c.HostUserId === me.value
}

function edit(item: Competition) {
    if (!isOwner(item)) {
        pop.value.show('仅举办者可编辑', 'failed')
        return
    }
    editing.value = { ...item }
    editingMatches.value = undefined
    orderedMatches.value = []
    initialMatchIds.value = []
    loadMatches(item.Id)
    sidebar.value?.extend()
}

async function loadMatches(id: number) {
    const detail = await api.competition.detail(id)
    if (detail && editing.value?.Id === id) {
        editingMatches.value = detail
        orderedMatches.value = [...detail.Matches].sort((a, b) => a.OrderIndex - b.OrderIndex)
        initialMatchIds.value = orderedMatches.value.map(m => m.MatchId)
    }
}

function moveMatchUp(index: number) {
    if (index <= 0 || orderedMatches.value.length < 2) return
    const arr = orderedMatches.value
    ;[arr[index - 1], arr[index]] = [arr[index], arr[index - 1]]
    orderedMatches.value = [...arr]
}

const isOrderChanged = computed(() => {
    if (orderedMatches.value.length !== initialMatchIds.value.length) return false
    return orderedMatches.value.some((m, i) => m.MatchId !== initialMatchIds.value[i])
})

async function saveMatchOrder() {
    if (!editing.value || !isOrderChanged.value) return
    const matchIds = orderedMatches.value.map(m => m.MatchId)
    const ok = await api.competition.updateMatchOrder(editing.value.Id, matchIds)
    if (ok) {
        initialMatchIds.value = [...matchIds]
        await load()
    }
}

async function confirm() {
    if (!editing.value) return
    if (!editing.value.Title?.trim()) {
        pop.value.show('比赛名称不能为空', 'failed')
        return
    }

    console.log({
        s: editing.value.StartTime,
        e: editing.value.EndTime
    })

    if (isNaN(editing.value.StartTime) || isNaN(editing.value.EndTime)) {
        pop.value.show('请选择有效的开始和结束时间', 'failed')
        return
    }

    const competition: Competition = { ...editing.value }

    let ok: boolean
    if (editing.value.Id > 0) {
        ok = await api.competition.update(competition)
    } else {
        ok = await api.competition.create(competition)
    }

    if (ok) {
        sidebar.value?.fold()
        await load()
    }
}

async function deleteCompetition(id: number) {
    if (!window.confirm('确认删除该比赛？')) return
    const ok = await api.competition.delete(id)
    if (ok) {
        await load()
        if (editing.value?.Id === id) {
            sidebar.value?.fold()
        }
    }
}

async function onGameSelected(_name: string, gameId: number) {
    if (!gameId || !editing.value) return
    const ok = await api.competition.addMatch(editing.value.Id, gameId)
    if (ok) {
        await loadMatches(editing.value.Id)
        await load()
    }
}

async function removeMatch(gameId: number) {
    if (!editing.value) return
    if (!window.confirm('确认从比赛中移除该对局？')) return
    const ok = await api.competition.removeMatch(editing.value.Id, gameId)
    if (ok) {
        await loadMatches(editing.value.Id)
        await load()
    }
}

function viewWidget(id: number) {
    const url = `${import.meta.env.VITE_BASEURL}/api/Embed/Widget?competitionId=${id}`
    window.open(url, '_blank')
}

function dateToTimestamp(d: Date): number {
    return d.getTime()
}

function timestampToDateTimeLocal(ts?: number): string {
    if (ts === undefined || isNaN(ts)) return ''
    return formatDateTimeLocal(new Date(ts))
}

function dateTimeLocalToTimestamp(local: string): number {
    return new Date(local).getTime()
}

function formatDateTimeLocal(d: Date): string {
    const pad = (n: number) => n.toString().padStart(2, '0')
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function formatTime(ts?: number): string {
    if (ts === undefined || ts === null || isNaN(ts)) return '-'
    const d = new Date(ts)
    return `${d.getFullYear()}/${(d.getMonth() + 1).toString().padStart(2, '0')}/${d.getDate().toString().padStart(2, '0')} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`
}

function formatDateTimeLocalFromTimestamp(ts?: number): string {
    if (ts === undefined || ts === null || isNaN(ts)) return ''
    return formatDateTimeLocal(new Date(ts))
}

async function updateMatchScheduledStartTime(m: CompetitionMatch, value: string) {
    const ts = dateTimeLocalToTimestamp(value)
    m.ScheduledStartTime = ts
    const ok = await api.competition.updateMatchScheduledStartTime(m.MatchId, ts)
    if (!ok) {
        await loadMatches(editing.value!.Id)
    }
}

const totalPageCount = () => Math.ceil(total.value / pageSize.value)
const pageCanPrev = () => pageIdx.value > 0
const pageCanNext = () => pageIdx.value < totalPageCount() - 1

async function switchPage(to: 'prev' | 'next') {
    if (to === 'prev' && pageCanPrev()) {
        pageIdx.value--
    } else if (to === 'next' && pageCanNext()) {
        pageIdx.value++
    } else {
        return
    }
    await load()
}

onMounted(async () => {
    me.value = (await injectUserInfo().getIdentityInfo()).Id
    await load()
})
</script>

<template>
<div class="competitions">
    <h1 class="h1WithBtns">
        比赛列表
        <div>
            <button @click="create" class="confirm">新建比赛</button>
        </div>
    </h1>

    <div class="tableWrap">
        <table v-if="!loading && data.length > 0" class="list desktopTable"><tbody>
            <tr>
                <th style="min-width: 160px;">名称</th>
                <th style="min-width: 80px;">状态</th>
                <th style="min-width: 160px;">预计时间</th>
                <th style="min-width: 80px;">举办人</th>
                <th style="min-width: 70px;">对局数</th>
                <th style="min-width: 70px;">参赛人数</th>
                <th style="min-width: 120px;"></th>
            </tr>
            <tr v-for="c in data" :key="c.Id">
                <td class="titleCell">{{ c.Title || '未命名比赛' }}</td>
                <td>
                    <span class="statusTag" :class="'status-' + c.Status">{{ CompetitionStatusText[c.Status] }}</span>
                </td>
                <td class="timeCell">
                    <div>{{ formatTime(c.StartTime) }}</div>
                    <div class="subTime">至 {{ formatTime(c.EndTime) }}</div>
                </td>
                <td>{{ c.HostName || '???' }}</td>
                <td>{{ c.MatchCount ?? 0 }}</td>
                <td>{{ c.ParticipantCount ?? 0 }}</td>
                <td>
                    <button v-if="isOwner(c)" class="minor" @click="edit(c)">编辑</button>
                    <button class="minor" @click="viewWidget(c.Id)">Widget</button>
                </td>
            </tr>
        </tbody></table>

        <div v-if="!loading && data.length > 0" class="mobileCards">
            <div v-for="c in data" :key="c.Id" class="card">
                <div class="cardHeader">
                    <div class="cardTitle">{{ c.Title || '未命名比赛' }}</div>
                    <span class="statusTag" :class="'status-' + c.Status">{{ CompetitionStatusText[c.Status] }}</span>
                </div>
                <div class="cardMeta">
                    <div>举办人：{{ c.HostName || '???' }}</div>
                    <div>时间：{{ formatTime(c.StartTime) }} 至 {{ formatTime(c.EndTime) }}</div>
                    <div>对局：{{ c.MatchCount ?? 0 }} &nbsp;|&nbsp; 参赛：{{ c.ParticipantCount ?? 0 }} 人</div>
                </div>
                <div class="cardOps">
                    <button v-if="isOwner(c)" class="minor" @click="edit(c)">编辑</button>
                </div>
            </div>
        </div>

        <div v-if="!loading && data.length === 0" class="empty">暂无比赛</div>
        <Loading v-if="loading"></Loading>
    </div>

    <div v-if="!loading && totalPageCount() > 0" class="pager">
        <button class="lite" @click="switchPage('prev')" :class="{canClick: pageCanPrev()}">上页</button>
        <span>第 {{ pageIdx + 1 }} / {{ totalPageCount() }} 页</span>
        <button class="lite" @click="switchPage('next')" :class="{canClick: pageCanNext()}">下页</button>
    </div>
</div>

<SideBar ref="sidebar">
    <h1>{{ editing && editing.Id > 0 ? '编辑比赛' : '新建比赛' }}</h1>
    <table v-if="editing"><tbody>
        <tr>
            <td>名称</td>
            <td><input v-model="editing.Title" placeholder="比赛名称" /></td>
        </tr>
        <tr>
            <td>说明</td>
            <td>
                <textarea v-model="editing.Description" rows="3" placeholder="可选"></textarea>
            </td>
        </tr>
        <tr>
            <td>开始时间</td>
            <td><input type="datetime-local"
                :value="timestampToDateTimeLocal(editing?.StartTime)"
                @input="(e) => editing && (editing.StartTime = dateTimeLocalToTimestamp((e.target as HTMLInputElement).value))" /></td>
        </tr>
        <tr>
            <td>结束时间</td>
            <td><input type="datetime-local"
                :value="timestampToDateTimeLocal(editing?.EndTime)"
                @input="(e) => editing && (editing.EndTime = dateTimeLocalToTimestamp((e.target as HTMLInputElement).value))" /></td>
        </tr>
        <tr>
            <td>状态</td>
            <td>
                <select v-model="editing.Status">
                    <option :value="CompetitionStatus.Planned">未开始</option>
                    <option :value="CompetitionStatus.Ongoing">进行中</option>
                    <option :value="CompetitionStatus.Completed">已结束</option>
                    <option :value="CompetitionStatus.Cancelled">已取消</option>
                </select>
            </td>
        </tr>
        <tr class="noneBackground">
            <td colspan="2"><button @click="confirm" class="confirm">保存</button></td>
        </tr>
    </tbody></table>

    <div v-if="editing && editing.Id > 0" class="matchesSection">
        <h2>对局管理</h2>
        <div class="addMatchHint">对局必须有名称才能添加</div>
        <div class="addMatch">
            <Search :source="api.game.quickSearch" :allow-free-input="false"
                @done="onGameSelected" :placeholder="'搜索对局名称'"></Search>
        </div>
        <div v-if="editingMatches" class="matchList">
            <div v-for="(m, idx) in orderedMatches" :key="m.MatchId" class="matchItem">
                <div class="matchInfo">
                    <div class="matchName">{{ m.GameName || '棋局 #' + m.GameId }}</div>
                    <div class="matchStage" v-if="m.Stage">{{ m.Stage }}</div>
                    <div class="matchHost">房主：{{ m.HostUserName || '???' }}</div>
                    <div class="matchScheduled">
                        预定
                        <input type="datetime-local" class="scheduledInput"
                            :value="formatDateTimeLocalFromTimestamp(m.ScheduledStartTime)"
                            @change="(e) => updateMatchScheduledStartTime(m, (e.target as HTMLInputElement).value)" />
                    </div>
                </div>
                <div class="matchOps">
                    <button @click="moveMatchUp(idx)" :disabled="idx === 0" class="lite" title="上移">↑</button>
                    <button @click="removeMatch(m.GameId)" class="lite">×</button>
                </div>
            </div>
            <div v-if="orderedMatches.length === 0" class="emptySmall">暂无对局</div>
            <div v-if="isOrderChanged" class="saveOrder">
                <button @click="saveMatchOrder" class="confirm">保存排序</button>
            </div>
        </div>
        <Loading v-else></Loading>
    </div>

    <div v-if="editing && editing.Id > 0" class="dangerZone">
        <button @click="deleteCompetition(editing.Id)" class="danger">删除比赛</button>
    </div>
</SideBar>
</template>

<style scoped>
.tableWrap {
    overflow-x: auto;
}
.desktopTable {
    min-width: 760px;
}
.titleCell {
    font-weight: 600;
}
.timeCell {
    font-size: 14px;
}
.subTime {
    color: #777;
    font-size: 12px;
}
.statusTag {
    display: inline-block;
    padding: 2px 8px;
    border-radius: 4px;
    font-size: 12px;
    color: white;
    background-color: #888;
}
.statusTag.status-0 { background-color: #5a9bd5; }
.statusTag.status-1 { background-color: #5cb85c; }
.statusTag.status-2 { background-color: #777; }
.statusTag.status-3 { background-color: #d9534f; }

.empty {
    text-align: center;
    padding: 40px 0;
    color: #999;
}
.pager {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 10px;
    margin: 10px 0 30px;
}
.pager .canClick {
    color: cornflowerblue;
}

.mobileCards {
    display: none;
    flex-direction: column;
    gap: 10px;
    padding: 0 5px 20px;
}
.card {
    border: 1px solid #ddd;
    border-radius: 8px;
    padding: 12px;
    background: #fafafa;
}
.cardHeader {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 8px;
}
.cardTitle {
    font-weight: 600;
    font-size: 16px;
    word-break: break-all;
}
.cardMeta {
    font-size: 13px;
    color: #555;
    line-height: 1.6;
    margin-bottom: 10px;
}
.cardOps {
    display: flex;
    gap: 6px;
}

.matchesSection {
    margin-top: 20px;
    border-top: 1px solid #ccc;
    padding-top: 15px;
}
.matchesSection h2 {
    font-size: 16px;
    color: #666;
    margin-bottom: 10px;
}
.addMatchHint {
    font-size: 12px;
    color: #888;
    margin-bottom: 6px;
}
.addMatch {
    margin-bottom: 10px;
}
.matchList {
    display: flex;
    flex-direction: column;
    gap: 8px;
}
.matchItem {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px;
    border: 1px solid #ddd;
    border-radius: 6px;
    background: white;
}
.matchInfo {
    flex: 1;
    min-width: 0;
}
.matchName {
    font-weight: 600;
    font-size: 14px;
    word-break: break-all;
}
.matchStage {
    display: inline-block;
    font-size: 11px;
    color: #666;
    background: #eee;
    padding: 1px 6px;
    border-radius: 4px;
    margin-top: 2px;
}
.matchHost {
    font-size: 12px;
    color: #888;
    margin-top: 2px;
}
.matchScheduled {
    font-size: 12px;
    color: #666;
    margin-top: 4px;
    display: flex;
    align-items: center;
    gap: 4px;
}
.scheduledInput {
    width: 170px;
    font-size: 12px;
    padding: 2px;
    margin: 0;
}
.emptySmall {
    text-align: center;
    color: #999;
    padding: 20px 0;
    font-size: 14px;
}
.matchOps {
    display: flex;
    gap: 6px;
    align-items: center;
}
.matchOps button:disabled {
    opacity: 0.4;
    cursor: not-allowed;
}
.saveOrder {
    margin-top: 10px;
    display: flex;
    justify-content: center;
}
.dangerZone {
    margin-top: 30px;
    padding-top: 15px;
    border-top: 1px solid #ccc;
    display: flex;
    justify-content: center;
}

textarea {
    width: 200px;
    font-size: unset;
    margin: 5px;
    padding: 3px;
    border: 1px solid #aaa;
    border-radius: 5px;
    resize: vertical;
}

@media screen and (max-width: 768px) {
    .desktopTable {
        display: none;
    }
    .mobileCards {
        display: flex;
    }
}
</style>
