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

        /// <summary>参赛用户Id列表，CSV格式</summary>
        public string? ParticipantUserIdCsv { get; set; }

        /// <summary>比赛包含的对局</summary>
        public List<CompetitionMatch> Matches { get; set; } = new();
    }

    public enum CompetitionStatus
    {
        Planned = 0,
        Ongoing = 1,
        Completed = 2,
        Cancelled = 3
    }
}
