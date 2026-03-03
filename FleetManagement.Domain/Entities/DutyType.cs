namespace FleetManagement.Domain.Entities
{
    public class DutyType : BaseEntity
    {
        public string? Code { get; set; }   // artık opsiyonel
        public string Name { get; set; } = null!;  // zorunlu
        public string? Description { get; set; }
    }
}