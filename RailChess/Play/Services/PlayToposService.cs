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
        public int GameId { get; set; }
        public PlayToposService(RailChessContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        private string TopoCacheKey() => $"topoOfGame_{GameId}";
        private static readonly Lock ourTopoLock = new();
        public RailChessTopo OurTopo()
        {
            lock (ourTopoLock)
            {
                string key = TopoCacheKey();
                var topo = _cache.Get<RailChessTopo>(key);
                if (topo is null)
                {
                    var map = (from m in _context.Maps
                               from g in _context.Games
                               where g.Id == GameId
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
        }
    }
}
