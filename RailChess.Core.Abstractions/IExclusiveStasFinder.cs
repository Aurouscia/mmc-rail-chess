using RailChess.GraphDefinition;

namespace RailChess.Core.Abstractions
{
    public class ExclusiveStasOptions
    {
        /// <summary>
        /// 队伍分组，每个内层列表代表一队玩家。
        /// 同一队玩家共享可达区域，队友占领的站不会被视为独占站。
        /// </summary>
        public List<List<int>>? Teams { get; set; }
    }

    public interface IExclusiveStasFinder
    {
        public List<int> FindExclusiveStas(Graph graph, int userId, ExclusiveStasOptions? options = null);
    }
}
