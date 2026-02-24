using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // açılış
            SetActiveSidebarButton(BtnDashboard);
            Navigate("Dashboard");
        }

        private void SidebarNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            SetActiveSidebarButton(btn);

            var key = btn.Tag?.ToString() ?? "Dashboard";
            Navigate(key);
        }

        private void SetActiveSidebarButton(Button active)
        {
            // Sidebar içindeki tüm butonları bul
            var buttons = FindVisualChildren<Button>(this)
                .Where(b => b.Tag is not null); // sadece menü butonlarını hedefle

            foreach (var b in buttons)
            {
                b.Style = (Style)FindResource("SidebarButtonStyle");
            }

            active.Style = (Style)FindResource("SidebarButtonActiveStyle");
        }

        private void Navigate(string key)
        {
            switch (key)
            {
                case "Dashboard":
                    HeaderTitleText.Text = "Dashboard";
                    HeaderSubtitleText.Text = "Genel görünüm";
                    MainFrame.Navigate(new Pages.DashboardPage());
                    break;

                case "Vehicles":
                    HeaderTitleText.Text = "Araçlar";
                    HeaderSubtitleText.Text = "Araç tanımları ve liste";
                    MainFrame.Navigate(new Pages.VehiclesPage());
                    break;

                case "Drivers":
                    HeaderTitleText.Text = "Sürücüler";
                    HeaderSubtitleText.Text = "Sürücü tanımları ve liste";
                    MainFrame.Navigate(new Pages.DriversPage());
                    break;

                case "Commanders":
                    HeaderTitleText.Text = "Araç Komutanı";
                    HeaderSubtitleText.Text = "Araç-komutan atama";
                    MainFrame.Navigate(new Pages.VehicleCommandersPage());
                    break;

                case "Movements":
                    HeaderTitleText.Text = "Araç Hareketi";
                    HeaderSubtitleText.Text = "Giriş/çıkış ve hareket kayıtları";
                    MainFrame.Navigate(new Pages.VehicleMovementsPage());
                    break;

                case "Settings":
                    HeaderTitleText.Text = "Ayarlar";
                    HeaderSubtitleText.Text = "Uygulama ayarları";
                    MainFrame.Navigate(new Pages.SettingsPage());
                    break;
            }
        }

        // Visual tree helper
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);

                if (child is T t) yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}