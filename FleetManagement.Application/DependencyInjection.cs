using FleetManagement.Application.Interfaces;
using FleetManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FleetManagement.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddScoped<IVehicleService, VehicleService>();
		return services;
	}
}


