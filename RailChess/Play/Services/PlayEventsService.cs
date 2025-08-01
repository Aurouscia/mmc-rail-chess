using Microsoft.Extensions.Caching.Memory;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Utils;

namespace RailChess.Play.Services
{
    public class PlayEventsService
    {
        private readonly PlayGameService _gameService;
        private readonly RailChessContext _context;
        private readonly IMemoryCache _cache;
        public int GameId { get; set; }
        public int UserId { get; set; }
        public int TFilterId { get; set; }
        public bool TFiltered => TFilterId > 0;
        public PlayEventsService(PlayGameService gameService, RailChessContext context, IMemoryCache cache)
        {
            _gameService = gameService;
            _context = context;
            _cache = cache;
        }
        private string EventsCacheKey() => $"rces_{GameId}";
        private static readonly Lock ourEventsLock = new();
        public List<RailChessEvent> OurEvents()
        {
            lock (ourEventsLock)
            {
                string key = EventsCacheKey();
                var list = _cache.Get<List<RailChessEvent>>(key);
                bool needRefetch = false;
                if (list is not null)
                {
                    //由于“缓存非最新”的问题无法解决，每次在这里检查是否最新
                    int maxIdInDb = _context.Events.OfGame(GameId)
                        .OrderByDescending(x => x.Id)
                        .Select(x => x.Id).FirstOrDefault();
                    int maxIdInCache = list.LastOrDefault()?.Id ?? 0;
                    if (maxIdInDb != maxIdInCache)
                        needRefetch = true;
                }
                if (list is null || needRefetch)
                {
                    list = _context.Events.OfGame(GameId).OrderBy(x => x.Id).ToList();
                    _cache.Set(key, list, new MemoryCacheEntryOptions()
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(30)
                    });
                }
                if (TFiltered)
                {
                    var firstExceed = list.FindIndex(x => x.Id >= TFilterId);
                    list = list.GetRange(0, firstExceed);
                }
                return list;
            }
        }
        public void ClearOurEventsCache()
        {
            lock (ourEventsLock)
            {
                _cache.Remove(EventsCacheKey());
            }
        }
        public List<RailChessEvent> MyEvents() 
            => OurEvents().FindAll(x => x.PlayerId == UserId);
        public bool MeJoined() 
            => MyEvents().Any(x => x.EventType == RailChessEventType.PlayerJoin);
        public bool SomeoneJoined(int userId)
            => OurEvents().Any(x => x.PlayerId==userId && x.EventType == RailChessEventType.PlayerJoin);
        public List<RailChessEvent> PlayersJoinEvents()
            => OurEvents().FindAll(x => x.EventType == RailChessEventType.PlayerJoin);
        public bool GameStarted()
            => OurEvents().Any(x => x.EventType == RailChessEventType.GameStart);
        public List<RailChessEvent> PlayerLocateEvents()
        {
            var events = OurEvents().FindAll(x =>
                x.EventType == RailChessEventType.PlayerMoveTo
                || x.EventType == RailChessEventType.PlayerJoin);
            return events.OrderByDescending(x=>x.Id).DistinctBy(x => x.PlayerId).ToList();
        }
        public List<RailChessEvent> PlayerCaptureEvents()
            => OurEvents().FindAll(x => x.EventType == RailChessEventType.PlayerCapture);
        public List<RailChessEvent> PlayerStuckEvents()
            => OurEvents().FindAll(x => x.EventType == RailChessEventType.PlayerStuck);
        public List<RailChessEvent> PlayerOutEvents()
            => OurEvents().FindAll(x=>x.EventType == RailChessEventType.PlayerOut);
        public bool GamedEnded()
            => OurEvents().Any(x => x.EventType == RailChessEventType.GameEnd);
        public RailChessEvent? LatestOperation()
        {
            var operations = OurEvents()
                .FindAll(x => x.EventType == RailChessEventType.PlayerMoveTo || x.EventType == RailChessEventType.PlayerStuck);
            return operations.LastOrDefault();
        }
        public int RandedResult()
        {
            int rand;
            var operations = OurEvents()
                .FindAll(x =>
                    x.EventType == RailChessEventType.PlayerStuck
                    || x.EventType == RailChessEventType.PlayerMoveTo
                    || x.EventType == RailChessEventType.RandNumGened);
            var last = operations.LastOrDefault();
            if (last is null || last.EventType != RailChessEventType.RandNumGened)
            {
                var ourGame = _gameService.OurGame();
                rand = RandNum.Run(ourGame.RandAlg, ourGame.RandMin, ourGame.RandMax);
                Add(RailChessEventType.RandNumGened, rand, true);
            }
            else 
                rand = last.StationId;
            return rand;
        }
        public int RandedResultOnlyGet()
        {
            return OurEvents().Where(x => x.EventType == RailChessEventType.RandNumGened).LastOrDefault()?.StationId ?? 0;
        }

        public void Add(RailChessEventType type, int stationId, int userId, bool saveChanges = true)
        {
            RailChessEvent ev = new()
            {
                EventType = type,
                GameId = this.GameId,
                PlayerId = userId,
                StationId = stationId,
                Time = DateTime.Now
            };
            _context.Events.Add(ev);
            if (saveChanges)
                _context.SaveChanges();
            ClearOurEventsCache();
        }

        public void Add(RailChessEventType type, int stationId, bool saveChanges = true)
        {
            Add(type, stationId, this.UserId, saveChanges);
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
