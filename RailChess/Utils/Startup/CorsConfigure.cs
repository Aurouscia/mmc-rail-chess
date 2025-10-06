using RailChess.Utils.Startup;

namespace RailChess.Utils.Startup
{
    public static class CorsConfigure
    {
        public const string corsPolicyName = "Cors";
        public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration config)
        {
            var originsConfig = config.GetSection("Cors");
            var origins = new List<string>();
            originsConfig.Bind(origins);
            services.AddCors(options =>
            {
                options.AddPolicy(corsPolicyName, b =>
                {
                    b.WithOrigins([..origins])
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders(nameof(HttpResponse.Headers.Authorization));
                });
            });
            return services;
        }
        public static WebApplication UseConfiguredCors(this WebApplication app)
        {
            app.UseCors(corsPolicyName);
            return app;
        }
    }
}
