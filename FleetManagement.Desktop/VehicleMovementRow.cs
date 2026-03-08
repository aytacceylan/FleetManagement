using System;
using System.Windows.Media;

namespace FleetManagement.Desktop.Pages
{
	public sealed class VehicleMovementRow
	{
		public int Id { get; set; }

		public int DailyNo { get; set; }

		public string? Driver { get; set; }
		public string Plate { get; set; } = "";

		public string ExitTimeText { get; set; } = "";
		public string ReturnTimeText { get; set; } = "";

		public string? VehicleBrand { get; set; }

		public string Status { get; set; } = "";
		public Brush StatusBrush { get; set; } = Brushes.Black;

		public string DateText { get; set; } = "";

		public string? Route { get; set; }
		public string? Commander { get; set; }
		//public string? Purpose { get; set; }

		public int? DoneKm { get; set; }
		public string KmText => DoneKm is null ? "—" : DoneKm.Value.ToString();

		//public string? LoadOrPassengerInfo { get; set; }
		//public string? Description { get; set; }

		public DateTime ExitDateTimeUtc { get; set; }
		public DateTime? ReturnDateTimeUtc { get; set; }

		public string? Departure { get; set; }
		public int? PassengerCount { get; set; }
		public int? LoadAmount { get; set; }
		public string? DutyType { get; set; }

	}
}