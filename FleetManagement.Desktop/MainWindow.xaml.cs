using FleetManagement.Desktop.Services;
using FleetManagement.Infrastructure.Data;
using System.Windows;
using System.Windows.Controls;
using System;

namespace FleetManagement.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
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

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext(App.DbOptions);

                await DriverAutoDeleteService.RunAsync(db);
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    "DriverAutoDelete",
                    "Otomatik sürücü temizleme hatası.",
                    ex);
            }

            NavigateTo("HomePage");
        }
    }
}