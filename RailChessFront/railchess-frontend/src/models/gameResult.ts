interface GameResultListItem {
    Rank: number;
    PlayerCount: number;
    GameId: number;
    StartTime: string;
    EloDelta: number;
    GameName: string;
    MapName: string;
    MapId: number;
    UserName: string;
    UserId: number;
}

interface GameResultListResponse {
    Logs: GameResultListItem[];
    OwnerName: string
}