using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleCommandersPage : Page
	{
		public VehicleCommandersPage()
		{
			InitializeComponent();

			// Sunum modu
			CommandersGrid.ItemsSource = Array.Empty<object>();
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			// TODO: DB'den yeniden çek
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("Yeni Komutan (yakında)", "Bilgi");
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// TODO: filtre
		}
	}
}