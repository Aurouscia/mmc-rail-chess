using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RailChess.Play.PlayHubRequestModel;
using RailChess.Services;

namespace RailChess.Play
{
    [Authorize]
    public class PlayHub : Hub
    {
        public PlayService Service { get; }
        public PlayHub(PlayService service)
        {
            Service = service;
        }

        public async Task Join(JoinRequest request)
        {
            var a = Service.GameId;
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
        //public async Task SendMessageExe(string str,string sender="服务器")
        //{
        //    await Clients.Group(_gameId.ToString()).SendAsync(MsgOutputModel.InvokeMethod, new MsgOutputModel()
        //    {
        //        Str = str,
        //        Time = Time(),
        //        User = sender
        //    });
        //}
        //public override async Task OnConnectedAsync()
        //{
        //    if (_userName is null || _gameId == RailChessConstants.InvalidGameId)
        //        Context.Abort();

        //    bool newlyEntered = _ea.PlayerConnect();
        //    _eq.ClearLazy(x => x.PlayerIds);
        //    _eq.ClearLazy(x => x.Events);
        //    string isAudience = "";
        //    if (!_eq.PlayerIds.Value.Contains(_userInfo.UID))
        //    {
        //        isAudience = "（观战模式）";
        //    }
        //    await Groups.AddToGroupAsync(Context.ConnectionId, _gameId.ToString());

        //    Thread.Sleep(500);

        //    await Clients.Caller.SendAsync(MsgOutputModel.InvokeMethod,
        //        new MsgOutputModel($"欢迎进入游戏[{_gameId}]{isAudience}{RailChessConstants.EnterHint}"));

        //    await Clients.OthersInGroup(_gameId.ToString())
        //        .SendAsync(MsgOutputModel.InvokeMethod,
        //        new MsgOutputModel($"用户[{_userName}]已经成功连接{isAudience}"));

        //    await Clients.Caller.SendAsync(CvsInitData.ResponseMethod, _initer.GetInitData(_userInfo.UID));

        //    Thread.Sleep(500);
        //    if (newlyEntered)
        //    {
        //        await Clients.Group(_gameId.ToString())
        //            .SendAsync(PlayerStaticData.InvokeMethod, new PlayerStaticData(_eq));
        //    }
        //    else
        //    {
        //        await Clients.Caller.SendAsync(PlayerStaticData.InvokeMethod, new PlayerStaticData(_eq));
        //    }
        //    await Clients.Group(_gameId.ToString()).SendAsync(RefreshData.InvokeMethod, new RefreshData(_eq,_ea));
        //}
        //public override async Task OnDisconnectedAsync(Exception? exception)
        //{
        //    _ea.PlayerDisconnect();
        //    await Clients.Group(_gameId.ToString()).SendAsync(MsgOutputModel.InvokeMethod,
        //        new MsgOutputModel($"用户[{_userName}]已经断开连接"));
        //}
        //public async Task RefreshAll()
        //{
        //    await Clients.Group(_gameId.ToString()).SendAsync(RefreshData.InvokeMethod, new RefreshData(_eq,_ea));
        //}
        //public async Task UpdatePlayerList()
        //{
        //    await Clients.Group(_gameId.ToString()).SendAsync(PlayerStaticData.InvokeMethod, new PlayerStaticData(_eq));
        //}


        /// <summary>
        /// 由当前轮到的玩家唤起的方法，表示自己选好了怎么移动
        /// </summary>
        /// <param name="selection">选择的选项</param>
        /// <returns></returns>
        //public async Task Select(MoveCapturePair selection)
        //{
        //    _ea.PlayerSelect(selection.moveTo, selection.occupy);
        //    _eq.ClearLazy(x => x.Events);
        //    _eq.ClearLazy(x => x.StationStatusAttr);
        //    RefreshData data = new(_eq, _ea);
        //    string nowPlaying = _userNameDic.Get(data.ActivePlayerId);
        //    await SendMessageExe($"<u><span style=\"color:green\">{_userName}</span>刚刚走了一步，接下来轮到：<span style=\"color:greenyellow\">{nowPlaying}</span>，随机数是{data.RandRes}</u>");
        //    await Clients.Group(_gameId.ToString()).SendAsync("Refresh",data);
        //}
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
    }
}
