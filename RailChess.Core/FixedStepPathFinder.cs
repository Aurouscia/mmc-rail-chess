using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core
{
    public class FixedStepPathFinder : IFixedStepPathFinder
    {
        public IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int steps)
        {
            List<Sta> active = new();
            List<List<Sta>> paths = new();
            int from = graph.UserPosition[userId];
            var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");
            active.Add(startPoint);

            if (steps == 0)
                return new List<List<int>>();

            paths.Add(new() { startPoint });

            var step = 0;
            while (step<steps)
            {
                foreach(var a in active)
                {
                    var aPath = paths.Find(x => x.Count >= 1 && x.Last().Id == a.Id);
                    if (aPath is null) continue;
                    foreach(var n in a.Neighbors)
                    {
                        if (n.Owner!=0 && n.Owner != userId) continue;//不是自己的/空的就不能往这走
                        if (aPath.Count >= 2)
                        {
                            var lastButOne = aPath[^2];
                            if (lastButOne.Id == n.Id) continue;
                        }
                        if (DuplicatePathRange(aPath, n))
                            continue;//不准走已经走过的区间
                        List<Sta> newPath = new(aPath){ n };
                        paths.Add(newPath);
                    }
                }
                step++;
                paths.RemoveAll(x => x.Count != step + 1);
                if (paths.Count == 0) break;
                paths = paths.DistinctBy(x => x.Last().Id).ToList();//去除当前终点相同的路径
                active = paths.ConvertAll(x => x.Last());
            }

            return paths.ConvertAll(x=>x.ConvertAll(s=>s.Id));
        }

        public bool IsValidMove(Graph graph, int userId, int from, int to, int steps)
        {
            var paths = FindAllPaths(graph, userId, steps);
            return paths.Any(x => x.FirstOrDefault() == from && x.LastOrDefault() == to);
        }

        private bool DuplicatePathRange(List<Sta> currentPath, Sta newPoint)
        {
            for(int i=0;i<currentPath.Count-1;i++)
            {
                int a = currentPath[i].Id;
                int b = currentPath[i+1].Id;
                if (a == currentPath[^1].Id && b == newPoint.Id)
                    return true;
            }
            return false;
        }
    }
}
