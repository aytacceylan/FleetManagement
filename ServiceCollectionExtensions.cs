using System;
using FleetManagement.Application.Interfaces;
using FleetManagement.Infrastructure.Data;
using FleetManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FleetManagement.Infrastructure
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
		{
			if (services is null) throw new ArgumentNullException(nameof(services));
			if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

			// DbContext
			services.AddDbContext<AppDbContext>(options =>
				options.UseNpgsql(connectionString));

			// Repositories
			services.AddScoped<IVehicleRepository, VehicleRepository>();

			return services;
		}
	}
}
