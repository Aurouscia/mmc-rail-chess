using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RailChess.Models.DbCtx;
using RailChess.Models.Map;

namespace RailChess.Play.Services
{
    public class PlayToposService
    {
        private readonly RailChessContext _context;
        private readonly IMemoryCache _cache;
        public PlayToposService(RailChessContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        private string MapCacheKey(int gameId)
        {
            return $"topoOfGame_{gameId}";
        }
        public RailChessTopo TopoOf(int gameId)
        {
            string key = MapCacheKey(gameId);
            var topo = _cache.Get<RailChessTopo>(key);
            if (topo is null)
            {
                var map = (from m in _context.Maps
                          from g in _context.Games
                          where g.Id == gameId
                          where m.Id == g.UseMapId
                          select m).FirstOrDefault() ?? throw new Exception("找不到指定棋局或棋盘");
                var json = map.TopoData ?? throw new Exception("棋盘内无数据");
                topo = JsonConvert.DeserializeObject<RailChessTopo>(json) ?? throw new Exception("棋盘数据解析异常");
                _cache.Set(key, topo, new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }
            return topo;
        }

        public List<int> PureTerminalIds(int gameId)
        {
            var topo = TopoOf(gameId);
            if (topo.Lines is null) throw new Exception("线路数量为0，无法进行游戏");
            var blackList = new List<int>();
            var terminals = new List<int>();
            topo.Lines.ForEach(x =>
            {
                if (x.Stas is not null)
                {
                    bool isLoop = x.Stas.Count>2 && x.Stas[0] == x.Stas[^1];
                    for(int i = 0; i < x.Stas.Count; i++)
                    {
                        if (isLoop || (i != 0 && i != x.Stas.Count - 1))
                            blackList.Add(x.Stas[i]);
                        else
                            terminals.Add(x.Stas[i]);
                    }
                }
            });
            terminals.RemoveAll(blackList.Contains);
            return terminals;
        }
    }
}
