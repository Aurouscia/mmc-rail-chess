﻿using Microsoft.EntityFrameworkCore;

namespace RailChess.Models
{
    public class RailChessContext : DbContext
    {
        public RailChessContext(DbContextOptions<RailChessContext> options) : base(options)
        { 
        }

        public DbSet<User> Users { get; set; }
    }
    public static class DbContextSetup
    {
        public static IServiceCollection AddDb(this IServiceCollection services, IConfiguration config)
        {
            var section = config.GetSection("Db");
            string connStr = section["ConnStr"] ?? throw new Exception("Db:ConnStr未填");

            services.AddDbContext<RailChessContext>(options =>
            {
                options.UseSqlite(connStr);
            });

            return services;
        }
    }
}
