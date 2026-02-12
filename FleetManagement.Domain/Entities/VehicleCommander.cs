namespace FleetManagement.Domain.Entities
{
	public class VehicleCommander : BaseEntity
	{
		public string CommanderNumber { get; set; } = null!;   // Araç komutanı no
		public string FullName { get; set; } = null!;
		public string PhoneNumber { get; set; } = null!;
		public string UnitName { get; set; } = null!;          // Bağlı olduğu birlik/birim
	}
}
