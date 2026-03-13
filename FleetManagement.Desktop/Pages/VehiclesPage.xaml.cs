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
using System.Windows.Media;

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
            LoadVehicleSituations();
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
                .Where(x => !x.IsDeleted)          // ✅ silinen gelmesin (madde 3)
                .OrderBy(x => x.Name)
                .Select(x => new LookupDisplay { Name = x.Name, Display = x.Name })
                .ToListAsync();

            VehicleTypeCombo.ItemsSource = types;
            VehicleTypeCombo.DisplayMemberPath = "Display";
            VehicleTypeCombo.SelectedValuePath = "Name";

            var models = await _db.VehicleModels.AsNoTracking()
                .Where(x => !x.IsDeleted)          // ✅ silinen gelmesin (madde 3)
                .OrderBy(x => x.Name)
                .Select(x => new LookupDisplay { Name = x.Name, Display = x.Name })
                .ToListAsync();

            ModelCombo.ItemsSource = models;
            ModelCombo.DisplayMemberPath = "Display";
            ModelCombo.SelectedValuePath = "Name";

            var units = await _db.Units.AsNoTracking()
                .Where(x => !x.IsDeleted)          // ✅ silinen gelmesin (soft delete varsa)
                .OrderBy(x => x.Name)
                .Select(x => new LookupDisplay { Name = x.Name, Display = x.Name })
                .ToListAsync();

            UnitCombo.ItemsSource = units;
            UnitCombo.DisplayMemberPath = "Display";
            UnitCombo.SelectedValuePath = "Name";

            // ✅ marka dolsun
            var brands = await _db.VehicleBrands.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => new LookupDisplay { Name = x.Name, Display = x.Name }) // sadece Name
                .ToListAsync();

            BrandBox.ItemsSource = brands;
            BrandBox.DisplayMemberPath = "Display";
            BrandBox.SelectedValuePath = "Name";


            // ✅ Araç Yılı dolsun
            var years = await _db.VehicleYears.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.Year)
                .Select(x => x.Year)
                .ToListAsync();

            VehicleYearBox.ItemsSource = years;

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
                        Brand = v.VehicleBrand,
                        Model = v.Model,
                        VehicleYear = v.VehicleYear,
                        VehicleSituation = string.IsNullOrWhiteSpace(v.VehicleSituation) ? "Müsait" : v.VehicleSituation,

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
                        MaintenanceStatus = null
                    })
                    .ToListAsync();

                foreach (var r in list)
                {
                    r.DriverFullName = await GetDriverNameForListAsync(r.Id);

                    r.MaintenanceStatus = CalcMaintenanceStatus(
                        r.VehicleKm,
                        r.MaintenanceIntervalKm,
                        r.MaintenanceIntervalMonths,
                        r.LastMaintenanceKm,
                        r.LastMaintenanceDate);

                    r.MaintenanceBrush = GetMaintenanceBrush(r.MaintenanceStatus);
                    r.VehicleSituationBrush = GetVehicleSituationBrush(r.VehicleSituation);
                }

                _allVehicles = list;
                VehiclesGrid.ItemsSource = _allVehicles;

                MaintInfoBox.Text = "—";
                MaintInfoBox.Foreground = Brushes.Black;
            }
            catch (Exception ex)
            {
                Notify("Hata: araçlar yüklenemedi.", "Hata");
                MessageBox.Show(ex.ToString(), "Hata (detay)");
            }
        }


        private async Task<string?> GetDriverNameForListAsync(int vehicleId)
        {
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

            await RefreshDutyAndMaintenanceAsync(vehicle);

            // Notify($"Seçildi: #{vehicle.Id}"); // istersen aç
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
                    
                    MaintInfoBox.Text = "";
                    Notify("Plaka bulunamadı. Yeni kayıt girebilirsin.", "Bilgi");
                    return;
                }

                _selectedId = vehicle.Id;

                FillFormFromVehicle(vehicle);

                await RefreshDutyAndMaintenanceAsync(vehicle);

                Notify($"Plaka bulundu, kayıt yüklendi: #{vehicle.Id}", "Bilgi");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Plaka arama hatası");
            }
        }

        private void FillFormFromVehicle(Vehicle v)
        {
            PlateBox.Text = v.Plate ?? "";
            InventoryNumberBox.Text = v.InventoryNumber ?? "";
            AssignedDriverCombo.SelectedValue = v.AssignedDriverId;
            UnitCombo.SelectedValue = v.VehicleUnit;

            VehicleTypeCombo.SelectedValue = v.VehicleType;
            BrandBox.SelectedValue = v.VehicleBrand;
            ModelCombo.SelectedValue = v.Model;
            VehicleYearBox.Text = v.VehicleYear?.ToString() ?? "";
            VehicleSituationCombo.Text = v.VehicleSituation ?? "Müsait";

            VehicleKmBox.Text = v.VehicleKm?.ToString() ?? "";
            PassengerCapacityBox.Text = v.PassengerCapacity?.ToString() ?? "";
            LoadCapacityBox.Text = v.LoadCapacity?.ToString() ?? "";

            MotorNoBox.Text = v.MotorNo ?? "";
            SaseNoBox.Text = v.SaseNo ?? "";
           

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
                    Notify("Plaka (Envanter No) zorunlu.", "Uyarı");
                    return;
                }

                var entity = _selectedId is null
                    ? new Vehicle { CreatedAt = DateTime.UtcNow, IsDeleted = false }
                    : await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

                if (entity is null)
                {
                    Notify("Kayıt bulunamadı.", "Uyarı");
                    return;
                }

                entity.Plate = plate;
                entity.InventoryNumber = EmptyToNull(InventoryNumberBox.Text);
                entity.AssignedDriverId = AssignedDriverCombo.SelectedValue is int did ? did : (int?)null;
                entity.VehicleUnit = UnitCombo.SelectedValue as string;

                entity.VehicleType = VehicleTypeCombo.SelectedValue as string;
                var brandValue = BrandBox.SelectedValue as string;
                entity.VehicleBrand = EmptyToNull(brandValue ?? BrandBox.Text);
                entity.Model = ModelCombo.SelectedValue as string;
                entity.VehicleYear = TryParseNullableInt(VehicleYearBox.Text);

                entity.VehicleKm = TryParseNullableInt(VehicleKmBox.Text);
                entity.PassengerCapacity = TryParseNullableInt(PassengerCapacityBox.Text);
                entity.LoadCapacity = TryParseNullableInt(LoadCapacityBox.Text);

                entity.MotorNo = EmptyToNull(MotorNoBox.Text);
                entity.SaseNo = EmptyToNull(SaseNoBox.Text);

                entity.LastMaintenanceKm = TryParseNullableInt(LastMaintenanceKmBox.Text);

                var dt = LastMaintenanceDatePicker.SelectedDate?.Date;
                entity.LastMaintenanceDate = dt is null ? null : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc);
                entity.VehicleSituation = string.IsNullOrWhiteSpace(VehicleSituationCombo.Text)
                    ? "Müsait"
                    : VehicleSituationCombo.Text.Trim();


                entity.MaintenanceIntervalKm = TryParseNullableInt(MaintenanceIntervalKmBox.Text);
                entity.MaintenanceIntervalMonths = TryParseNullableInt(MaintenanceIntervalMonthsBox.Text);

                if (_selectedId is null)
                    _db.Vehicles.Add(entity);

                await _db.SaveChangesAsync();

                Notify(_selectedId is null
                    ? $"Kaydedildi: #{entity.Id}"
                    : $"Güncellendi: #{entity.Id}");

                await LoadVehiclesAsync();
                ClearForm();
            }
            catch (DbUpdateException ex)
            {
                Notify("Hata: Plaka veya Sivil Plaka tekrar ediyor olabilir.", "DB Hatası");
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message, "DB Hatası");
            }
            catch (Exception ex)
            {
                Notify("Hata: kaydetme başarısız.", "Hata");
                MessageBox.Show(ex.Message, "Hata");
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

                var confirm = MessageBox.Show("Seçili araç silinsin mi?", "Onay", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                var entity = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);
                if (entity is null) return;

                entity.IsDeleted = true;
                await _db.SaveChangesAsync();

                Notify($"Silindi: #{_selectedId.Value}");

                await LoadVehiclesAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                Notify("Hata: silme başarısız.", "Hata");
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
            Notify("Yenilendi.");
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            // Notify("Yeni kayıt için form hazır.");
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Notify("Temizlendi");
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
            VehicleSituationCombo.SelectedIndex = -1;
            VehicleSituationCombo.Text = "";

            VehicleKmBox.Text = "";
            PassengerCapacityBox.Text = "";
            LoadCapacityBox.Text = "";

            MotorNoBox.Text = "";
            SaseNoBox.Text = "";

            LastMaintenanceKmBox.Text = "";
            LastMaintenanceDatePicker.SelectedDate = null;
            MaintenanceIntervalKmBox.Text = "";
            MaintenanceIntervalMonthsBox.Text = "";

            MaintInfoBox.Text = "—";
            MaintInfoBox.Foreground = Brushes.Black;
        }

        private async Task RefreshDutyAndMaintenanceAsync(Vehicle v)
        {
            var maint = CalcMaintenanceStatus(
                v.VehicleKm,
                v.MaintenanceIntervalKm,
                v.MaintenanceIntervalMonths,
                v.LastMaintenanceKm,
                v.LastMaintenanceDate
            );

            MaintInfoBox.Text = string.IsNullOrWhiteSpace(maint) ? "—" : maint;
            MaintInfoBox.Foreground = GetMaintenanceBrush(maint);

            await Task.CompletedTask;
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

            // KM bazlı bakım
            if (intervalKm is not null && currentKm is not null && lastKm is not null)
            {
                var dueKm = lastKm.Value + intervalKm.Value;

                if (currentKm.Value >= dueKm)
                    overdue = true;
                else if (currentKm.Value >= dueKm - 1000)
                    soon = true;
            }

            // Tarih bazlı bakım
            if (intervalMonths is not null && lastDate is not null)
            {
                var dueDate = lastDate.Value.Date.AddMonths(intervalMonths.Value);
                var today = DateTime.Today;

                if (today >= dueDate)
                    overdue = true;
                else if (today >= dueDate.AddDays(-30))
                    soon = true;
            }

            if (overdue) return "Gecikti";
            if (soon) return "Yaklaşıyor";

            return "Normal";
        }

        private static void Notify(string message, string title = "Bilgi")
        {
            // Sessiz mod istersen:
            // return;

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static Brush GetMaintenanceBrush(string? status)
        {
            return status switch
            {
                "Gecikti" => Brushes.Red,
                "Yaklaşıyor" => Brushes.DarkOrange,
                "Normal" => Brushes.Green,
                "Bakım Gir" => Brushes.SteelBlue,
                "Tanımsız" => Brushes.Gray,
                _ => Brushes.Black
            };
        }

        
        private void LoadVehicleSituations()
        {
            VehicleSituationCombo.ItemsSource = new List<string>
            {
                "Müsait",
                "Görevde",
                "Servis",
                "Kademe",
                "Fabrika"
            };
        }

        private static Brush GetVehicleSituationBrush(string? status)
        {
            return status switch
            {
                "Müsait" => Brushes.Green,
                "Görevde" => Brushes.Red,
                "Kademe" => Brushes.DarkOrange,
                "Servis" => Brushes.MediumPurple,
                "Fabrika" => Brushes.SteelBlue,
                _ => Brushes.Black
            };
        }


    }
}