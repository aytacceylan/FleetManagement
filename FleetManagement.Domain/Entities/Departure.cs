namespace FleetManagement.Domain.Entities
{
    public class Departure : BaseEntity
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}