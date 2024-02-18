using Microsoft.Extensions.Caching.Memory;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;

namespace RailChess.Play.Services
{
    public class PlayGameService
    {
        private readonly RailChessContext _context;
        private readonly IMemoryCache _cache;
        public int GameId { get; set; }    
        public PlayGameService(RailChessContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        private string CacheKey()
        {
            return $"gameMeta_{GameId}";
        }
        public RailChessGame OurGame() 
        {
            RailChessGame? game = _cache.Get<RailChessGame>(CacheKey());
            if(game is null)
            {
                game = _context.Games.Where(x => x.Id == GameId).FirstOrDefault();
                if (game is null)
                    throw new Exception("找不到指定棋局");
                _cache.Set(CacheKey(), game);
            }
            return game;
        }
    }
}
