using RailChess.Core;
using RailChess.Core.Abstractions;
using RailChess.Play.Services;
using RailChess.Play.Services.Core;

namespace RailChess.Play
{
    public static class AddToService
    {
        public static IServiceCollection AddPlayLogics(this IServiceCollection services)
        {
            services.AddScoped<PlayService>();

            services.AddScoped<PlayGameService>();
            services.AddScoped<PlayPlayerService>();
            services.AddScoped<PlayToposService>();
            services.AddScoped<PlayEventsService>();

            services.AddScoped<IExclusiveStasFinder, ExclusiveStasFinder>();
            services.AddScoped<IFixedStepPathFinder, FixedStepPathFinder>();
            services.AddScoped<CoreGraphProvider>();
            services.AddScoped<CoreCaller>();

            services.AddSingleton<PlayInvokeInfoFilter>();
            return services;
        }
    }
}
