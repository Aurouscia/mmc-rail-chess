using RailChess.GraphDefinition;

namespace RailChess.Core.Abstractions
{
    public interface IFixedStepPathFinder
    {
        /// <summary>
        /// 给定图、当前玩家id、步数，得到可走的路径（二维车站id数组）<br/>
        /// 约定：<br/>
        /// 1.步数为0-99时正常计算<br/>
        /// 2.-1时可一步走到任意(未被他人占的)点<br/>
        /// 3.100以上时，千位百位算B，十位个位算A，A与A+B两种步数均可
        /// </summary>
        /// <param name="graph">当前图</param>
        /// <param name="userId">当前玩家id</param>
        /// <param name="steps">步数</param>
        /// <param name="maxiumTransfer">最多换乘次数</param>
        /// <returns></returns>
        public IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int steps, int maxiumTransfer = int.MaxValue);
        /// <summary>
        /// 是否是有效的移动
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="userId"></param>
        /// <param name="to"></param>
        /// <param name="steps"></param>
        /// <param name="maxiumTransfer"></param>
        /// <returns></returns>
        public bool IsValidMove(Graph graph, int userId, int to, int steps, int maxiumTransfer = int.MaxValue);
    }
}
