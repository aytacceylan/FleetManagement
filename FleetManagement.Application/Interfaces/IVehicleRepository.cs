using FleetManagement.Domain.Entities;

namespace FleetManagement.Application.Interfaces
{
	public interface IVehicleRepository
	{
		Task<Vehicle?> GetByIdAsync(int id);      // ✅ int
		Task<IReadOnlyList<Vehicle>> GetAllAsync();
		Task AddAsync(Vehicle vehicle);
		Task UpdateAsync(Vehicle vehicle);
		Task DeleteAsync(int id);                // ✅ int
	}
}
