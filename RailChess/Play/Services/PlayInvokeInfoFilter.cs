using Microsoft.AspNetCore.SignalR;
using RailChess.Play.PlayHubRequestModel;

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
            return await next(invocationContext);
        }
    }
}
