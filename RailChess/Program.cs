using Microsoft.AspNetCore.SignalR;
using RailChess.Models.DbCtx;
using RailChess.Play;
using RailChess.Play.Services;
using RailChess.Services;
using RailChess.Utils.Startup;
using Serilog;


try
{
    var builder = WebApplication.CreateBuilder(args);
    var c = builder.Configuration;

    builder.Services.AddSerilog(c);
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddScoped<HttpUserIdProvider>();
    builder.Services.AddScoped<HttpUserInfoService>();
    builder.Services.AddDb(c);
    builder.Services.AddJwtService(c);
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiExceptionFilter>();
    });
    builder.Services.AddSignalR(options =>
    {
        options.AddFilter<PlayInvokeInfoFilter>();
    });
    builder.Services.AddPlayLogics();

    builder.Services.AddCors(c);

    var app = builder.Build();

    app.UseConfiguredCors();
    app.UseFileServer();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    app.MapHub<PlayHub>("play");
    app.MapControllerRoute(
        name: "default",
        pattern: "api/{controller=Home}/{action=Index}/{id?}");

    Log.Information("启动成功=============================================");
    app.Run();
}
catch (Exception ex)
{
    if (ex is not HostAbortedException)
        Log.Error(ex, "启动失败=============================================");
}
finally
{
    Log.CloseAndFlush();
}