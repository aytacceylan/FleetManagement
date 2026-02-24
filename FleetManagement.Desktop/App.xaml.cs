using System.Windows;

namespace FleetManagement.Desktop
{
    public partial class MainWindow : Window
    {

        private void NavigateToDashboard()
        {
            HeaderTitleText.Text = "Dashboard";
            HeaderSubtitleText.Text = "Genel görünüm";
            MainFrame.Navigate(new Pages.DashboardPage());
        }

        private void NavVehicles_Click(object sender, RoutedEventArgs e)
        {
            HeaderTitleText.Text = "Araçlar";
            HeaderSubtitleText.Text = "Araç tanımları ve liste";
            MainFrame.Navigate(new Pages.VehiclesPage());
        }

        private void NavDrivers_Click(object sender, RoutedEventArgs e)
        {
            HeaderTitleText.Text = "Sürücüler";
            HeaderSubtitleText.Text = "Sürücü tanımları ve liste";
            MainFrame.Navigate(new Pages.DriversPage());
        }

        private void NavCommanders_Click(object sender, RoutedEventArgs e)
        {
            HeaderTitleText.Text = "Araç Komutanı";
            HeaderSubtitleText.Text = "Araç-komutan atama";
            MainFrame.Navigate(new Pages.VehicleCommandersPage());
        }

        private void NavMovements_Click(object sender, RoutedEventArgs e)
        {
            HeaderTitleText.Text = "Araç Hareketi";
            HeaderSubtitleText.Text = "Giriş/çıkış ve hareket kayıtları";
            MainFrame.Navigate(new Pages.VehicleMovementsPage());
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            HeaderTitleText.Text = "Ayarlar";
            HeaderSubtitleText.Text = "Uygulama ayarları";
            MainFrame.Navigate(new Pages.SettingsPage());
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Mevcut sayfayı yenilemek için basit yöntem
            var current = MainFrame.Content;
            if (current != null)
                MainFrame.Navigate(current);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

