using Microsoft.Extensions.Caching.Memory;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;

namespace RailChess.Play.Services
{
    public class PlayEventsService
    {
        private readonly RailChessContext _context;
        private readonly IMemoryCache _cache;
        public int GameId { get; set; }
        public int UserId { get; set; }
        public PlayEventsService(RailChessContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        private string EventsCacheKey() => $"rces_{GameId}";
        public List<RailChessEvent> OurEvents()
        {
            string key = EventsCacheKey();
            var list = _cache.Get<List<RailChessEvent>>(key);
            if (list is null)
            {
                list = _context.Events.OfGame(GameId).ToList();
                _cache.Set(key, list, new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            }
            return list;
        }
        public List<RailChessEvent> MyEvents() 
            => OurEvents().FindAll(x => x.PlayerId == UserId);
        public bool MeJoined() 
            => MyEvents().Any(x => x.EventType == RailChessEventType.PlayerJoin);
        public List<RailChessEvent> PlayersJoinEvents()
            => OurEvents().FindAll(x => x.EventType == RailChessEventType.PlayerJoin);
        public bool GameStarted()
            => OurEvents().Any(x => x.EventType == RailChessEventType.GameStart);

        public void Add(RailChessEventType type, int stationId, bool saveChanges=true)
        {
            RailChessEvent ev = new()
            {
                EventType = type,
                GameId = this.GameId,
                PlayerId = this.UserId,
                StationId = stationId,
                Time = DateTime.Now
            };
            _context.Events.Add(ev);
            if(saveChanges)
                _context.SaveChanges();
            var list = OurEvents();
            list.Add(ev);
        }
    }

    public static class EventQueryableExtension
    {
        public static IQueryable<RailChessEvent> OfGame(this IQueryable<RailChessEvent> events, int gameId)
        {
            return events.Where(e => e.GameId == gameId);
        }
        public static IQueryable<RailChessEvent> OfUser(this IQueryable<RailChessEvent> events, int userId)
        {
            return events.Where(e => e.PlayerId == userId);
        }
        public static List<RailChessEvent> OfUser(this List<RailChessEvent> events, int userId)
        {
            return events.FindAll(e => e.PlayerId == userId);
        }
    }

}
