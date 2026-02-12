namespace FleetManagement.Domain.Entities
{
	public class VehicleMovement : BaseEntity
	{
		// 🔗 İLİŞKİLER (FK)
		public int VehicleId { get; set; }
		public Vehicle Vehicle { get; set; } = null!;

		public int DriverId { get; set; }
		public Driver Driver { get; set; } = null!;

		public int VehicleCommanderId { get; set; }
		public VehicleCommander VehicleCommander { get; set; } = null!;

		// 🕒 TARİH / SAAT
		public DateTime ExitDateTime { get; set; }
		public DateTime? ReturnDateTime { get; set; }

		// 📍 GÖREV BİLGİLERİ
		public string Route { get; set; } = null!;
		public string Purpose { get; set; } = null!;
		public string? Description { get; set; }

		// 📦 YÜK / YOLCU
		public string? LoadOrPassengerInfo { get; set; }

		// 🚗 KM
		public int StartKm { get; set; }
		public int? EndKm { get; set; }
	}
}
