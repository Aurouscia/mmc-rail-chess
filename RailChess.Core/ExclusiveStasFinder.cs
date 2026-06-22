using RailChess.Core.Abstractions;
using RailChess.GraphDefinition;

namespace RailChess.Core
{
    public class ExclusiveStasFinder : IExclusiveStasFinder
    {
        public List<int> FindExclusiveStas(Graph graph, int userId, ExclusiveStasOptions? options = null)
        {
            var allStas = graph.Stations;
            var teammateMap = BuildTeammateMap(options?.Teams);
            if (!teammateMap.TryGetValue(userId, out var myTeammates))
                myTeammates = new HashSet<int>();
            var userOwnedStations = allStas.Where(x => x.Owner == userId).Select(x => x.Id).ToHashSet();
            var teammateOwnedStations = allStas.Where(x => myTeammates.Contains(x.Owner)).Select(x => x.Id).ToHashSet();
            var othersReachable = new HashSet<int>();
            var limit = DateTime.Now.AddSeconds(3);
            foreach(var p in graph.UserPosition)
            {
                var he = p.Key;
                var from = p.Value;
                if (he == userId || myTeammates.Contains(he)) continue;
                if (!teammateMap.TryGetValue(he, out var hisTeammates))
                    hisTeammates = new HashSet<int>();
                HashSet<int> capturedByOthers = new(allStas.Where(x => x.Owner != 0 && x.Owner != he && !hisTeammates.Contains(x.Owner)).Select(x => x.Id));
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
                            continue;//不能往非队友的人占了的点走
                        if (!hisReachable.Add(n.Station.Id))
                            continue;//不能往回走
                        active.Enqueue(n.Station);
                    }
                    if (active.Count == 0) break;
                }

                foreach(var r in hisReachable)
                {
                    othersReachable.Add(r);
                }

                // 优化：若所有未被他到达的站都已被当前玩家或队友占领，则不会产生新的自动占领
                if (othersReachable.Count + userOwnedStations.Count + teammateOwnedStations.Count >= allStas.Count)
                    return userOwnedStations.ToList();
            }
            return allStas.ConvertAll(x=>x.Id).Except(othersReachable).Except(teammateOwnedStations).ToList();
        }

        /// <summary>
        /// 预计算每个玩家的队友集合（不含自己），支持一个玩家同时属于多个队伍
        /// </summary>
        private static Dictionary<int, HashSet<int>> BuildTeammateMap(List<List<int>>? teams)
        {
            var dict = new Dictionary<int, HashSet<int>>();
            if (teams is null) return dict;
            foreach (var team in teams)
            {
                foreach (var player in team)
                {
                    if (!dict.TryGetValue(player, out var set))
                    {
                        set = new HashSet<int>();
                        dict[player] = set;
                    }
                    foreach (var other in team)
                    {
                        if (other != player)
                            set.Add(other);
                    }
                }
            }
            return dict;
        }

        public static bool DisableTimeoutTestOnly { get; set; } = false;
    }
}
