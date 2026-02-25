namespace FleetManagement.Domain.Entities
{
	public class Driver : BaseEntity
	{
		public string DriverNumber { get; set; } = null!;   // Sicil / sürücü no
		public string FullName { get; set; } = null!;
		public string PhoneNumber { get; set; } = null!;

        public override string ToString() => FullName;

    }
}
