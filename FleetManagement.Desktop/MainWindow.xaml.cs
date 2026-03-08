using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigateTo("HomePage");
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
                // =======================
                // Ana Sayfa
                // =======================
                "HomePage" => new Pages.DashboardPage(),

                // =======================
                // Faaliyet / Raporlar
                // =======================
                "VehicleDispatchPreparePage" => new Pages.VehicleMovementsPage(), // şimdilik sevk hazırlaya bağlı
                "QueriesPage" => new Pages.VehicleMovementReportsPage(),

                // =======================
                // Tanımlamalar
                // =======================
                "VehiclesPage" => new Pages.VehiclesPage(),
                "DriversPage" => new Pages.DriversPage(),
                "UnitsPage" => new Pages.UnitsPage(),
                "VehicleTypesPage" => new Pages.VehicleTypesPage(),

                "VehicleBrandsPage" => new Pages.VehicleBrandsPage(),
				"VehicleModelsPage" => new Pages.VehicleModelsPage(),
                "VehicleYearsPage" => new Pages.VehicleYearsPage(),

				"RoutesPage" => new Pages.RoutesPage(),
                "VehicleCommandersPage" => new Pages.VehicleCommandersPage(),
                "DeparturesPage" => new Pages.DeparturesPage(),

                "VehicleGuardsPage" => new Pages.VehicleGuardsPage(),
                "DutyTypesPage" => new Pages.DutyTypesPage(),

                // =======================
                // Yardım
                // =======================
                "HelpPage" => new Pages.HelpPage(),

                // =======================
                // Default
                // =======================
                _ => new Pages.DashboardPage()
            };
        }
    }
}