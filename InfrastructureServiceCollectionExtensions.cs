using Microsoft.Extensions.DependencyInjection;

namespace FleetManagement.Infrastructure
{
	public static class InfrastructureServiceCollectionExtensions
	{
		/// <summary>
		/// Minimal placeholder extension to allow passing a connection string from the WPF startup.
		/// Replace or extend this implementation with the real infrastructure registrations.
		/// </summary>
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
		{
			// TODO: register infrastructure services (DbContext, repositories, etc.) using the provided connection string.
			return services;
		}
	}
}