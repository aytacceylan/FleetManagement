using FleetManagement.Domain.Entities;

namespace FleetManagement.Domain.Entities
{
    public class VehicleYear : BaseEntity
    {
        public int Year { get; set; }
        public string? Note { get; set; } // opsiyonel (istersen kaldır)
    }
}