namespace FleetManagement.Domain.Entities
{
	public class Unit : BaseEntity
	{
		public string Code { get; set; } = null!;        // Birlik/Bölük Kodu
		public string Name { get; set; } = null!;        // Birlik/Bölük Adı
		public string? ParentName { get; set; }          // Üst Birlik (şimdilik text)
		public string? Description { get; set; }         // Açıklama
	}
}