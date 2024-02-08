using Microsoft.AspNetCore.SignalR;
using RailChess.Play.PlayHubRequestModel;

namespace RailChess.Play.Services
{
    public class PlayInvokeInfoFilter : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            var playHub = invocationContext.Hub as PlayHub;
            if (playHub is not null)
            {
                var req = invocationContext.HubMethodArguments.FirstOrDefault() as RequestModelBase;
                if (req is not null)
                    playHub.Service.GameId = req.GameId;
                int.TryParse(invocationContext.Context.UserIdentifier, out int userId);
                playHub.Service.UserId = userId;
            }
            return await next(invocationContext);
        }
    }
}
