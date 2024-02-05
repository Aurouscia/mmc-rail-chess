using RailChess.Models.DbCtx;
using RailChess.Services;
using Serilog;


try
{
    var builder = WebApplication.CreateBuilder(args);
    var c = builder.Configuration;

    builder.Services.AddSerilog(c);
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddScoped<HttpUserIdProvider>();
    builder.Services.AddScoped<HttpUserInfoService>();
    builder.Services.AddDb(c);
    builder.Services.AddJwtService(c);
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiExceptionFilter>();
    });
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
    app.UseStaticFiles();
    app.UseCors(localVueCors);

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

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