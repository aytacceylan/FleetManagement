using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosedXML.Excel;


namespace FleetManagement.Desktop.Pages
{
	public partial class VehicleMovementsPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<VehicleMovementRow> _all = new();


		public VehicleMovementsPage()
		{
			InitializeComponent();

			ExitDatePicker.SelectedDate = DateTime.Today;
			ExitTimeBox.Text = "08:00";
			ReturnDatePicker.SelectedDate = null;
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
		// GRID LOAD
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
				var returnLocal = m.ReturnDateTime?.ToLocalTime();
				var parsed = ParseLoadOrPassengerInfo(m.LoadOrPassengerInfo);

				var status = CalcStatus(m.ExitDateTime, m.ReturnDateTime);

				int? doneKm = null;
				if (m.StartKm.HasValue && m.EndKm.HasValue && m.EndKm.Value >= m.StartKm.Value)
					doneKm = m.EndKm.Value - m.StartKm.Value;

				return new VehicleMovementRow
				{
					Id = m.Id,
					Driver = m.Driver?.FullName ?? m.DriverText,
					Plate = m.Vehicle?.Plate ?? m.VehiclePlateText ?? "",
					ExitTimeText = exitLocal.ToString("HH:mm"),
					ReturnTimeText = returnLocal is null ? "—" : returnLocal.Value.ToString("HH:mm"),
					VehicleBrand = GetVehicleBrandSafe(m.Vehicle),
					Status = status,
					StatusBrush = GetStatusBrush(status),
					DateText = exitLocal.ToString("dd.MM.yyyy"),
					Route = m.Route,
					Commander = m.VehicleCommander?.FullName ?? m.CommanderText,

					// XAML ile uyumlu alanlar
					Departure = m.Purpose,
					DoneKm = doneKm,
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

		private static void ApplyDailyNumbers(List<VehicleMovementRow> rows)
		{
			var groups = rows
				.GroupBy(x => x.ExitDateTimeUtc.ToLocalTime().Date)
				.ToList();

			foreach (var group in groups)
			{
				var ordered = group
					.OrderBy(x => x.ExitDateTimeUtc)
					.ThenBy(x => x.Id)
					.ToList();

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

				var vehicleId = VehicleCombo.SelectedValue is int vid ? vid : (int?)null;
				var plateText = EmptyToNull(VehicleCombo.Text);

				if (vehicleId is null && string.IsNullOrWhiteSpace(plateText))
				{
					Notify("Plaka zorunlu. Listeden araç seçin.", "Uyarı");
					return;
				}

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

				entity.VehiclePlateText = vehicleId is null ? plateText : null;
				entity.DriverText = entity.DriverId is null ? EmptyToNull(DriverCombo.Text) : null;
				entity.CommanderText = entity.VehicleCommanderId is null ? EmptyToNull(CommanderCombo.Text) : null;

				entity.ExitDateTime = DateTime.SpecifyKind(exitDtLocal, DateTimeKind.Local).ToUniversalTime();
				entity.ReturnDateTime = returnDtLocal is null
					? null
					: DateTime.SpecifyKind(returnDtLocal.Value, DateTimeKind.Local).ToUniversalTime();

				entity.Route = EmptyToNull(RouteCombo.Text);
				entity.Purpose = EmptyToNull(DepartureCombo.Text);     // Başkanlık
				entity.Description = EmptyToNull(DutyTypeCombo.Text);  // Görev Türü

				var passenger = TryParseNullableInt(PassengerCountBox.Text);
				var load = TryParseNullableInt(LoadAmountBox.Text);
				entity.LoadOrPassengerInfo = BuildLoadOrPassengerInfo(passenger, load);

				var doneKm = TryParseNullableInt(DoneKmBox.Text);

				if (vehicleId is not null)
				{
					var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId.Value && !v.IsDeleted);

					if (vehicle is not null)
					{
						var startKm = vehicle.VehicleKm ?? 0;
						entity.StartKm = startKm;
						entity.EndKm = doneKm is null ? null : startKm + doneKm.Value;

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
			if (MovementsGrid.SelectedItem is not VehicleMovementRow row)
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
			ReturnTimeBox.Text = m.ReturnDateTime is null ? "17:00" : m.ReturnDateTime.Value.ToLocalTime().ToString("HH:mm");

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
			ReturnTimeBox.Text = "17:00";

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

			// Projendeki gerçek property hangisiyse onu bırak
			return v.VehicleBrand;
			// return v.Brand;
		}

		private static string CalcKmText(int? startKm, int? endKm)
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
		private static Brush GetStatusBrush(string status)
		{
			return status switch
			{
				"Devam Ediyor" => Brushes.Red,
				"Planlandı" => Brushes.DarkOrange,
				"Tamamlandı" => Brushes.Green,
				"Başlamadı" => Brushes.SteelBlue,
				_ => Brushes.Black
			};
		}


		//I/O
		private void ExportDaily_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var today = DateTime.Today;
				var now = DateTime.Now;

				var rows = _all
					.Where(x => x.ExitDateTimeUtc.ToLocalTime().Date == today)
					.OrderBy(x => x.DailyNo)
					.ToList();

				var path = ExportRowsToExcel(rows, $"VehicleMovements_{now:yyyy-MM-dd_HH-mm}.xlsx");

				Notify($"Günlük Excel export tamamlandı.\n{path}");
			}
			catch (Exception ex)
			{
				Notify("Günlük Excel export başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}


		private void ExportMonthly_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var today = DateTime.Today;
				var now = DateTime.Now;

				var rows = _all
					.Where(x =>
						x.ExitDateTimeUtc.ToLocalTime().Year == today.Year &&
						x.ExitDateTimeUtc.ToLocalTime().Month == today.Month)
					.OrderBy(x => x.ExitDateTimeUtc)
					.ThenBy(x => x.DailyNo)
					.ToList();

				var path = ExportRowsToExcel(rows, $"VehicleMovements_{now:yyyy-MM_HH-mm}.xlsx");

				Notify($"Aylık Excel export tamamlandı.\n{path}");
			}
			catch (Exception ex)
			{
				Notify("Aylık Excel export başarısız.", "Hata");
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private static string ExportRowsToCsv(List<VehicleMovementRow> rows, string fileName)
		{
			var folder = @"C:\Araç Görev Kayıt Defteri";
			Directory.CreateDirectory(folder);

			var path = Path.Combine(folder, fileName);

			var sb = new StringBuilder();

			sb.AppendLine("Sıra No,Sürücü,Plaka,Çıkış Saati,Dönüş Saati,Araç Marka,Durum,Tarih,Güzergah,Araç Komutanı,Başkanlık,Yapılan Km,Taşınan Yolcu,Taşınan Yük,Görev Türü");

			foreach (var x in rows)
			{
				sb.AppendLine(string.Join(",",
					Csv(x.DailyNo.ToString()),
					Csv(x.Driver),
					Csv(x.Plate),
					Csv(x.ExitTimeText),
					Csv(x.ReturnTimeText),
					Csv(x.VehicleBrand),
					Csv(x.Status),
					Csv(x.DateText),
					Csv(x.Route),
					Csv(x.Commander),
					Csv(x.Departure),
					Csv(x.KmText),
					Csv(x.PassengerCount?.ToString()),
					Csv(x.LoadAmount?.ToString()),
					Csv(x.DutyType)
				));
			}

			File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
			return path;
		}

		private static string Csv(string? value)
		{
			var s = value ?? "";
			s = s.Replace("\"", "\"\"");
			return $"\"{s}\"";
		}
		private static string ExportRowsToExcel(List<VehicleMovementRow> rows, string fileName)
		{
			var folder = @"C:\FleetReports";
			Directory.CreateDirectory(folder);

			var path = Path.Combine(folder, fileName);

			using var wb = new XLWorkbook();
			var ws = wb.Worksheets.Add("Araç Hareketleri");

			ws.Cell(1, 1).Value = "Sıra No";
			ws.Cell(1, 2).Value = "Sürücü";
			ws.Cell(1, 3).Value = "Plaka";
			ws.Cell(1, 4).Value = "Çıkış Saati";
			ws.Cell(1, 5).Value = "Dönüş Saati";
			ws.Cell(1, 6).Value = "Araç Marka";
			ws.Cell(1, 7).Value = "Durum";
			ws.Cell(1, 8).Value = "Tarih";
			ws.Cell(1, 9).Value = "Güzergah";
			ws.Cell(1, 10).Value = "Araç Komutanı";
			ws.Cell(1, 11).Value = "Başkanlık";
			ws.Cell(1, 12).Value = "Yapılan Km";
			ws.Cell(1, 13).Value = "Taşınan Yolcu";
			ws.Cell(1, 14).Value = "Taşınan Yük";
			ws.Cell(1, 15).Value = "Görev Türü";

			int row = 2;
			foreach (var x in rows)
			{
				ws.Cell(row, 1).Value = x.DailyNo;
				ws.Cell(row, 2).Value = x.Driver ?? "";
				ws.Cell(row, 3).Value = x.Plate ?? "";
				ws.Cell(row, 4).Value = x.ExitTimeText ?? "";
				ws.Cell(row, 5).Value = x.ReturnTimeText ?? "";
				ws.Cell(row, 6).Value = x.VehicleBrand ?? "";
				ws.Cell(row, 7).Value = x.Status ?? "";
				ws.Cell(row, 8).Value = x.DateText ?? "";
				ws.Cell(row, 9).Value = x.Route ?? "";
				ws.Cell(row, 10).Value = x.Commander ?? "";
				ws.Cell(row, 11).Value = x.Departure ?? "";
				ws.Cell(row, 12).Value = x.KmText ?? "";
				ws.Cell(row, 13).Value = x.PassengerCount?.ToString() ?? "";
				ws.Cell(row, 14).Value = x.LoadAmount?.ToString() ?? "";
				ws.Cell(row, 15).Value = x.DutyType ?? "";
				row++;
			}

			var headerRange = ws.Range(1, 1, 1, 15);
			headerRange.Style.Font.Bold = true;
			headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

			var tableRange = ws.Range(1, 1, Math.Max(row - 1, 1), 15);
			tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
			tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
			tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

			tableRange.SetAutoFilter();
			ws.Columns().AdjustToContents();
			ws.SheetView.FreezeRows(1);

			wb.SaveAs(path);
			return path;
		}
	}
}