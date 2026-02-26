using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			NavigateTo("DashboardPage");
		}

		private void Menu_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag is string tag)
				NavigateTo(tag);
		}

		private void NavigateTo(string tag)
		{
			ContentFrame.Content = tag switch
			{
				"DashboardPage" => new Views.DashboardView(),

				"VehiclesPage" => new Views.VehicleView(),
				"DriversPage" => new Views.DriverView(),
				"CommandersPage" => new Views.CommanderView(),

				"VehicleMovementsPage" => new Views.VehicleMovementView(),

				// henüz yoksa dashboard'a düş
				"RoutesPage" => new Views.DashboardView(),
				"VehicleTypesPage" => new Views.DashboardView(),
				"CategoriesPage" => new Views.DashboardView(),
				"ModelsPage" => new Views.DashboardView(),
				"UnitsPage" => new Views.DashboardView(),
				"VehicleMaintenancesPage" => new Views.DashboardView(),
				"QueriesPage" => new Views.DashboardView(),
				"ReportsPage" => new Views.DashboardView(),
				"AuthorizationPage" => new Views.DashboardView(),
				"HelpPage" => new Views.DashboardView(),

				_ => new Views.DashboardView()
			};
		}
	}
}