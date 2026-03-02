using FleetManagement.Domain.Entities;

namespace FleetManagement.Domain.Entities
{
    public class VehicleBrand : BaseEntity
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}