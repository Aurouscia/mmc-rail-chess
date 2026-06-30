using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace RailChess.Models.Game
{
    /// <summary>
    /// 比赛对局：一场 Competition 与一场 RailChessGame 的关联，可携带在该比赛中的元信息
    /// </summary>
    [Index(nameof(CompetitionId))]
    public class CompetitionMatch
    {
        public int Id { get; set; }

        /// <summary>所属比赛Id</summary>
        public int CompetitionId { get; set; }

        /// <summary>关联的具体游戏Id</summary>
        public int GameId { get; set; }

        /// <summary>在比赛中的展示/进行顺序</summary>
        public int OrderIndex { get; set; }

        /// <summary>阶段说明，例如：小组赛、八强、半决赛、决赛</summary>
        public string? Stage { get; set; }

        /// <summary>预计开始时间</summary>
        public DateTime ScheduledStartTime { get; set; }

        public Competition? Competition { get; set; }
        public RailChessGame? Game { get; set; }
    }
}
