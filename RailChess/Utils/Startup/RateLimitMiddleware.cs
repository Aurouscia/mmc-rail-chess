using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace RailChess.Utils.Startup;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private const int spanSec = 30;
    private const int maxReq = 15;

    public RateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 必须是有endpoint才限流
        var endpoint = context.GetEndpoint();
        if (endpoint is null) { await _next(context); return; }

        // 如果是对于 SignalR 的请求：直接放行
        var hubMetadata = endpoint.Metadata.GetMetadata<HubMetadata>();
        if(hubMetadata is not null) {
            await _next(context);
            return;
        }
        
        var ip = GetClientIp(context);
        var span = TimeSpan.FromSeconds(spanSec);

        // 原子操作：拿到或创建该IP+该endpoint的时间戳队列
        var key = $"ratelimit-{ip}-{endpoint.DisplayName}";
        var queue = _cache.GetOrCreate<Queue<DateTime>>(key, entry =>
        {
            entry.SlidingExpiration = span;   // 10 秒无访问就自动回收
            return new Queue<DateTime>(maxReq);
        })!;

        lock (queue)   // 队列实例维度锁，粒度足够小
        {
            // 踢掉过期时间戳
            while (queue.Count > 0 && DateTime.UtcNow - queue.Peek() > span)
                queue.Dequeue();

            // 超过阈值 => 短路
            if (queue.Count >= maxReq)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return;
            }

            queue.Enqueue(DateTime.UtcNow);
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext ctx)
    {
        var ip = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(ip))
            return ip.Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public static class RateLimitExtensions
{
    public static IApplicationBuilder UseIpRateLimit(this IApplicationBuilder builder)
        => builder.UseMiddleware<RateLimitMiddleware>();
}