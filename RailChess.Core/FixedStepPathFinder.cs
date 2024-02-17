using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core
{
    public class FixedStepPathFinder : IFixedStepPathFinder
    {
        public IEnumerable<IEnumerable<int>> FindAllPaths(Graph graph, int userId, int from, int steps)
        {
            List<Sta> active = new();
            List<List<Sta>> paths = new();
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
                        if (n.Owner != userId) continue;
                        if (aPath.Count >= 2)
                        {
                            var lastButOne = aPath[^2];
                            if (lastButOne.Id == n.Id) continue;
                        }
                        List<Sta> newPath = new(aPath){ n };
                        paths.Add(newPath);
                    }
                }
                step++;
                paths.RemoveAll(x => x.Count != step + 1);
                if (paths.Count == 0) break;
                paths = paths.DistinctBy(x => x.Last().Id).ToList();
                active = paths.ConvertAll(x => x.Last());
            }

            return paths.ConvertAll(x=>x.ConvertAll(s=>s.Id));
        }

        public bool IsValidMove(Graph graph, int userId, int from, int to, int steps)
        {
            throw new NotImplementedException();
        }
    }
}
