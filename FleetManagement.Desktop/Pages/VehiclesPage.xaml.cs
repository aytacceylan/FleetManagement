using FleetManagement.Desktop.Dtos;
using FleetManagement.Domain.Entities;
using FleetManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FleetManagement.Desktop.Pages
{
	public partial class VehiclesPage : Page
	{
		private readonly AppDbContext _db = new(App.DbOptions);

		private int? _selectedId;
		private List<VehicleListRow> _allVehicles = new();

		public VehiclesPage()
		{
			InitializeComponent();

			Loaded += async (_, __) =>
			{
				await LoadDriversAsync();
				await LoadLookupsAsync();
				await LoadVehiclesAsync();
			};
		}

		// ==============================
		// LOOKUPS
		// ==============================
		private sealed class LookupDisplay
		{
			public string Name { get; set; } = "";
			public string Display { get; set; } = "";
		}

		private async Task LoadLookupsAsync()
		{
			var types = await _db.VehicleTypes.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new LookupDisplay { Name = x.Name, Display = $"{x.Code} - {x.Name}" })
				.ToListAsync();

			VehicleTypeCombo.ItemsSource = types;
			VehicleTypeCombo.DisplayMemberPath = "Display";
			VehicleTypeCombo.SelectedValuePath = "Name";

			var models = await _db.VehicleModels.AsNoTracking()
				.OrderBy(x => x.Name)
				.Select(x => new LookupDisplay { Name = x.Name, Display = $"{x.Code} - {x.Name}" })
				.ToListAsync();

			ModelCombo.ItemsSource = models;
			ModelCombo.DisplayMemberPath = "Display";
			ModelCombo.SelectedValuePath = "Name";

			var units = await _db.Units.AsNoTracking()
				.OrderBy(x => x.Code)
				.Select(x => new LookupDisplay { Name = x.Name, Display = $"{x.Code} - {x.Name}" })
				.ToListAsync();

			UnitCombo.ItemsSource = units;
			UnitCombo.DisplayMemberPath = "Display";
			UnitCombo.SelectedValuePath = "Name";
		}

		// ==============================
		// DRIVERS
		// ==============================
		private async Task LoadDriversAsync()
		{
			var drivers = await _db.Drivers.AsNoTracking()
				.OrderBy(d => d.FullName)
				.ToListAsync();

			AssignedDriverCombo.ItemsSource = drivers;
			AssignedDriverCombo.DisplayMemberPath = "FullName";
			AssignedDriverCombo.SelectedValuePath = "Id";
		}

		// ==============================
		// LOAD LIST
		// ==============================
		private async Task LoadVehiclesAsync()
		{
			try
			{
				FormInfo.Text = "Yükleniyor...";

				var list = await _db.Vehicles
					.AsNoTracking()
					.Where(v => !v.IsDeleted)
					.OrderByDescending(v => v.Id)
					.Select(v => new VehicleListRow
					{
						Id = v.Id,

						Plate = v.Plate,
						InventoryNumber = v.InventoryNumber,

						// 1. kolon grubu
						VehicleUnit = v.VehicleUnit,

						// 2. kolon grubu
						VehicleType = v.VehicleType,
						Brand = v.Brand,
						Model = v.Model,
						VehicleYear = v.VehicleYear,

						// 3. kolon grubu
						VehicleKm = v.VehicleKm,
						PassengerCapacity = v.PassengerCapacity,
						LoadCapacity = v.LoadCapacity,

						// 4. kolon grubu
						MotorNo = v.MotorNo,
						SaseNo = v.SaseNo,

						// 5. kolon grubu
						LastMaintenanceKm = v.LastMaintenanceKm,
						LastMaintenanceDate = v.LastMaintenanceDate,
						MaintenanceIntervalKm = v.MaintenanceIntervalKm,
						MaintenanceIntervalMonths = v.MaintenanceIntervalMonths,

						// computed later
						DriverFullName = null,
						DutyStatus = null,
						IsOnDuty = false,
						MaintenanceStatus = null
					})
					.ToListAsync();

				// Computed fields
				foreach (var r in list)
				{
					r.DutyStatus = await GetDutyStatusAsync(r.Id);
					r.IsOnDuty = r.DutyStatus == "Görevde";

					r.DriverFullName = await GetDriverNameForListAsync(r.Id);

					r.MaintenanceStatus = CalcMaintenanceStatus(
						r.VehicleKm,
						r.MaintenanceIntervalKm,
						r.MaintenanceIntervalMonths,
						r.LastMaintenanceKm,
						r.LastMaintenanceDate);
				}

				_allVehicles = list;
				VehiclesGrid.ItemsSource = _allVehicles;

				FormInfo.Text = $"Yüklendi: {_allVehicles.Count} kayıt";

				// Form duty/maintenance boş başlasın
				DutyInfoBox.Text = "";
				MaintInfoBox.Text = "";
			}
			catch (Exception ex)
			{
				FormInfo.Text = "Hata: araçlar yüklenemedi.";
				MessageBox.Show(ex.ToString(), "Hata (detay)");
			}
		}

		private async Task<string> GetDutyStatusAsync(int vehicleId)
		{
			// ✅ VehicleMovement entity'nde ReturnDateTime var -> görevde = ReturnDateTime null olan hareket var
			var onDuty = await _db.VehicleMovements
				.AsNoTracking()
				.AnyAsync(m => m.VehicleId == vehicleId && m.ReturnDateTime == null);

			return onDuty ? "Görevde" : "Müsait";
		}

		private async Task<string?> GetDriverNameForListAsync(int vehicleId)
		{
			// 1) Assigned Driver (varsa)
			var assignedDriverId = await _db.Vehicles.AsNoTracking()
				.Where(v => v.Id == vehicleId)
				.Select(v => v.AssignedDriverId)
				.FirstOrDefaultAsync();

			if (assignedDriverId is not null)
			{
				return await _db.Drivers.AsNoTracking()
					.Where(d => d.Id == assignedDriverId.Value)
					.Select(d => d.FullName)
					.FirstOrDefaultAsync();
			}

			// 2) Son movement sürücüsü (fallback)
			var lastDriverId = await _db.VehicleMovements.AsNoTracking()
				.Where(m => m.VehicleId == vehicleId && m.DriverId != null)
				.OrderByDescending(m => m.Id)
				.Select(m => m.DriverId)
				.FirstOrDefaultAsync();

			if (lastDriverId is null) return null;

			return await _db.Drivers.AsNoTracking()
				.Where(d => d.Id == lastDriverId.Value)
				.Select(d => d.FullName)
				.FirstOrDefaultAsync();
		}

		

		// ==============================
		// SELECTION
		// ==============================
		private async void VehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (VehiclesGrid.SelectedItem is not VehicleListRow row)
				return;

			var vehicle = await _db.Vehicles.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Id == row.Id && !x.IsDeleted);

			if (vehicle is null) return;

			_selectedId = vehicle.Id;

			FillFormFromVehicle(vehicle);

			// ✅ kesin doldur
			await RefreshDutyAndMaintenanceAsync(vehicle);

			FormInfo.Text = $"Seçildi: #{vehicle.Id}";
		}

		// ==============================
		// AUTO FILL BY PLATE
		// ==============================
		private async void PlateBox_LostFocus(object sender, RoutedEventArgs e)
		{
			try
			{
				var plate = (PlateBox.Text ?? "").Trim();
				if (string.IsNullOrWhiteSpace(plate))
					return;

				var vehicle = await _db.Vehicles.AsNoTracking()
					.FirstOrDefaultAsync(x => !x.IsDeleted && x.Plate == plate);

				if (vehicle is null)
				{
					_selectedId = null;
					DutyInfoBox.Text = "";
					MaintInfoBox.Text = "";
					FormInfo.Text = "Plaka bulunamadı. Yeni kayıt girebilirsin.";
					return;
				}

				_selectedId = vehicle.Id;

				FillFormFromVehicle(vehicle);

				// ✅ kesin doldur
				await RefreshDutyAndMaintenanceAsync(vehicle);

				FormInfo.Text = $"Plaka bulundu, kayıt yüklendi: #{vehicle.Id}";
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Plaka arama hatası");
			}
		}

		private void FillFormFromVehicle(Vehicle v)
		{
			// 1. kolon
			PlateBox.Text = v.Plate ?? "";
			InventoryNumberBox.Text = v.InventoryNumber ?? "";
			AssignedDriverCombo.SelectedValue = v.AssignedDriverId;
			UnitCombo.SelectedValue = v.VehicleUnit;

			// 2. kolon
			VehicleTypeCombo.SelectedValue = v.VehicleType;
			BrandBox.Text = v.Brand ?? "";
			ModelCombo.SelectedValue = v.Model;
			VehicleYearBox.Text = v.VehicleYear?.ToString() ?? "";

			// 3. kolon
			VehicleKmBox.Text = v.VehicleKm?.ToString() ?? "";
			PassengerCapacityBox.Text = v.PassengerCapacity?.ToString() ?? "";
			LoadCapacityBox.Text = v.LoadCapacity?.ToString() ?? "";

			// 4. kolon
			MotorNoBox.Text = v.MotorNo ?? "";
			SaseNoBox.Text = v.SaseNo ?? "";

			// 5. kolon
			LastMaintenanceKmBox.Text = v.LastMaintenanceKm?.ToString() ?? "";
			LastMaintenanceDatePicker.SelectedDate = v.LastMaintenanceDate?.Date;
			MaintenanceIntervalKmBox.Text = v.MaintenanceIntervalKm?.ToString() ?? "";
			MaintenanceIntervalMonthsBox.Text = v.MaintenanceIntervalMonths?.ToString() ?? "";
		}

		// ==============================
		// SAVE / DELETE
		// ==============================
		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var plate = (PlateBox.Text ?? "").Trim();
				if (string.IsNullOrWhiteSpace(plate))
				{
					FormInfo.Text = "Plaka (Envanter No) zorunlu.";
					return;
				}

				var entity = _selectedId is null
					? new Vehicle { CreatedAt = DateTime.UtcNow, IsDeleted = false }
					: await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

				if (entity is null)
				{
					FormInfo.Text = "Kayıt bulunamadı.";
					return;
				}

				// 1. kolon
				entity.Plate = plate;
				entity.InventoryNumber = EmptyToNull(InventoryNumberBox.Text);
				entity.AssignedDriverId = AssignedDriverCombo.SelectedValue is int did ? did : (int?)null;
				entity.VehicleUnit = UnitCombo.SelectedValue as string;

				// 2. kolon
				entity.VehicleType = VehicleTypeCombo.SelectedValue as string;
				entity.Brand = EmptyToNull(BrandBox.Text);
				entity.Model = ModelCombo.SelectedValue as string;
				entity.VehicleYear = TryParseNullableInt(VehicleYearBox.Text);

				// 3. kolon
				entity.VehicleKm = TryParseNullableInt(VehicleKmBox.Text);
				entity.PassengerCapacity = TryParseNullableInt(PassengerCapacityBox.Text);
				entity.LoadCapacity = TryParseNullableInt(LoadCapacityBox.Text);

				// 4. kolon
				entity.MotorNo = EmptyToNull(MotorNoBox.Text);
				entity.SaseNo = EmptyToNull(SaseNoBox.Text);

				// 5. kolon
				entity.LastMaintenanceKm = TryParseNullableInt(LastMaintenanceKmBox.Text);

				var dt = LastMaintenanceDatePicker.SelectedDate?.Date;
				entity.LastMaintenanceDate = dt is null ? null : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);

				entity.MaintenanceIntervalKm = TryParseNullableInt(MaintenanceIntervalKmBox.Text);
				entity.MaintenanceIntervalMonths = TryParseNullableInt(MaintenanceIntervalMonthsBox.Text);

				if (_selectedId is null)
					_db.Vehicles.Add(entity);

				await _db.SaveChangesAsync();

				FormInfo.Text = _selectedId is null
					? $"Kaydedildi: #{entity.Id}"
					: $"Güncellendi: #{entity.Id}";

				await LoadVehiclesAsync();
				ClearForm();
			}
			catch (DbUpdateException ex)
			{
				FormInfo.Text = "Hata: Plaka veya Sivil Plaka tekrar ediyor olabilir.";
				MessageBox.Show(ex.InnerException?.Message ?? ex.Message, "DB Hatası");
			}
			catch (Exception ex)
			{
				FormInfo.Text = "Hata: kaydetme başarısız.";
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_selectedId is null)
				{
					FormInfo.Text = "Silmek için kayıt seç.";
					return;
				}

				var confirm = MessageBox.Show("Seçili araç silinsin mi?", "Onay", MessageBoxButton.YesNo);
				if (confirm != MessageBoxResult.Yes) return;

				var entity = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
				if (entity is null) return;

				entity.IsDeleted = true;
				await _db.SaveChangesAsync();

				FormInfo.Text = $"Silindi: #{_selectedId.Value}";

				await LoadVehiclesAsync();
				ClearForm();
			}
			catch (Exception ex)
			{
				FormInfo.Text = "Hata: silme başarısız.";
				MessageBox.Show(ex.Message, "Hata");
			}
		}

		// ==============================
		// UI
		// ==============================
		private async void Refresh_Click(object sender, RoutedEventArgs e)
		{
			await LoadDriversAsync();
			await LoadLookupsAsync();
			await LoadVehiclesAsync();
		}

		private void New_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			FormInfo.Text = "Yeni kayıt için form hazır.";
		}

		private void Clear_Click(object sender, RoutedEventArgs e)
		{
			ClearForm();
			FormInfo.Text = "Form temizlendi.";
		}

		private void ClearForm()
		{
			_selectedId = null;
			VehiclesGrid.SelectedItem = null;

			PlateBox.Text = "";
			InventoryNumberBox.Text = "";

			AssignedDriverCombo.SelectedIndex = -1;
			UnitCombo.SelectedIndex = -1;

			VehicleTypeCombo.SelectedIndex = -1;
			BrandBox.Text = "";
			ModelCombo.SelectedIndex = -1;
			VehicleYearBox.Text = "";

			VehicleKmBox.Text = "";
			PassengerCapacityBox.Text = "";
			LoadCapacityBox.Text = "";

			MotorNoBox.Text = "";
			SaseNoBox.Text = "";

			DutyInfoBox.Text = "";
			MaintInfoBox.Text = "";

			LastMaintenanceKmBox.Text = "";
			LastMaintenanceDatePicker.SelectedDate = null;
			MaintenanceIntervalKmBox.Text = "";
			MaintenanceIntervalMonthsBox.Text = "";
		}
		private async Task RefreshDutyAndMaintenanceAsync(Vehicle v)
		{
			var duty = await GetDutyStatusAsync(v.Id);
			DutyInfoBox.Text = string.IsNullOrWhiteSpace(duty) ? "—" : duty;

			var maint = CalcMaintenanceStatus(
				v.VehicleKm,
				v.MaintenanceIntervalKm,
				v.MaintenanceIntervalMonths,
				v.LastMaintenanceKm,
				v.LastMaintenanceDate
			);
			MaintInfoBox.Text = string.IsNullOrWhiteSpace(maint) ? "—" : maint;
		}

		// ==============================
		// HELPERS
		// ==============================
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

		// ✅ KM veya Ay hangisi erken dolarsa
		private static string CalcMaintenanceStatus(
			int? currentKm,
			int? intervalKm,
			int? intervalMonths,
			int? lastKm,
			DateTime? lastDate)
		{
			if (intervalKm is null && intervalMonths is null)
				return "Tanımsız";

			if (lastKm is null && lastDate is null)
				return "Bakım Gir";

			bool overdue = false;
			bool soon = false;

			// KM
			if (intervalKm is not null && currentKm is not null && lastKm is not null)
			{
				var dueKm = lastKm.Value + intervalKm.Value;

				if (currentKm.Value >= dueKm) overdue = true;
				else if (currentKm.Value >= dueKm - 1000) soon = true;
			}

			// Tarih
			if (intervalMonths is not null && lastDate is not null)
			{
				var dueDate = lastDate.Value.Date.AddMonths(intervalMonths.Value);
				var today = DateTime.UtcNow.Date;

				if (today >= dueDate) overdue = true;
				else if (today >= dueDate.AddDays(-30)) soon = true;
			}

			if (overdue) return "Gecikti";
			if (soon) return "Yaklaşıyor";
			return "Normal";
		}
	}

}