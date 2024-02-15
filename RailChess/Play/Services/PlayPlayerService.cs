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

        private static string CacheKey(List<int> ids)
        {
            ids.Sort();
            return $"users_{string.Concat(ids)}";
        }
        public List<User> Get(List<int> ids)
        {
            var res = _cache.Get<List<User>>(CacheKey(ids));
            if(res is null)
            {
                res = _context.Users.Where(x=>ids.Contains(x.Id)).ToList();
                res.Sort((x, y) =>
                {
                    int idx1 = ids.IndexOf(x.Id);
                    int idx2 = ids.IndexOf(y.Id);
                    return idx1 - idx2;
                });
                _cache.Set<List<User>>(CacheKey(ids), res, new MemoryCacheEntryOptions()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(35)
                });
            }
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
        
    }
}
