interface GameResultListItem {
    Rank: number;
    GameId: number;
    StartTime: string;
    EloDelta: number;
    MapName: string;
    UserName: string;
    UserId: number;
}

interface GameResultListResponse {
    Logs: GameResultListItem[];
}