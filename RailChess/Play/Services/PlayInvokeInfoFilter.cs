using Microsoft.AspNetCore.SignalR;
using RailChess.Play.PlayHubRequestModel;
using RailChess.Play.PlayHubResponseModel;

namespace RailChess.Play.Services
{
    public class PlayInvokeInfoFilter : IHubFilter
    {
        private readonly ILogger<PlayInvokeInfoFilter> _logger;

        public PlayInvokeInfoFilter(ILogger<PlayInvokeInfoFilter> logger)
        {
            _logger = logger;
        }
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            var playHub = invocationContext.Hub as PlayHub;
            if (playHub is not null)
            {
                var req = invocationContext.HubMethodArguments.FirstOrDefault() as RequestModelBase;
                int.TryParse(invocationContext.Context.UserIdentifier, out int userId);
                playHub.Service.UserId = userId;
                if (req is not null)
                {
                    playHub.Service.GameId = req.GameId;
                    _logger.LogInformation("游戏[{gameId}]_玩家[{userId}]_发出[{method}]", req.GameId, userId, req.GetType().Name);
                }
            }
            try
            {
                return await next(invocationContext);
            }
            catch(Exception ex)
            {
                //TODO：没那么异常的异常（房间已满之类的）与真异常应该区分开
                _logger.LogError(ex, "[异常]{msg}", ex.Message);
                await invocationContext.Hub.Clients.Caller.SendAsync(
                    method: PlayHub.textMsgMethod,
                    arg1: new TextMsg(ex.Message, PlayHub.defaultSender, TextMsgType.Err));
                return Task.CompletedTask;
            }
        }
    }
}
