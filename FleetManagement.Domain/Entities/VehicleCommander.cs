namespace FleetManagement.Domain.Entities
{
    public class VehicleCommander : BaseEntity
    {
        public string? CommanderNumber { get; set; }   // uniq index var diye boş bırakma, UI zaten zorunlu
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }       // ✅ nullable
        public string? UnitName { get; set; }          // ✅ nullable

        public override string ToString() => FullName ?? "";
    }
}