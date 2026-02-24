using FleetManagement.Desktop.Data;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleMovementsPage : Page
	{
		private int? _selectedId;

		// Grid için küçük DTO
		private sealed class MovementRow
		{
			public int Id { get; set; }
			public string? VehicleDisplay { get; set; }
			public string? DriverDisplay { get; set; }
			public string? CommanderDisplay { get; set; }
			public DateTime ExitDateTime { get; set; }
			public DateTime? ReturnDateTime { get; set; }
			public string? Route { get; set; }
			public string? Purpose { get; set; }
			public int? StartKm { get; set; }
			public int? EndKm { get; set; }
		}

		public VehicleMovementsPage()
		{
			InitializeComponent();
			Loaded += async (_, __) =>
			{
				await LoadCombosAsync();
				await LoadGridAsync();
			};
		}

		private async Task LoadCombosAsync()
		{
			using var db = Db.Create<AppDbContext>();

			VehicleCombo.ItemsSource = await db.Vehicles.AsNoTracking().OrderBy(x => x.Plate).ToListAsync();
			DriverCombo.ItemsSource = await db.Drivers.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
			CommanderCombo.ItemsSource = await db.VehicleCommanders.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
		}

		private async Task LoadGridAsync(string? q = null)
		{
			using var db = Db.Create<AppDbContext>();

			var query = db.VehicleMovements
				.AsNoTracking()
				.Include(x => x.Vehicle)
				.Include(x => x.Driver)
				.Include(x => x.VehicleCommander)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(q))
			{
				q = q.Trim();
				query = query.Where(x =>
					(x.Vehicle != null && x.Vehicle.Plate.Contains(q)) ||
					(x.Driver != null && x.Driver.FullName.Contains(q)) ||
					(x.VehicleCommander != null && x.VehicleCommander.FullName.Contains(q)) ||
					(x.VehiclePlateText != null && x.VehiclePlateText.Contains(q)) ||
					(x.DriverText != null && x.DriverText.Contains(q)) ||
					(x.CommanderText != null && x.CommanderText.Contains(q)) ||
					(x.Route != null && x.Route.Contains(q)) ||
					(x.Purpose != null && x.Purpose.Contains(q)));
			}

			var list = await query
				.OrderByDescending(x => x.Id)
				.Select(x => new MovementRow
				{
					Id = x.Id,
					VehicleDisplay = x.Vehicle != null ? x.Vehicle.Plate : x.VehiclePlateText,
					DriverDisplay = x.Driver != null ? x.Driver.FullName : x.DriverText,
					CommanderDisplay = x.VehicleCommander != null ? x.VehicleCommander.FullName : x.CommanderText,
					ExitDateTime = x.ExitDateTime,
					ReturnDateTime = x.ReturnDateTime,
					Route = x.Route,
					Purpose = x.Purpose,
					StartKm = x.StartKm,
					EndKm = x.EndKm
				})
				.ToListAsync();

			GridMovements.ItemsSource = list;
			FormInfo.Text = $"Kayıt: {list.Count}";
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			_selectedId = null;

			VehicleCombo.SelectedItem = null;
			VehicleCombo.Text = "";

			DriverCombo.SelectedItem = null;
			DriverCombo.Text = "";

			CommanderCombo.SelectedItem = null;
			CommanderCombo.Text = "";

			ReturnDatePicker.SelectedDate = null;
			ReturnTimeBox.Text = "";

			RouteBox.Text = "";
			PurposeBox.Text = "";
			DescriptionBox.Text = "";
			LoadInfoBox.Text = "";
			StartKmBox.Text = "";
			EndKmBox.Text = "";

			GridMovements.UnselectAll();
			FormInfo.Text = "Yeni kayıt";
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			using var db = Db.Create<AppDbContext>();

			VehicleMovement entity;

			if (_selectedId is null)
			{
				entity = new VehicleMovement
				{
					ExitDateTime = DateTime.Now,   // ✅ kayıt anı
					CreatedAt = DateTime.UtcNow
				};
				db.VehicleMovements.Add(entity);
			}
			else
			{
				entity = await db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null) { MessageBox.Show("Kayıt bulunamadı."); return; }
				// ExitDateTime güncellemiyoruz (istediğin gibi kayıt anı kalsın)
			}

			// ✅ Araç: seçiliyse FK, değilse serbest metin
			if (VehicleCombo.SelectedItem is Vehicle vSel)
			{
				entity.VehicleId = vSel.Id;
				entity.VehiclePlateText = null;
			}
			else
			{
				var text = (VehicleCombo.Text ?? "").Trim();
				entity.VehicleId = null;
				entity.VehiclePlateText = string.IsNullOrWhiteSpace(text) ? null : text;
			}

			// ✅ Sürücü
			if (DriverCombo.SelectedItem is Driver dSel)
			{
				entity.DriverId = dSel.Id;
				entity.DriverText = null;
			}
			else
			{
				var text = (DriverCombo.Text ?? "").Trim();
				entity.DriverId = null;
				entity.DriverText = string.IsNullOrWhiteSpace(text) ? null : text;
			}

			// ✅ Komutan
			if (CommanderCombo.SelectedItem is VehicleCommander cSel)
			{
				entity.VehicleCommanderId = cSel.Id;
				entity.CommanderText = null;
			}
			else
			{
				var text = (CommanderCombo.Text ?? "").Trim();
				entity.VehicleCommanderId = null;
				entity.CommanderText = string.IsNullOrWhiteSpace(text) ? null : text;
			}

			// ✅ ReturnDateTime: DatePicker + opsiyonel saat
			entity.ReturnDateTime = BuildReturnDateTime(ReturnDatePicker.SelectedDate, ReturnTimeBox.Text);

			// Opsiyonel alanlar
			entity.Route = NullIfWhite(RouteBox.Text);
			entity.Purpose = NullIfWhite(PurposeBox.Text);
			entity.Description = NullIfWhite(DescriptionBox.Text);
			entity.LoadOrPassengerInfo = NullIfWhite(LoadInfoBox.Text);

			entity.StartKm = TryInt(StartKmBox.Text);
			entity.EndKm = TryInt(EndKmBox.Text);

			await db.SaveChangesAsync();

			await LoadGridAsync(SearchBox.Text);
			FormInfo.Text = "Kaydedildi.";
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			if (_selectedId is null) { MessageBox.Show("Silmek için kayıt seç."); return; }

			if (MessageBox.Show("Seçili hareket silinsin mi?", "Onay",
				MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

			using var db = Db.Create<AppDbContext>();
			var entity = await db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
			if (entity is null) return;

			db.VehicleMovements.Remove(entity);
			await db.SaveChangesAsync();

			New_Click(sender, e);
			await LoadGridAsync(SearchBox.Text);
		}

		private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
			=> await LoadGridAsync(SearchBox.Text);

		private async void GridMovements_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (GridMovements.SelectedItem is not MovementRow row) return;

			_selectedId = row.Id;

			// Edit ekranını doldurmak için entity’yi çekelim (FK/text ayrımı için)
			using var db = Db.Create<AppDbContext>();
			var entity = await db.VehicleMovements
				.Include(x => x.Vehicle)
				.Include(x => x.Driver)
				.Include(x => x.VehicleCommander)
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == row.Id);

			if (entity is null) return;

			// Araç
			if (entity.VehicleId is not null)
			{
				VehicleCombo.SelectedValue = entity.VehicleId.Value;
			}
			else
			{
				VehicleCombo.SelectedItem = null;
				VehicleCombo.Text = entity.VehiclePlateText ?? "";
			}

			// Sürücü
			if (entity.DriverId is not null)
			{
				DriverCombo.SelectedValue = entity.DriverId.Value;
			}
			else
			{
				DriverCombo.SelectedItem = null;
				DriverCombo.Text = entity.DriverText ?? "";
			}

			// Komutan
			if (entity.VehicleCommanderId is not null)
			{
				CommanderCombo.SelectedValue = entity.VehicleCommanderId.Value;
			}
			else
			{
				CommanderCombo.SelectedItem = null;
				CommanderCombo.Text = entity.CommanderText ?? "";
			}

			// Dönüş tarihi
			if (entity.ReturnDateTime.HasValue)
			{
				ReturnDatePicker.SelectedDate = entity.ReturnDateTime.Value.Date;
				ReturnTimeBox.Text = entity.ReturnDateTime.Value.ToString("HH:mm");
			}
			else
			{
				ReturnDatePicker.SelectedDate = null;
				ReturnTimeBox.Text = "";
			}

			RouteBox.Text = entity.Route ?? "";
			PurposeBox.Text = entity.Purpose ?? "";
			DescriptionBox.Text = entity.Description ?? "";
			LoadInfoBox.Text = entity.LoadOrPassengerInfo ?? "";
			StartKmBox.Text = entity.StartKm?.ToString() ?? "";
			EndKmBox.Text = entity.EndKm?.ToString() ?? "";

			FormInfo.Text = $"Seçili Id: {_selectedId} • Çıkış: {entity.ExitDateTime:dd.MM.yyyy HH:mm}";
		}

		private static DateTime? BuildReturnDateTime(DateTime? date, string? timeText)
		{
			if (date is null) return null;

			var t = (timeText ?? "").Trim();
			if (string.IsNullOrWhiteSpace(t))
				return date.Value.Date;

			// HH:mm parse
			if (TimeSpan.TryParseExact(t, @"hh\:mm", CultureInfo.InvariantCulture, out var ts) ||
				TimeSpan.TryParse(t, out ts))
			{
				return date.Value.Date + ts;
			}

			// Saat formatı yanlışsa sadece tarihi al
			return date.Value.Date;
		}

		private static string? NullIfWhite(string? s)
		{
			var t = (s ?? "").Trim();
			return string.IsNullOrWhiteSpace(t) ? null : t;
		}

		private static int? TryInt(string? s)
		{
			var t = (s ?? "").Trim();
			if (string.IsNullOrWhiteSpace(t)) return null;
			return int.TryParse(t, out var v) ? v : null;
		}
	}
}