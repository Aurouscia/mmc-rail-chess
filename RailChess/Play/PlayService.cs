using Microsoft.Extensions.Caching.Memory;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Play.Services;
using RailChess.Utils;

namespace RailChess.Play
{
    public class PlayService
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly RailChessContext _context;
        private int _gameId;
        private int _userId;
        public int GameId { get => _gameId; set 
            {
                if (value <= 0) throw new Exception("请从正确入口进入");
                _gameId = value;
                _eventsService.GameId = value;
            }
        }
        public int UserId { get => _gameId; set
            {
                _userId = value;
                _eventsService.UserId = value;
            }
        }
        public PlayService(PlayEventsService eventsService, PlayToposService toposService, RailChessContext context) 
        { 
            _eventsService = eventsService;
            _topoService = toposService;
            _context = context;
        }

        public SyncData

        public string? Join()
        {
            if(UserId<=0)
                return "无身份信息";
            if (_eventsService.GameStarted())
                return "对局已开始，不能加入";
            if (_eventsService.MeJoined())
                return "已在对局中";
            var pureTerminals = _topoService.PureTerminalIds(GameId);
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

        //public Dictionary<int,int> PlayersOcpStatus()
        //{
        //    var moveEvents = _eventsService.EventsOf(GameId).Where(x=>x.EventType==RailChessEventType.PlayerMoveTo);
        //    var lastMoves = moveEvents.Reverse().DistinctBy(x => x.PlayerId)
        //        .Select(x => new {x.PlayerId,x.StationId});
        //    return lastMoves.ToDictionary(x => x.PlayerId, x => x.StationId);
        //}
    }
}
