using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Models.Map;
using RailChess.Services;

namespace RailChess.Controllers
{
    [Authorize]
    public class GameController:Controller
    {
        private readonly RailChessContext _context;
        private readonly int _userId;
        private const int unplayedGameTimeoutMins = 12 * 60;

        public GameController(RailChessContext context, HttpUserIdProvider httpUserIdProvider)
        {
            _context = context;
            _userId = httpUserIdProvider.Get();
        }

        public IActionResult Active()
        {
            var g = _context.Games.Where(x => !x.Ended && !x.Deleted).OrderByDescending(x => x.Id).Take(20).ToList();
            var timeoutSpan = TimeSpan.FromMinutes(unplayedGameTimeoutMins);
            var timeouts = g.FindAll(x => DateTime.Now - x.StartTime > timeoutSpan);
            if (timeouts.Count > 0)
            {
                timeouts.ForEach(game =>
                {
                    var itsLastEvent = _context.Events
                        .Where(x => x.GameId == game.Id)
                        .OrderByDescending(x=>x.Time)
                        .FirstOrDefault();
                    if (itsLastEvent is null 
                        || DateTime.Now - itsLastEvent.Time > timeoutSpan)
                    {
                        game.Deleted = true;
                        _context.Update(g);
                    }
                });
                _context.SaveChanges();
                g.RemoveAll(x => x.Deleted);
            }

            List<int> userIds = g.Select(x=>x.HostUserId).ToList();
            List<int> mapIds = g.Select(x=>x.UseMapId).ToList();
            var users = _context.Users.Where(x => userIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToList();
            var maps = _context.Maps.Where(x => mapIds.Contains(x.Id)).Select(x => new { x.Id, x.Title }).ToList();
            var res = new GameActiveResult();
            g.ForEach(x =>
            {
                string mapName = maps.FirstOrDefault(m => m.Id == x.UseMapId)?.Title ?? "???";
                string userName = users.FirstOrDefault(u => u.Id == x.HostUserId)?.Name ?? "???";
                res.Items.Add(new(x, mapName, userName));
            });
            return this.ApiResp(res);
        }
        public IActionResult Create([FromBody]RailChessGame game)
        {
            var m = _context.Maps.Where(x => x.Id == game.UseMapId).Select(x => x.Author).FirstOrDefault();
            if (m == 0) return this.ApiFailedResp("找不到指定地图");
            if (game.RandMin < 0 || game.RandMax > 20 || game.RandMax <= game.RandMin)
                return this.ApiFailedResp("随机数设置有问题");
            if (game.StucksToLose < 1 || game.StucksToLose > 20)
                return this.ApiFailedResp("卡住出局次数设置有问题");

            game.HostUserId = _userId;
            game.CreateTime = DateTime.Now;
            game.Started = false;
            game.StartTime = DateTime.Now;

            _context.Games.Add(game);
            _context.SaveChanges();
            return this.ApiResp();
        }
        public IActionResult Init(int id)
        {
            var g = _context.Games.Find(id);
            if (g is null)
                return this.ApiFailedResp("找不到指定棋局");
            var m = _context.Maps.Find(g.UseMapId);
            if (m is null)
                return this.ApiFailedResp("找不到指定地图");
            return this.ApiResp(new InitData(m, g));
        }
        public IActionResult Delete(int id)
        {
            var g = _context.Games.Find(id);
            if (g is null)
                return this.ApiFailedResp("找不到指定棋局");
            if (g.HostUserId != _userId)
                return this.ApiFailedResp("只有创建者能删除");
            g.Deleted = true;
            _context.SaveChanges();
            return this.ApiResp();
        }

        public class GameActiveResult
        {
            public List<GameActiveResultItem> Items { get; set; }
            public GameActiveResult()
            {
                Items = new();
            }
            public class GameActiveResultItem
            {
                public RailChessGame Data { get; set; }
                public string? MapName { get; set; }
                public string? HostUserName { get; set; }
                public int StartedMins { get; set; }
                public GameActiveResultItem(RailChessGame game, string mapName, string hostUserName)
                {
                    Data = game;
                    MapName = mapName;
                    HostUserName = hostUserName;
                    if (game.Started)
                        StartedMins = (int)(DateTime.Now - game.StartTime).TotalMinutes;
                    else
                        StartedMins = -1;
                }
            }
        }
        public class InitData
        {
            public string? BgFileName { get; set; }
            public string? TopoData { get; set; }
            public RailChessGame GameInfo { get; set; }
            public InitData(RailChessMap map, RailChessGame game)
            {
                BgFileName = map.ImgFileName;
                TopoData = map.TopoData;
                GameInfo = game;
            }
        }
    }
}
