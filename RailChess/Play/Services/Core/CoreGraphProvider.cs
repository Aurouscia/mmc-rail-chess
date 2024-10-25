using Microsoft.Extensions.Caching.Memory;
using RailChess.GraphDefinition;

namespace RailChess.Play.Services.Core
{
    public class CoreGraphProvider
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly CoreGraphConverter _converter;
        private readonly CoreGraphEvaluator _evaluator;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CoreGraphProvider> _logger;

        public CoreGraphProvider(
            PlayEventsService eventsService, PlayToposService toposService,
            CoreGraphConverter converter, CoreGraphEvaluator evaluator,
            IMemoryCache cache, ILogger<CoreGraphProvider> logger) 
        {
            _eventsService = eventsService;
            _topoService = toposService;
            _converter = converter;
            _evaluator = evaluator;
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
            var outPlayerIds = _eventsService.PlayerOutEvents().ConvertAll(x=>x.PlayerId);
            locEvents.ForEach(x =>
            {
                if (!outPlayerIds.Contains(x.PlayerId))
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
            var graph = _converter.Convert(topo) ?? throw new Exception("地图数据异常(无法构建图)");
            return graph;
        }

        public Dictionary<int,int> StationDirections()
        {
            var graph = GetPlainGraph();
            return _evaluator.StationDirections(graph);
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
