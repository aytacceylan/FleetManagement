using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

using FleetManagement.Infrastructure.Data;
using FleetManagement.Domain.Entities;
using FleetManagement.Desktop.Dtos;

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
            Loaded += async (_, __) => await LoadVehiclesAsync();
        }

        // ==============================
        // LOAD
        // ==============================

        private async Task LoadVehiclesAsync()
        {
            try
            {
                FormInfo.Text = "Yükleniyor...";

                var list = await _db.Vehicles
                    .AsNoTracking()
                    .Select(v => new VehicleListRow
                    {
                        Id = v.Id,
                        Plate = v.Plate,
                        InventoryNumber = v.InventoryNumber,
                        VehicleType = v.VehicleType,
                        VehicleCategory = v.VehicleCategory,
                        Model = v.Model,
                        MotorNo = v.MotorNo,
                        SaseNo = v.SaseNo,

                        DriverName = _db.VehicleMovements
                            .Where(m => m.VehicleId == v.Id)
                            .OrderByDescending(m => m.Id)
                            .Select(m => m.Driver.FullName)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                _allVehicles = list;
                VehiclesGrid.ItemsSource = _allVehicles;

                FormInfo.Text = $"Yüklendi: {_allVehicles.Count} kayıt";
            }
            catch (Exception ex)
            {
                FormInfo.Text = "Hata: araçlar yüklenemedi.";
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        // ==============================
        // GRID SELECTION
        // ==============================

        private async void VehiclesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VehiclesGrid.SelectedItem is not VehicleListRow row)
                return;

            _selectedId = row.Id;

            var vehicle = await _db.Vehicles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == row.Id);

            if (vehicle == null) return;

            PlateBox.Text = vehicle.Plate ?? "";
            InventoryNumberBox.Text = vehicle.InventoryNumber ?? "";
            BrandBox.Text = vehicle.Brand ?? "";
            ModelBox.Text = vehicle.Model ?? "";
            VehicleTypeBox.Text = vehicle.VehicleType ?? "";
            VehicleCategoryBox.Text = vehicle.VehicleCategory ?? "";
            VehicleUnitBox.Text = vehicle.VehicleUnit ?? "";
            MotorNoBox.Text = vehicle.MotorNo ?? "";
            SaseNoBox.Text = vehicle.SaseNo ?? "";
            VehicleKmBox.Text = vehicle.VehicleKm?.ToString() ?? "";
            PassengerCapacityBox.Text = vehicle.PassengerCapacity?.ToString() ?? "";
            LoadCapacityBox.Text = vehicle.LoadCapacity?.ToString() ?? "";
            VehicleSituationBox.Text = vehicle.VehicleSituation ?? "";

            FormInfo.Text = $"Seçildi: #{vehicle.Id}";
        }

        // ==============================
        // SAVE
        // ==============================

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var plate = (PlateBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(plate))
                {
                    FormInfo.Text = "Araç No (Envanter) zorunlu.";
                    return;
                }

                var entity = _selectedId is null
                    ? new Vehicle { CreatedAt = DateTime.UtcNow, IsDeleted = false }
                    : await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == _selectedId.Value);

                if (entity == null)
                {
                    FormInfo.Text = "Kayıt bulunamadı.";
                    return;
                }

                entity.Plate = plate;
                entity.InventoryNumber = EmptyToNull(InventoryNumberBox.Text);
                entity.Brand = EmptyToNull(BrandBox.Text);
                entity.Model = EmptyToNull(ModelBox.Text);
                entity.VehicleType = EmptyToNull(VehicleTypeBox.Text);
                entity.VehicleCategory = EmptyToNull(VehicleCategoryBox.Text);
                entity.VehicleUnit = EmptyToNull(VehicleUnitBox.Text);
                entity.MotorNo = EmptyToNull(MotorNoBox.Text);
                entity.SaseNo = EmptyToNull(SaseNoBox.Text);
                entity.VehicleSituation = EmptyToNull(VehicleSituationBox.Text);

                entity.VehicleKm = TryParseNullableInt(VehicleKmBox.Text);
                entity.PassengerCapacity = TryParseNullableInt(PassengerCapacityBox.Text);
                entity.LoadCapacity = TryParseNullableInt(LoadCapacityBox.Text);

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
                FormInfo.Text = "Hata: Araç No veya Plaka tekrar ediyor olabilir.";
                MessageBox.Show(ex.InnerException?.Message ?? ex.Message, "DB Hatası");
            }
            catch (Exception ex)
            {
                FormInfo.Text = "Hata: kaydetme başarısız.";
                MessageBox.Show(ex.Message, "Hata");
            }
        }

        // ==============================
        // DELETE (SOFT)
        // ==============================

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
                if (entity == null) return;

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
        // SEARCH
        // ==============================

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(q))
            {
                VehiclesGrid.ItemsSource = _allVehicles;
                return;
            }

            VehiclesGrid.ItemsSource = _allVehicles
                .Where(x =>
                    (x.Plate ?? "").ToLower().Contains(q) ||
                    (x.InventoryNumber ?? "").ToLower().Contains(q) ||
                    (x.DriverName ?? "").ToLower().Contains(q) ||
                    (x.VehicleType ?? "").ToLower().Contains(q) ||
                    (x.VehicleCategory ?? "").ToLower().Contains(q) ||
                    (x.Model ?? "").ToLower().Contains(q) ||
                    (x.MotorNo ?? "").ToLower().Contains(q) ||
                    (x.SaseNo ?? "").ToLower().Contains(q))
                .ToList();
        }

        // ==============================
        // HELPERS
        // ==============================

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
            BrandBox.Text = "";
            ModelBox.Text = "";
            VehicleTypeBox.Text = "";
            VehicleCategoryBox.Text = "";
            VehicleUnitBox.Text = "";
            MotorNoBox.Text = "";
            SaseNoBox.Text = "";
            VehicleKmBox.Text = "";
            PassengerCapacityBox.Text = "";
            LoadCapacityBox.Text = "";
            VehicleSituationBox.Text = "";
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
        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadVehiclesAsync();
        }
    }
}