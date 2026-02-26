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
				"DashboardPage" => new Pages.DashboardPage(),

				"VehiclesPage" => new Pages.VehiclesPage(),
				"DriversPage" => new Pages.DriversPage(),
				"CommandersPage" => new Pages.VehicleCommandersPage(),

				"VehicleMovementsPage" => new Views.VehicleMovementView(),

				// henüz yoksa dashboard'a düş
				"RoutesPage" => new Views.PlaceholderView("Tanımlamalar > Route"),
				"VehicleTypesPage" => new Views.PlaceholderView("Tanımlamalar > Araç Tipi"),
				"CategoriesPage" => new Views.PlaceholderView("Tanımlamalar > Kategori"),
				"ModelsPage" => new Views.PlaceholderView("Tanımlamalar > Model"),
				"UnitsPage" => new Views.PlaceholderView("Tanımlamalar > Birlik/Bölük-Kısım"),

				"VehicleMaintenancesPage" => new Views.PlaceholderView("İşlemler/Faaliyet > Araç Bakım/Yağlama"),

				"QueriesPage" => new Views.PlaceholderView("Sorgulama/Raporlar > Sorgulamalar"),
				"ReportsPage" => new Views.PlaceholderView("Sorgulama/Raporlar > Raporlar"),

				"AuthorizationPage" => new Views.PlaceholderView("Ayarlar > Yetkilendirme"),
				"HelpPage" => new Views.PlaceholderView("Yardım"),

				_ => new Views.DashboardView()
			};
		}
	}
}