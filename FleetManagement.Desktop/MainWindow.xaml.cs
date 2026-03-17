using FleetManagement.Desktop.Services;
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
                "HomePage" => new Pages.HomePage(),

                // =======================
                // Faaliyet / Raporlar
                // =======================
                "VehicleDispatchPreparePage" => new Pages.VehicleMovementsPage(),
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
                "DutyTypesPage" => new Pages.DutyTypesPage(),

                // =======================
                // Yardım
                // =======================
                "HelpPage" => new Pages.HelpPage(),

                // =======================
                // DEFAULT (ÇOK ÖNEMLİ)
                // =======================
                _ => HandleUnknownPage(tag)
            };
        }
        private Page HandleUnknownPage(string tag)
        {
            AppLogger.Error("Navigation", $"Bilinmeyen sayfa çağrıldı: {tag}", null);
            return new Pages.HomePage();
        }

    }
}