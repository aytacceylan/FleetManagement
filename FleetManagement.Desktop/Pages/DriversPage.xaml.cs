using System;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class DriversPage : Page
	{
		public DriversPage()
		{
			InitializeComponent();

			// Sunum modu: boş liste
			DriversGrid.ItemsSource = Array.Empty<object>();
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			// TODO: DB'den listeyi çek
			FormInfo.Text = "Yenilendi (yakında DB bağlanacak).";
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			FormInfo.Text = "Yeni kayıt için form hazır.";
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Kaydet
			FormInfo.Text = "Kaydet (yakında).";
		}

		private void Delete_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Sil
			FormInfo.Text = "Sil (yakında).";
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			FormInfo.Text = "Form temizlendi.";
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// TODO: filtre
		}

		private void DriversGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// TODO: seçilen satırı forma doldur
		}

		private void ClearForm()
		{
			DriverNumberBox.Text = string.Empty;
			FullNameBox.Text = string.Empty;
			PhoneBox.Text = string.Empty;
		}
	}
}