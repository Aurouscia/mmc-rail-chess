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

        public IActionResult OfUser(int userId)
        {
            var data = (
                from r in _context.GameResults
                from m in _context.Maps
                from g in _context.Games
                where r.UserId == userId
                where r.GameId == g.Id
                where g.UseMapId == m.Id
                select new { r.Rank, r.GameId, g.StartTime, r.EloDelta, MapName = m.Title }).ToList();
            data.Sort((x, y) => DateTime.Compare(y.StartTime, x.StartTime));
            var userName = _context.Users.Where(x => x.Id == userId).Select(x => x.Name).FirstOrDefault();
            var res = new GameResultListResponse();
            data.ForEach(d =>
            {
                res.Logs.Add(new(d.Rank, d.GameId, d.StartTime, d.EloDelta, d.MapName, userName??"??", userId));
            });
            return this.ApiResp(res);
        }
        public IActionResult OfGame(int gameId)
        {
            var data = (
                from r in _context.GameResults
                from u in _context.Users
                where r.GameId == gameId
                where r.UserId == u.Id
                select new { r.Rank, r.GameId, r.EloDelta, UserName = u.Name, UserId = u.Id}).ToList();
            var game = _context.Games.Where(x => x.Id == gameId).Select(x => new { x.StartTime, x.UseMapId }).FirstOrDefault();
            if (game is null)
                return this.ApiFailedResp("找不到指定棋局");
            var mapName = _context.Maps.Where(x=>x.Id == game.UseMapId).Select(x=>x.Title).FirstOrDefault();
            var res = new GameResultListResponse();
            data.Sort((x, y) => x.Rank - y.Rank);
            data.ForEach(d =>
            {
                res.Logs.Add(new(d.Rank, d.GameId, game.StartTime, d.EloDelta, mapName ?? "??", d.UserName, d.UserId));
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

        public class GameResultListResponse
        {
            public List<GameResultListItem> Logs { get; set; }
            public GameResultListResponse()
            {
                Logs = new();
            }
            public class GameResultListItem
            {
                public GameResultListItem(
                    int rank, int gameId, DateTime startTime, int eloDelta, string mapName, string userName, int userId)
                {
                    Rank = rank;
                    GameId = gameId;
                    StartTime = startTime.ToString("yy/MM/dd HH:mm");
                    EloDelta = eloDelta;
                    MapName = mapName;
                    UserName = userName;
                    UserId = userId;
                }
                public int Rank { get; set; }
                public int GameId { get; set; }
                public string StartTime { get; set; }
                public int EloDelta { get; set; }
                public string MapName { get; set; }
                public string UserName { get; set; }
                public int UserId { get; set; }
            }
        }
    }
}
