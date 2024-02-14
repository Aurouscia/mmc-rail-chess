using Microsoft.Extensions.Caching.Memory;
using RailChess.Models;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Play.PlayHubResponseModel;
using RailChess.Play.Services;
using RailChess.Utils;
using static RailChess.Models.Map.RailChessTopo;

namespace RailChess.Play
{
    public class PlayService
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly PlayPlayerService _playerService;
        private readonly RailChessContext _context;
        private int _gameId;
        private int _userId;
        public int GameId { get => _gameId; set 
            {
                if (value <= 0) throw new Exception("请从正确入口进入");
                _gameId = value;
                _eventsService.GameId = value;
                _topoService.GameId = value;
            }
        }
        public int UserId { get => _userId; set
            {
                _userId = value;
                _eventsService.UserId = value;
            }
        }
        public PlayService(PlayEventsService eventsService, PlayToposService toposService, PlayPlayerService playerService, RailChessContext context) 
        { 
            _eventsService = eventsService;
            _topoService = toposService;
            _playerService = playerService;
            _context = context;
        }

        public SyncData GetSyncData()
        {
            var playerIds = _eventsService.PlayersJoinEvents().ConvertAll(x => x.PlayerId);
            var players = _playerService.Get(playerIds);
            List<Player> playerStatus = new();
            var locEvents = _eventsService.PlayerLocateEvents();
            var stuckEvents = _eventsService.PlayerStuckEvents();
            var captureEvents = _eventsService.PlayerCaptureEvents();
            players.ForEach(p =>
            {
                int atSta = locEvents.OfUser(p.Id).Select(x=>x.StationId).FirstOrDefault();
                if (atSta <= 0) throw new Exception($"玩家[{p.Name}]定位失败");
                int stuckTimes = stuckEvents.OfUser(p.Id).Count;
                List<int> hisStations = captureEvents.OfUser(p.Id).ConvertAll(x=>x.StationId);
                playerStatus.Add(new()
                {
                    Id = p.Id,
                    Name = p.Name ?? "???",
                    AtSta = atSta,
                    AvtFileName = p?.AvatarName ?? "???",
                    StuckTimes = stuckTimes,
                    Score = _topoService.TotalDirections(hisStations)
                });
            });
            var res = new SyncData()
            {
                PlayerStatus = playerStatus,
            };
        }

        public string? Join()
        {
            if(UserId<=0)
                return "无身份信息";
            if (_eventsService.GameStarted())
                return "对局已开始，不能加入";
            if (_eventsService.MeJoined())
                return "已在对局中";
            var pureTerminals = _topoService.PureTerminalIds();
            var otherPlayersJoinEvents = _eventsService.PlayersJoinEvents();
            var occupiedStations = otherPlayersJoinEvents.ConvertAll(x => x.StationId);
            var available = pureTerminals.Except(occupiedStations).ToList();
            if (!available.Any())
                throw new Exception("房间已满，无法加入");
            int startAt = available.RandomSelect();
            _eventsService.Add(RailChessEventType.PlayerJoin, startAt);
            return null;
        }

        public string? StartGame()
        {
            var game = _context.Games.Where(x => x.Id == GameId).FirstOrDefault();
            if (game is null) throw new Exception("数据异常，找不到房主");
            if (game.Started)
                return "对局已开始过";
            if (UserId != game.HostUserId)
                return "只有房主能开始对局";

            _eventsService.Add(RailChessEventType.GameStart, 0, false);
            game.Started = true;
            game.StartTime = DateTime.Now;
            _context.SaveChanges();
            return null;
        }

        //public Dictionary<int, int> PlayersOcpStatus()
        //{
        //    var moveEvents = _eventsService.OurEvents().Where(x => x.EventType == RailChessEventType.PlayerMoveTo);
        //    var lastMoves = moveEvents.Reverse().DistinctBy(x => x.PlayerId)
        //        .Select(x => new { x.PlayerId, x.StationId });
        //    return lastMoves.ToDictionary(x => x.PlayerId, x => x.StationId);
        //}
    }
}
