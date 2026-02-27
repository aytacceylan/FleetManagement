namespace FleetManagement.Desktop.Dtos
{
    public class VehicleListRow
    {
        public int Id { get; set; }

        public string? Plate { get; set; }
        public string? InventoryNumber { get; set; }

        public string? DriverName { get; set; }

        public string? VehicleType { get; set; }
        public string? VehicleCategory { get; set; }

        public string? Model { get; set; }
        public string? MotorNo { get; set; }
        public string? SaseNo { get; set; }
    }
}