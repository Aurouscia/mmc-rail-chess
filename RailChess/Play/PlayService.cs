using Microsoft.Extensions.Caching.Memory;
using RailChess.Models;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Play.PlayHubResponseModel;
using RailChess.Play.Services;
using RailChess.Utils;

namespace RailChess.Play
{
    public class PlayService
    {
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly PlayPlayerService _playerService;
        private readonly PlayGameService _gameService;
        private readonly RailChessContext _context;
        private int _gameId;
        private int _userId;
        public int GameId { get => _gameId; set 
            {
                if (value <= 0) throw new Exception("请从正确入口进入");
                _gameId = value;
                _eventsService.GameId = value;
                _topoService.GameId = value;
                _gameService.GameId = value;
            }
        }
        public int UserId { get => _userId; set
            {
                _userId = value;
                _eventsService.UserId = value;
            }
        }
        public PlayService(
            PlayEventsService eventsService,
            PlayToposService toposService,
            PlayPlayerService playerService,
            PlayGameService gameService,
            RailChessContext context) 
        { 
            _eventsService = eventsService;
            _topoService = toposService;
            _playerService = playerService;
            _gameService = gameService;
            _context = context;
        }

        public SyncData GetSyncData()
        {

            var playerIds = _eventsService.PlayersJoinEvents().ConvertAll(x => x.PlayerId);//已经按加入顺序排列好
            var latestOp = _eventsService.LatestOperation();
            int lastPlayer = -1;//默认情况下：还没有任何操作
            if(latestOp is not null)
                lastPlayer = latestOp.PlayerId;
            var players = _playerService.GetOrdered(playerIds,lastPlayer);

            var locEvents = _eventsService.PlayerLocateEvents();
            var stuckEvents = _eventsService.PlayerStuckEvents();
            var captureEvents = _eventsService.PlayerCaptureEvents();

            List<Player> playerStatus = new();
            List<OcpStatus> ocps = new();
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
                ocps.Add(new()
                {
                    PlayerId = p.Id,
                    Stas = hisStations
                });
            });

            OcpStatus? newOcps = null;
            if (latestOp is not null)
            {
                var lastCaptures = captureEvents.FindAll(x => x.Time >= latestOp.Time);
                newOcps = new()
                {
                    PlayerId = latestOp.PlayerId,
                    Stas = lastCaptures.ConvertAll(x => x.StationId)
                };
            }

            var game = _gameService.Get();
            int rand = _eventsService.RandedResult();
            if (rand < 0)
                rand = RandNum.Uniform(game.RandMin, game.RandMax);
            var selections = new List<StepSelection>();
            var started = _eventsService.GameStarted();
            if (started)
            {
                //计算selections
            }
            var res = new SyncData()
            {
                PlayerStatus = playerStatus,
                Ocps = ocps,
                NewOcps = newOcps,
                RandNumber = rand,
                Selections = selections,
                GameStarted = started
            };
            return res;
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
            if (game is null || game.HostUserId==0) throw new Exception("数据异常，找不到房主");
            if (game.Started)
                return "对局已开始过";
            if (UserId != game.HostUserId)
                return "只有房主能开始对局";
            var playersCount = _eventsService.PlayersJoinEvents().Count;
            if (playersCount == 0)
                return "没有玩家加入的游戏不能开始";

            _eventsService.Add(RailChessEventType.GameStart, 0, false);
            game.Started = true;
            game.StartTime = DateTime.Now;
            _context.SaveChanges();
            return null;
        }
        public string? ResetGame()
        {
            var game = _context.Games.Where(x => x.Id == GameId).FirstOrDefault() ?? throw new Exception("数据异常，找不到棋局");
            if (game.Ended)
                return "对局已结束，不允许重置";
            game.Started = false;

            var events = _eventsService.OurEvents();
            _context.RemoveRange(events);
            _eventsService.ClearOurEventsCache();
            _context.SaveChanges();
            return null;
        }
    }
}
