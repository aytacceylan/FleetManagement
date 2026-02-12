namespace FleetManagement.Application.DTOs;

public class VehicleDto
{
	public int Id { get; set; }

	public string Plate { get; set; } = null!;

	public string Brand { get; set; } = null!;

	public string Model { get; set; } = null!;
}