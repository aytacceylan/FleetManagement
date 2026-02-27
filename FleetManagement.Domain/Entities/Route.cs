namespace FleetManagement.Domain.Entities
{
    public class Route : BaseEntity
    {
        public string? Code { get; set; } = null!;     // Rota Kodu (zorunlu, unique önerilir)
        public string? Name { get; set; } = null!;     // Rota Adı (zorunlu)

        public string? StartPoint { get; set; }       // Başlangıç
        public string? EndPoint { get; set; }         // Bitiş
        public string? Description { get; set; }      // Açıklama
    }
}