namespace FleetManagement.Desktop
{
	public class VehicleMovementRow
	{
		public int Id { get; set; }

		public int DailyNo { get; set; }
		public string? Driver { get; set; }
		public string Plate { get; set; } = "";
		public string ExitTimeText { get; set; } = "";
		public string ReturnTimeText { get; set; } = "";
		public string? VehicleBrand { get; set; }
		public string Status { get; set; } = "";
		public string DateText { get; set; } = "";
		public string? Route { get; set; }
		public string? Commander { get; set; }
		public string? Purpose { get; set; }

		public int? DoneKm { get; set; }
		public string KmText => DoneKm is null ? "—" : DoneKm.Value.ToString();

		// 👇 EKLENECEK
		public string? LoadOrPassengerInfo { get; set; }
		public string? Description { get; set; }

		public DateTime ExitDateTimeUtc { get; set; }
		public DateTime? ReturnDateTimeUtc { get; set; }

	}
}