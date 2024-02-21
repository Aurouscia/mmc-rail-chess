using Microsoft.Extensions.Caching.Memory;
using RailChess.GraphDefinition;

namespace RailChess.Play.Services.Core
{
    public class CoreGraphProvider
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CoreGraphProvider> _logger;

        public CoreGraphProvider(PlayEventsService eventsService, PlayToposService toposService, IMemoryCache cache, ILogger<CoreGraphProvider> logger) 
        {
            _eventsService = eventsService;
            _topoService = toposService;
            _cache = cache;
            _logger = logger;
        }
        private string PlainGraphCacheKey => $"plainGraphOfGame_{_topoService.GameId}";

        public Graph GetGraph()
        {
            var graph = GetPlainGraph();
            var ocps = _eventsService.PlayerCaptureEvents().ConvertAll(x =>new { x.PlayerId, x.StationId });
            ocps.ForEach(o =>
            {
                var sta = graph.Stations.Find(s => s.Id == o.StationId);
                if (sta is not null)
                    sta.Owner = o.PlayerId;
            });
            var locEvents = _eventsService.PlayerLocateEvents();
            locEvents.ForEach(x =>
            {
                graph.UserPosition.Add(x.PlayerId, x.StationId);
            });
            return graph;
        }
        private Graph GetPlainGraph()
        {
            var plainGraph = _cache.Get<Graph>(PlainGraphCacheKey);
            if(plainGraph is null) 
            {
                plainGraph = BuildPlainGraph();
                _cache.Set(PlainGraphCacheKey, plainGraph, new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }
            else
                _logger.LogDebug("游戏[{gameId}]_从缓存取出核心图", _topoService.GameId);
            plainGraph.UserPosition.Clear();
            plainGraph.Stations.ForEach(x => x.Owner = 0);
            return plainGraph;
        }
        private Graph BuildPlainGraph()
        {
            _logger.LogDebug("游戏[{gameId}]_构建核心图", _topoService.GameId);
            var topo = _topoService.OurTopo();
            if (topo.Lines is null || topo.Stations is null) throw new Exception("地图数据异常(无法构建图)");

            List<Sta> ss = topo.Stations.ConvertAll(x => new Sta(x.Id));
            topo.Lines.ForEach(line =>
            {
                if (line.Stas is not null && line.Stas.Count > 1)
                {
                    for (int i = 0; i < line.Stas.Count; i++)
                    {
                        int staId = line.Stas[i];
                        var target = ss.Find(x => x.Id == staId);
                        if (target is null) continue;
                        List<int> neighborHere = new(2);
                        if (i == 0)
                            neighborHere.Add(line.Stas[1]);//至少有两个站才会进来，1肯定有东西
                        else if (i == line.Stas.Count - 1)
                            neighborHere.Add(line.Stas[^2]);
                        else
                        {
                            neighborHere.Add(line.Stas[i - 1]);
                            neighborHere.Add(line.Stas[i + 1]);
                        }
                        
                        neighborHere.ForEach(n =>
                        {
                            var ns = ss.Find(s => s.Id == n);
                            if (ns is not null)
                            {
                                target.Neighbors.Add(new(line.Id, ns));
                            }
                        });
                    }
                }
            });
            return new Graph(ss);
        }

        public Dictionary<int,int> StationDirections()
        {
            var graph = GetPlainGraph();
            Dictionary<int, int> dict = new();
            graph.Stations.ForEach(x =>
            {
                var count = x.Neighbors.Select(x => x.Station).Distinct().Count();
                dict.Add(x.Id, count);
            });
            return dict;
        }
        public int TotalDirections(List<int> staIds)
        {
            var dirDict = StationDirections();
            return TotalDirections(staIds, dirDict);
        }
        public int TotalDirections(List<int> staIds, Dictionary<int,int> dirDict)
        {
            int sum = 0;
            staIds.ForEach(x =>
            {
                if (dirDict.TryGetValue(x, out int value))
                {
                    sum += value;
                }
            });
            return sum;
        }
        public List<int> PureTerminals()
        {
            var dirDict = StationDirections();
            var res = dirDict.Where(x=>x.Value==1).Select(x=>x.Key).ToList();
            return res;
        }
    }
}
