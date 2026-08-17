namespace FleetManagement.Domain.Entities
{
    public class Driver : BaseEntity
    {
        public string DriverNumber { get; set; } = null!;   // Sicil / sürücü no
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public bool IsActive { get; set; }

        public bool IsExternal { get; set; }

        public string DriverSituation { get; set; } = "Müsait";

        public override string ToString() => FullName;
    }
}
