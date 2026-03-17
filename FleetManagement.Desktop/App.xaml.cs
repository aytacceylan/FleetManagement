using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FleetManagement.Infrastructure.Data;
using FleetManagement.Desktop.Services;

namespace FleetManagement.Desktop
{
    public partial class App
    {
        public static DbContextOptions<AppDbContext> DbOptions { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 🔹 Log başlangıç
            AppLogger.Info("App.OnStartup", "Uygulama başlatıldı.");

            // 🔹 GLOBAL HATA YAKALAMA (DOĞRU YER)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            base.OnStartup(e);

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var cs = config.GetConnectionString("FleetDb")
                     ?? throw new InvalidOperationException("ConnectionStrings:FleetDb bulunamadı.");
            AppLogger.Info("App.OnStartup", "Connection string başarıyla alındı.");

            DbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(cs)
                .Options;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Info("App.OnExit", "Uygulama kapatıldı.");
            base.OnExit(e);
        }

        // 🔹 BURASI YENİ EKLENEN METHOD
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogger.Error("App.DispatcherUnhandledException", "Yakalanmamış hata.", e.Exception);

            MessageBox.Show(
                "Beklenmeyen bir hata oluştu. Detay log dosyasına yazıldı.",
                "Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}