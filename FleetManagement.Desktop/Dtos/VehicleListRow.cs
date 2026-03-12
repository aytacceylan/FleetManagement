using System.Windows.Media;

namespace FleetManagement.Desktop.Dtos
{
	public class VehicleListRow
	{
		public int Id { get; set; }

		public string? Plate { get; set; }
		public string? InventoryNumber { get; set; }

		public string? DriverFullName { get; set; }

		public string? VehicleCategory { get; set; }
		public string? VehicleType { get; set; }
		public string? Model { get; set; }

		public int? PassengerCapacity { get; set; }
		public int? VehicleKm { get; set; }
		public string? VehicleUnit { get; set; }

        public string? VehicleSituation { get; set; }

        public Brush VehicleSituationBrush { get; set; } = Brushes.Black;


        // Formda var, listede sağa doğru gösterilecek
        public string? Brand { get; set; }

		public int? VehicleYear { get; set; }

		public string? MotorNo { get; set; } = null;


		public string? SaseNo { get; set; }
		public int? LoadCapacity { get; set; }


        // bakım hesabı için (gridde göstermeyeceğiz)

        public string? MaintenanceStatus { get; set; }
        public int? MaintenanceIntervalKm { get; set; }
		public int? MaintenanceIntervalMonths { get; set; }
		public int? LastMaintenanceKm { get; set; }
		public DateTime? LastMaintenanceDate { get; set; }

		public Brush? MaintenanceBrush { get; set; }


	}
}