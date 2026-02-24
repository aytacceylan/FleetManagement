using FleetManagement.Desktop.Data;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class DriversPage : Page
	{
		private int? _selectedId;

		public DriversPage()
		{
			InitializeComponent();
			Loaded += async (_, __) => await LoadAsync();
		}

		private async Task LoadAsync(string? q = null)
		{
			using var db = Db.Create<AppDbContext>();

			var query = db.Drivers.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(q))
			{
				q = q.Trim();
				query = query.Where(x =>
					x.DriverNumber.Contains(q) ||
					x.FullName.Contains(q) ||
					x.PhoneNumber.Contains(q));
			}

			var list = await query
				.OrderByDescending(x => x.Id)
				.ToListAsync();

			GridDrivers.ItemsSource = list;
			FormInfo.Text = $"Kayıt: {list.Count}";
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			_selectedId = null;
			DriverNumberBox.Text = "";
			FullNameBox.Text = "";
			PhoneBox.Text = "";
			GridDrivers.UnselectAll();
			FormInfo.Text = "Yeni kayıt";
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			var driverNo = DriverNumberBox.Text?.Trim();
			var fullName = FullNameBox.Text?.Trim();
			var phone = PhoneBox.Text?.Trim();

			if (string.IsNullOrWhiteSpace(driverNo) ||
				string.IsNullOrWhiteSpace(fullName))
			{
				MessageBox.Show("Sürücü No ve Ad Soyad zorunludur.");
				return;
			}

			using var db = Db.Create<AppDbContext>();

			if (_selectedId is null)
			{
				db.Drivers.Add(new Driver
				{
					DriverNumber = driverNo!,
					FullName = fullName!,
					PhoneNumber = phone ?? "",
					CreatedAt = DateTime.UtcNow
				});
			}
			else
			{
				var entity = await db.Drivers.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null) return;

				entity.DriverNumber = driverNo!;
				entity.FullName = fullName!;
				entity.PhoneNumber = phone ?? "";
			}

			await db.SaveChangesAsync();
			await LoadAsync(SearchBox.Text);
			FormInfo.Text = "Kaydedildi.";
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedId is null)
			{
				MessageBox.Show("Silmek için kayıt seç.");
				return;
			}

			if (MessageBox.Show("Seçili sürücüyü silmek istiyor musun?",
				"Onay", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				return;

			using var db = Db.Create<AppDbContext>();
			var entity = await db.Drivers.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
			if (entity is null) return;

			db.Drivers.Remove(entity);
			await db.SaveChangesAsync();

			New_Click(sender, e);
			await LoadAsync(SearchBox.Text);
		}

		private void GridDrivers_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (GridDrivers.SelectedItem is not Driver d) return;

			_selectedId = d.Id;
			DriverNumberBox.Text = d.DriverNumber;
			FullNameBox.Text = d.FullName;
			PhoneBox.Text = d.PhoneNumber;
			FormInfo.Text = $"Seçili Id: {_selectedId}";
		}

		private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
			=> await LoadAsync(SearchBox.Text);
	}
}