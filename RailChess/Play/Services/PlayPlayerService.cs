using Microsoft.Extensions.Caching.Memory;
using RailChess.Models;
using RailChess.Models.DbCtx;

namespace RailChess.Play.Services
{
    public class PlayPlayerService
    {
        private readonly RailChessContext _context;
        private readonly IMemoryCache _cache;

        public PlayPlayerService(RailChessContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private static string UserIdCacheKey(int id)
        {
            return $"user_{id}";
        }
        public List<User> Get(List<int> ids)
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
                    SlidingExpiration = TimeSpan.FromMinutes(35)
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
        public List<User> GetOrdered(List<int> ids, int lastPlayer = -1)
        {
            var users = Get(ids);
            if (lastPlayer == -1)
                return users;
            int idx = users.FindIndex(x=>x.Id==lastPlayer);
            if (idx == ids.Count - 1)
                return users;
            var slice = users.GetRange(0, idx + 1);
            users.RemoveRange(0, idx + 1);
            users.AddRange(slice);
            return users;
        }
        
        private string ConnUserCacheKey(string connId)
        {
            return $"connId_{connId}";
        }
        public void InsertByConn(string connectionId, int userId, int gameId)
        {
            var u = Get(new() { userId }).FirstOrDefault() ?? throw new Exception("找不到指定用户");
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
