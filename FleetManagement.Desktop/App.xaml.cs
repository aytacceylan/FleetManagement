using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FleetManagement.Infrastructure.Data;

namespace FleetManagement.Desktop
{
    public partial class App
    {
        public static DbContextOptions<AppDbContext> DbOptions { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var cs = config.GetConnectionString("FleetDb")
                     ?? throw new InvalidOperationException("ConnectionStrings:FleetDb bulunamadı.");

            DbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs)
                .Options;
        }
    }
}