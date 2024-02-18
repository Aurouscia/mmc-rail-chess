using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;
using System;

namespace RailChess.Core
{
    public class ExclusiveStasFinder : IExclusiveStasFinder
    {
        public IEnumerable<int> FindExclusiveStas(Graph graph, int userId)
        {
            var allStas = graph.Stations.ConvertAll(x => x.Id);
            HashSet<int> otherReachable = new();
            foreach(var p in graph.UserPosition)
            {
                var user = p.Key;
                var from = p.Value;
                if (user == userId) continue;
                List<Sta> active = new();
                List<List<Sta>> paths = new();
                
                var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");
                active.Add(startPoint);
                paths.Add(new() { startPoint });
                otherReachable.Add(startPoint.Id);
                var step = 0;
                while (true)
                {
                    int originalCount = otherReachable.Count;

                    foreach (var a in active)
                    {
                        var aPath = paths.Find(x => x.Count >= 1 && x.Last().Id == a.Id);
                        if (aPath is null) continue;
                        foreach (var n in a.Neighbors)
                        {
                            if (n.Owner != 0 && n.Owner != user) continue;//不是自己的/空的就不能往这走
                            if (aPath.Count >= 2)
                            {
                                var lastButOne = aPath[^2];
                                if (lastButOne.Id == n.Id) continue;
                            }
                            otherReachable.Add(n.Id);
                            List<Sta> newPath = new(aPath) { n };
                            paths.Add(newPath);
                        }
                    }
                    step++;
                    paths.RemoveAll(x => x.Count != step + 1);
                    if (paths.Count == 0) break;
                    paths = paths.DistinctBy(x => x.Last().Id).ToList();//去除当前终点相同的路径
                    active = paths.ConvertAll(x => x.Last());

                    if (originalCount == otherReachable.Count)
                        break;
                }
            }
            allStas.RemoveAll(otherReachable.Contains);
            return allStas;
        }
    }
}
