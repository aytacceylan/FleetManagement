using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Desktop.Services;

public static class DriverAutoDeleteService
{
    public static async Task RunAsync(AppDbContext db)
    {
        try
        {
            var today = DateTime.Today;

            var drivers = await db.Drivers
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            bool changed = false;

            foreach (var driver in drivers)
            {
                var name = (driver.FullName ?? "").ToUpperInvariant();

                bool temporary =
                    name.Contains(" ER ")
                    || name.StartsWith("ER ")
                    || name.Contains(" ONB ")
                    || name.StartsWith("ONB ")
                    || name.Contains(" ÇVŞ ")
                    || name.StartsWith("ÇVŞ ");

                if (!temporary)
                    continue;

                if (driver.CreatedAt.Date.AddMonths(7) <= today)
                {
                    driver.IsDeleted = true;
                    changed = true;

                    AppLogger.Info(
                        "DriverAutoDelete",
                        $"Görev süresi dolduğu için otomatik silindi. Id:{driver.Id}, Ad:{driver.FullName}");
                }
            }

            if (changed)
                await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Error(
                "DriverAutoDelete",
                "Otomatik sürücü silme işlemi sırasında hata oluştu.",
                ex);

            throw;
        }
    }
}