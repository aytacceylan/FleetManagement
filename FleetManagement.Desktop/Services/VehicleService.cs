using FleetManagement.Domain.Entities;
using System;
using System.Windows.Media;

namespace FleetManagement.Desktop.Services
{
    public static class VehicleService
    {
        public static string GetPlanningStatus(Vehicle vehicle, VehicleMovement? movement)
        {
            if (vehicle == null)
                return "Müsait";

            var situation = vehicle.VehicleSituation ?? "Müsait";

            // Araç zaten görevde veya bakımdaysa
            if (situation != "Müsait")
                return situation;

            if (movement == null)
                return "Müsait";

            var exit = movement.ExitDateTime.ToLocalTime();

            return exit > DateTime.Now
                ? "Planlandı"
                : "Görev Gecikti";
        }

        public static Brush GetPlanningBrush(string status)
        {
            return status switch
            {
                "Planlandı" => Brushes.DodgerBlue,
                "Görev Gecikti" => Brushes.Red,
                "Görevde" => Brushes.Green,
                "Müsait" => Brushes.Black,
                _ => Brushes.Black
            };
        }
    }
}