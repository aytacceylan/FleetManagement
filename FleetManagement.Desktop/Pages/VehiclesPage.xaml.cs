using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehiclesPage 
	{
		public VehiclesPage()
		{
			InitializeComponent();

			// Sunum modu: örnek boş liste
			VehiclesGrid.ItemsSource = Array.Empty<object>();
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			// TODO: DB'den yeniden çek
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Yeni araç formu/dialog
			MessageBox.Show("Yeni Araç (yakında)", "Bilgi");
		}

		private void SearchPlateBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// TODO: filtre
		}

		private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// TODO: filtre
		}
	}
}