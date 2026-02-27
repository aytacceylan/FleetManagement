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
				// ===== Dashboard =====
				"DashboardPage" => new Pages.DashboardPage(),

				// ===== 1) Tanımlamalar =====
				"VehiclesPage" => new Pages.VehiclesPage(),
				"DriversPage" => new Pages.DriversPage(),
				"CommandersPage" => new Pages.VehicleCommandersPage(),

                "RoutesPage" => new Pages.RoutesPage(),
                "VehicleTypesPage" => new Pages.VehicleTypesPage(),
                "CategoriesPage" => new Pages.VehicleCategoriesPage(),
                "ModelsPage" => new Pages.PlaceholderPage("Tanımlamalar > Model"),
				"UnitsPage" => new Pages.PlaceholderPage("Tanımlamalar > Birlik/Bölük-Kısım"),

				// ===== 2) İşlemler/Faaliyet =====
				"VehicleMovementsPage" => new Pages.VehicleMovementsPage(),
				"VehicleMaintenancesPage" => new Pages.PlaceholderPage("İşlemler/Faaliyet > Araç Bakım/Yağlama"),

				// ===== 3) Sorgulama/Raporlar =====
				"QueriesPage" => new Pages.PlaceholderPage("Sorgulama/Raporlar > Sorgulamalar"),
				"ReportsPage" => new Pages.PlaceholderPage("Sorgulama/Raporlar > Raporlar"),

				// ===== 4) Ayarlar =====
				"AuthorizationPage" => new Pages.PlaceholderPage("Ayarlar > Yetkilendirme"),

				// ===== 5) Yardım =====
				"HelpPage" => new Pages.PlaceholderPage("Yardım"),

				// ===== Default =====
				_ => new Pages.DashboardPage()
			};
		}
	}
}