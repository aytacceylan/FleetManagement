using FleetManagement.Application.DTOs;

namespace FleetManagement.Application.Interfaces;

public interface IVehicleService
{
	Task<VehicleDto?> GetByIdAsync(int id);
	Task<IReadOnlyList<VehicleDto>> GetAllAsync();
	Task AddAsync(VehicleDto dto);
}
