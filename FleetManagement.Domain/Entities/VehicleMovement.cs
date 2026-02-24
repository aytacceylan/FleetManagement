namespace FleetManagement.Domain.Entities
{
	public class VehicleMovement : BaseEntity
	{
		// 🔗 Opsiyonel ilişkiler
		public int? VehicleId { get; set; }
		public Vehicle? Vehicle { get; set; }

		public int? DriverId { get; set; }
		public Driver? Driver { get; set; }

		public int? VehicleCommanderId { get; set; }
		public VehicleCommander? VehicleCommander { get; set; }

		// ✍️ Elle girilen serbest metin alanları (FK seçilmezse burası dolacak)
		public string? VehiclePlateText { get; set; }
		public string? DriverText { get; set; }
		public string? CommanderText { get; set; }

		// 🕒 TARİH / SAAT
		public DateTime ExitDateTime { get; set; }          // Kaydederken = DateTime.Now
		public DateTime? ReturnDateTime { get; set; }       // Elle girilecek (opsiyonel)

		// 📍 GÖREV BİLGİLERİ (opsiyonel yapmak için ?)
		public string? Route { get; set; }
		public string? Purpose { get; set; }
		public string? Description { get; set; }

		// 📦 YÜK / YOLCU
		public string? LoadOrPassengerInfo { get; set; }

		// 🚗 KM (opsiyonel)
		public int? StartKm { get; set; }
		public int? EndKm { get; set; }
	}
}