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
				// ===== DASHBOARD =====
				"DashboardPage" => new Pages.DashboardPage(),

				// ===== TANIMLAMALAR =====
				"VehiclesPage" => new Pages.VehicleView(),
				"DriversPage" => new Pages.DriverView(),
				"CommandersPage" => new Pages.CommanderView(),

				"RoutesPage" => new Pages.PlaceholderView("Tanımlamalar > Route"),
				"VehicleTypesPage" => new Pages.PlaceholderView("Tanımlamalar > Araç Tipi"),
				"CategoriesPage" => new Pages.PlaceholderView("Tanımlamalar > Kategori"),
				"ModelsPage" => new Pages.PlaceholderView("Tanımlamalar > Model"),
				"UnitsPage" => new Pages.PlaceholderView("Tanımlamalar > Birlik/Bölük-Kısım"),

				// ===== İŞLEMLER =====
				"VehicleMovementsPage" => new Pages.VehicleMovementView(),
				"VehicleMaintenancesPage" => new Pages.PlaceholderView("İşlemler/Faaliyet > Araç Bakım/Yağlama"),

				// ===== RAPORLAR =====
				"QueriesPage" => new Pages.PlaceholderView("Sorgulama/Raporlar > Sorgulamalar"),
				"ReportsPage" => new Pages.PlaceholderView("Sorgulama/Raporlar > Raporlar"),

				// ===== AYARLAR =====
				"AuthorizationPage" => new Pages.PlaceholderView("Ayarlar > Yetkilendirme"),

				// ===== YARDIM =====
				"HelpPage" => new Pages.PlaceholderView("Yardım"),

				// ===== DEFAULT =====
				_ => new Pages.DashboardPage()
			};
		}
	}
}