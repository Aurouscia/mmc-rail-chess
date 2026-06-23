using RailChess.Controllers;

namespace RailChess.Utils.Startup
{
    public static class EmbedConfigure
    {
        public static WebApplication UseEmbedHeaders(this WebApplication app)
        {
            app.UseMiddleware<EmbedHeadersMiddleware>();
            return app;
        }
    }

    public class EmbedHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public EmbedHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var controllerName = context.Request.RouteValues["controller"] as string;
            bool isEmbed = !string.IsNullOrEmpty(controllerName)
                && $"{controllerName}Controller" == nameof(EmbedController);

            context.Response.OnStarting(() =>
            {
                if (isEmbed)
                {
                    // 仅 EmbedController 允许被任意站点 iframe 嵌入
                    context.Response.Headers.Remove("X-Frame-Options");
                    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                    {
                        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors *");
                    }
                }
                else
                {
                    // 其他所有路由端点禁止被 iframe 嵌入
                    if (!context.Response.Headers.ContainsKey("X-Frame-Options"))
                    {
                        context.Response.Headers.Append("X-Frame-Options", "DENY");
                    }
                    if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
                    {
                        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'none'");
                    }
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}
