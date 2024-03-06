using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RailChess.Models.DbCtx;
using RailChess.Play;
using RailChess.Play.Services;

namespace RailChess.Controllers
{
    public class GameResultController:Controller
    {
        private readonly RailChessContext _context;
        private readonly PlayService _playService;
        private readonly IMemoryCache _cache;

        public GameResultController(RailChessContext context, PlayService playService, IMemoryCache cache) 
        {
            _context = context;
            _playService = playService;
            _cache = cache;
        }

        public IActionResult UserGameLog(int userId)
        {
            var data = (
                from r in _context.GameResults
                from g in _context.Games
                where r.UserId == userId
                where r.GameId == g.Id
                orderby g.StartTime
                select new { r.Rank, r.GameId, g.StartTime, r.EloDelta }).ToList();
            var userName = _context.Users.Where(x => x.Id == userId).Select(x => x.Name).FirstOrDefault();
            var res = new UserGameLogResult(userName ?? "");
            data.ForEach(d =>
            {
                res.Logs.Add(new(d.Rank, d.GameId, d.StartTime, d.EloDelta));
            });
            return this.ApiResp(res);
        }

        [Route("/rgr")]
        public IActionResult RecalculateResults()
        {
            _context.GameResults.Where(x => x.Id > 0).ExecuteDelete();
            _context.Users.ExecuteUpdate(x => x.SetProperty(u => u.Elo, 0));
            var userIds = _context.Users.Select(x => x.Id).ToList();

            userIds.ForEach(i => PlayPlayerService.ClearCache(_cache, i));

            List<int> endedGames = _context.Games.Where(x => x.Ended == true).Select(x => x.Id).ToList();
            endedGames.ForEach(id =>
            {
                _playService.GameId = id;
                _playService.GetSyncData(true);
            });

            userIds.ForEach(i => PlayPlayerService.ClearCache(_cache, i));
            return this.ApiResp();
        }

        public class UserGameLogResult
        {
            public List<UserGameLogResultItem> Logs { get; set; }
            public string UserName { get; set; }
            public UserGameLogResult(string userName)
            {
                Logs = new();
                UserName = userName;
            }
            public class UserGameLogResultItem
            {
                public UserGameLogResultItem(int rank, int gameId, DateTime startTime, int eloDelta)
                {
                    Rank = rank;
                    GameId = gameId;
                    StartTime = startTime.ToString("yy/MM/dd HH:mm");
                    EloDelta = eloDelta;
                }
                public int Rank { get; set; }
                public int GameId { get; set; }
                public string StartTime { get; set; }
                public int EloDelta { get; set; }
            }
        }
    }
}
