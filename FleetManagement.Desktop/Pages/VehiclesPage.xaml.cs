using System;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehiclesPage : Page
	{
		public VehiclesPage()
		{
			InitializeComponent();

			VehiclesGrid.ItemsSource = Array.Empty<object>();
			VehicleTypeCombo.ItemsSource = Array.Empty<object>();
			CategoryCombo.ItemsSource = Array.Empty<object>();
			ModelCombo.ItemsSource = Array.Empty<object>();
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			FormInfo.Text = "Yenilendi (yakında DB bağlanacak).";
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			FormInfo.Text = "Kaydet (yakında).";
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			FormInfo.Text = "Sil (yakında).";
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			PlateBox.Text = string.Empty;
			StatusCombo.SelectedIndex = 0;
			VehicleTypeCombo.SelectedIndex = -1;
			CategoryCombo.SelectedIndex = -1;
			ModelCombo.SelectedIndex = -1;

			FormInfo.Text = "Form temizlendi.";
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// TODO: filtre
		}

		private void VehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// TODO: seçilen satırı forma doldur
		}
	}
}