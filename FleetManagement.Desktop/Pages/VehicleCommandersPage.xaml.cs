using System;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleCommandersPage : Page
	{
		public VehicleCommandersPage()
		{
			InitializeComponent();
			CommandersGrid.ItemsSource = Array.Empty<object>();
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
			CommanderNumberBox.Text = string.Empty;
			FullNameBox.Text = string.Empty;
			PhoneBox.Text = string.Empty;
			FormInfo.Text = "Form temizlendi.";
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// TODO: filtre
		}

		private void CommandersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// TODO: seçilen satırı forma doldur
		}
	}
}