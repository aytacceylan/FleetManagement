using System.Windows;
using FleetManagement.Application.Interfaces;

namespace FleetManagement.Desktop
{
	public partial class MainWindow : Window
	{
		private readonly IVehicleService _vehicleService;

		public MainWindow()
		{
			InitializeComponent();

			_vehicleService =
				(IVehicleService)App.Services.GetService(typeof(IVehicleService))!;
		}
	}
}


