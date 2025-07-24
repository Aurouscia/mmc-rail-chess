using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core
{
    public class FixedStepPathFinder : IFixedStepPathFinder
    {
        /// <inheritdoc/>
        public IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int steps, int maxiumTransfer = int.MaxValue)
        {
            if (steps < 100)
                return FindAllPaths(graph, userId, steps, 0, maxiumTransfer);
            else
            {
                //十位个位算A
                int stepsA = steps % 100;
                //千位百位算B
                int stepsB = steps / 100;
                return FindAllPaths(graph, userId, stepsA, stepsB, maxiumTransfer);
            }
        }
        private static IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int stepsA, int stepsB, int maxiumTransfer = int.MaxValue)
        {
            var limit = DateTime.Now.AddSeconds(3);
            int stepsAB = stepsA + stepsB;
            if (stepsA == 0 && stepsB == 0)
                return new List<List<int>>();

            if (!graph.UserPosition.TryGetValue(userId, out int from))
                throw new Exception("算路异常:找不到玩家位置");

            if (stepsA == -1 && stepsB == 0)
            {
                //如果步数为-1，可一步走到任何未被其他玩家占领的地方
                var mineOrEmpty = graph.Stations.Where(x =>
                    (x.Owner == 0 || x.Owner == userId) && x.Id != from);
                return mineOrEmpty
                    .Select(x => new List<int> { from, x.Id })
                    .ToList();
            }

            Queue<LinedPath> paths = new();
            bool conjudge(LinedSta s0, LinedSta s1, LinedSta? s2)
                => IsRangeConsecutiveByLine(graph, s0, s1, s2);
            var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");
            //出发：可从该站的任何线路出发，所以按每个“邻点(LinedSta)”的线路创建一个出发点
            var startStas = startPoint.Neighbors.ConvertAll(x => new LinedSta(x.LineId, startPoint));
            //路径均有首个点（出发点）
            startStas.ConvertAll(x => new LinedPath(x, conjudge)).ForEach(paths.Enqueue);

            List<LinedPath>? stepsAArchive = null;
            //如果有两种情况，则存储记录步数为A的路径中间产物
            if (stepsA != stepsAB)
                stepsAArchive = [];

            while (true)
            {
                if (!DisableTimeoutTestOnly)
                {
                    //大抵是开销极低的
                    if (DateTime.Now > limit)
                        throw new Exception("计算超时，请联系管理员");
                }
                var p = paths.Dequeue();
                var tail = p.Tail;
                if (tail is null) continue;
                foreach (var n in tail.Station.Neighbors)
                {
                    if (p.TransferredTimes == maxiumTransfer)
                        if (p.Tail?.LineId != n.LineId)
                            continue;//已经到了换乘上限，不能还往别的线跑
                    if (n.Station.Owner != 0 && n.Station.Owner != userId)
                        continue;//不是自己的/空的就不能往这走
                    if (p.Stations.Count >= 2)
                    {
                        var lastButOne = p.Stations[^2];
                        if (lastButOne.Station.Id == n.Station.Id) continue; //不能掉头往回跑
                    }
                    if (DuplicatePathRange(p.Stations, n))
                        continue;//不准走已经走过的区间（但是已经过的站还是可以再次经过的）
                    LinedPath newPath = new(p, n);
                    if (newPath.TransferredTimes > maxiumTransfer)
                        continue;//不准换乘次数超出限制
                    paths.Enqueue(newPath);
                    if (stepsAArchive is { } && newPath.Count == stepsA + 1)
                        stepsAArchive.Add(newPath);
                }
                if (paths.Count == 0) break;
                if (paths.All(x => x.Count >= stepsAB + 1)) break;
            }

            IEnumerable<LinedPath> pathsFinal = paths.AsEnumerable();
            if (stepsAArchive is { })
                pathsFinal = pathsFinal.Concat(stepsAArchive);
            return pathsFinal
                .Where(x => (x.Count == stepsA + 1) || (x.Count == stepsAB + 1))
                .DistinctBy(x => x.Tail!.Station.Id)
                .Select(x => x.ToIds())
                .ToList();
        }

        public bool IsValidMove(Graph graph, int userId, int to, int steps, int maxiumTransfer = int.MaxValue)
        {
            var paths = FindAllPaths(graph, userId, steps, maxiumTransfer);
            return paths.Any(x => x.LastOrDefault() == to);
        }

        public static bool DisableTimeoutTestOnly { get; set; } = false;

        private static bool DuplicatePathRange(List<LinedSta> currentPath, LinedSta newPoint)
        {
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                var a = currentPath[i];
                var b = currentPath[i + 1];
                if (a.Equals(currentPath[^1]) && b.Equals(newPoint))
                    return true;
            }
            return false;
        }

        private static bool IsRangeConsecutiveByLine(Graph graph,
            LinedSta newSta, LinedSta tailSta, LinedSta? beforeTailSta)
        {
            //  o-----o---->o
            //  ↑     ↑     ↑
            //  |     |     newSta（线路新延长的点）
            //  |     tailSta（线路中已有）
            //  beforeTailSta（线路中已有）
            //
            //站点的线路号变化：必然换乘了
            if (newSta.LineId != tailSta.LineId)
                return false;
            if (graph.Lines.Count == 0 || beforeTailSta is null)
            {
                //未提供线路信息（单元测试环境）或目前只有两个点
                //无法进行换乘判断，直接返回true
                return true;
            }
            //判断自交换乘
            if (graph.Lines.TryGetValue(tailSta.LineId, out var line))
            {
                var tailId = tailSta.Station.Id;
                //找出tailSta在线路中的所有索引
                var tailStaIdxs = new List<int>();
                for(int i = 0; i < line.Count; i++)
                {
                    if (line[i] == tailId)
                        tailStaIdxs.Add(i);
                }
                //如果tailSta仅出现过一次，那么这里肯定不存在自交，直接返回true
                if (tailStaIdxs.Count <= 1)
                    return true;
                var lastIdx = line.Count - 1;
                var isRing = tailStaIdxs.First() == 0 && tailStaIdxs.Last() == lastIdx;
                foreach(var i in tailStaIdxs)
                {
                    int adjIdx0 = -1;
                    int adjIdx1 = -1;
                    if(i == 0 || i == lastIdx)
                    {
                        if (isRing)
                        {
                            //是环线：至少长度为3
                            adjIdx0 = 1;
                            adjIdx1 = lastIdx - 1;
                        }
                        else
                        {
                            //不是环线，在这肯定不可能“连续区间”
                            continue;
                        }
                    }
                    else
                    {
                        adjIdx0 = i - 1;
                        adjIdx1 = i + 1;
                    }
                    int adj0 = line.ElementAt(adjIdx0);
                    int adj1 = line.ElementAt(adjIdx1);
                    if (adj0 == newSta.Station.Id && adj1 == beforeTailSta.Station.Id)
                        return true;
                    if (adj1 == newSta.Station.Id && adj0 == beforeTailSta.Station.Id)
                        return true;
                }
                return false;//没有找到连续走法
            }
            //找不到线路
            throw new Exception("算路异常：找不到线路" + tailSta.LineId);
        }

        private delegate bool ConsecutiveJudgment(
            LinedSta newSta, LinedSta tailSta, LinedSta? beforeTailSta);
        private class LinedPath
        {
            public List<LinedSta> Stations { get; }
            public int TransferredTimes { get; private set; }
            public LinedSta? Tail => Stations.LastOrDefault();
            public int Count => Stations.Count;
            public ConsecutiveJudgment ConJudge { get; } 
            public LinedPath(LinedSta head, ConsecutiveJudgment conJudge)
            {
                Stations = [ head ];
                ConJudge = conJudge;
            }
            public LinedPath(LinedPath basedOn, LinedSta newSta)
            {
                Stations = [..basedOn.Stations];
                TransferredTimes = basedOn.TransferredTimes;
                ConJudge = basedOn.ConJudge;
                Grow(newSta);
            }

            private void Grow(LinedSta sta)
            {
                if (Tail is not null)
                {
                    LinedSta? beforeTailSta = null;
                    if(Stations.Count >= 2)
                        beforeTailSta = Stations[^2];
                    if (!ConJudge(sta, Tail, beforeTailSta))
                        TransferredTimes++;
                }
                Stations.Add(sta);
            }

            public IEnumerable<int> ToIds()
            {
                return Stations.ConvertAll(x=>x.Station.Id);
            }
        }
    }
}