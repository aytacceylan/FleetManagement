namespace FleetManagement.Domain.Entities
{
    public class Vehicle : BaseEntity
    {
		public int Id { get; set; }           // ✅ int

		public string Plate { get; set; } = string.Empty;
		public string Brand { get; set; } = string.Empty;

		public string Model { get; set; } = string.Empty;  // ✅ Model eklendi

		public bool IsDeleted { get; set; } = false;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	}
}

