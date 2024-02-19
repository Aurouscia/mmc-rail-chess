using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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

        public IClientProxy Group => Clients.Group(GroupName);

        private const string textMsgMethod = "textmsg";
        private const string syncMethod = "sync";
        private const string defaultSender = "服务器";
        
        public PlayHub(PlayService playService, PlayPlayerService playerService, PlayEventsService eventsService)
        {
            Service = playService;
            _playerService = playerService;
            _eventsService = eventsService;
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

        //public async Task SendMessage(MsgInputModel model)
        //{
        //    await SendMessageExe(model.MessageStr,_userName);
        //    if (model.MessageStr == RailChessConstants.GameStartCommand)
        //    {
        //        if (_userInfo.UID == _eq.GameCreatorId())
        //        {
        //            if (_ea.GameStart())
        //            {
        //                _eq.ClearLazy(x => x.Events);
        //                await SendMessageExe("<u>房主已下令游戏开始</u>");
        //                await RefreshAll();
        //            }
        //        }
        //        else
        //        {
        //            await SendMessageExe("只有房主才有权限开始游戏");
        //        }
        //    }
        //    else if(model.MessageStr == RailChessConstants.GameResetCommand)
        //    {
        //        if(_userInfo.UID == _eq.GameCreatorId())
        //        {
        //            _ea.GameReset();
        //            _eq.ClearLazy(x => x.Events);
        //            _eq.ClearLazy(x => x.PlayerIds);
        //            _eq.ClearLazy(x => x.StationStatusAttr);
        //            await SendMessageExe("<u>房主已下令重置游戏，所有玩家返回起点，游戏恢复\"未开始\"状态</u>");
        //            await UpdatePlayerList();
        //            await RefreshAll();
        //        }
        //        else
        //        {
        //            await SendMessageExe("只有房主才有权限重置游戏");
        //        }
        //    }
        //    else if (model.MessageStr.StartsWith(RailChessConstants.GameKickCommand))
        //    {
        //        if (_userInfo.UID == _eq.GameCreatorId())
        //        {
        //            string wantKickName = model.MessageStr.Replace(RailChessConstants.GameKickCommand, string.Empty);
        //            if (_ea.KickPlayer(wantKickName))
        //            {
        //                _eq.ClearLazy(x => x.Events);
        //                _eq.ClearLazy(x => x.PlayerIds);
        //                _eq.ClearLazy(x => x.StationStatusAttr);
        //                await SendMessageExe($"<u>房主已将{wantKickName}移出游戏</u>");
        //                await UpdatePlayerList();
        //                await RefreshAll();
        //            }
        //        }
        //        else
        //        {
        //            await SendMessageExe("只有房主才有权限移出用户");
        //        }
        //    }
        //    else if (model.MessageStr.StartsWith(RailChessConstants.GameGiveupCommand))
        //    {
        //        if (model.MessageStr.StartsWith(RailChessConstants.GameGiveupOthersCommand))
        //        {
        //            //把别人给放弃了
        //            if(_userInfo.UID == _eq.GameCreatorId())
        //            {
        //                string wantGiveupName = model.MessageStr.Replace(RailChessConstants.GameGiveupOthersCommand, string.Empty);
        //                if (_ea.GiveUpPlayer(wantGiveupName))
        //                {
        //                    await SendMessageExe($"<u>房主已将{wantGiveupName}放弃</u>");
        //                }
        //            }
        //            else
        //            {
        //                await SendMessageExe("只有房主才有权替用户放弃");
        //            }
        //        }
        //        else
        //        {
        //            //自己放弃
        //            if (_ea.GiveUpPlayer(_userInfo.UID))
        //            {
        //                var name = _context.Users.Find(_userInfo.UID)?.Name;
        //                await SendMessageExe($"<u>{name}已经放弃继续游戏</u>");
        //            }
        //        }
        //    }
        //}
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
            await to.SendAsync(textMsgMethod, new TextMsg(str, sender, type));
        }
        public async Task Enter(EnterRequest _)
        {
            if (Service.UserId == 0)
            {
                await SendTextMsg("请先登录再进入房间",defaultSender,TextMsgType.Err, Clients.Caller);
                Context.Abort();
                return;
            }
            _playerService.InsertByConn(Context.ConnectionId, Service.UserId, Service.GameId);

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
            await Clients.Caller.SendAsync(syncMethod, Service.GetSyncData());

            bool meJoined = _eventsService.MeJoined();
            string callerMsg = meJoined ? "您已成功返回房间，请继续游戏" : "您已进入房间观战";
            string othersMsg = meJoined ? $"玩家<b>{SenderName()}</b>已返回房间" : $"<b>{SenderName()}</b>已进入房间观战";
            await SendTextMsg(callerMsg, defaultSender, TextMsgType.Plain, Clients.Caller);
            await SendTextMsg(othersMsg, defaultSender, TextMsgType.Important, Clients.OthersInGroup(GroupName));
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await SendTextMsg($"用户<b>{SenderName()}</b>离开房间", defaultSender, TextMsgType.Important);
        }

        /// <summary>
        /// 由当前轮到的玩家唤起的方法，表示自己选好了怎么移动
        /// </summary>
        /// <param name="selection">选择的选项</param>
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
                await SyncAll();
                return;
            }
            string? errmsg = Service.Select(request.Path.Last(), out int captured);
            if (errmsg is not null) 
            {
                await SendTextMsg(errmsg, defaultSender, TextMsgType.Err, Clients.Caller);
                return;
            }
            await SendTextMsg($"<b>{name}</b>已落子，新占领{captured}个车站", defaultSender, TextMsgType.Important);
            await SyncAll();
        }
        /// <summary>
        /// 表示当前玩家没有可以选择的路径点，只能放弃
        /// </summary>
        /// <returns></returns>
        //public async Task Waive()
        //{
        //    _ea.Waive();
        //    _eq.ClearLazy(x => x.Events);
        //    _eq.ClearLazy(x => x.StationStatusAttr);
        //    RefreshData data = new(_eq, _ea);
        //    string nowPlaying = _userNameDic.Get(data.ActivePlayerId);
        //    await SendMessageExe($"<u><span style=\"color:green\">{_userName}</span>因为没有可走的站点，<span style=\"color:red\">受伤一次</span>，接下来轮到：<span style=\"color:greenyellow\">{nowPlaying}</span>，随机数是{data.RandRes}</u>");
        //    await Clients.Group(_gameId.ToString()).SendAsync("Refresh", data);
        //}
        private async Task SyncAll()
        {
            var data = Service.GetSyncData();
            await Group.SendAsync(syncMethod, data);
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
