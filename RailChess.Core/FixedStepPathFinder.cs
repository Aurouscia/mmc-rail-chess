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
            var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");
            var startStas = startPoint.Neighbors.ConvertAll(x => new LinedSta(x.LineId, startPoint));
            //路径均有首个点（出发点）
            startStas.ConvertAll(x => new LinedPath(x)).ForEach(paths.Enqueue);

            List<LinedPath>? stepsAArchive = null;
            //如果有两种情况，则存储记录步数为A的路径中间产物
            if (stepsA != stepsAB)
                stepsAArchive = [];

            while (true)
            {
                var p = paths.Dequeue();
                var tail = p.Tail;
                if (tail is null) continue;
                foreach (var n in tail.Station.Neighbors)
                {
                    if (n.Station.Owner != 0 && n.Station.Owner != userId) continue;//不是自己的/空的就不能往这走
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
                    if(stepsAArchive is { } && newPath.Count == stepsA + 1)
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

        private static bool DuplicatePathRange(List<LinedSta> currentPath, LinedSta newPoint)
        {
            for(int i=0;i<currentPath.Count-1;i++)
            {
                var a = currentPath[i];
                var b = currentPath[i+1];
                if (a.Equals(currentPath[^1]) && b.Equals(newPoint))
                    return true; 
            }
            return false;
        }
    }
    public class LinedPath
    {
        public List<LinedSta> Stations { get; }
        public int TransferredTimes { get; private set; }
        public LinedSta? Tail { get; private set; }
        public int Count => Stations.Count;
        public LinedPath()
        {
            Stations = new();
            Tail = null;
        }
        public LinedPath(LinedSta head)
        {
            Stations = new() { head };
            Tail = head;
        }
        public LinedPath(LinedPath basedOn, LinedSta newSta)
        {
            Stations = new(basedOn.Stations);
            TransferredTimes = basedOn.TransferredTimes;
            Grow(newSta);
        }

        public void Grow(LinedSta sta)
        {
            if (Stations.Count >= 1)
            {
                if (sta.LineId != Stations[^1].LineId)
                    TransferredTimes += 1;
            }
            Stations.Add(sta);
            Tail = sta;
        }

        public IEnumerable<int> ToIds()
        {
            return Stations.ConvertAll(x=>x.Station.Id);
        }
    }
}
