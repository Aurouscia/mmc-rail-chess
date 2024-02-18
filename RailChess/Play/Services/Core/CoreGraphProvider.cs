using Microsoft.Extensions.Caching.Memory;
using RailChess.GraphDefinition;

namespace RailChess.Play.Services.Core
{
    public class CoreGraphProvider
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly IMemoryCache _cache;

        public CoreGraphProvider(PlayEventsService eventsService, PlayToposService toposService, IMemoryCache cache) 
        {
            _eventsService = eventsService;
            _topoService = toposService;
            _cache = cache;
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
            _eventsService.PlayerLocateEvents().ForEach(x =>
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
            return plainGraph;
        }
        private Graph BuildPlainGraph()
        {
            var topo = _topoService.OurTopo();
            if (topo.Lines is null || topo.Stations is null) throw new Exception("地图数据异常(无法构建图)");

            List<Sta> ss = topo.Stations.ConvertAll(x => new Sta(x.Id));
            topo.Lines.ForEach(x =>
            {
                if (x.Stas is not null && x.Stas.Count > 1)
                {
                    for (int i = 0; i < x.Stas.Count; i++)
                    {
                        int staId = x.Stas[i];
                        var target = ss.Find(x => x.Id == staId);
                        if (target is null) continue;
                        List<int> neighborHere = new(2);
                        if (i == 0)
                            neighborHere.Add(x.Stas[1]);//至少有两个站才会进来，1肯定有东西
                        else if (i == x.Stas.Count - 1)
                            neighborHere.Add(x.Stas[^2]);
                        else
                        {
                            neighborHere.Add(x.Stas[i - 1]);
                            neighborHere.Add(x.Stas[i + 1]);
                        }
                        
                        neighborHere.ForEach(n =>
                        {
                            var ns = ss.Find(s => s.Id == n);
                            if (ns is not null)
                            {
                                target.Neighbors.Add(ns);
                            }
                        });
                    }
                }
            });
            return new Graph(ss);
        }
    }
}
