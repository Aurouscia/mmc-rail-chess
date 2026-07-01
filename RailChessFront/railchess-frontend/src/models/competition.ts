export interface Competition {
    Id: number
    Title?: string
    Description?: string
    HostUserId: number
    HostName?: string
    CreateTime?: number
    StartTime: number
    EndTime: number
    Status: CompetitionStatus
    HomepageUrl?: string
    ParticipantsJson?: string
    ParticipantCount?: number
    MatchCount?: number
}

export enum CompetitionStatus {
    Planned = 0,
    Ongoing = 1,
    Completed = 2,
    Cancelled = 3
}

export const CompetitionStatusText: Record<CompetitionStatus, string> = {
    [CompetitionStatus.Planned]: '未开始',
    [CompetitionStatus.Ongoing]: '进行中',
    [CompetitionStatus.Completed]: '已结束',
    [CompetitionStatus.Cancelled]: '已取消'
}

export interface CompetitionParticipant {
    UserId: number
    Number?: string
}

export interface CompetitionMatchScoringRule {
    PlayerCount: number
    Points: number[]
}

export interface CompetitionMatchScoring {
    Rules: CompetitionMatchScoringRule[]
}

export interface CompetitionListResponse {
    total: number
    items: Competition[]
}

export interface CompetitionMatch {
    MatchId: number
    GameId: number
    OrderIndex: number
    Stage?: string
    ScheduledStartTime?: number
    ScoringJson?: string
    GameName?: string
    HostUserName?: string
}

export interface CompetitionDetail extends Competition {
    Matches: CompetitionMatch[]
}
