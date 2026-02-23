namespace FleetManagement.Desktop
{
	public class VehicleMovementRow
	{
		public int Id { get; set; }
		public string VehiclePlate { get; set; } = "";
		public string DriverName { get; set; } = "";
		public string CommanderName { get; set; } = "";
		public DateTime ExitDateTime { get; set; }
		public DateTime? ReturnDateTime { get; set; }
		public string Route { get; set; } = "";
		public string Purpose { get; set; } = "";
		public string KmInfo { get; set; } = "";
	}
}