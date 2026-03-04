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

			// Grid kolonları (istenen sıra için)
			public int DailyNo { get; set; }                 // Sıra No (günlük)
			public string? Driver { get; set; }              // Sürücü
			public string Plate { get; set; } = "";          // Plaka (zorunlu)
			public string ExitTimeText { get; set; } = "";   // Çıkış Saati
			public string ReturnTimeText { get; set; } = ""; // Dönüş Saati
			public string? VehicleBrand { get; set; }        // Araç Marka
			public string Status { get; set; } = "";         // GörevdeMi (Tamamlandı/Devam Ediyor/Başlamadı/Planlandı)
			public string DateText { get; set; } = "";       // Tarih (date)
			public string? Route { get; set; }               // Güzergah
			public string? Commander { get; set; }           // Araç Komutanı
			public string? Purpose { get; set; }             // (şimdilik burada)
			public int? DoneKm { get; set; }                 // Yapılan Km (End-Start)
			public string KmText => DoneKm is null ? "—" : DoneKm.Value.ToString();

			// iç hesaplar için
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
			};
		}

		// =========================
		// LOOKUPS (ComboBox)
		// =========================
		private async Task LoadLookupsAsync()
		{
			var vehicles = await _db.Vehicles.AsNoTracking()
				.Where(x => !x.IsDeleted)
				.OrderBy(x => x.Plate)
				.Select(x => new
				{
					x.Id,
					Display = x.Plate
				})
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
		}

		// =========================
		// LOAD GRID
		// =========================
		private async Task LoadAsync()
		{
			try
			{
				var raw = await _db.VehicleMovements.AsNoTracking()
					.Where(x => !x.IsDeleted)
					.Include(x => x.Vehicle)
					.Include(x => x.Driver)
					.Include(x => x.VehicleCommander)
					.OrderByDescending(x => x.Id)
					.ToListAsync();

				// Row mapping + status + brand
				var rows = raw.Select(m =>
				{
					var plate = m.Vehicle?.Plate ?? m.VehiclePlateText ?? "";

					// ✅ Marka alanı: burada TEK satırı projendeki property adına göre seç
					// 1) Eğer Vehicle'da Brand varsa:
					// var brand = m.Vehicle?.Brand;
					// 2) Eğer Vehicle'da VehicleBrand varsa:
					var brand = GetVehicleBrandSafe(m.Vehicle);

					var exitLocal = m.ExitDateTime.ToLocalTime();
					var retLocal = m.ReturnDateTime?.ToLocalTime();

					var status = CalcStatus(m.ExitDateTime, m.ReturnDateTime);

					int? doneKm = null;
					if (m.StartKm is not null && m.EndKm is not null)
					{
						var diff = m.EndKm.Value - m.StartKm.Value;
						if (diff >= 0) doneKm = diff;
					}

					return new MovementRow
					{
						Id = m.Id,
						Driver = m.Driver?.FullName ?? m.DriverText,
						Plate = plate,
						ExitTimeText = exitLocal.ToString("HH:mm"),
						ReturnTimeText = retLocal is null ? "—" : retLocal.Value.ToString("HH:mm"),
						VehicleBrand = brand,
						Status = status,
						DateText = exitLocal.ToString("dd.MM.yyyy"),
						Route = m.Route,
						Commander = m.VehicleCommander?.FullName ?? m.CommanderText,
						Purpose = m.Purpose,
						DoneKm = doneKm,



						ExitDateTimeUtc = m.ExitDateTime,
						ReturnDateTimeUtc = m.ReturnDateTime
					};
				}).ToList();

				// ✅ Günlük sıra: her gün 1..N (DB'ye alan eklemeden)
				ApplyDailyNumbers(rows);

				_all = rows;
				MovementsGrid.ItemsSource = _all;
				UpdateCount(_all.Count);
			}
			catch (Exception ex)
			{
				Notify("Hata: hareketler yüklenemedi.", "Hata");
				MessageBox.Show(ex.ToString(), "Hata (detay)");
			}
		}

		private static void ApplyDailyNumbers(List<MovementRow> rows)
		{
			// Aynı gün: ExitDateTimeUtc (local güne göre) sıralanıp 1..N
			var groups = rows
				.GroupBy(r => r.ExitDateTimeUtc.ToLocalTime().Date)
				.ToList();

			foreach (var g in groups)
			{
				var ordered = g.OrderBy(x => x.ExitDateTimeUtc).ThenBy(x => x.Id).ToList();
				for (int i = 0; i < ordered.Count; i++)
					ordered[i].DailyNo = i + 1;
			}
		}

		private void UpdateCount(int count) => FilterInfo.Text = $"Toplam kayıt: {count}";

		// =========================
		// UI EVENTS
		// =========================
		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await LoadLookupsAsync();
			await LoadAsync();
			Notify("Yenilendi");
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			Notify("Yeni kayıt için form hazır");
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			Notify("Temizlendi");
		}

		// =========================
		// SAVE
		// =========================
		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!TryBuildDateTime(ExitDatePicker, ExitTimeBox, out var exitDtLocal, out var err1))
				{
					Notify("Çıkış zamanı hatalı: " + err1, "Uyarı");
					return;
				}

				if (!TryBuildNullableDateTime(ReturnDatePicker, ReturnTimeBox, out var returnDtLocal, out var err2))
				{
					Notify("Dönüş zamanı hatalı: " + err2, "Uyarı");
					return;
				}

				if (returnDtLocal is not null && returnDtLocal < exitDtLocal)
				{
					Notify("Dönüş zamanı çıkıştan önce olamaz.", "Uyarı");
					return;
				}

				// ✅ Plaka zorunlu:
				var vehicleId = VehicleCombo.SelectedValue is int vid ? vid : (int?)null;
				var plateFree = EmptyToNull(VehiclePlateTextBox.Text);

				if (vehicleId is null && string.IsNullOrWhiteSpace(plateFree))
				{
					Notify("Plaka zorunlu. Araç seçin veya Serbest Plaka girin.", "Uyarı");
					return;
				}

				// ✅ Yeni görev engeli (aynı araç için açık görev varsa)
				if (_selectedId is null && vehicleId is not null)
				{
					var hasOpen = await _db.VehicleMovements.AnyAsync(m =>
						!m.IsDeleted &&
						m.VehicleId == vehicleId &&
						m.ReturnDateTime == null);

					if (hasOpen)
					{
						MessageBox.Show("Bu araç halen görevde. Yeni görev tanımlanamaz. Önce dönüş saatini girin.", "Uyarı");
						return;
					}
				}

				// entity load/create
				var entity = _selectedId is null
					? new VehicleMovement { CreatedAt = DateTime.UtcNow, IsDeleted = false }
					: await _db.VehicleMovements.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

				if (entity is null)
				{
					Notify("Kayıt bulunamadı.", "Uyarı");
					return;
				}

				// eski değerler (KM artırma için)
				var wasOpen = entity.ReturnDateTime is null;
				var oldStart = entity.StartKm;
				var oldEnd = entity.EndKm;
				var oldVehicleId = entity.VehicleId;

				// FK seçilirse ID bas, seçilmezse serbest metin bas
				entity.VehicleId = vehicleId;
				entity.DriverId = DriverCombo.SelectedValue is int did ? did : (int?)null;
				entity.VehicleCommanderId = CommanderCombo.SelectedValue is int cid ? cid : (int?)null;

				entity.VehiclePlateText = entity.VehicleId is null ? plateFree : null;
				entity.DriverText = entity.DriverId is null ? EmptyToNull(DriverTextBox.Text) : null;
				entity.CommanderText = entity.VehicleCommanderId is null ? EmptyToNull(CommanderTextBox.Text) : null;

				// UTC sakla
				var exitUtc = DateTime.SpecifyKind(exitDtLocal, DateTimeKind.Local).ToUniversalTime();
				DateTime? retUtc = null;
				if (returnDtLocal is not null)
					retUtc = DateTime.SpecifyKind(returnDtLocal.Value, DateTimeKind.Local).ToUniversalTime();

				entity.ExitDateTime = exitUtc;
				entity.ReturnDateTime = retUtc;

				entity.Route = EmptyToNull(RouteBox.Text);
				entity.Purpose = EmptyToNull(PurposeBox.Text);
				
				entity.LoadOrPassengerInfo = EmptyToNull(LoadOrPassengerBox.Text);

				entity.StartKm = TryParseNullableInt(StartKmBox.Text);
				entity.EndKm = TryParseNullableInt(EndKmBox.Text);

				// ✅ Yeni kayıt ekle
				if (_selectedId is null)
					_db.VehicleMovements.Add(entity);

				// ✅ KM otomatik artırma:
				// Sadece "görev kapanırken" (önceden Return null iken şimdi doldu) ve araç seçili ise
				if (wasOpen && entity.ReturnDateTime is not null && entity.VehicleId is not null)
				{
					// yapılankm = End - Start (negatifse uygulama)
					if (entity.StartKm.HasValue && entity.EndKm.HasValue)
					{
						var doneKm = entity.EndKm.Value - entity.StartKm.Value;
						if (doneKm >= 0)
						{
							// aynı entity editlenirken yanlışlıkla iki kez eklemeyi engeller:
							// sadece "kapanma anında" ekliyoruz (wasOpen)
							var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == entity.VehicleId.Value && !v.IsDeleted);
							if (vehicle is not null)
							{
								vehicle.VehicleKm = (vehicle.VehicleKm ?? 0) + doneKm;
							}
						}
					}
				}

				await _db.SaveChangesAsync();

				Notify(_selectedId is null ? $"Kaydedildi: #{entity.Id}" : $"Güncellendi: #{entity.Id}");
				await LoadAsync();
				ClearForm();
			}
			catch (Exception ex)
			{
				Notify("Hata: kaydetme başarısız.", "Hata");
				MessageBox.Show(ex.ToString(), "Hata (detay)");
			}
		}

		// =========================
		// DELETE (soft)
		// =========================
		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_selectedId is null)
				{
					Notify("Silmek için listeden kayıt seç.", "Uyarı");
					return;
				}

				var confirm = MessageBox.Show("Seçili hareket silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes) return;

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
			}
			catch (Exception ex)
			{
				Notify("Hata: silme başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		// =========================
		// SELECTION
		// =========================
		private async void MovementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MovementsGrid.SelectedItem is not MovementRow row) return;

			var m = await _db.VehicleMovements.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == row.Id && !x.IsDeleted);

			if (m is null) return;

			_selectedId = m.Id;

			VehicleCombo.SelectedValue = m.VehicleId;
			DriverCombo.SelectedValue = m.DriverId;
			CommanderCombo.SelectedValue = m.VehicleCommanderId;

			VehiclePlateTextBox.Text = m.VehiclePlateText ?? "";
			DriverTextBox.Text = m.DriverText ?? "";
			CommanderTextBox.Text = m.CommanderText ?? "";

			ExitDatePicker.SelectedDate = m.ExitDateTime.ToLocalTime().Date;
			ExitTimeBox.Text = m.ExitDateTime.ToLocalTime().ToString("HH:mm");

			ReturnDatePicker.SelectedDate = m.ReturnDateTime?.ToLocalTime().Date;
			ReturnTimeBox.Text = m.ReturnDateTime is null ? "17:00" : m.ReturnDateTime.Value.ToLocalTime().ToString("HH:mm");

			RouteBox.Text = m.Route ?? "";
			PurposeBox.Text = m.Purpose ?? "";
		
			LoadOrPassengerBox.Text = m.LoadOrPassengerInfo ?? "";

			StartKmBox.Text = m.StartKm?.ToString() ?? "";
			EndKmBox.Text = m.EndKm?.ToString() ?? "";
		}

		// =========================
		// SEARCH
		// =========================
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
				(x.Plate ?? "").ToLowerInvariant().Contains(q) ||
				(x.Driver ?? "").ToLowerInvariant().Contains(q) ||
				(x.Commander ?? "").ToLowerInvariant().Contains(q) ||
				(x.VehicleBrand ?? "").ToLowerInvariant().Contains(q) ||
				(x.Route ?? "").ToLowerInvariant().Contains(q) ||
				(x.Purpose ?? "").ToLowerInvariant().Contains(q) ||
				(x.Status ?? "").ToLowerInvariant().Contains(q) ||
				(x.DateText ?? "").ToLowerInvariant().Contains(q))
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

			VehiclePlateTextBox.Text = "";
			DriverTextBox.Text = "";
			CommanderTextBox.Text = "";

			ExitDatePicker.SelectedDate = DateTime.Today;
			ExitTimeBox.Text = "08:00";
			ReturnDatePicker.SelectedDate = null;
			ReturnTimeBox.Text = "17:00";

			RouteBox.Text = "";
			PurposeBox.Text = "";
			
			LoadOrPassengerBox.Text = "";

			StartKmBox.Text = "";
			EndKmBox.Text = "";
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
				error = "Dönüş saat formatı geçersiz. Örnek: 17:15";
				return false;
			}

			value = date.Date.Add(time);
			return true;
		}

		private static string CalcStatus(DateTime exitUtc, DateTime? returnUtc)
		{
			if (returnUtc is not null) return "Tamamlandı";

			var exitLocal = exitUtc.ToLocalTime();
			var nowLocal = DateTime.Now;

			if (exitLocal.Date > nowLocal.Date) return "Planlandı";
			if (exitLocal.Date == nowLocal.Date && exitLocal.TimeOfDay > nowLocal.TimeOfDay) return "Başlamadı";

			return "Devam Ediyor";
		}

		// Marka property ismi projende değiştiği için burada tek noktadan yönetiyoruz.
		// Vehicle.Brand veya Vehicle.VehicleBrand hangisi varsa onu döndür.
		private static string? GetVehicleBrandSafe(Vehicle? v)
		{
			if (v is null) return null;

			// ✅ Burayı projendeki property adına göre AYARLA:
			// return v.Brand;
			return v.VehicleBrand; // eğer sende VehicleBrand ise bunu: return v.VehicleBrand;
		}

		private static void Notify(string message, string title = "Bilgi")
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}