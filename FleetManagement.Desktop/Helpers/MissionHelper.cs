using FleetManagement.Domain.Entities;

namespace FleetManagement.Application.Helpers
{
    public static class MissionHelper
    {
        /// <summary>
        /// Görev halen aktif mi?
        /// </summary>
        public static bool IsActiveMission(VehicleMovement movement)
        {
            return movement.Status == "Planlandı"
                || movement.Status == "Görevde";
        }

        public static bool IsMissionInProgress(VehicleMovement movement)
        {
            return movement.Status == "Görevde";
        }

        /// <summary>
        /// Görev kapanmış mı?
        /// </summary>
        public static bool IsClosedMission(VehicleMovement movement)
        {
            return movement.Status == "Tamamlandı"
                || movement.Status == "İptal";
        }
    }
}