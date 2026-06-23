using Microsoft.Extensions.Options;

namespace RailChess.Utils.Startup
{
    public class EmbedOptions
    {
        /// <summary>
        /// 允许通过 iframe 嵌入小部件的源站列表（例如 https://example.com）。
        /// 留空时表示允许所有来源。
        /// </summary>
        public List<string> AllowedOrigins { get; set; } = new();
    }

    public static class EmbedConfigure
    {
        public const string EmbedSectionName = "Embed";

        public static IServiceCollection AddEmbed(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<EmbedOptions>(config.GetSection(EmbedSectionName));
            return services;
        }

        public static WebApplication UseEmbedHeaders(this WebApplication app)
        {
            app.UseMiddleware<EmbedHeadersMiddleware>();
            return app;
        }
    }

    public class EmbedHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly EmbedOptions _options;

        public EmbedHeadersMiddleware(RequestDelegate next, IOptions<EmbedOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/api/embed/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var origins = _options.AllowedOrigins;
            string cspValue = origins?.Count > 0
                ? $"frame-ancestors 'self' {string.Join(" ", origins)}"
                : "frame-ancestors *";

            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Remove("X-Frame-Options");
                if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    context.Response.Headers.Append("Content-Security-Policy", cspValue);
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
