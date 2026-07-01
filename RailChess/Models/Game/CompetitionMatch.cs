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

        /// <summary>
        /// 积分规则，JSON格式，例如：
        /// { "Rules": [{ "PlayerCount": 4, "Points": [10,6,3,1] }] }
        /// 为 null 表示本局不积分
        /// 由业务代码自行序列化/反序列化
        /// </summary>
        public string? ScoringJson { get; set; }

        public Competition? Competition { get; set; }
        public RailChessGame? Game { get; set; }
    }

    /// <summary>对局积分规则，供业务代码解析 ScoringJson 使用</summary>
    public class CompetitionMatchScoring
    {
        public List<CompetitionMatchScoringRule> Rules { get; set; } = new();
    }

    /// <summary>针对特定人数的对局积分规则</summary>
    public class CompetitionMatchScoringRule
    {
        /// <summary>该局实际参赛人数</summary>
        public int PlayerCount { get; set; }

        /// <summary>名次积分列表，索引 0 为第一名</summary>
        public List<int> Points { get; set; } = new();
    }
}
