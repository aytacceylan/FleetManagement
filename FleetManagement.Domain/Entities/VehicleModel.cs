namespace FleetManagement.Domain.Entities
{
	public class VehicleModel : BaseEntity
	{
		public string Code { get; set; } = null!;
		public string Name { get; set; } = null!;
		public string? Description { get; set; }
	}
}