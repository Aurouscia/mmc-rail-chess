using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RailChess.Models;
using RailChess.Models.Game;
using RailChess.Play.PlayHubRequestModel;
using RailChess.Play.PlayHubResponseModel;
using RailChess.Play.Services;

namespace RailChess.Play
{
    [Authorize]
    public class PlayHub : Hub
    {
        public PlayService Service { get; }
        private readonly PlayPlayerService _playerService;
        private readonly PlayEventsService _eventsService;
        private readonly PlayGameService _gameService;
        private ILogger _logger;

        public IClientProxy Group => Clients.Group(GroupName);

        private const string textMsgMethod = "textmsg";
        private const string syncMethod = "sync";
        private const string defaultSender = "服务器";
        
        public PlayHub(
            PlayService playService,
            PlayPlayerService playerService,
            PlayEventsService eventsService,
            PlayGameService gameService,
            ILogger<PlayHub> logger)
        {
            Service = playService;
            _playerService = playerService;
            _eventsService = eventsService;
            _gameService = gameService;
            _logger = logger;
        }
        public string GroupName
        {
            get
            {
                int gameId = Service.GameId;
                if (gameId == 0)
                {
                    var info = _playerService.GetByConn(Context.ConnectionId);
                    if(info is not null)
                        gameId = info.GameId;
                }
                return $"gameGroup_{gameId}";
            }
        }

        public async Task Join(JoinRequest _)
        {
            var errmsg = Service.Join();
            if(errmsg is null)
            {
                //无报错信息，成功加入，需要让房间里所有人同步
                await SyncAll();
                await SendTextMsg($"用户<b>{SenderName()}</b>加入了棋局",defaultSender, TextMsgType.Important);
            }
            else
            {
                //有报错信息，未能加入
                await SendTextMsg(errmsg, defaultSender, TextMsgType.Err, Clients.Caller);
            }
        }
        public async Task GameStart(GameStartRequest _)
        {
            var errmsg = Service.StartGame();
            if (errmsg is null)
            {
                await SyncAll();
                await SendTextMsg("房主已下令棋局开始",defaultSender,TextMsgType.Important);
            }
            else
                await SendTextMsg(errmsg, defaultSender, TextMsgType.Err, Clients.Caller);
        }
        public async Task GameReset(GameResetRequest _)
        {
            var errmsg = Service.ResetGame();
            if (errmsg is null)
            {
                await SyncAll();
                await SendTextMsg("房主已下令棋局重置，需要所有玩家重新加入", defaultSender, TextMsgType.Err);
            }
            else
                await SendTextMsg(errmsg, defaultSender, TextMsgType.Err, Clients.Caller);
        }
        public async Task SendTextMsg(SendTextMsgRequest request)
        {
            string? senderName = SenderName();
            if (senderName is null || request.Content is null)
                return;
            await SendTextMsg(request.Content, senderName);
        }
        private async Task SendTextMsg(string str, string sender = defaultSender, TextMsgType type = TextMsgType.Plain, IClientProxy? to = null)
        {
            to ??= Group;
            _logger.LogDebug("游戏[{gameId}]_发送者[{sender}]_{str}", Service.GameId, sender, str);
            await to.SendAsync(textMsgMethod, new TextMsg(str, sender, type));
        }
        public async Task Enter(EnterRequest _)
        {
            if (Service.UserId == 0)
            {
                await SendTextMsg("请先登录再进入房间", defaultSender, TextMsgType.Err, Clients.Caller);
                Context.Abort();
                return;
            }
            _playerService.InsertByConn(Context.ConnectionId, Service.UserId, Service.GameId);

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
            await Clients.Caller.SendAsync(syncMethod, Service.GetSyncData());

            var ended = _eventsService.GamedEnded();
            if (!ended)
            {
                bool meJoined = _eventsService.MeJoined();
                string callerMsg = meJoined ? "您已成功返回房间，请继续游戏" : "您已进入房间观战";

                var g = _gameService.OurGame();
                string rules = $"<br/>随机数范围<b>{g.RandMin}-{g.RandMax}</b>";
                rules += $"<br/>允许每回合换乘<b>{g.AllowTransfer}</b>次";
                callerMsg += rules;

                string othersMsg = meJoined ? $"玩家<b>{SenderName()}</b>已返回房间" : $"<b>{SenderName()}</b>已进入房间观战";
                await SendTextMsg(callerMsg, defaultSender, TextMsgType.Plain, Clients.Caller);
                await SendTextMsg(othersMsg, defaultSender, TextMsgType.Important, Clients.OthersInGroup(GroupName));
            }
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await SendTextMsg($"用户<b>{SenderName()}</b>离开房间", defaultSender, TextMsgType.Important);
        }

        /// <summary>
        /// 由当前轮到的玩家唤起的方法，表示自己选好了怎么移动
        /// </summary>
        /// <returns></returns>
        public async Task Select(SelectRequest request)
        {
            int userId = Service.UserId;
            if(userId != _playerService.CurrentPlayer())
            {
                await SendTextMsg("不是你的回合",defaultSender, TextMsgType.Err, Clients.Caller);
                return;
            }
            var name = _playerService.Get(userId).Name;
            if (request.Path is null || request.Path.Count<=1)
            {
                var loc = _eventsService.PlayerLocateEvents().Where(x => x.PlayerId == userId).Select(x=>x.StationId).FirstOrDefault();
                _eventsService.Add(RailChessEventType.PlayerStuck, loc, true);
                await SendTextMsg($"<b>{name}</b>无路可走，卡住一次",defaultSender,TextMsgType.Important);

                var stuckTimes = _eventsService.PlayerStuckEvents().OfUser(userId).Count;
                if (stuckTimes >= _gameService.OurGame().StucksToLose)
                {
                    await SendTextMsg($"<b>{name}</b>已卡住{stuckTimes}次，出局", defaultSender, TextMsgType.Err);
                    Service.Leave();
                }
                await SyncAll();
                return;
            }
            string? errmsg = Service.Select(request.Path.Last(), out int captured);
            if (errmsg is not null) 
            {
                await SendTextMsg(errmsg, defaultSender, TextMsgType.Err, Clients.Caller);
                return;
            }
            await SendTextMsg($"<b>{name}</b>已落子，新占领{captured}个车站");
            await SyncAll();
        }
        public async Task Out(OutRequest _)
        {
            Service.Leave();
            var name = _playerService.Get(Service.UserId).Name;
            await SendTextMsg($"<b>{name}</b>退出了对局", defaultSender, TextMsgType.Important);
            await SyncAll();
        }
        public async Task KickAfk(KickAfkRequest _)
        {
            var errmsg = Service.KickAfk(out string? clearedPlayerName);
            if (errmsg is not null)
            {
                await SendTextMsg(errmsg, defaultSender, TextMsgType.Err, Clients.Caller);
                return;
            }
            else
            {
                await SendTextMsg($"<b>{clearedPlayerName}</b>挂机太久，已被房主移出", defaultSender, TextMsgType.Important);
                await SyncAll();
            }
        }
        public async Task SyncMe(SyncMeRequest req)
        {
            var data = Service.GetSyncData(false, req.TFilterId);
            await Clients.Caller.SendAsync(syncMethod, data);
        }
        private async Task SyncAll()
        {
            _logger.LogInformation("游戏[{gameId}]_开始同步所有人",Service.GameId);
            var data = Service.GetSyncData();
            await Group.SendAsync(syncMethod, data);
            _logger.LogInformation("游戏[{gameId}]_成功同步所有人", Service.GameId);
        }
        private string? SenderName()
        {
            var u = _playerService.GetByConn(Context.ConnectionId);
            if (u is not null)
                return u.UserName;
            return null;
        }
    }
}
