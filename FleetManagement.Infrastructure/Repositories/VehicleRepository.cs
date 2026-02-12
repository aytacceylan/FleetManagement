using FleetManagement.Application.Interfaces;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Infrastructure.Repositories
{
	public class VehicleRepository : IVehicleRepository
	{
		private readonly AppDbContext _context;

		public VehicleRepository(AppDbContext context)
		{
			_context = context;
		}

		public async Task<Vehicle?> GetByIdAsync(int id)
		{
			return await _context.Vehicles
				.AsNoTracking()
				.FirstOrDefaultAsync(v => v.Id == id); // ✅ int == int
		}

		public async Task<IReadOnlyList<Vehicle>> GetAllAsync()
		{
			return await _context.Vehicles
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task AddAsync(Vehicle vehicle)
		{
			await _context.Vehicles.AddAsync(vehicle);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Vehicle vehicle)
		{
			_context.Vehicles.Update(vehicle);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var vehicle = await _context.Vehicles.FindAsync(id);
			if (vehicle != null)
			{
				_context.Vehicles.Remove(vehicle);
				await _context.SaveChangesAsync();
			}
		}
	}
}
