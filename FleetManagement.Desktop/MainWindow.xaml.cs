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
                "QueriesPage" => new Pages.PlaceholderPage("Faaliyet/Raporlar > Sorgula"),

                // =======================
                // Tanımlamalar
                // =======================
                "VehiclesPage" => new Pages.VehiclesPage(),
                "DriversPage" => new Pages.DriversPage(),
                "UnitsPage" => new Pages.UnitsPage(),
                "VehicleTypesPage" => new Pages.VehicleTypesPage(),

                "VehicleBrandsPage" => new Pages.PlaceholderPage("Tanımlamalar > Marka (yakında)"),
                "VehicleModelsPage" => new Pages.VehicleModelsPage(),
                "VehicleYearsPage" => new Pages.PlaceholderPage("Tanımlamalar > Araç Yılı (yakında)"),

                "RoutesPage" => new Pages.RoutesPage(),
                "VehicleCommandersPage" => new Pages.VehicleCommandersPage(),
                "MakamsPage" => new Pages.MakamsPage(),

                "VehicleGuardsPage" => new Pages.PlaceholderPage("Tanımlamalar > Araç Muhafızı (yakında)"),
                "DutyTypesPage" => new Pages.PlaceholderPage("Tanımlamalar > Görev Türü (yakında)"),

                // =======================
                // Yardım
                // =======================
                "HelpPage" => new Pages.PlaceholderPage("Yardım"),

                // =======================
                // Default
                // =======================
                _ => new Pages.DashboardPage()
            };
        }
    }
}