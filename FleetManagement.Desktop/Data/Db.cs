using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace FleetManagement.Desktop.Data
{
    public static class Db
    {
        public static TContext Create<TContext>() where TContext : DbContext
        {
            // appsettings.json Desktop output'a kopyalanmalı
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var cs = config.GetConnectionString("FleetDb");

            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("ConnectionStrings:FleetDb bulunamadı.");

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseNpgsql(cs);

            // DbContext'in (Infrastructure'daki) ctor'u DbContextOptions almalı:
            // public FleetDbContext(DbContextOptions<FleetDbContext> options) : base(options) {}
            return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
        }
    }
}