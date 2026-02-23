using FleetManagement.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using FleetManagement.Application;

namespace FleetManagement.Desktop
{
	public partial class App : System.Windows.Application
	{
		public static IServiceProvider Services { get; private set; } = null!;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

	
			var connectionString = "Host=127.0.0.1;Port=5432;Database=FleetDb;Username=postgres;Password=1234";

			var services = new ServiceCollection();

			services.AddApplication();       // Service katmanı
			services.AddInfrastructure(connectionString); // Db + repo

			Services = services.BuildServiceProvider();


		}


	}
}


