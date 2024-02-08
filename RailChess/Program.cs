using Microsoft.AspNetCore.SignalR;
using RailChess.Models.DbCtx;
using RailChess.Play;
using RailChess.Play.Services;
using RailChess.Services;
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
    builder.Services.AddSingleton<PlayInvokeInfoFilter>();
    builder.Services.AddScoped<PlayEventsService>();
    builder.Services.AddScoped<PlayService>();
    
    

    string localVueCors = "localVueCors";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(localVueCors, builder =>
        {
            builder.WithOrigins("http://127.0.0.1:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            builder.WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    //app.UseHttpsRedirection();
    app.UseFileServer();
    app.UseCors(localVueCors);

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

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