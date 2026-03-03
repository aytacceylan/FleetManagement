using System;

namespace FleetManagement.Domain.Entities
{
    public class HelpNote : BaseEntity
    {
        public string Title { get; set; } = null!;     // zorunlu
        public string? Content { get; set; }           // opsiyonel
    }
}