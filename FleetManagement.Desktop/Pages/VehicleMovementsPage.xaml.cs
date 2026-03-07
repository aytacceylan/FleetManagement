using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleMovementsPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<MovementRow> _all = new();

		private sealed class MovementRow
		{
			public int Id { get; set; }

			public int DailyNo { get; set; }
			public string? Driver { get; set; }
			public string Plate { get; set; } = "";
			public string ExitTimeText { get; set; } = "";
			public string ReturnTimeText { get; set; } = "";
			public string? VehicleBrand { get; set; }
			public string Status { get; set; } = "";
			public string DateText { get; set; } = "";
			public string? Route { get; set; }
			public string? Commander { get; set; }
			public string? Departure { get; set; }
			public string? KmText { get; set; }
			public int? PassengerCount { get; set; }
			public int? LoadAmount { get; set; }
			public string? DutyType { get; set; }

			public DateTime ExitDateTimeUtc { get; set; }
			public DateTime? ReturnDateTimeUtc { get; set; }
		}

		public VehicleMovementsPage()
		{
			InitializeComponent();

			ExitDatePicker.SelectedDate = DateTime.Today;
			ExitTimeBox.Text = "08:00";
			ReturnTimeBox.Text = "17:00";

			Loaded += async (_, __) =>
			{
				await LoadLookupsAsync();
				await LoadAsync();
				PrepareNewFormState();
			};
		}

		// =========================
		// LOOKUPS
		// =========================
		private async Task LoadLookupsAsync()
		{
			var vehicles = await _db.Vehicles.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Plate)
				.Select(x => new { x.Id, Display = x.Plate })
				.ToListAsync();

			VehicleCombo.ItemsSource = vehicles;
			VehicleCombo.DisplayMemberPath = "Display";
			VehicleCombo.SelectedValuePath = "Id";

			var drivers = await _db.Drivers.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.FullName)
				.Select(x => new { x.Id, Display = x.FullName })
				.ToListAsync();

			DriverCombo.ItemsSource = drivers;
			DriverCombo.DisplayMemberPath = "Display";
			DriverCombo.SelectedValuePath = "Id";

			var commanders = await _db.VehicleCommanders.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.FullName)
				.Select(x => new { x.Id, Display = x.FullName })
				.ToListAsync();

			CommanderCombo.ItemsSource = commanders;
			CommanderCombo.DisplayMemberPath = "Display";
			CommanderCombo.SelectedValuePath = "Id";

			var routes = await _db.Routes.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new { Display = x.Name })
				.ToListAsync();

			RouteCombo.ItemsSource = routes;
			RouteCombo.DisplayMemberPath = "Display";
			RouteCombo.SelectedValuePath = "Display";

			var departures = await _db.Departures.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Name)
				.Select(x => new { Display = x.Name })
				.ToListAsync();

			DepartureCombo.ItemsSource = departures;
			DepartureCombo.DisplayMemberPath = "Display";
			DepartureCombo.SelectedValuePath = "Display";

			var dutyTypes = await _db.DutyTypes.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Name)
				.Select(x => new { Display = x.Name })
				.ToListAsync();

			DutyTypeCombo.ItemsSource = dutyTypes;
			DutyTypeCombo.DisplayMemberPath = "Display";
			DutyTypeCombo.SelectedValuePath = "Display";

			var brands = await _db.VehicleBrands.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Name)
				.Select(x => new { Display = x.Name })
				.ToListAsync();

			VehicleBrandCombo.ItemsSource = brands;
			VehicleBrandCombo.DisplayMemberPath = "Display";
			VehicleBrandCombo.SelectedValuePath = "Display";
		}

		// =========================
		// LOAD GRID
		// =========================
		private async Task LoadAsync()
		{
			var raw = await _db.VehicleMovements.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.Include(x => x.Vehicle)
				.Include(x => x.Driver)
				.Include(x => x.VehicleCommander)
				.OrderByDescending(x => x.Id)
				.ToListAsync();

			var rows = raw.Select(m =>
			{
				var exitLocal = m.ExitDateTime.ToLocalTime();
				var retLocal = m.ReturnDateTime?.ToLocalTime();

				var parsed = ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);

				return new MovementRow
				{
					Id = m.Id,
					Driver = m.Driver?.FullName ?? m.DriverText,
					Plate = m.Vehicle?.Plate ?? m.VehiclePlateText ?? "",
					ExitTimeText = exitLocal.ToString("HH:mm"),
					ReturnTimeText = retLocal is null ? "—" : retLocal.Value.ToString("HH:mm"),
					VehicleBrand = GetVehicleBrandSafe(m.Vehicle),
					Status = CalcStatus(m.ExitDateTime, m.ReturnDateTime),
					DateText = exitLocal.ToString("dd.MM.yyyy"),
					Route = m.Route,
					Commander = m.VehicleCommander?.FullName ?? m.CommanderText,
					Departure = m.Purpose,
					KmText = CalcKmText(m.StartKm, m.EndKm),
					PassengerCount = parsed.passenger,
					LoadAmount = parsed.load,
					DutyType = m.Description,

					ExitDateTimeUtc = m.ExitDateTime,
					ReturnDateTimeUtc = m.ReturnDateTime
				};
			}).ToList();

			ApplyDailyNumbers(rows);

			_all = rows;
			MovementsGrid.ItemsSource = _all;
			UpdateCount(_all.Count);
		}

		private static void ApplyDailyNumbers(List<MovementRow> rows)
		{
			var groups = rows.GroupBy(x => x.ExitDateTimeUtc.ToLocalTime().Date).ToList();

			foreach (var group in groups)
			{
				var ordered = group.OrderBy(x => x.ExitDateTimeUtc).ThenBy(x => x.Id).ToList();
				for (int i = 0; i < ordered.Count; i++)
					ordered[i].DailyNo = i + 1;
			}
		}

		private void UpdateCount(int count)
		{
			FilterInfo.Text = $"Toplam kayıt: {count}";
		}

		// =========================
		// UI EVENTS
		// =========================
		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await LoadLookupsAsync();
			await LoadAsync();
			PrepareNewFormState();
			Notify("Yenilendi");
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			PrepareNewFormState();
			Notify("Yeni kayıt için form hazır");
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			PrepareNewFormState();
			Notify("Temizlendi");
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!TryBuildDateTime(ExitDatePicker, ExitTimeBox, out var exitDtLocal, out var err1))
				{
					Notify("Çıkış zamanı hatalı: " + err1, "Uyarı");
					return;
				}

				DateTime? returnDtLocal = null;
				if (!string.IsNullOrWhiteSpace(ReturnTimeBox.Text) || ReturnDatePicker.SelectedDate is not null)
				{
					if (!TryBuildNullableDateTime(ReturnDatePicker, ReturnTimeBox, out returnDtLocal, out var err2))
					{
						Notify("Dönüş zamanı hatalı: " + err2, "Uyarı");
						return;
					}
				}

				if (returnDtLocal is not null && returnDtLocal < exitDtLocal)
				{
					Notify("Dönüş zamanı çıkıştan önce olamaz.", "Uyarı");
					return;
				}

				var vehicleId = VehicleCombo.SelectedValue is int vid ? vid : (int?)null;
				var selectedPlateText = EmptyToNull(VehicleCombo.Text);

				if (vehicleId is null && string.IsNullOrWhiteSpace(selectedPlateText))
				{
					Notify("Plaka zorunlu. Listeden araç seçin.", "Uyarı");
					return;
				}

				// Aynı araçta açık görev kontrolü
				if (_selectedId is null && vehicleId is not null)
				{
					var hasOpenMovement = await _db.VehicleMovements.AnyAsync(m =>
						!m.IsDeleted &&
						m.VehicleId == vehicleId &&
						m.ReturnDateTime == null);

					if (hasOpenMovement)
					{
						MessageBox.Show(
							"Bu araç halen görevde. Yeni görev tanımlanamaz. Önce dönüş saatini girin.",
							"Uyarı",
							MessageBoxButton.OK,
							MessageBoxImage.Warning);
						return;
					}
				}

				var entity = _selectedId is null
					? new VehicleMovement { CreatedAt = DateTime.UtcNow, IsDeleted = false }
					: await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

				if (entity is null)
				{
					Notify("Kayıt bulunamadı.", "Uyarı");
					return;
				}

				var wasOpen = entity.ReturnDateTime is null;

				entity.VehicleId = vehicleId;
				entity.DriverId = DriverCombo.SelectedValue is int did ? did : (int?)null;
				entity.VehicleCommanderId = CommanderCombo.SelectedValue is int cid ? cid : (int?)null;

				entity.VehiclePlateText = vehicleId is null ? selectedPlateText : null;
				entity.DriverText = entity.DriverId is null ? EmptyToNull(DriverCombo.Text) : null;
				entity.CommanderText = entity.VehicleCommanderId is null ? EmptyToNull(CommanderCombo.Text) : null;

				entity.ExitDateTime = DateTime.SpecifyKind(exitDtLocal, DateTimeKind.Local).ToUniversalTime();
				entity.ReturnDateTime = returnDtLocal is null
					? null
					: DateTime.SpecifyKind(returnDtLocal.Value, DateTimeKind.Local).ToUniversalTime();

				// Mevcut entity ile geçici eşleme
				entity.Route = EmptyToNull(RouteCombo.Text);
				entity.Purpose = EmptyToNull(DepartureCombo.Text);      // Başkanlık
				entity.Description = EmptyToNull(DutyTypeCombo.Text);   // Görev Türü

				var passenger = TryParseNullableInt(PassengerCountBox.Text);
				var load = TryParseNullableInt(LoadAmountBox.Text);
				entity.LoadOrPassengerInfo = BuildLoadOrPassengerInfo(passenger, load);

				// Yapılan KM
				var doneKm = TryParseNullableInt(DoneKmBox.Text);
				if (vehicleId is not null)
				{
					var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId.Value && !v.IsDeleted);

					if (vehicle is not null)
					{
						var startKm = vehicle.VehicleKm ?? 0;
						entity.StartKm = startKm;
						entity.EndKm = doneKm is null ? null : startKm + doneKm.Value;

						// Marka comboda gösterim amaçlı; entity'de ayrı alan yok
						if (string.IsNullOrWhiteSpace(VehicleBrandCombo.Text))
							VehicleBrandCombo.Text = GetVehicleBrandSafe(vehicle) ?? "";
					}
					else
					{
						entity.StartKm = null;
						entity.EndKm = null;
					}
				}
				else
				{
					entity.StartKm = null;
					entity.EndKm = null;
				}

				if (_selectedId is null)
					_db.VehicleMovements.Add(entity);

				// Araç KM artırma: sadece açık görev ilk kez kapanırken
				if (wasOpen && entity.ReturnDateTime is not null && entity.VehicleId is not null && doneKm is not null && doneKm >= 0)
				{
					var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.VehicleId.Value && !v.IsDeleted);
					if (vehicle is not null)
						vehicle.VehicleKm = (vehicle.VehicleKm ?? 0) + doneKm.Value;
				}

				await _db.SaveChangesAsync();

				Notify(_selectedId is null
					? $"Kaydedildi: #{entity.Id}"
					: $"Güncellendi: #{entity.Id}");

				await LoadAsync();
				ClearForm();
				PrepareNewFormState();
			}
			catch (Exception ex)
			{
				Notify("Hata: kaydetme başarısız.", "Hata");
				MessageBox.Show(ex.ToString(), "Hata (detay)");
			}
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_selectedId is null)
				{
					Notify("Silmek için kayıt seç.", "Uyarı");
					return;
				}

				var confirm = MessageBox.Show("Seçili hareket silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes)
					return;

				var entity = await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null)
				{
					Notify("Kayıt bulunamadı.", "Uyarı");
					return;
				}

				entity.IsDeleted = true;
				await _db.SaveChangesAsync();

				Notify($"Silindi: #{_selectedId.Value}");
				await LoadAsync();
				ClearForm();
				PrepareNewFormState();
			}
			catch (Exception ex)
			{
				Notify("Hata: silme başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private async void MovementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MovementsGrid.SelectedItem is not MovementRow row)
				return;

			var m = await _db.VehicleMovements.AsNoTracking()
				.Include(x => x.Vehicle)
				.FirstOrDefaultAsync(x => x.Id == row.Id && !x.IsDeleted);

			if (m is null) return;

			_selectedId = m.Id;

			VehicleCombo.SelectedValue = m.VehicleId;
			DriverCombo.SelectedValue = m.DriverId;
			CommanderCombo.SelectedValue = m.VehicleCommanderId;

			ExitDatePicker.SelectedDate = m.ExitDateTime.ToLocalTime().Date;
			ExitTimeBox.Text = m.ExitDateTime.ToLocalTime().ToString("HH:mm");

			ReturnDatePicker.SelectedDate = m.ReturnDateTime?.ToLocalTime().Date;
			ReturnTimeBox.Text = m.ReturnDateTime is null ? "" : m.ReturnDateTime.Value.ToLocalTime().ToString("HH:mm");

			RouteCombo.Text = m.Route ?? "";
			DepartureCombo.Text = m.Purpose ?? "";
			DutyTypeCombo.Text = m.Description ?? "";

			var parsed = ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);
			PassengerCountBox.Text = parsed.passenger?.ToString() ?? "";
			LoadAmountBox.Text = parsed.load?.ToString() ?? "";

			if (m.StartKm.HasValue && m.EndKm.HasValue && m.EndKm.Value >= m.StartKm.Value)
				DoneKmBox.Text = (m.EndKm.Value - m.StartKm.Value).ToString();
			else
				DoneKmBox.Text = "";

			VehicleBrandCombo.Text = GetVehicleBrandSafe(m.Vehicle) ?? "";
			StatusBox.Text = CalcStatus(m.ExitDateTime, m.ReturnDateTime);
			DailyNoBox.Text = row.DailyNo.ToString();
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

			if (string.IsNullOrWhiteSpace(q))
			{
				MovementsGrid.ItemsSource = _all;
				UpdateCount(_all.Count);
				return;
			}

			var filtered = _all.Where(x =>
				(x.Driver ?? "").ToLowerInvariant().Contains(q) ||
				(x.Plate ?? "").ToLowerInvariant().Contains(q) ||
				(x.VehicleBrand ?? "").ToLowerInvariant().Contains(q) ||
				(x.Status ?? "").ToLowerInvariant().Contains(q) ||
				(x.Route ?? "").ToLowerInvariant().Contains(q) ||
				(x.Commander ?? "").ToLowerInvariant().Contains(q) ||
				(x.Departure ?? "").ToLowerInvariant().Contains(q) ||
				(x.DutyType ?? "").ToLowerInvariant().Contains(q))
				.ToList();

			MovementsGrid.ItemsSource = filtered;
			UpdateCount(filtered.Count);
		}

		// =========================
		// HELPERS
		// =========================
		private void ClearForm()
		{
			_selectedId = null;
			MovementsGrid.SelectedItem = null;

			VehicleCombo.SelectedIndex = -1;
			DriverCombo.SelectedIndex = -1;
			CommanderCombo.SelectedIndex = -1;
			RouteCombo.SelectedIndex = -1;
			DepartureCombo.SelectedIndex = -1;
			DutyTypeCombo.SelectedIndex = -1;
			VehicleBrandCombo.SelectedIndex = -1;

			DailyNoBox.Text = "";
			StatusBox.Text = "Başlamadı";

			ExitDatePicker.SelectedDate = DateTime.Today;
			ExitTimeBox.Text = "08:00";
			ReturnDatePicker.SelectedDate = null;
			ReturnTimeBox.Text = "";

			DoneKmBox.Text = "";
			PassengerCountBox.Text = "";
			LoadAmountBox.Text = "";
		}

		private void PrepareNewFormState()
		{
			DailyNoBox.Text = GetNextDailyNo().ToString();
			StatusBox.Text = "Başlamadı";
		}

		private int GetNextDailyNo()
		{
			var today = DateTime.Today;

			var max = _all
				.Where(x => x.ExitDateTimeUtc.ToLocalTime().Date == today)
				.Select(x => (int?)x.DailyNo)
				.Max() ?? 0;

			return max + 1;
		}

		private static string? EmptyToNull(string? value)
		{
			var v = (value ?? "").Trim();
			return string.IsNullOrWhiteSpace(v) ? null : v;
		}

		private static int? TryParseNullableInt(string? text)
		{
			var t = (text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(t)) return null;
			return int.TryParse(t, out var v) ? v : null;
		}

		private static bool TryBuildDateTime(DatePicker datePicker, TextBox timeBox, out DateTime value, out string error)
		{
			error = "";
			value = default;

			if (datePicker.SelectedDate is not DateTime date)
			{
				error = "Tarih seçilmedi.";
				return false;
			}

			var timeText = (timeBox.Text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(timeText))
				timeText = "00:00";

			if (!TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time))
			{
				error = "Saat formatı geçersiz. Örnek: 08:30";
				return false;
			}

			value = date.Date.Add(time);
			return true;
		}

		private static bool TryBuildNullableDateTime(DatePicker datePicker, TextBox timeBox, out DateTime? value, out string error)
		{
			error = "";
			value = null;

			if (datePicker.SelectedDate is not DateTime date)
				return true;

			var timeText = (timeBox.Text ?? "").Trim();
			if (string.IsNullOrWhiteSpace(timeText))
				timeText = "00:00";

			if (!TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out var time))
			{
				error = "Saat formatı geçersiz. Örnek: 17:15";
				return false;
			}

			value = date.Date.Add(time);
			return true;
		}

		private static string CalcStatus(DateTime exitUtc, DateTime? returnUtc)
		{
			if (returnUtc is not null)
				return "Tamamlandı";

			var exitLocal = exitUtc.ToLocalTime();
			var now = DateTime.Now;

			if (exitLocal.Date > now.Date)
				return "Planlandı";

			if (exitLocal.Date == now.Date && exitLocal.TimeOfDay > now.TimeOfDay)
				return "Başlamadı";

			return "Devam Ediyor";
		}

		private static string? GetVehicleBrandSafe(Vehicle? v)
		{
			if (v is null) return null;

			// Projende hangisi aktifse onu bırak:
			// return v.Brand;
			return v.VehicleBrand;
		}

		private static string? CalcKmText(int? startKm, int? endKm)
		{
			if (startKm is null || endKm is null) return "—";
			var diff = endKm.Value - startKm.Value;
			return diff >= 0 ? diff.ToString() : "—";
		}

		private static string? BuildLoadOrPassengerInfo(int? passenger, int? load)
		{
			if (passenger is null && load is null) return null;
			return $"Yolcu:{passenger?.ToString() ?? ""};Yük:{load?.ToString() ?? ""}";
		}

		private static (int? passenger, int? load) ParseLoadOrPassengerInfo(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return (null, null);

			int? passenger = null;
			int? load = null;

			var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries);
			foreach (var part in parts)
			{
				var item = part.Trim();

				if (item.StartsWith("Yolcu:", StringComparison.OrdinalIgnoreCase))
				{
					var val = item.Substring("Yolcu:".Length).Trim();
					if (int.TryParse(val, out var p))
						passenger = p;
				}
				else if (item.StartsWith("Yük:", StringComparison.OrdinalIgnoreCase))
				{
					var val = item.Substring("Yük:".Length).Trim();
					if (int.TryParse(val, out var l))
						load = l;
				}
			}

			return (passenger, load);
		}

		private static void Notify(string message, string title = "Bilgi")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}