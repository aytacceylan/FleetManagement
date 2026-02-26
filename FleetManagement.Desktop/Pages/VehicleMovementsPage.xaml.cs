using System;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleMovementsPage : Page
	{
		public VehicleMovementsPage()
		{
			InitializeComponent();

			// Sunum modu: boş liste/combobox
			MovementsGrid.ItemsSource = Array.Empty<object>();
			VehicleCombo.ItemsSource = Array.Empty<object>();
			DriverCombo.ItemsSource = Array.Empty<object>();
			CommanderCombo.ItemsSource = Array.Empty<object>();
		}

		private void RefreshMovements_Click(object sender, RoutedEventArgs e)
		{
			MovementInfo.Text = "Yenilendi (yakında DB bağlanacak).";
		}

		private void SaveMovement_Click(object sender, RoutedEventArgs e)
		{
			MovementInfo.Text = "Kaydet (yakında).";
		}
	}
}