using System;

namespace FleetManagement.Domain.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }

        // Envanter/araç no (010000)
        public string Plate { get; set; } = null!;
          
        // Gerçek plaka (34 AAA 34)
        public string? InventoryNumber { get; set; }

        public int? AssignedDriverId { get; set; }
        public Driver? AssignedDriver { get; set; }

        public string? Brand { get; set; }
        public string? Model { get; set; }

        public string? VehicleType { get; set; }
        public string? VehicleCategory { get; set; }
        public string? VehicleUnit { get; set; }

        public string? MotorNo { get; set; }
        public string? SaseNo { get; set; }

        public int? VehicleKm { get; set; }
        public int? PassengerCapacity { get; set; }
        public int? LoadCapacity { get; set; }

        public string? VehicleSituation { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}

