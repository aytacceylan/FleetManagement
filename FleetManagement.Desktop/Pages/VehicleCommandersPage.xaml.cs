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
	public partial class VehicleCommandersPage : Page
	{
		private int? _selectedId;

		public VehicleCommandersPage()
		{
			InitializeComponent();
			Loaded += async (_, __) => await LoadAsync();
		}

		private async Task LoadAsync(string? q = null)
		{
			using var db = Db.Create<AppDbContext>();

			var query = db.VehicleCommanders.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(q))
			{
				q = q.Trim();
				query = query.Where(x =>
					x.CommanderNumber.Contains(q) ||
					x.FullName.Contains(q) ||
					x.UnitName.Contains(q));
			}

			var list = await query
				.OrderByDescending(x => x.Id)
				.ToListAsync();

			GridCommanders.ItemsSource = list;
			FormInfo.Text = $"Kayıt: {list.Count}";
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			_selectedId = null;
			CommanderNumberBox.Text = "";
			FullNameBox.Text = "";
			PhoneBox.Text = "";
			UnitBox.Text = "";
			GridCommanders.UnselectAll();
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			var no = CommanderNumberBox.Text?.Trim();
			var name = FullNameBox.Text?.Trim();
			var phone = PhoneBox.Text?.Trim();
			var unit = UnitBox.Text?.Trim();

			if (string.IsNullOrWhiteSpace(no) ||
				string.IsNullOrWhiteSpace(name))
			{
				MessageBox.Show("Komutan No ve Ad Soyad zorunludur.");
				return;
			}

			using var db = Db.Create<AppDbContext>();

			if (_selectedId is null)
			{
				db.VehicleCommanders.Add(new VehicleCommander
				{
					CommanderNumber = no!,
					FullName = name!,
					PhoneNumber = phone ?? "",
					UnitName = unit ?? "",
					CreatedAt = DateTime.UtcNow
				});
			}
			else
			{
				var entity = await db.VehicleCommanders
					.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

				if (entity is null) return;

				entity.CommanderNumber = no!;
				entity.FullName = name!;
				entity.PhoneNumber = phone ?? "";
				entity.UnitName = unit ?? "";
			}

			await db.SaveChangesAsync();
			await LoadAsync(SearchBox.Text);
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedId is null) return;

			if (MessageBox.Show("Seçili komutan silinsin mi?",
				"Onay", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				return;

			using var db = Db.Create<AppDbContext>();

			var entity = await db.VehicleCommanders
				.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

			if (entity is null) return;

			db.VehicleCommanders.Remove(entity);
			await db.SaveChangesAsync();

			New_Click(sender, e);
			await LoadAsync(SearchBox.Text);
		}

		private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (GridCommanders.SelectedItem is not VehicleCommander c) return;

			_selectedId = c.Id;
			CommanderNumberBox.Text = c.CommanderNumber;
			FullNameBox.Text = c.FullName;
			PhoneBox.Text = c.PhoneNumber;
			UnitBox.Text = c.UnitName;
		}

		private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
			=> await LoadAsync(SearchBox.Text);
	}
}