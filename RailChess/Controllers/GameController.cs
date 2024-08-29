using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Models.Map;
using RailChess.Services;
using RailChess.Utils;
using static RailChess.Controllers.GameController.GameTimeline;

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
            return this.ApiResp(new GameInitData(m, g));
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
        public IActionResult LoadTimeline(int id)
        {
            var events = _context.Events.Where(x => x.GameId == id && 
                (x.EventType == RailChessEventType.PlayerMoveTo || x.EventType == RailChessEventType.PlayerStuck 
                || x.EventType == RailChessEventType.PlayerCapture || x.EventType == RailChessEventType.RandNumGened))
                .OrderBy(x => x.Id).ToList();
            var game = (
                from g in _context.Games
                from m in _context.Maps
                where g.Id == id && g.UseMapId == m.Id
                select new
                {
                    GameStartTime = g.StartTime,
                    MapUpdateTime = m.UpdateTime
                }).FirstOrDefault();
            if (game is null)
                return this.ApiFailedResp("棋局或棋盘信息异常");
            var uids = events.Select(x=>x.PlayerId).Distinct().ToList();
            var users = _context.Users.Where(x => uids.Contains(x.Id)).Select(x => new { x.Id, x.AvatarName }).ToList();
            int rand = 0;
            GameTimeline timeline = new();
            GameTimelineItem? activeItem = null;
            foreach (var e in events)
            {
                if(e.EventType == RailChessEventType.RandNumGened)
                {
                    rand = e.StationId;
                }
                else if(e.EventType == RailChessEventType.PlayerMoveTo || e.EventType == RailChessEventType.PlayerStuck)
                {
                    if(activeItem is not null)
                    {
                        timeline.Items.Add(activeItem);
                    }
                    activeItem = new()
                    {
                        UId = e.PlayerId,
                        Cap = 0,
                        Rand = rand,
                        T = TimeStamp.DateTime2Long(e.Time)
                    };
                }
                else
                {
                    if (activeItem is { })
                    {
                        activeItem.Cap += 1;
                        activeItem.T = TimeStamp.DateTime2Long(e.Time);
                        //events按时间排列，e.Time必 >= activeItem.T
                    }
                }
            }
            if (activeItem is not null)
            {
                timeline.Items.Add(activeItem);
            }
            users.ForEach(x =>
            {
                timeline.Avts.Add(x.Id, x.AvatarName);
            });
            if (game.MapUpdateTime > game.GameStartTime)
                timeline.Warning = "【注意】地图在棋局后有改动，可能与当时有差异";
            return this.ApiResp(timeline);
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
        public class GameInitData
        {
            public string? BgFileName { get; set; }
            public string? TopoData { get; set; }
            public RailChessGame GameInfo { get; set; }
            public GameInitData(RailChessMap map, RailChessGame game)
            {
                BgFileName = map.ImgFileName;
                TopoData = map.TopoData;
                GameInfo = game;
            }
        }
        public class GameTimeline
        {
            public List<GameTimelineItem> Items { get; set; } = new();
            public Dictionary<int, string?> Avts { get; set; } = new();
            public string? Warning { get; set; }
            public class GameTimelineItem
            {
                public int UId { get; set; }
                public int Rand { get; set; }
                public int Cap { get; set; }
                public long T { get; set; }
            }
        }
    }
}
