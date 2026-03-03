using FleetManagement.Domain.Entities;

namespace FleetManagement.Domain.Entities
{
    public class VehicleGuard : BaseEntity
    {
        public string? GuardNumber { get; set; }
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
    }
}