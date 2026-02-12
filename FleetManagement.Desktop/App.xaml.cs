using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace FleetManagement.Desktop
{
	public partial class App : System.Windows.Application
	{
		public static IServiceProvider Services { get; private set; } = null!;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

	
			var connectionString = "Host=::1;Port=5432;Database=FleetDb;Username=postgres;Password=1234";

			var services = new ServiceCollection();
			services.AddInfrastructure(connectionString);
			Services = services.BuildServiceProvider();

			
		}


	}
}


