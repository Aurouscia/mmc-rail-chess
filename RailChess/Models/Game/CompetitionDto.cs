namespace RailChess.Models.Game
{
    /// <summary>比赛信息传输对象，时间字段使用 Unix 毫秒时间戳</summary>
    public class CompetitionDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int HostUserId { get; set; }
        public string? HostName { get; set; }
        public long CreateTime { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public CompetitionStatus Status { get; set; }
        public string? HomepageUrl { get; set; }
        public string? ParticipantsJson { get; set; }
        public int MatchCount { get; set; }
        public int ParticipantCount { get; set; }
        public List<CompetitionMatchDto> Matches { get; set; } = new();

        public static long ToTimestamp(DateTime dt)
        {
            // SQLite 读回的 DateTime 通常是 DateTimeKind.Unspecified，
            // 若直接调用 ToUniversalTime() 会被当作本地时间再转一次 UTC，导致时区偏移。
            // 这些字段实际存储的是 UTC 时间，因此 Unspecified 时先标记为 Utc。
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        public static DateTime FromTimestamp(long ts)
            => DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime;

        public static CompetitionDto From(
            Competition c,
            Dictionary<int, string?>? hostNames = null,
            int matchCount = 0,
            int participantCount = 0)
        {
            return new CompetitionDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                HostUserId = c.HostUserId,
                HostName = hostNames?.GetValueOrDefault(c.HostUserId),
                CreateTime = ToTimestamp(c.CreateTime),
                StartTime = ToTimestamp(c.StartTime),
                EndTime = ToTimestamp(c.EndTime),
                Status = c.Status,
                HomepageUrl = c.HomepageUrl,
                ParticipantsJson = c.ParticipantsJson,
                MatchCount = matchCount,
                ParticipantCount = participantCount
            };
        }

        public Competition ToCompetition()
        {
            return new Competition
            {
                Id = Id,
                Title = Title,
                Description = Description,
                HostUserId = HostUserId,
                CreateTime = FromTimestamp(CreateTime),
                StartTime = FromTimestamp(StartTime),
                EndTime = FromTimestamp(EndTime),
                Status = Status,
                HomepageUrl = HomepageUrl,
                ParticipantsJson = ParticipantsJson
            };
        }
    }

    /// <summary>比赛对局传输对象，时间字段使用 Unix 毫秒时间戳</summary>
    public class CompetitionMatchDto
    {
        public int MatchId { get; set; }
        public int GameId { get; set; }
        public int OrderIndex { get; set; }
        public string? Stage { get; set; }
        public long? ScheduledStartTime { get; set; }
        public string? ScoringJson { get; set; }
        public string? GameName { get; set; }
        public string? HostUserName { get; set; }
    }
}
