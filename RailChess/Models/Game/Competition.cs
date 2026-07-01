using System.ComponentModel.DataAnnotations;

namespace RailChess.Models.Game
{
    /// <summary>
    /// 比赛：由多场已创建的 RailChessGame 组成的高层面赛事
    /// </summary>
    public class Competition
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        /// <summary>创建者/主办方用户Id</summary>
        public int HostUserId { get; set; }

        public DateTime CreateTime { get; set; }

        /// <summary>计划开始时间</summary>
        public DateTime StartTime { get; set; }

        /// <summary>结束时间</summary>
        public DateTime EndTime { get; set; }

        public CompetitionStatus Status { get; set; }

        public bool Deleted { get; set; }

        /// <summary>比赛主页URL</summary>
        public string? HomepageUrl { get; set; }

        /// <summary>
        /// 参赛选手列表，JSON格式，例如：[{ "UserId": 1, "Number": "A01" }]
        /// 由业务代码自行序列化/反序列化
        /// </summary>
        public string? ParticipantsJson { get; set; }

        /// <summary>比赛包含的对局</summary>
        public List<CompetitionMatch> Matches { get; set; } = new();
    }

    /// <summary>参赛选手信息，供业务代码解析 ParticipantsJson 使用</summary>
    public class CompetitionParticipant
    {
        public int UserId { get; set; }
        public string? Number { get; set; }
    }

    public enum CompetitionStatus
    {
        Planned = 0,
        Ongoing = 1,
        Completed = 2,
        Cancelled = 3
    }
}
