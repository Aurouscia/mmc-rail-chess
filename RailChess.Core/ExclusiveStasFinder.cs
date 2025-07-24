using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;
using System;

namespace RailChess.Core
{
    public class ExclusiveStasFinder : IExclusiveStasFinder
    {
        public IEnumerable<int> FindExclusiveStas(Graph graph, int userId)
        {
            var allStas = graph.Stations;
            var othersReachable = new HashSet<int>();
            var limit = DateTime.Now.AddSeconds(3);
            foreach(var p in graph.UserPosition)
            {
                var he = p.Key;
                var from = p.Value;
                if (he == userId) continue;
                HashSet<int> capturedByOthers = new(allStas.Where(x => x.Owner != 0 && x.Owner != he).Select(x => x.Id));
                HashSet<int> hisReachable = new();
                Queue<Sta> active = new();
                var startPoint = graph.Stations.Find(x => x.Id == from) ?? throw new Exception("算路异常:找不到指定起始点");

                active.Enqueue(startPoint);
                hisReachable.Add(startPoint.Id);

                while (true)
                {
                    if (!DisableTimeoutTestOnly)
                    {
                        if (DateTime.Now > limit)
                            throw new Exception("计算超时，请联系管理员");
                    }
                    var a = active.Dequeue();
                    foreach (var n in a.Neighbors)
                    {
                        if (capturedByOthers.Contains(n.Station.Id))
                            continue;//不能往其他人占了的点走
                        if (hisReachable.Contains(n.Station.Id))
                            continue;//不能往回走
                        hisReachable.Add(n.Station.Id);
                        active.Enqueue(n.Station);
                    }
                    if (active.Count == 0) break;
                }

                foreach(var r in hisReachable)
                {
                    othersReachable.Add(r);
                }
            }
            return allStas.ConvertAll(x=>x.Id).Except(othersReachable);
        }

        public static bool DisableTimeoutTestOnly { get; set; } = false;
    }
}
