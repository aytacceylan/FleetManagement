using FleetManagement.Application.DTOs;
using FleetManagement.Application.Interfaces;
using FleetManagement.Domain.Entities;

namespace FleetManagement.Application.Services;

public class VehicleService : IVehicleService
{
	private readonly IVehicleRepository _repository;

	public VehicleService(IVehicleRepository repository)
	{
		_repository = repository;
	}

	public async Task<VehicleDto?> GetByIdAsync(int id)
	{
		var vehicle = await _repository.GetByIdAsync(id);
		if (vehicle == null) return null;

		return new VehicleDto
		{
			Id = vehicle.Id,
			Plate = vehicle.Plate,
			Brand = vehicle.Brand,
			Model = vehicle.Model
		};
	}

	public async Task<IReadOnlyList<VehicleDto>> GetAllAsync()
	{
		var vehicles = await _repository.GetAllAsync();

		return vehicles
			.Select(v => new VehicleDto
			{
				Id = v.Id,
				Plate = v.Plate,
				Brand = v.Brand,
				Model = v.Model
			})
			.ToList();
	}

	public async Task AddAsync(VehicleDto dto)
	{
		var vehicle = new Vehicle
		{
			Plate = dto.Plate,
			Brand = dto.Brand,
			Model = dto.Model
		};

		await _repository.AddAsync(vehicle);
	}
}