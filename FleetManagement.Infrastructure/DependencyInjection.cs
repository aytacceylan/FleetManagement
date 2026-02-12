using Microsoft.Extensions.DependencyInjection;

namespace FleetManagement.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructureServices(
			this IServiceCollection services,
			string connectionString)
		{
			// Şimdilik boş
			return services;
		}
	}
}