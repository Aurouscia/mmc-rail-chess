﻿using Microsoft.Extensions.Caching.Memory;
using RailChess.Models;
using RailChess.Models.DbCtx;
using RailChess.Models.Game;
using RailChess.Play.PlayHubResponseModel;
using RailChess.Play.Services;
using RailChess.Play.Services.Core;
using RailChess.Utils;

namespace RailChess.Play
{
    public class PlayService
    {
        private readonly CoreCaller _coreCaller;
        private readonly CoreGraphProvider _coreGraphProvider;
        private readonly PlayEventsService _eventsService;
        private readonly PlayToposService _topoService;
        private readonly PlayPlayerService _playerService;
        private readonly PlayGameService _gameService;
        private readonly PlayResultService _resultService;
        private readonly RailChessContext _context;
        
        private int _gameId;
        private int _userId;
        private const int timeoutMins = 180;
        private const int allowKickSecs = 60;
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
            CoreCaller coreCaller,
            CoreGraphProvider coreGraphProvider,
            PlayEventsService eventsService,
            PlayToposService toposService,
            PlayPlayerService playerService,
            PlayGameService gameService,
            PlayResultService resultService,
            RailChessContext context) 
        {
            _coreCaller = coreCaller;
            _coreGraphProvider = coreGraphProvider;
            _eventsService = eventsService;
            _topoService = toposService;
            _playerService = playerService;
            _gameService = gameService;
            _resultService = resultService;
            _context = context;
        }

        public SyncData GetSyncData(bool onlyWriteResult = false, int tFilterId = 0)
        {
            bool playback = false;
            if (tFilterId > 0)
            {
                _eventsService.TFilterId = tFilterId;
                playback = true;
            }
            var players = _playerService.GetOrdered();
            var locEvents = _eventsService.PlayerLocateEvents();
            var stuckEvents = _eventsService.PlayerStuckEvents();
            var captureEvents = _eventsService.PlayerCaptureEvents();
            var outEvents = _eventsService.PlayerOutEvents();
            var latestOp = _eventsService.LatestOperation();
            var game = _gameService.OurGame();
            var ourTopo = _topoService.OurTopo();

            List<Player> playerStatus = new();
            List<OcpStatus> ocps = new();
            var dirDict = _coreGraphProvider.StationDirections();
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
                    Score = _coreGraphProvider.TotalDirections(hisStations, dirDict),
                    Out = outEvents.Any(x => p is not null && x.PlayerId == p.Id)
                });
                ocps.Add(new()
                {
                    PlayerId = p!.Id,
                    Stas = hisStations
                });
            });
            bool ended = false;
            bool playerAllOut = playerStatus.Count > 0 && playerStatus.All(x => x.Out == true);
            if (ourTopo.Stations is null)
                throw new Exception("无车站，无法进行游戏");
            bool stationAllCaptured = captureEvents.Count >= ourTopo.Stations.Count;

            bool timeout = latestOp is not null && (DateTime.Now - latestOp.Time).TotalMinutes > timeoutMins;

            if (!playback && (timeout || playerAllOut || stationAllCaptured || game.Ended))
            {
                ended = true;
                if (!_eventsService.GamedEnded())
                {
                    var notOutPlayers = playerStatus.FindAll(x => x.Out == false);
                    notOutPlayers.ForEach(x =>
                    {
                        x.Out = true;
                        _eventsService.Add(RailChessEventType.PlayerOut, 0, x.Id, false);
                    });

                    _eventsService.Add(RailChessEventType.GameEnd, 0, false);
                    game.Ended = true;
                    game.DurationMins = (int)(DateTime.Now - game.StartTime).TotalMinutes;
                    _context.Update(game);
                    _context.SaveChanges();
                }
                if(!_context.GameResults.Any(x=>x.GameId == GameId))
                {
                    _resultService.RunFor(GameId, playerStatus);
                    if (onlyWriteResult)
                        return new();
                }
            }

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

            int rand = 0;
            var selections = new List<List<int>>();
            var started = _eventsService.GameStarted();
            if (started && !ended && !playback)
            {
                var paths = _coreCaller.GetSelections().ToList();
                paths.ForEach(p =>
                {
                    if (!p.Any())
                        return;
                    selections.Add(p.ToList());
                });

                rand = _eventsService.RandedResult();
            }
            if (playback)
            {
                rand = _eventsService.RandedResultOnlyGet();
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
            var spawnRule = _gameService.OurGame().SpawnRule;
            List<int> spawnCandidates;
            if (spawnRule == SpawnRuleType.TwinExchange)
                spawnCandidates = _coreGraphProvider.TwinExchanges();
            else
                spawnCandidates = _coreGraphProvider.PureTerminals();
            var otherPlayersJoinEvents = _eventsService.PlayersJoinEvents();
            var occupiedStations = otherPlayersJoinEvents.ConvertAll(x => x.StationId);
            var spawnAvailable = spawnCandidates.Except(occupiedStations).ToList();
            if (spawnAvailable.Count == 0)
                throw new Exception("房间已满，无法加入");
            int startAt = spawnAvailable.RandomSelect();
            _eventsService.Add(RailChessEventType.PlayerJoin, startAt,false);
            _eventsService.Add(RailChessEventType.PlayerCapture, startAt);
            return null;
        }

        public string? StartGame()
        {
            var game = _gameService.OurGame();
            if (game is null || game.HostUserId==0) throw new Exception("数据异常，找不到房主");
            if (game.Started)
                return "对局已开始过";
            if (UserId != game.HostUserId)
                return "只有房主能开始对局";
            var playersCount = _eventsService.PlayersJoinEvents().Count;
            if (playersCount <= 1)
                return "至少需要两人加入(在顶部显示)";

            _eventsService.Add(RailChessEventType.GameStart, 0, false);
            game.Started = true;
            game.StartTime = DateTime.Now;
            _context.Update(game);
            _context.SaveChanges();
            return null;
        }
        public string? ResetGame()
        {
            var game = _gameService.OurGame();
            if (game.Started)
                return "对局已开始，不允许重制";
            if (game.Ended)
                return "对局已结束，不允许重置";
            game.Started = false;

            var events = _eventsService.OurEvents();
            _context.RemoveRange(events);
            _eventsService.ClearOurEventsCache();
            _context.Update(game);
            _context.SaveChanges();
            return null;
        }

        public string? Select(int dist, out int captured)
        {
            if (!_coreCaller.IsValidMove(dist))
            {
                captured = 0;
                return "移动不合法，请刷新后重选";
            }
            _eventsService.Add(RailChessEventType.PlayerMoveTo, dist, false);
            if (!_eventsService.PlayerCaptureEvents().Any(x => x.StationId == dist))
                _eventsService.Add(RailChessEventType.PlayerCapture, dist, true);
            var existing = _eventsService.PlayerCaptureEvents().ConvertAll(x=>x.StationId);
            var captures = _coreCaller.AutoCapturables().ToList();
            captures.RemoveAll(existing.Contains);
            foreach(var capture in captures)
            {
                _eventsService.Add(RailChessEventType.PlayerCapture, capture, false);
            }
            _context.SaveChanges();
            captured = captures.Count + 1;
            return null;
        }
        public string? Leave()
        {
            _eventsService.Add(RailChessEventType.PlayerOut, 0, true);
            return null;
        }
        public string? KickAfk(out string? clearedPlayerName)
        {
            if (!_eventsService.MeJoined()) 
            {
                clearedPlayerName = null;
                return "仅棋局内玩家可踢出挂机者";
            }
            int player = _playerService.CurrentPlayer();
            clearedPlayerName = _playerService.Get(player).Name;
            DateTime lastOpTime = DateTime.MinValue;
            var lastOp = _eventsService.LatestOperation();
            if (lastOp is { })
            {
                lastOpTime = lastOp.Time;
            }
            else { 
                var startEvent = _eventsService.OurEvents()
                    .FirstOrDefault(x => x.EventType == RailChessEventType.GameStart);
                if(startEvent is { })
                {
                    lastOpTime = startEvent.Time;
                }
            }
            TimeSpan stuckTime = DateTime.Now - lastOpTime;
            if (stuckTime.TotalSeconds > allowKickSecs)
            {
                _eventsService.Add(RailChessEventType.PlayerOut, 0, player, true);
                return null;
            }
            return $"请等待，玩家挂机{allowKickSecs}秒后才可移出";
        }
    }
}
