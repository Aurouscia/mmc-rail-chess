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
        public string? ParticipantUserIdCsv { get; set; }
        public int MatchCount { get; set; }
        public int ParticipantCount { get; set; }
        public List<CompetitionMatchDto> Matches { get; set; } = new();

        public static long ToTimestamp(DateTime dt)
            => new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds();

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
                ParticipantUserIdCsv = c.ParticipantUserIdCsv,
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
                ParticipantUserIdCsv = ParticipantUserIdCsv
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
        public long ScheduledStartTime { get; set; }
        public string? GameName { get; set; }
        public string? HostUserName { get; set; }
    }
}
