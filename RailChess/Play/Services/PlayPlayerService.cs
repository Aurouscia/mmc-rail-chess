using Microsoft.Extensions.Caching.Memory;
using RailChess.Models;
using RailChess.Models.DbCtx;

namespace RailChess.Play.Services
{
    public class PlayPlayerService
    {
        private readonly PlayEventsService _eventsService;
        private readonly RailChessContext _context;
        private readonly IMemoryCache _cache;

        public PlayPlayerService(PlayEventsService eventsService, RailChessContext context, IMemoryCache cache)
        {
            _eventsService = eventsService;
            _context = context;
            _cache = cache;
        }

        public static string UserIdCacheKey(int id)
        {
            return $"user_{id}";
        }
        public static void ClearCache(IMemoryCache cacheInstance, int id)
        {
            cacheInstance.Remove(UserIdCacheKey(id));
        }
        private readonly static Lock playerGetLock = new();
        public List<User> Get(List<int> ids)
        {
            lock (playerGetLock)
            {
                List<int> notFound = new();
                List<User> res = new();
                ids.ForEach(id =>
                {
                    var u = _cache.Get<User>(UserIdCacheKey(id));
                    if (u is null)
                        notFound.Add(id);
                    else
                        res.Add(u);
                });
                if (notFound.Count == 0)
                    return res;

                var notFoundUs = _context.Users.Where(x => notFound.Contains(x.Id)).ToList();
                notFoundUs.ForEach(u =>
                {
                    _cache.Set<User>(UserIdCacheKey(u.Id), u, new MemoryCacheEntryOptions()
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(30)
                    });
                });
                res.AddRange(notFoundUs);

                res.Sort((x, y) =>
                {
                    int idx1 = ids.IndexOf(x.Id);
                    int idx2 = ids.IndexOf(y.Id);
                    return idx1 - idx2;
                });
                return res;
            }
        }
        public User Get(int id)
        {
            return Get(new List<int> { id }).FirstOrDefault() ?? new User()
            {
                Id = 0,
                Name = "游客"
            };
        }
        private List<User> GetOrdered(List<int> ids, int lastPlayer = -1)
        {
            var users = Get(ids);
            if (lastPlayer != -1)
            {
                int idx = users.FindIndex(x => x.Id == lastPlayer);
                if (idx != ids.Count - 1)
                {
                    var slice = users.GetRange(0, idx + 1);
                    users.RemoveRange(0, idx + 1);
                    users.AddRange(slice);
                }
            }
            var outPlayers = _eventsService.PlayerOutEvents().ConvertAll(x=>x.PlayerId).Distinct().ToList();
            if (outPlayers.Count == ids.Count || outPlayers.Count==0)
                return users;
            int firstNotOut = users.FindIndex(x=>!outPlayers.Contains(x.Id));
            if(firstNotOut == -1)return users;
            var slice2 = users.GetRange(0, firstNotOut);
            users.RemoveRange(0, firstNotOut);
            users.AddRange(slice2);

            return users;
        }
        public List<User> GetOrdered()
        {
            var latestOp = _eventsService.LatestOperation();
            int lastPlayer = -1;//默认情况下：还没有任何操作
            if (latestOp is not null)
                lastPlayer = latestOp.PlayerId;
            var playerIds = _eventsService.PlayersJoinEvents().ConvertAll(x => x.PlayerId);//已经按加入顺序排列好
            var players = GetOrdered(playerIds, lastPlayer);
            return players;
        }
        public int CurrentPlayer()
        {
            var list = GetOrdered();
            if (list.Count == 0)
                throw new Exception("无玩家加入");
            return list[0].Id;
        }
        
        private string ConnUserCacheKey(string connId)
        {
            return $"connId_{connId}";
        }
        public void InsertByConn(string connectionId, int userId, int gameId)
        {
            var u = Get(userId);
            var info = new ConnPlayerInfo() {
                GameId = gameId,
                UserId = userId,
                UserName = u.Name
            };
            _cache.Set<ConnPlayerInfo>(ConnUserCacheKey(connectionId), info, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromMinutes(33)
            });
        }
        public ConnPlayerInfo? GetByConn(string connectionId)
        {
            var info = _cache.Get<ConnPlayerInfo>(ConnUserCacheKey(connectionId));
            return info;
        }
    }

    public class ConnPlayerInfo
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int GameId { get; set; }
    }
}
